using System.Text.Json.Serialization;

namespace NarratoriaClient.Services;

public enum ChatMessageRole
{
    Narrator,
    Player
}

public sealed record ApiSettings
{
    public string Endpoint { get; init; } = string.Empty;
    public bool ApiKeyRequired { get; init; } = true;
    public string ApiKey { get; init; } = string.Empty;
    public ModelPathwaySettings Narrator { get; init; } = ModelPathwaySettings.CreateNarratorDefault();
    public ModelPathwaySettings System { get; init; } = ModelPathwaySettings.CreateSystemDefault();
    public ModelPathwaySettings Image { get; init; } = ModelPathwaySettings.CreateImageDefault();
}

public sealed record ModelPathwaySettings
{
    public string Key { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public string Mode { get; init; } = string.Empty;
    public bool Enabled { get; init; } = true;

    public static ModelPathwaySettings CreateNarratorDefault() => new()
    {
        Key = "narrator",
        Model = "gpt-4o-mini",
        Mode = "Narration",
        Enabled = true
    };

    public static ModelPathwaySettings CreateSystemDefault() => new()
    {
        Key = "system",
        Model = "gpt-4o-mini",
        Mode = "System",
        Enabled = true
    };

    public static ModelPathwaySettings CreateImageDefault() => new()
    {
        Key = "image",
        Model = "gpt-image-1",
        Mode = "Images",
        Enabled = false
    };
}

public sealed record SystemPromptSettings
{
    public PromptProfile Narrator { get; init; } = PromptProfile.CreateNarratorDefault();
    public PromptProfile System { get; init; } = PromptProfile.CreateSystemDefault();
}

public sealed record PromptProfile
{
    public string Key { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string Mode { get; init; } = string.Empty;
    public bool IsRecommended { get; init; } = true;

    private const string DefaultNarratorPrompt = """
You are Narratoria, an evocative tabletop roleplaying narrator. Guide the conversation like a human game master: pace the story, describe vivid scenes, and fold in player contributions. Keep responses concise (2-4 paragraphs) unless the player requests extra detail. Ask clarifying questions sparingly when they help move the adventure forward. Avoid resolving the player’s declared actions for them—describe likely outcomes, invite decisions, and keep the narrative collaborative.
""";

    private const string DefaultSystemPrompt = """
You are Narratoria's system agent. Your job is to observe player and narrator messages, extract key facts, and maintain structured summaries that future automation can consume. Keep explanations factual, neutral, and focused on trackable state (characters, goals, locations, unresolved threads). When answering questions, cite the relevant session details and avoid inventing new lore unless explicitly instructed.
""";

    public static PromptProfile CreateNarratorDefault() => new()
    {
        Key = "narrator",
        Title = "Narrator guidance",
        Content = DefaultNarratorPrompt,
        Mode = "Narration",
        IsRecommended = true
    };

    public static PromptProfile CreateSystemDefault() => new()
    {
        Key = "system",
        Title = "Narratoria system agent",
        Content = DefaultSystemPrompt,
        Mode = "System",
        IsRecommended = false
    };
}

public sealed record AppSettings
{
    public ApiSettings Api { get; init; } = new();
    public SystemPromptSettings Prompt { get; init; } = new();
    public List<PersonaProfile> Personas { get; init; } = new();

    public static AppSettings CreateDefault() => new();
}

public sealed record ChatMessageEntry
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Author { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ChatMessageRole Role { get; init; } = ChatMessageRole.Narrator;
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    public static ChatMessageEntry Create(string author, string content, ChatMessageRole role)
    {
        return new ChatMessageEntry
        {
            Id = Guid.NewGuid().ToString("N"),
            Author = author,
            Content = content,
            Role = role,
            Timestamp = DateTimeOffset.UtcNow
        };
    }
}

public sealed record ChatSessionState
{
    public string SessionId { get; init; } = Guid.NewGuid().ToString("N");
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;
    public string Name { get; init; } = "Session";
    public List<ChatMessageEntry> Messages { get; init; } = new();

    public static ChatSessionState CreateNew(string? name = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new ChatSessionState
        {
            CreatedAt = now,
            UpdatedAt = now,
            Name = string.IsNullOrWhiteSpace(name) ? "New Session" : name
        };
    }
}

public sealed record AppExportModel
{
    public int Version { get; init; } = 1;
    public AppSettings Settings { get; init; } = AppSettings.CreateDefault();
    public List<ChatSessionState> Sessions { get; init; } = new();
}

public sealed record PersonaProfile
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Name { get; init; } = string.Empty;
    public string Concept { get; init; } = string.Empty;
    public string Backstory { get; init; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;
}

public sealed record AppSessionsState
{
    public string ActiveSessionId { get; init; } = string.Empty;
    public List<ChatSessionState> Sessions { get; init; } = new();

    public static AppSessionsState CreateDefault()
    {
        var first = ChatSessionState.CreateNew("Session 1");
        return new AppSessionsState
        {
            ActiveSessionId = first.SessionId,
            Sessions = new List<ChatSessionState> { first }
        };
    }
}

public sealed record ChatSessionSummary
{
    public string SessionId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public int MessageCount { get; init; }
    public bool IsActive { get; init; }
}
