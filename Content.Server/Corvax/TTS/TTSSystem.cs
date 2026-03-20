using System.Threading.Tasks;
using Content.Server.Radio; // LP edit
using Content.Server.Radio.EntitySystems; // LP edit
using Content.Shared.Chat;
using Content.Server.Chat.Systems;
using Content.Shared.Corvax.CCCVars;
using Content.Shared.Corvax.TTS;
using Content.Shared.GameTicking;
using Content.Shared.Players.RateLimiting;
using Content.Shared.Radio.Components; // LP edit
using Content.Server.Station.Systems; // LP edit
using Content.Shared.Station.Components; // LP edit
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Corvax.TTS;

// ReSharper disable once InconsistentNaming
public sealed partial class TTSSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly TTSManager _ttsManager = default!;
    [Dependency] private readonly SharedTransformSystem _xforms = default!;
    [Dependency] private readonly IRobustRandom _rng = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!; // LP edit

    private readonly List<string> _sampleText =
        new()
        {
            "Съешь же ещё этих мягких французских булок, да выпей чаю.",
            "Клоун, прекрати разбрасывать банановые кожурки офицерам под ноги!",
            "Капитан, вы уверены что хотите назначить клоуна на должность главы персонала?",
            "Эс Бэ! Тут человек в сером костюме, с тулбоксом и в маске! Помогите!!",
            "Учёные, тут странная аномалия в баре! Она уже съела мима!",
            "Я надеюсь что инженеры внимательно следят за сингулярностью...",
            "Вы слышали эти странные крики в техах? Мне кажется туда ходить небезопасно.",
            "Вы не видели Гамлета? Мне кажется он забегал к вам на кухню.",
            "Здесь есть доктор? Человек умирает от отравленного пончика! Нужна помощь!",
            "Вам нужно согласие и печать квартирмейстера, если вы хотите сделать заказ на партию дробовиков.",
            "Возле эвакуационного шаттла разгерметизация! Инженеры, нам срочно нужна ваша помощь!",
            "Бармен, налей мне самого крепкого вина, которое есть в твоих запасах!"
        };

    private const int MaxMessageChars = 100 * 2; // same as SingleBubbleCharLimit * 2
    private bool _isEnabled = false;
    private string _announceVoiceId = string.Empty; // LP edit

    public override void Initialize()
    {
        _cfg.OnValueChanged(CCCVars.TTSEnabled, v => _isEnabled = v, true);
        _cfg.OnValueChanged(CCCVars.TTSAnnounceVoiceId, v => _announceVoiceId = v, true); // LP edit

        SubscribeLocalEvent<TransformSpeechEvent>(OnTransformSpeech);
        SubscribeLocalEvent<AnnouncementTTSEvent>(OnAnnouncementTTS); // LP edit
        // LP edit start - ordering before HeadsetSystem/RadioSystem to see Channel before it's null'd
        SubscribeLocalEvent<TTSComponent, EntitySpokeEvent>(OnEntitySpoke,
            before: [typeof(HeadsetSystem), typeof(RadioSystem)]);
        SubscribeLocalEvent<ActiveRadioComponent, RadioReceiveEvent>(OnRadioReceive);
        // LP edit end
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);

        SubscribeNetworkEvent<RequestPreviewTTSEvent>(OnRequestPreviewTTS);

        RegisterRateLimits();
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _ttsManager.ResetCache();
    }

    private async void OnRequestPreviewTTS(RequestPreviewTTSEvent ev, EntitySessionEventArgs args)
    {
        if (!_isEnabled ||
            !_prototypeManager.TryIndex<TTSVoicePrototype>(ev.VoiceId, out var protoVoice))
            return;

        if (HandleRateLimit(args.SenderSession) != RateLimitStatus.Allowed)
            return;

        var previewText = _rng.Pick(_sampleText);
        var soundData = await GenerateTTS(previewText, protoVoice.Speaker);
        if (soundData is null)
            return;

        RaiseNetworkEvent(new PlayTTSEvent(soundData), Filter.SinglePlayer(args.SenderSession));
    }

    private async void OnEntitySpoke(EntityUid uid, TTSComponent component, EntitySpokeEvent args)
    {
        var voiceId = component.VoicePrototypeId;
        if (!_isEnabled ||
            args.Message.Length > MaxMessageChars ||
            voiceId == null)
            return;

        // LP edit start - skip local TTS for radio messages, radio TTS handled in OnRadioReceive
        if (args.Channel != null)
            return;
        // LP edit end

        var voiceEv = new TransformSpeakerVoiceEvent(uid, voiceId);
        RaiseLocalEvent(uid, voiceEv);
        voiceId = voiceEv.VoiceId;

        if (!_prototypeManager.TryIndex<TTSVoicePrototype>(voiceId, out var protoVoice))
            return;

        if (args.ObfuscatedMessage != null)
        {
            HandleWhisper(uid, args.Message, args.ObfuscatedMessage, protoVoice.Speaker);
            return;
        }

        HandleSay(uid, args.Message, protoVoice.Speaker);
    }

    private async void HandleSay(EntityUid uid, string message, string speaker)
    {
        var soundData = await GenerateTTS(message, speaker);
        if (soundData is null) return;
        RaiseNetworkEvent(new PlayTTSEvent(soundData, GetNetEntity(uid)), Filter.Pvs(uid));
    }

    // LP edit start - fixed whisper: don't block full TTS if obfuscated text fails to generate
    private async void HandleWhisper(EntityUid uid, string message, string obfMessage, string speaker)
    {
        var fullSoundData = await GenerateTTS(message, speaker);
        if (fullSoundData is null) return;

        // Obfuscated text may sanitize to empty — don't let that block the full TTS
        var obfSoundData = await GenerateTTS(obfMessage, speaker);

        var netEntity = GetNetEntity(uid);
        var fullTtsEvent = new PlayTTSEvent(fullSoundData, netEntity, isWhisper: true);
        var obfTtsEvent = obfSoundData != null
            ? new PlayTTSEvent(obfSoundData, netEntity, isWhisper: true)
            : null;

        var xformQuery = GetEntityQuery<TransformComponent>();
        var sourcePos = _xforms.GetWorldPosition(xformQuery.GetComponent(uid), xformQuery);
        var receptions = Filter.Pvs(uid).Recipients;
        foreach (var session in receptions)
        {
            if (!session.AttachedEntity.HasValue) continue;
            var xform = xformQuery.GetComponent(session.AttachedEntity.Value);
            var distance = (sourcePos - _xforms.GetWorldPosition(xform, xformQuery)).Length();
            if (distance > SharedChatSystem.WhisperMuffledRange)
                continue;

            if (distance > SharedChatSystem.WhisperClearRange)
            {
                // Far players hear obfuscated version (if available)
                if (obfTtsEvent != null)
                    RaiseNetworkEvent(obfTtsEvent, session);
            }
            else
            {
                // Nearby players hear the full version
                RaiseNetworkEvent(fullTtsEvent, session);
            }
        }
    }
    // LP edit end

    // LP edit start - radio TTS handler via RadioReceiveEvent
    // Note: RadioReceiveEvent is a ByRefEvent struct, so ref params can't be used in async methods.
    // We extract needed data synchronously, then delegate to async HandleRadio.
    private void OnRadioReceive(EntityUid uid, ActiveRadioComponent component, ref RadioReceiveEvent args)
    {
        if (!_isEnabled)
            return;

        // For headsets, the player is the parent; for intrinsic receivers, the entity itself
        var playerUid = HasComp<HeadsetComponent>(uid) ? Transform(uid).ParentUid : uid;

        if (!TryComp<ActorComponent>(playerUid, out var actor))
            return;

        // Get the sender's voice
        if (!TryComp<TTSComponent>(args.MessageSource, out var ttsComp) || ttsComp.VoicePrototypeId == null)
            return;

        if (args.Message.Length > MaxMessageChars)
            return;

        // Voice mask check (TransformSpeakerVoiceEvent relays through InventoryRelay → MASK slot)
        var voiceEv = new TransformSpeakerVoiceEvent(args.MessageSource, ttsComp.VoicePrototypeId);
        RaiseLocalEvent(args.MessageSource, voiceEv);

        if (!_prototypeManager.TryIndex<TTSVoicePrototype>(voiceEv.VoiceId, out var protoVoice))
            return;

        HandleRadio(args.MessageSource, args.Message, protoVoice.Speaker, actor.PlayerSession);
    }

    private async void HandleRadio(EntityUid source, string message, string speaker, ICommonSession session)
    {
        var soundData = await GenerateTTS(message, speaker, effect: "radio");
        if (soundData is null)
            return;

        RaiseNetworkEvent(
            new PlayTTSEvent(soundData, GetNetEntity(source), isRadio: true),
            Filter.SinglePlayer(session));
    }
    // LP edit end

    // LP edit start - TTS for announcements
    private void OnAnnouncementTTS(AnnouncementTTSEvent ev)
    {
        if (!_isEnabled)
            return;

        // Priority: voice from event (per-prototype) → global CVar
        var voiceId = ev.VoiceId ?? _announceVoiceId;
        if (string.IsNullOrEmpty(voiceId))
            return;

        if (!_prototypeManager.TryIndex<TTSVoicePrototype>(voiceId, out var protoVoice))
            return;

        HandleAnnouncement(ev.Message, protoVoice.Speaker, ev.Station);
    }

    private async void HandleAnnouncement(string message, string speaker, EntityUid? station)
    {
        var soundData = await GenerateTTS(message, speaker);
        if (soundData is null)
            return;

        Filter filter;
        if (station != null && TryComp<StationDataComponent>(station, out var stationData))
            filter = _stationSystem.GetInStation(stationData);
        else
            filter = Filter.Broadcast();

        RaiseNetworkEvent(new PlayTTSEvent(soundData), filter);
    }
    // LP edit end

    // LP edit start - simplified GenerateTTS, removed pitch/rate (not in API), added effect param
    private async Task<byte[]?> GenerateTTS(string text, string speaker, string? effect = null)
    {
        var textSanitized = Sanitize(text);
        if (string.IsNullOrWhiteSpace(textSanitized))
            return null;

        if (char.IsLetter(textSanitized[^1]))
            textSanitized += ".";

        return await _ttsManager.ConvertTextToSpeech(speaker, textSanitized, effect);
    }
    // LP edit end
}
