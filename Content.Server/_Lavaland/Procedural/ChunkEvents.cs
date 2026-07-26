namespace Content.Server._Lavaland.Procedural;

/// <summary>
/// Raised when biome chunk is about to unload.
/// </summary>
[ByRefEvent]
public record struct UnLoadChunkEvent(Vector2i Chunk, bool Cancelled = false);

/// <summary>
/// Raised when biome chunk is about to load.
/// </summary>
[ByRefEvent]
public record struct BeforeLoadChunkEvent(Vector2i Chunk, bool Cancelled = false);
