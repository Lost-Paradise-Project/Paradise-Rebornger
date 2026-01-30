using Content.Shared._GoobStation.Barks;
using Robust.Shared.Configuration;
using Content.Shared._GoobStation.CCVar;
using Content.Shared.Chat;
using Robust.Shared.Player;

namespace Content.Server._GoobStation.Barks;

public sealed class BarkSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SpeechSynthesisComponent, EntitySpokeEvent>(OnEntitySpoke);
    }

    private void OnEntitySpoke(EntityUid uid, SpeechSynthesisComponent comp, EntitySpokeEvent args)
    {
        if (comp.VoicePrototypeId is null
            || !_configurationManager.GetCVar(GoobCVars.BarksEnabled))
            return;

        var sourceEntity = GetNetEntity(uid);
        RaiseNetworkEvent(new PlayBarkEvent(sourceEntity, args.Message, false), Filter.Pvs(uid));
    }
}
