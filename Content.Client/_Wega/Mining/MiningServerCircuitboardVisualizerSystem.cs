using Content.Shared._Wega.Mining.Components;
using Robust.Client.GameObjects;

namespace Content.Client._Wega.Mining;

public sealed class MiningServerCircuitboardVisualizerSystem : VisualizerSystem<MiningServerCircuitboardVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, MiningServerCircuitboardVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!AppearanceSystem.TryGetData<bool>(uid, MiningServerCircuitboardVisuals.IsBroken, out var isBroken, args.Component))
            return;

        // Set the appropriate sprite state based on broken status
        var state = isBroken ? "engineering_crack" : "engineering";
        args.Sprite.LayerSetState(0, state);
    }
}

