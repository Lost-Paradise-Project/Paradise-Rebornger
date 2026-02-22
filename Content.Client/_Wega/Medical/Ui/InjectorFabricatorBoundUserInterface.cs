using Content.Shared._Wega.Medical.Ui;
using JetBrains.Annotations;

namespace Content.Client._Wega.Medical.Ui;

[UsedImplicitly]
public sealed class InjectorFabricatorBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private InjectorFabricatorWindow? _window;

    public InjectorFabricatorBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _window = new InjectorFabricatorWindow();
        _window.OnClose += Close;

        _window.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;

        _window.TransferToBufferPressed += (reagent, amount) =>
            SendMessage(new InjectorFabricatorTransferBeakerToBufferMessage(reagent, amount));
        _window.TransferToBeakerPressed += (reagent, amount) =>
            SendMessage(new InjectorFabricatorTransferBufferToBeakerMessage(reagent, amount));
        _window.EjectButtonPressed += () => SendMessage(new InjectorFabricatorEjectMessage());
        _window.ProduceButtonPressed += (amount, name) =>
            SendMessage(new InjectorFabricatorProduceMessage(amount, name));
        _window.ReagentAdded += (reagent, amount) =>
            SendMessage(new InjectorFabricatorSetReagentMessage(reagent, amount));
        _window.ReagentRemoved += reagent =>
            SendMessage(new InjectorFabricatorRemoveReagentMessage(reagent));

        _window.OpenCenteredLeft();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not InjectorFabricatorBoundUserInterfaceState castState)
            return;

        _window?.UpdateState(castState);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        _window?.Close();
        _window = null;
    }
}
