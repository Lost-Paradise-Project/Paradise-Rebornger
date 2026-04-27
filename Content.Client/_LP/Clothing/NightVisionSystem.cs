using Content.Shared._LP.Clothing.Components;
using Content.Shared.GameTicking;
using Robust.Client.Player;
using Robust.Client.Graphics;
using Content.Shared._LP.Clothing.Systems;
using Content.Client._LP.Overlayes;
using Content.Shared.Inventory;

namespace Content.Client._LP.Clothing;

public sealed class NightVisionSystem : SharedNightVisionSystem
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly ILightManager _lightManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    private NightVisionOverlay _overlay = default!;
    private bool _enabled;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRestart);

        _overlay = new(this);
    }

    public bool IsEnabled() => _enabled;

    private EntityUid? GetPlayer()
    {
        return _playerManager.LocalSession?.AttachedEntity;
    }

    public NightVisionComponent? GetNightComp()
    {
        var player = GetPlayer();

        if (player == null)
            return null;

        if (!_entMan.TryGetComponent(player.Value, out InventoryComponent? inventory))
            return null;

        foreach (var slot in inventory.Slots)
        {
            if (!_inventory.TryGetSlotEntity(player.Value, slot.Name, out var item))
                continue;

            if (!_entMan.TryGetComponent(item, out NightVisionComponent? nv))
                continue;

            if (nv.RequiredSlot != null && nv.RequiredSlot != slot.Name)
                continue;

            return nv;
        }

        return null;
    }

    protected override void UpdateNightVisionEffects(EntityUid user, Entity<NightVisionComponent> ent, bool state)
    {
        if (GetPlayer() != user)
            return;

        _enabled = state;

        _lightManager.DrawLighting = !state;

        if (state)
            _overlayMan.AddOverlay(_overlay);
        else
            _overlayMan.RemoveOverlay(_overlay);
    }

    private void OnRestart(RoundRestartCleanupEvent ev)
    {
        _overlayMan.RemoveOverlay(_overlay);
        _lightManager.DrawLighting = true;
    }
}
