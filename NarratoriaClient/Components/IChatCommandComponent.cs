using Microsoft.AspNetCore.Components;

namespace NarratoriaClient.Components;

/// <summary>
/// Identifies a component that can be embedded in the chat scrollback via a command token (e.g. @settings).
/// </summary>
public interface IChatCommandComponent
{
    /// <summary>
    /// Gets the command token (without the @ prefix) that should render this component.
    /// </summary>
    string CommandToken { get; }

    /// <summary>
    /// Gets the display name presented to users in help listings.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Gets a concise description of what this command renders or does.
    /// </summary>
    string Description { get; }
}
