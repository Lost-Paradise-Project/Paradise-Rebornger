using Robust.Shared.Utility;

namespace Content.Server._LP.Shuttles;

[RegisterComponent]
public sealed partial class StationSalvageShuttleComponent : Component
{
    [DataField(required: true)]
    public ResPath Path = new("/Maps/_LP/Shuttles/LP_mining.yml");
}
