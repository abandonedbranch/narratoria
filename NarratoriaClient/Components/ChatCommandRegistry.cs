using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Components;

namespace NarratoriaClient.Components;

public static class ChatCommandRegistry
{
    private sealed record CommandRegistration(Type ComponentType, IChatCommandComponent Prototype);

    public sealed record ChatCommandDescriptor(string Token, string DisplayName, string Description);

    private static readonly Lazy<IReadOnlyDictionary<string, CommandRegistration>> _registrations = new(BuildRegistrations, LazyThreadSafetyMode.ExecutionAndPublication);

    public static string NormalizeToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return string.Empty;
        }

        return token.Trim().TrimStart('@');
    }

    public static bool TryGetComponentType(string token, out Type? componentType)
    {
        var normalized = NormalizeToken(token);
        if (_registrations.Value.TryGetValue(normalized, out var registration))
        {
            componentType = registration.ComponentType;
            return true;
        }

        componentType = null;
        return false;
    }

    public static RenderFragment? TryCreateFragment(string token)
    {
        var normalized = NormalizeToken(token);
        if (_registrations.Value.TryGetValue(normalized, out var registration))
        {
            return builder =>
            {
                builder.OpenComponent(0, registration.ComponentType);
                builder.CloseComponent();
            };
        }

        return null;
    }

    public static IReadOnlyList<ChatCommandDescriptor> GetCommandDescriptors()
    {
        return _registrations.Value
            .Select(pair => new ChatCommandDescriptor(
                pair.Key,
                pair.Value.Prototype.DisplayName,
                pair.Value.Prototype.Description))
            .OrderBy(descriptor => descriptor.Token, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IReadOnlyDictionary<string, CommandRegistration> BuildRegistrations()
    {
        var assembly = typeof(ChatCommandRegistry).Assembly;
        var result = new Dictionary<string, CommandRegistration>(StringComparer.OrdinalIgnoreCase);

        foreach (var type in assembly.GetTypes())
        {
            if (type.IsAbstract)
            {
                continue;
            }

            if (!typeof(IChatCommandComponent).IsAssignableFrom(type))
            {
                continue;
            }

            if (Activator.CreateInstance(type) is not IChatCommandComponent instance)
            {
                continue;
            }

            var token = NormalizeToken(instance.CommandToken ?? string.Empty);
            if (string.IsNullOrWhiteSpace(token))
            {
                continue;
            }

            if (!result.ContainsKey(token))
            {
                result[token] = new CommandRegistration(type, instance);
            }
        }

        return result;
    }
}
