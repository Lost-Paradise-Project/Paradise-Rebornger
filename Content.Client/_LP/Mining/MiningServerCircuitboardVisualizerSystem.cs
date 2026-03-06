using Content.Shared._LP.Mining.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client._LP.Mining;

public sealed class MiningServerCircuitboardVisualizerSystem : VisualizerSystem<MiningServerCircuitboardVisualsComponent>
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    protected override void OnAppearanceChange(EntityUid uid, MiningServerCircuitboardVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!AppearanceSystem.TryGetData<bool>(uid, MiningServerCircuitboardVisuals.IsBroken, out var isBroken, args.Component))
            return;

        // Set the appropriate sprite state based on broken status
        if (isBroken)
        {
            // Use LP folder for crack animation
            _sprite.LayerSetSprite(uid, 0, new SpriteSpecifier.Rsi(
                new ResPath("_LP/Objects/Misc/module.rsi"),
                "engineering_crack"));
        }
        else
        {
            // Use standard folder for normal state
            _sprite.LayerSetSprite(uid, 0, new SpriteSpecifier.Rsi(
                new ResPath("Objects/Misc/module.rsi"),
                "engineering"));
        }
    }
}
