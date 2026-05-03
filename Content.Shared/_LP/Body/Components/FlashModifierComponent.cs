using Robust.Shared.GameStates;

namespace Content.Shared._LP.Body.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class FlashModifierComponent : Component
{
    [DataField]
    public float Modifier = 1f;
}
