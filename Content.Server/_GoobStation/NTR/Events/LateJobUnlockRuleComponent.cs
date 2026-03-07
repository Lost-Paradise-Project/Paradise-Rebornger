using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server._GoobStation.NTR.Events;

[RegisterComponent]
public sealed partial class LateJobUnlockRuleComponent : Component
{
    /// <summary>
    /// Jobs to add slots for (jobId, slotCount)
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<JobPrototype>, int> JobsToAdd = new();
}
