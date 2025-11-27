using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace NarratoriaClient.Services;

public interface IAppDataService
{
    event EventHandler<AppSettings>? SettingsChanged;
    event EventHandler<IReadOnlyList<ChatMessageEntry>>? ChatSessionChanged;
    event EventHandler<IReadOnlyList<ChatSessionSummary>>? SessionsChanged;
    event EventHandler<IReadOnlyList<PersonaProfile>>? PersonasChanged;

    Task<AppSettings> GetSettingsAsync(CancellationToken cancellationToken = default);
    Task<ApiSettings> GetApiSettingsAsync(CancellationToken cancellationToken = default);
    Task<SystemPromptSettings> GetPromptSettingsAsync(CancellationToken cancellationToken = default);
    Task UpdateApiSettingsAsync(ApiSettings settings, CancellationToken cancellationToken = default);
    Task UpdatePromptSettingsAsync(SystemPromptSettings settings, CancellationToken cancellationToken = default);

    Task<ChatSessionState> GetSessionStateAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChatMessageEntry>> GetChatHistoryAsync(CancellationToken cancellationToken = default);
    Task<ChatMessageEntry> AppendPlayerMessageAsync(string content, CancellationToken cancellationToken = default);
    Task<ChatMessageEntry> AppendNarratorMessageAsync(string content, CancellationToken cancellationToken = default);
    Task DeleteMessageAsync(string messageId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ChatSessionSummary>> GetSessionsAsync(CancellationToken cancellationToken = default);
    Task<ChatSessionSummary> GetActiveSessionSummaryAsync(CancellationToken cancellationToken = default);
    Task<ChatSessionState> CreateSessionAsync(string? name = null, CancellationToken cancellationToken = default);
    Task SwitchSessionAsync(string sessionId, CancellationToken cancellationToken = default);
    Task DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PersonaProfile>> GetPersonasAsync(CancellationToken cancellationToken = default);
    Task<PersonaProfile> UpsertPersonaAsync(PersonaProfile persona, CancellationToken cancellationToken = default);
    Task DeletePersonaAsync(string personaId, CancellationToken cancellationToken = default);

    Task<string> ExportAsync(CancellationToken cancellationToken = default);
    Task ImportAsync(string json, CancellationToken cancellationToken = default);
    Task NotifyClientReadyAsync(CancellationToken cancellationToken = default);
}

public sealed class AppDataService : IAppDataService
{
    private const string SettingsStorageKey = "narratoria/v1/settings";
    private const string SessionsStorageKey = "narratoria/v1/sessions";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };

    private readonly IClientStorageService _storage;
    private readonly ILogBuffer _logBuffer;
    private readonly SemaphoreSlim _mutex = new(1, 1);

    private AppSettings _settingsCache = AppSettings.CreateDefault();
    private bool _settingsLoaded;

    private AppSessionsState _sessionsState = AppSessionsState.CreateDefault();
    private ChatSessionState _currentSession;
    private bool _sessionsLoaded;
    private bool _jsAvailable;

    public AppDataService(IClientStorageService storage, ILogBuffer logBuffer)
    {
        _storage = storage;
        _logBuffer = logBuffer ?? throw new ArgumentNullException(nameof(logBuffer));
        _currentSession = Clone(_sessionsState.Sessions.First());
        Log(LogLevel.Information, "AppDataService initialized.", new Dictionary<string, object?>
        {
            ["defaultSessionId"] = _currentSession.SessionId
        });
    }

    public event EventHandler<AppSettings>? SettingsChanged;
    public event EventHandler<IReadOnlyList<ChatMessageEntry>>? ChatSessionChanged;
    public event EventHandler<IReadOnlyList<ChatSessionSummary>>? SessionsChanged;
    public event EventHandler<IReadOnlyList<PersonaProfile>>? PersonasChanged;

    public async Task<AppSettings> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureSettingsLoadedAsync(cancellationToken);
        return Clone(_settingsCache);
    }

    public async Task<ApiSettings> GetApiSettingsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureSettingsLoadedAsync(cancellationToken);
        return Clone(_settingsCache.Api);
    }

    public async Task<SystemPromptSettings> GetPromptSettingsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureSettingsLoadedAsync(cancellationToken);
        return Clone(_settingsCache.Prompt);
    }

    public async Task UpdateApiSettingsAsync(ApiSettings settings, CancellationToken cancellationToken = default)
    {
        if (settings is null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        await _mutex.WaitAsync(cancellationToken);
        try
        {
            await EnsureSettingsLoadedAsync(cancellationToken);
            _settingsCache = _settingsCache with { Api = Clone(settings) };
            await PersistSettingsAsync(cancellationToken);
            RaiseSettingsChanged();

            Log(LogLevel.Information, "API settings updated.", new Dictionary<string, object?>
            {
                ["endpoint"] = settings.Endpoint,
                ["apiKeyRequired"] = settings.ApiKeyRequired,
                ["narratorModel"] = settings.Narrator.Model,
                ["systemModel"] = settings.System.Model,
                ["imageModel"] = settings.Image.Model
            });
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task UpdatePromptSettingsAsync(SystemPromptSettings settings, CancellationToken cancellationToken = default)
    {
        if (settings is null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        await _mutex.WaitAsync(cancellationToken);
        try
        {
            await EnsureSettingsLoadedAsync(cancellationToken);
            _settingsCache = _settingsCache with { Prompt = Clone(settings) };
            await PersistSettingsAsync(cancellationToken);
            RaiseSettingsChanged();

            Log(LogLevel.Information, "Prompt settings updated.", new Dictionary<string, object?>
            {
                ["narratorTitle"] = settings.Narrator.Title,
                ["narratorLength"] = settings.Narrator.Content?.Length ?? 0,
                ["systemTitle"] = settings.System.Title,
                ["systemLength"] = settings.System.Content?.Length ?? 0
            });
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<ChatSessionState> GetSessionStateAsync(CancellationToken cancellationToken = default)
    {
        await EnsureSessionsLoadedAsync(cancellationToken);
        return Clone(_currentSession);
    }

    public async Task<IReadOnlyList<ChatMessageEntry>> GetChatHistoryAsync(CancellationToken cancellationToken = default)
    {
        await EnsureSessionsLoadedAsync(cancellationToken);
        return _currentSession.Messages.Select(Clone).ToList();
    }

    public Task<ChatMessageEntry> AppendPlayerMessageAsync(string content, CancellationToken cancellationToken = default)
    {
        return AppendMessageAsync("Player", content, ChatMessageRole.Player, cancellationToken);
    }

    public Task<ChatMessageEntry> AppendNarratorMessageAsync(string content, CancellationToken cancellationToken = default)
    {
        return AppendMessageAsync("Narrator", content, ChatMessageRole.Narrator, cancellationToken);
    }

    public async Task DeleteMessageAsync(string messageId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(messageId))
        {
            return;
        }

        await _mutex.WaitAsync(cancellationToken);
        try
        {
            await EnsureSessionsLoadedAsync(cancellationToken);

            var active = _sessionsState.Sessions.First(s => string.Equals(s.SessionId, _sessionsState.ActiveSessionId, StringComparison.Ordinal));
            var filtered = active.Messages
                .Where(m => !string.Equals(m.Id, messageId, StringComparison.Ordinal))
                .Select(Clone)
                .ToList();

            if (filtered.Count == active.Messages.Count)
            {
                return;
            }

            var updated = active with
            {
                Messages = filtered,
                UpdatedAt = filtered.Count > 0 ? filtered.Max(m => m.Timestamp) : active.UpdatedAt
            };

            ReplaceSession(updated);
            _currentSession = Clone(updated);

            await PersistSessionsAsync(cancellationToken);
            RaiseSessionsChanged();
            RaiseChatSessionChanged();

            Log(LogLevel.Warning, "Chat message deleted.", new Dictionary<string, object?>
            {
                ["messageId"] = messageId,
                ["sessionId"] = updated.SessionId,
                ["remainingMessages"] = updated.Messages.Count
            });
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<IReadOnlyList<ChatSessionSummary>> GetSessionsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureSessionsLoadedAsync(cancellationToken);
        return BuildSessionSummaries();
    }

    public async Task<ChatSessionSummary> GetActiveSessionSummaryAsync(CancellationToken cancellationToken = default)
    {
        await EnsureSessionsLoadedAsync(cancellationToken);
        var active = _sessionsState.Sessions.First(s => string.Equals(s.SessionId, _sessionsState.ActiveSessionId, StringComparison.Ordinal));
        return new ChatSessionSummary
        {
            SessionId = active.SessionId,
            Name = active.Name,
            CreatedAt = active.CreatedAt,
            UpdatedAt = active.Messages.Count > 0 ? active.Messages.Max(m => m.Timestamp) : active.UpdatedAt,
            MessageCount = active.Messages.Count,
            IsActive = true
        };
    }

    public async Task<ChatSessionState> CreateSessionAsync(string? name = null, CancellationToken cancellationToken = default)
    {
        await _mutex.WaitAsync(cancellationToken);
        try
        {
            await EnsureSessionsLoadedAsync(cancellationToken);

            var sessionName = string.IsNullOrWhiteSpace(name)
                ? $"Session {_sessionsState.Sessions.Count + 1}"
                : name.Trim();

            var newSession = ChatSessionState.CreateNew(sessionName);
            var sessions = _sessionsState.Sessions.Select(Clone).ToList();
            sessions.Add(Clone(newSession));

            _sessionsState = _sessionsState with
            {
                Sessions = sessions,
                ActiveSessionId = newSession.SessionId
            };

            _currentSession = Clone(newSession);

            await PersistSessionsAsync(cancellationToken);
            RaiseSessionsChanged();
            RaiseChatSessionChanged();

            Log(LogLevel.Information, "Session created.", new Dictionary<string, object?>
            {
                ["sessionId"] = newSession.SessionId,
                ["name"] = newSession.Name
            });

            return Clone(newSession);
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task SwitchSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return;
        }

        await _mutex.WaitAsync(cancellationToken);
        try
        {
            await EnsureSessionsLoadedAsync(cancellationToken);

            var session = _sessionsState.Sessions.FirstOrDefault(s => string.Equals(s.SessionId, sessionId, StringComparison.Ordinal));
            if (session is null)
            {
                return;
            }

            if (string.Equals(_sessionsState.ActiveSessionId, session.SessionId, StringComparison.Ordinal))
            {
                return;
            }

            _sessionsState = _sessionsState with { ActiveSessionId = session.SessionId };
            _currentSession = Clone(session);

            await PersistSessionsAsync(cancellationToken);

            RaiseSessionsChanged();
            RaiseChatSessionChanged();

            Log(LogLevel.Information, "Session switched.", new Dictionary<string, object?>
            {
                ["sessionId"] = session.SessionId,
                ["name"] = session.Name
            });
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return;
        }

        await _mutex.WaitAsync(cancellationToken);
        try
        {
            await EnsureSessionsLoadedAsync(cancellationToken);

            var existing = _sessionsState.Sessions.ToList();
            if (!existing.Any(s => string.Equals(s.SessionId, sessionId, StringComparison.Ordinal)))
            {
                return;
            }

            var sessions = existing
                .Where(s => !string.Equals(s.SessionId, sessionId, StringComparison.Ordinal))
                .Select(Clone)
                .ToList();

            if (sessions.Count == 0)
            {
                var fresh = ChatSessionState.CreateNew("Session 1");
                sessions.Add(fresh);
                _sessionsState = new AppSessionsState
                {
                    ActiveSessionId = fresh.SessionId,
                    Sessions = sessions
                };
                _currentSession = Clone(fresh);
            }
            else
            {
                ChatSessionState newActive;
                if (!sessions.Any(s => string.Equals(s.SessionId, _sessionsState.ActiveSessionId, StringComparison.Ordinal)))
                {
                    newActive = sessions.First();
                }
                else
                {
                    newActive = sessions.First(s => string.Equals(s.SessionId, _sessionsState.ActiveSessionId, StringComparison.Ordinal));
                }

                _sessionsState = _sessionsState with
                {
                    Sessions = sessions,
                    ActiveSessionId = newActive.SessionId
                };

                _currentSession = Clone(newActive);
            }

            await PersistSessionsAsync(cancellationToken);

            RaiseSessionsChanged();
            RaiseChatSessionChanged();

            Log(LogLevel.Warning, "Session deleted.", new Dictionary<string, object?>
            {
                ["sessionId"] = sessionId,
                ["remainingSessions"] = _sessionsState.Sessions.Count
            });
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<IReadOnlyList<PersonaProfile>> GetPersonasAsync(CancellationToken cancellationToken = default)
    {
        await EnsureSettingsLoadedAsync(cancellationToken);
        return _settingsCache.Personas.Select(Clone).ToList();
    }

    public async Task<PersonaProfile> UpsertPersonaAsync(PersonaProfile persona, CancellationToken cancellationToken = default)
    {
        if (persona is null)
        {
            throw new ArgumentNullException(nameof(persona));
        }

        await _mutex.WaitAsync(cancellationToken);
        try
        {
            await EnsureSettingsLoadedAsync(cancellationToken);

            var targetId = string.IsNullOrWhiteSpace(persona.Id) ? Guid.NewGuid().ToString("N") : persona.Id;
            var updatedPersona = persona with
            {
                Id = targetId,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            var personas = _settingsCache.Personas.ToList();
            var existingIndex = personas.FindIndex(p => string.Equals(p.Id, targetId, StringComparison.Ordinal));
            if (existingIndex >= 0)
            {
                personas[existingIndex] = Clone(updatedPersona);
            }
            else
            {
                personas.Add(Clone(updatedPersona));
            }

            _settingsCache = _settingsCache with { Personas = personas.Select(Clone).ToList() };
            await PersistSettingsAsync(cancellationToken);

            RaiseSettingsChanged();
            RaisePersonasChanged();

            Log(LogLevel.Information, "Persona saved.", new Dictionary<string, object?>
            {
                ["personaId"] = updatedPersona.Id,
                ["name"] = updatedPersona.Name
            });

            return Clone(updatedPersona);
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task DeletePersonaAsync(string personaId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(personaId))
        {
            return;
        }

        await _mutex.WaitAsync(cancellationToken);
        try
        {
            await EnsureSettingsLoadedAsync(cancellationToken);

            var personas = _settingsCache.Personas.ToList();
            var removed = personas.RemoveAll(p => string.Equals(p.Id, personaId, StringComparison.Ordinal));
            if (removed == 0)
            {
                return;
            }

            _settingsCache = _settingsCache with { Personas = personas.Select(Clone).ToList() };
            await PersistSettingsAsync(cancellationToken);

            RaiseSettingsChanged();
            RaisePersonasChanged();

            Log(LogLevel.Warning, "Persona deleted.", new Dictionary<string, object?>
            {
                ["personaId"] = personaId
            });
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<string> ExportAsync(CancellationToken cancellationToken = default)
    {
        await EnsureSettingsLoadedAsync(cancellationToken);
        await EnsureSessionsLoadedAsync(cancellationToken);

        var export = new AppExportModel
        {
            Settings = Clone(_settingsCache),
            Sessions = _sessionsState.Sessions.Select(Clone).ToList()
        };

        var options = new JsonSerializerOptions(SerializerOptions) { WriteIndented = true };
        var payload = JsonSerializer.Serialize(export, options);

        Log(LogLevel.Information, "Application data exported.", new Dictionary<string, object?>
        {
            ["bytes"] = payload.Length,
            ["sessionCount"] = export.Sessions.Count
        });

        return payload;
    }

    public async Task ImportAsync(string json, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        AppExportModel? importModel;
        try
        {
            importModel = JsonSerializer.Deserialize<AppExportModel>(json, SerializerOptions);
        }
        catch (JsonException)
        {
            return;
        }

        if (importModel is null)
        {
            return;
        }

        await _mutex.WaitAsync(cancellationToken);
        try
        {
            await EnsureSettingsLoadedAsync(cancellationToken);
            await EnsureSessionsLoadedAsync(cancellationToken);

            var newSettings = importModel.Settings ?? AppSettings.CreateDefault();
            if (!SettingsAreEqual(_settingsCache, newSettings))
            {
                _settingsCache = Clone(newSettings);
                await PersistSettingsAsync(cancellationToken);
                RaiseSettingsChanged();
                RaisePersonasChanged();
            }

            if (importModel.Sessions.Count > 0)
            {
                var merged = MergeSessions(importModel.Sessions);
                if (merged)
                {
                    await PersistSessionsAsync(cancellationToken);
                    RaiseSessionsChanged();
                    RaiseChatSessionChanged();
                }
            }

            var personaCount = importModel.Settings?.Personas?.Count ?? 0;
            Log(LogLevel.Information, "Application data imported.", new Dictionary<string, object?>
            {
                ["importedSessions"] = importModel.Sessions.Count,
                ["personaCount"] = personaCount,
                ["activeSessionId"] = _sessionsState.ActiveSessionId
            });
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task NotifyClientReadyAsync(CancellationToken cancellationToken = default)
    {
        await _mutex.WaitAsync(cancellationToken);
        try
        {
            if (_jsAvailable)
            {
                return;
            }

            var previousSettings = Clone(_settingsCache);
            var previousSessionId = _sessionsState.ActiveSessionId;

            _jsAvailable = true;
            _settingsLoaded = false;
            _sessionsLoaded = false;

            try
            {
                await EnsureSettingsLoadedAsync(cancellationToken);
                await EnsureSessionsLoadedAsync(cancellationToken);
            }
            catch (ClientStorageUnavailableException)
            {
                _jsAvailable = false;
                _settingsLoaded = false;
                _sessionsLoaded = false;
                throw;
            }

            if (!SettingsAreEqual(previousSettings, _settingsCache))
            {
                RaiseSettingsChanged();
            }

            RaisePersonasChanged();

            if (!string.Equals(previousSessionId, _sessionsState.ActiveSessionId, StringComparison.Ordinal))
            {
                RaiseSessionsChanged();
                RaiseChatSessionChanged();
            }
            else
            {
                RaiseSessionsChanged();
                RaiseChatSessionChanged();
            }

            Log(LogLevel.Information, "Client storage became available.", new Dictionary<string, object?>
            {
                ["activeSessionId"] = _sessionsState.ActiveSessionId,
                ["sessionCount"] = _sessionsState.Sessions.Count
            });
        }
        finally
        {
            _mutex.Release();
        }
    }

    private async Task<ChatMessageEntry> AppendMessageAsync(string author, string content, ChatMessageRole role, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Content must not be empty.", nameof(content));
        }

        await _mutex.WaitAsync(cancellationToken);
        try
        {
            await EnsureSessionsLoadedAsync(cancellationToken);

            var entry = ChatMessageEntry.Create(author, content, role);

            var current = _sessionsState.Sessions.First(s => string.Equals(s.SessionId, _sessionsState.ActiveSessionId, StringComparison.Ordinal));
            var updatedMessages = current.Messages.Concat(new[] { entry })
                .OrderBy(m => m.Timestamp)
                .Select(Clone)
                .ToList();

            var updatedSession = current with
            {
                Messages = updatedMessages,
                UpdatedAt = entry.Timestamp
            };

            ReplaceSession(updatedSession);
            _currentSession = Clone(updatedSession);

            await PersistSessionsAsync(cancellationToken);
            RaiseSessionsChanged();
            RaiseChatSessionChanged();

            Log(LogLevel.Information, "Chat message appended.", new Dictionary<string, object?>
            {
                ["author"] = author,
                ["role"] = role.ToString(),
                ["messageId"] = entry.Id,
                ["preview"] = BuildPreview(content)
            });

            return Clone(entry);
        }
        finally
        {
            _mutex.Release();
        }
    }

    private async Task EnsureSettingsLoadedAsync(CancellationToken cancellationToken)
    {
        if (_settingsLoaded)
        {
            return;
        }

        if (!_jsAvailable)
        {
            _settingsLoaded = true;
            return;
        }

        try
        {
            var stored = await _storage.GetItemAsync(StorageArea.Local, SettingsStorageKey, cancellationToken);
            if (!string.IsNullOrEmpty(stored))
            {
                try
                {
                    _settingsCache = JsonSerializer.Deserialize<AppSettings>(stored, SerializerOptions) ?? AppSettings.CreateDefault();
                }
                catch (JsonException)
                {
                    _settingsCache = AppSettings.CreateDefault();
                }
            }
        }
        catch (ClientStorageUnavailableException)
        {
            _jsAvailable = false;
            throw;
        }

        if (_settingsCache.Personas is null)
        {
            _settingsCache = _settingsCache with { Personas = new List<PersonaProfile>() };
        }
        _settingsLoaded = true;
    }

    private async Task EnsureSessionsLoadedAsync(CancellationToken cancellationToken)
    {
        if (_sessionsLoaded)
        {
            return;
        }

        if (!_jsAvailable)
        {
            _sessionsLoaded = true;
            _currentSession = Clone(_sessionsState.Sessions.First());
            return;
        }

        try
        {
            var stored = await _storage.GetItemAsync(StorageArea.Local, SessionsStorageKey, cancellationToken);
            if (!string.IsNullOrEmpty(stored))
            {
                try
                {
                    var state = JsonSerializer.Deserialize<AppSessionsState>(stored, SerializerOptions);
                    if (state is not null && state.Sessions.Count > 0)
                    {
                        _sessionsState = new AppSessionsState
                        {
                            ActiveSessionId = state.ActiveSessionId,
                            Sessions = state.Sessions
                                .Select(session => session with
                                {
                                    Messages = session.Messages.OrderBy(m => m.Timestamp).Select(Clone).ToList()
                                })
                                .ToList()
                        };
                    }
                }
                catch (JsonException)
                {
                    _sessionsState = AppSessionsState.CreateDefault();
                }
            }
        }
        catch (ClientStorageUnavailableException)
        {
            _jsAvailable = false;
            throw;
        }

        if (_sessionsState.Sessions.Count == 0)
        {
            _sessionsState = AppSessionsState.CreateDefault();
        }

        if (string.IsNullOrWhiteSpace(_sessionsState.ActiveSessionId) ||
            !_sessionsState.Sessions.Any(s => string.Equals(s.SessionId, _sessionsState.ActiveSessionId, StringComparison.Ordinal)))
        {
            _sessionsState = _sessionsState with
            {
                ActiveSessionId = _sessionsState.Sessions.First().SessionId
            };
        }

        var active = _sessionsState.Sessions.First(s => string.Equals(s.SessionId, _sessionsState.ActiveSessionId, StringComparison.Ordinal));
        _currentSession = Clone(active);

        _sessionsLoaded = true;
    }

    private async Task PersistSettingsAsync(CancellationToken cancellationToken)
    {
        if (!_jsAvailable)
        {
            return;
        }

        var payload = JsonSerializer.Serialize(_settingsCache, SerializerOptions);
        try
        {
            await _storage.SetItemAsync(StorageArea.Local, SettingsStorageKey, payload, cancellationToken);
        }
        catch (ClientStorageUnavailableException)
        {
            _jsAvailable = false;
            _settingsLoaded = false;
        }
    }

    private async Task PersistSessionsAsync(CancellationToken cancellationToken)
    {
        if (!_jsAvailable)
        {
            return;
        }

        var normalized = new AppSessionsState
        {
            ActiveSessionId = _sessionsState.ActiveSessionId,
            Sessions = _sessionsState.Sessions
                .Select(session => session with
                {
                    UpdatedAt = session.Messages.Count > 0 ? session.Messages.Max(m => m.Timestamp) : session.UpdatedAt
                })
                .ToList()
        };

        var payload = JsonSerializer.Serialize(normalized, SerializerOptions);
        try
        {
            await _storage.SetItemAsync(StorageArea.Local, SessionsStorageKey, payload, cancellationToken);
        }
        catch (ClientStorageUnavailableException)
        {
            _jsAvailable = false;
            _sessionsLoaded = false;
        }
    }

    private void ReplaceSession(ChatSessionState session)
    {
        var sessions = _sessionsState.Sessions.ToList();
        var index = sessions.FindIndex(s => string.Equals(s.SessionId, session.SessionId, StringComparison.Ordinal));
        if (index >= 0)
        {
            sessions[index] = Clone(session);
            _sessionsState = _sessionsState with { Sessions = sessions };
        }
    }

    private bool MergeSessions(IEnumerable<ChatSessionState> sessions)
    {
        var existing = _sessionsState.Sessions.ToDictionary(s => s.SessionId, StringComparer.Ordinal);
        var changed = false;

        foreach (var incoming in sessions)
        {
            if (existing.TryGetValue(incoming.SessionId, out var current))
            {
                var combinedMessages = current.Messages.Concat(incoming.Messages)
                    .OrderBy(m => m.Timestamp)
                    .GroupBy(m => m.Id)
                    .Select(g => g.First())
                    .Select(Clone)
                    .ToList();

                var updated = current with
                {
                    Messages = combinedMessages,
                    Name = string.IsNullOrWhiteSpace(incoming.Name) ? current.Name : incoming.Name,
                    UpdatedAt = new[] { current.UpdatedAt, incoming.UpdatedAt }.Max()
                };

                ReplaceSession(updated);
                existing[updated.SessionId] = updated;
                changed = true;
            }
            else
            {
                var cloned = Clone(incoming);
                ReplaceOrAddSession(cloned);
                existing[cloned.SessionId] = cloned;
                changed = true;
            }
        }

        if (!_sessionsState.Sessions.Any(s => string.Equals(s.SessionId, _sessionsState.ActiveSessionId, StringComparison.Ordinal)))
        {
            var first = _sessionsState.Sessions.First();
            _sessionsState = _sessionsState with { ActiveSessionId = first.SessionId };
            _currentSession = Clone(first);
        }
        else
        {
            var active = _sessionsState.Sessions.First(s => string.Equals(s.SessionId, _sessionsState.ActiveSessionId, StringComparison.Ordinal));
            _currentSession = Clone(active);
        }

        return changed;
    }

    private void ReplaceOrAddSession(ChatSessionState session)
    {
        var sessions = _sessionsState.Sessions.ToList();
        var index = sessions.FindIndex(s => string.Equals(s.SessionId, session.SessionId, StringComparison.Ordinal));
        if (index >= 0)
        {
            sessions[index] = Clone(session);
        }
        else
        {
            sessions.Add(Clone(session));
        }

        _sessionsState = _sessionsState with { Sessions = sessions };
    }

    private IReadOnlyList<ChatSessionSummary> BuildSessionSummaries()
    {
        return _sessionsState.Sessions
            .Select(session => new ChatSessionSummary
            {
                SessionId = session.SessionId,
                Name = session.Name,
                CreatedAt = session.CreatedAt,
                UpdatedAt = session.Messages.Count > 0 ? session.Messages.Max(m => m.Timestamp) : session.UpdatedAt,
                MessageCount = session.Messages.Count,
                IsActive = string.Equals(session.SessionId, _sessionsState.ActiveSessionId, StringComparison.Ordinal)
            })
            .OrderByDescending(summary => summary.UpdatedAt)
            .ToList();
    }

    private void RaiseSettingsChanged()
    {
        var snapshot = Clone(_settingsCache);
        SettingsChanged?.Invoke(this, snapshot);
    }

    private void RaiseChatSessionChanged()
    {
        var snapshot = _currentSession.Messages.Select(Clone).ToList();
        ChatSessionChanged?.Invoke(this, snapshot);
    }

    private void RaiseSessionsChanged()
    {
        var summaries = BuildSessionSummaries();
        SessionsChanged?.Invoke(this, summaries);
    }

    private void RaisePersonasChanged()
    {
        var personas = _settingsCache.Personas.Select(Clone).ToList();
        PersonasChanged?.Invoke(this, personas);
    }

    private static bool SettingsAreEqual(AppSettings left, AppSettings right)
    {
        return ApiSettingsEqual(left.Api, right.Api) &&
               PromptSettingsEqual(left.Prompt, right.Prompt) &&
               PersonasEqual(left.Personas, right.Personas);
    }

    private static bool ApiSettingsEqual(ApiSettings left, ApiSettings right)
    {
        return string.Equals(left.Endpoint, right.Endpoint, StringComparison.Ordinal) &&
               string.Equals(left.ApiKey, right.ApiKey, StringComparison.Ordinal) &&
               left.ApiKeyRequired == right.ApiKeyRequired &&
               PathwayEqual(left.Narrator, right.Narrator) &&
               PathwayEqual(left.System, right.System) &&
               PathwayEqual(left.Image, right.Image);
    }

    private static bool PathwayEqual(ModelPathwaySettings left, ModelPathwaySettings right)
    {
        return string.Equals(left.Model, right.Model, StringComparison.Ordinal) &&
               string.Equals(left.Mode, right.Mode, StringComparison.Ordinal) &&
               left.Enabled == right.Enabled;
    }

    private static bool PromptSettingsEqual(SystemPromptSettings left, SystemPromptSettings right)
    {
        return PromptProfileEqual(left.Narrator, right.Narrator) &&
               PromptProfileEqual(left.System, right.System);
    }

    private static bool PromptProfileEqual(PromptProfile left, PromptProfile right)
    {
        return string.Equals(left.Title, right.Title, StringComparison.Ordinal) &&
               string.Equals(left.Content, right.Content, StringComparison.Ordinal) &&
               string.Equals(left.Mode, right.Mode, StringComparison.Ordinal);
    }

    private static bool PersonasEqual(IReadOnlyList<PersonaProfile> left, IReadOnlyList<PersonaProfile> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        var orderedLeft = left.OrderBy(p => p.Id, StringComparer.Ordinal).ToArray();
        var orderedRight = right.OrderBy(p => p.Id, StringComparer.Ordinal).ToArray();

        for (var i = 0; i < orderedLeft.Length; i++)
        {
            var l = orderedLeft[i];
            var r = orderedRight[i];
            if (!string.Equals(l.Id, r.Id, StringComparison.Ordinal) ||
                !string.Equals(l.Name, r.Name, StringComparison.Ordinal) ||
                !string.Equals(l.Concept, r.Concept, StringComparison.Ordinal) ||
                !string.Equals(l.Backstory, r.Backstory, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static ChatSessionState Clone(ChatSessionState state)
    {
        return new ChatSessionState
        {
            SessionId = state.SessionId,
            Name = state.Name,
            CreatedAt = state.CreatedAt,
            UpdatedAt = state.UpdatedAt,
            Messages = state.Messages.Select(Clone).ToList()
        };
    }

    private static AppSettings Clone(AppSettings value)
    {
        return new AppSettings
        {
            Api = Clone(value.Api),
            Prompt = Clone(value.Prompt),
            Personas = value.Personas.Select(Clone).ToList()
        };
    }

    private static ApiSettings Clone(ApiSettings value)
    {
        return new ApiSettings
        {
            Endpoint = value.Endpoint,
            ApiKey = value.ApiKey,
            ApiKeyRequired = value.ApiKeyRequired,
            Narrator = Clone(value.Narrator),
            System = Clone(value.System),
            Image = Clone(value.Image)
        };
    }

    private static ModelPathwaySettings Clone(ModelPathwaySettings value)
    {
        return new ModelPathwaySettings
        {
            Key = value.Key,
            Model = value.Model,
            Mode = value.Mode,
            Enabled = value.Enabled
        };
    }

    private static SystemPromptSettings Clone(SystemPromptSettings value)
    {
        return new SystemPromptSettings
        {
            Narrator = Clone(value.Narrator),
            System = Clone(value.System)
        };
    }

    private static PromptProfile Clone(PromptProfile value)
    {
        return new PromptProfile
        {
            Key = value.Key,
            Title = value.Title,
            Content = value.Content,
            Mode = value.Mode,
            IsRecommended = value.IsRecommended
        };
    }

    private static ChatMessageEntry Clone(ChatMessageEntry value)
    {
        return new ChatMessageEntry
        {
            Id = value.Id,
            Author = value.Author,
            Content = value.Content,
            Role = value.Role,
            Timestamp = value.Timestamp
        };
    }

    private static PersonaProfile Clone(PersonaProfile value)
    {
        return new PersonaProfile
        {
            Id = value.Id,
            Name = value.Name,
            Concept = value.Concept,
            Backstory = value.Backstory,
            UpdatedAt = value.UpdatedAt
        };
    }


    private void Log(LogLevel level, string message, IReadOnlyDictionary<string, object?>? metadata = null)
    {
        _logBuffer.Log(nameof(AppDataService), level, message, metadata);
    }

    private static string BuildPreview(string? content, int maxLength = 80)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        var trimmed = content.Trim();
        if (trimmed.Length <= maxLength)
        {
            return trimmed;
        }

        return trimmed[..maxLength] + "…";
    }
}
