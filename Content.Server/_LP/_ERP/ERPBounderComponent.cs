#if LP
using Content.Shared._ERPModule.Data.Helpers;
#endif

namespace Content.Server._LP._ERP;

[RegisterComponent]
public sealed partial class ERPBounderComponent : Component
{
#if LP
    [DataField]
    public List<GenitalSlot> CustomGenitals { get; set; } = new();
#endif
}
