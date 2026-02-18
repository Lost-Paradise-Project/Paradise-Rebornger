using Content.Shared._StarLight.Power.Components;
using Content.Shared.Power;
using Robust.Shared.GameObjects;

namespace Content.Shared._StarLight.Power.EntitySystems;

public sealed class PoweredLockerSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public void TogglePower(EntityUid uid, PoweredLockerComponent? powerComp = null, bool? powered = null)
    {
        if (!Resolve(uid, ref powerComp))
            return;

        if (powered == null)
            powered = !powerComp.Powered;

        _appearance.SetData(uid, PowerDeviceVisuals.Powered, powered);

        powerComp.Powered = powered.Value;
        Dirty(uid, powerComp);
    }
}
