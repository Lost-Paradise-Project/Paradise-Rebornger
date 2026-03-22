using Content.Shared._Wega.Mining;
using Robust.Client.UserInterface;

namespace Content.Client._Wega.Mining;

public sealed class MiningConsoleBoundInterface : BoundUserInterface
{
    [ViewVariables]
    private MiningConsoleWindow? _window;

    public MiningConsoleBoundInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<MiningConsoleWindow>();

        _window.AllSetStagePressed += stage =>
            SendMessage(new MiningConsoleSetAllStagesMessage(stage));

        _window.ToggleModePressed += () =>
            SendMessage(new MiningConsoleToggleModeMessage());

        // LP edit start
        _window.ActivateAllPressed += () =>
            SendMessage(new MiningConsoleActivateAllMessage());

        _window.DeactivateAllPressed += () =>
            SendMessage(new MiningConsoleDeactivateAllMessage());
        // LP edit end

        _window.ToggleWithdrawPressed += () =>
            SendMessage(new MiningConsoleWithdrawMessage());

        _window.ToggleServerActivationPressed += (serverUid) =>
            SendMessage(new MiningConsoleToggleServerActivationMessage(serverUid));

        _window.ServerStageChangePressed += (serverUid, delta) =>
            SendMessage(new MiningConsoleChangeServerStageMessage(serverUid, delta));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is MiningConsoleBoundInterfaceState cast)
            _window?.UpdateState(cast);
    }
}
