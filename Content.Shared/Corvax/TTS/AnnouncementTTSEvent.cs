namespace Content.Shared.Corvax.TTS;

// LP edit start
/// <summary>
/// Raised when an announcement should be voiced by TTS.
/// Station == null means global (all players).
/// VoiceId == null means use the default CVar voice.
/// </summary>
// ReSharper disable once InconsistentNaming
public sealed class AnnouncementTTSEvent : EntityEventArgs
{
    public string Message { get; }
    public EntityUid? Station { get; }
    public string? VoiceId { get; }

    public AnnouncementTTSEvent(string message, EntityUid? station = null, string? voiceId = null)
    {
        Message = message;
        Station = station;
        VoiceId = voiceId;
    }
}
// LP edit end
