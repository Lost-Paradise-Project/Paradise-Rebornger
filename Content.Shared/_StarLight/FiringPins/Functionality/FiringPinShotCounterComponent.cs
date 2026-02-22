using Robust.Shared.GameStates;

namespace Content.Shared._StarLight.FiringPins.Functionality;

[RegisterComponent, AutoGenerateComponentState, NetworkedComponent]
public sealed partial class FiringPinShotCounterComponent : Component
{
    [AutoNetworkedField]
    public int ShotsFired = 0;
}
