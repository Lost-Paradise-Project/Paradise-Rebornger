using Content.Shared.Whitelist;

namespace Content.Goobstation.Server.Singularity.EventHorizon;

[RegisterComponent]
public sealed partial class EventHorizonIgnoreComponent : Component
{
    [DataField]
    public EntityWhitelist? HorizonWhitelist;
}
