namespace Content.Server._DV.Curation;

/// <summary>
/// Body for cwoink action requests from Discord webhook.
/// </summary>
public sealed class CwoinkActionBody
{
    public required string Text { get; init; }
    public required string Username { get; init; }
    public required Guid Guid { get; init; }
    public bool UserOnly { get; init; }
    public required bool WebhookUpdate { get; init; }
    public required string RoleName { get; init; }
    public required string RoleColor { get; init; }
    public bool AdminOnly { get; init; }
}
