using Content.Shared.Whitelist;

namespace Content.Server._GoobStation.Singularity.EventHorizon;

[RegisterComponent]
public sealed partial class EventHorizonIgnoreComponent : Component
{
    [DataField]
    public EntityWhitelist? HorizonWhitelist;
}
