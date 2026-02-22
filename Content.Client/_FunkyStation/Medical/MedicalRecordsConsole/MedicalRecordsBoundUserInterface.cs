using Content.Client._FunkyStation.Medical.MedicalRecordsConsole.UI;
using Content.Shared._FunkyStation.Medical.MedicalRecords;
using Content.Shared.StationRecords;
using JetBrains.Annotations;

namespace Content.Client._FunkyStation.Medical.MedicalRecordsConsole;

[UsedImplicitly]
public sealed class MedicalRecordsBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables] private MedicalRecordsMenu? _menu;

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not MedicalRecordsConsoleState cast)
            return;

        _menu?.UpdateState(cast);
    }

    protected override void Open()
    {
        base.Open();

        _menu = new MedicalRecordsMenu();
        _menu.OnClose += Close;

        _menu.OnListingItemSelected += meta =>
        {
            SendMessage(new MedicalRecordsConsoleSelectMsg(meta?.CharacterRecordKey));
        };

        _menu.OnFiltersChanged += (ty, txt) =>
        {
            SendMessage(txt == null
                ? new MedicalRecordsConsoleFilterMsg(null)
                : new MedicalRecordsConsoleFilterMsg(new StationRecordsFilter(ty, txt)));
        };

        _menu.OpenCentered();
    }


    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _menu?.Close();
    }

}
