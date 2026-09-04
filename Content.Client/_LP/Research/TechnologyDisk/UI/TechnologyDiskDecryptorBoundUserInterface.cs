using Content.Shared._LP.Research.TechnologyDisk.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._LP.Research.TechnologyDisk.UI;

[UsedImplicitly]
public sealed class TechnologyDiskDecryptorBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private TechnologyDiskDecryptorWindow? _window;

    public TechnologyDiskDecryptorBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<TechnologyDiskDecryptorWindow>();
        _window.OnStartManual += () => SendMessage(new DiskDecryptorStartManualMessage());
        _window.OnStartAuto += () => SendMessage(new DiskDecryptorStartAutoMessage());
        _window.OnClaim += () => SendMessage(new DiskDecryptorClaimMessage());
        _window.OnLockCalibration += () => SendMessage(new DiskDecryptorLockCalibrationMessage());
        _window.OnGridClick += index => SendMessage(new DiskDecryptorGridClickMessage(index));
        _window.OnSubmitCode += () => SendMessage(new DiskDecryptorSubmitCodeMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not DiskDecryptorBoundUserInterfaceState cast)
            return;

        _window?.UpdateState(cast);
    }
}
