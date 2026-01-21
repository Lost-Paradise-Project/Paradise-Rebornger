using Robust.Shared.GameStates;

namespace Content.Shared._Wega.NightLightning.Components;

/// <summary>
/// Компонент отвечающий за обработку ночного света
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class NightLightningComponent : Component
{
    [DataField]
    public float NextTimeTick { get; set; }

    [DataField]
    public bool IsNight = false;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class NightLightComponent : Component
{
}

[RegisterComponent, NetworkedComponent]
public sealed partial class NightLightBlockedComponent : Component
{
}
