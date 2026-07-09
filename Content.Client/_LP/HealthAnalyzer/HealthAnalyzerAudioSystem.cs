using Content.Shared._LP.HealthAnalyzer;
using Content.Shared.Mobs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;

namespace Content.Client._LP.HealthAnalyzer;

public sealed class HealthAnalyzerAudioSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private readonly SoundSpecifier _heartbeat = new SoundPathSpecifier("/Audio/_LP/HealthAnalyzer/heartbeat.ogg");

    private readonly SoundSpecifier _softCrit = new SoundPathSpecifier("/Audio/_LP/HealthAnalyzer/soft_critical.ogg");

    private readonly SoundSpecifier _critical = new SoundPathSpecifier("/Audio/_LP/HealthAnalyzer/critical.ogg");
    private MobState? _currentState;
    private EntityUid? _currentAudio;

    public override void Initialize()
    {
        SubscribeNetworkEvent<HealthAnalyzerAudioEvent>(OnAudioEvent);
        SubscribeNetworkEvent<HealthAnalyzerStopAudioEvent>(OnStopAudio);
    }

    private void OnAudioEvent(HealthAnalyzerAudioEvent ev)
    {
        if (!ev.ForceRestart && _currentState == ev.State)
            return;

        switch (ev.State)
        {
            case MobState.Alive:
                Play(_heartbeat, MobState.Alive);
                break;
            case MobState.SoftCritical:
                Play(_softCrit, MobState.SoftCritical);
                break;
            case MobState.Critical:
                Play(_critical, MobState.Critical);
                break;
        }
    }

    private void OnStopAudio(HealthAnalyzerStopAudioEvent ev)
    {
        Stop();
    }

    private void Play(SoundSpecifier sound, MobState state)
    {
        Stop();

        _currentState = state;

        var audio = _audio.PlayGlobal(
            sound,
            Filter.Local(),
            true,
            AudioParams.Default
                .WithLoop(true)
                .WithVolume(-5f));

        if (audio != null)
            _currentAudio = audio.Value.Entity;
    }

    private void Stop()
    {
        if (_currentAudio != null &&
            Exists(_currentAudio.Value))
        {
            Del(_currentAudio.Value);
        }

        _currentAudio = null;
        _currentState = null;
    }
}
