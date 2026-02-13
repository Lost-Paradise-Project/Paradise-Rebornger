using Content.Shared._FunkyStation.Medical.MedicalRecords;
using Content.Shared.StationRecords;

namespace Content.Server._FunkyStation.Medical.MedicalRecords;

[RegisterComponent]
public sealed partial class MedicalRecordsConsoleComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public uint? SelectedIndex { get; set; }

    [ViewVariables(VVAccess.ReadOnly)]
    public StationRecordsFilter? Filter;
}
