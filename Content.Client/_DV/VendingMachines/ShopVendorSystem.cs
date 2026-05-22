using Content.Shared._DV.VendingMachines;
using Content.Shared.VendingMachines;
using Robust.Client.Animations;
using Robust.Client.GameObjects;

namespace Content.Client._DV.VendingMachines;

public sealed class ShopVendorSystem : SharedShopVendorSystem
{
    [Dependency] private readonly AnimationPlayerSystem _animationPlayer = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShopVendorComponent, AppearanceChangeEvent>(OnAppearanceChange);
        SubscribeLocalEvent<ShopVendorComponent, AnimationCompletedEvent>(OnAnimationCompleted);
    }

    // copied from vending machines because its not reusable in other systems :)
    private void OnAnimationCompleted(Entity<ShopVendorComponent> ent, ref AnimationCompletedEvent args)
    {
        UpdateAppearance((ent, ent.Comp));
    }

    private void OnAppearanceChange(Entity<ShopVendorComponent> ent, ref AppearanceChangeEvent args)
    {
        UpdateAppearance((ent, ent.Comp, args.Sprite));
    }

    private void UpdateAppearance(Entity<ShopVendorComponent, SpriteComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp2))
            return;

        if (!_appearance.TryGetData<VendingMachineVisualState>(ent, VendingMachineVisuals.VisualState, out var state))
            state = VendingMachineVisualState.Normal;

        var sprite = ent.Comp2;
        SetLayerState(VendingMachineVisualLayers.Base, ent.Comp1.OffState, (ent, sprite));
        SetLayerState(VendingMachineVisualLayers.Screen, ent.Comp1.ScreenState, (ent, sprite));
        switch (state)
        {
            case VendingMachineVisualState.Normal:
                SetLayerState(VendingMachineVisualLayers.BaseUnshaded, ent.Comp1.NormalState, (ent, sprite));
                break;

            case VendingMachineVisualState.Deny:
                if (ent.Comp1.LoopDenyAnimation)
                    SetLayerState(VendingMachineVisualLayers.BaseUnshaded, ent.Comp1.DenyState, (ent, sprite));
                else
                    PlayAnimation(ent, VendingMachineVisualLayers.BaseUnshaded, ent.Comp1.DenyState, ent.Comp1.DenyDelay, (ent, sprite));
                break;

            case VendingMachineVisualState.Eject:
                PlayAnimation(ent, VendingMachineVisualLayers.BaseUnshaded, ent.Comp1.EjectState, ent.Comp1.EjectDelay, (ent, sprite));
                break;

            case VendingMachineVisualState.Broken:
                HideLayers((ent, sprite));
                SetLayerState(VendingMachineVisualLayers.Base, ent.Comp1.BrokenState, (ent, sprite));
                break;

            case VendingMachineVisualState.Off:
                HideLayers((ent, sprite));
                break;
        }
    }

    private void SetLayerState(VendingMachineVisualLayers layer, string? state, Entity<SpriteComponent> ent)
    {
        if (state == null)
            return;

        _sprite.LayerSetVisible((ent, ent.Comp), layer, true);
        _sprite.LayerSetAutoAnimated((ent, ent.Comp), layer, true);
        _sprite.LayerSetRsiState((ent, ent.Comp), layer, state);
    }

    private void PlayAnimation(EntityUid uid, VendingMachineVisualLayers layer, string? state, TimeSpan time, Entity<SpriteComponent> ent)
    {
        if (state == null || _animationPlayer.HasRunningAnimation(uid, state))
            return;

        var animation = GetAnimation(layer, state, time);
        _sprite.LayerSetVisible((ent, ent.Comp), layer, true);
        _animationPlayer.Play(uid, animation, state);
    }

    private static Animation GetAnimation(VendingMachineVisualLayers layer, string state, TimeSpan time)
    {
        return new Animation
        {
            Length = time,
            AnimationTracks =
            {
                new AnimationTrackSpriteFlick
                {
                    LayerKey = layer,
                    KeyFrames =
                    {
                        new AnimationTrackSpriteFlick.KeyFrame(state, 0f)
                    }
                }
            }
        };
    }

    private void HideLayers(Entity<SpriteComponent> ent)
    {
        HideLayer(VendingMachineVisualLayers.BaseUnshaded, ent);
        HideLayer(VendingMachineVisualLayers.Screen, ent);
    }

    private void HideLayer(VendingMachineVisualLayers layer, Entity<SpriteComponent> ent)
    {
        if (!_sprite.LayerMapTryGet((ent.Owner, ent.Comp), layer, out var actualLayer, false))
            return;

        _sprite.LayerSetVisible((ent.Owner, ent.Comp), actualLayer, false);
    }
}
