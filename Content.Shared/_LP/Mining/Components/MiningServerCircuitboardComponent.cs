using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared._Wega.Mining;

namespace Content.Shared._LP.Mining.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MiningServerCircuitboardComponent : Component
{
    /// <summary>
    /// Состояние здоровья платы в процентах (0-100)
    /// TODO: потом можно развить степени поломки в процентах ?
    /// </summary>
    [DataField("condition")]
    [ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public float Condition = 100f;

    /// <summary>
    /// Максимальное здоровье платы
    /// </summary>
    public const float MaxCondition = 100f;

    /// <summary>
    /// Минимальное здоровье платы, при котором она сломается
    /// </summary>
    public const float MinCondition = 0f;

    /// <summary>
    /// Является ли плата сломанной
    /// </summary>
    public bool IsBroken => Condition <= MinCondition;

    /// <summary>
    /// Время выполнения операции сварки в секундах
    /// </summary>
    [DataField("weldTime")]
    public float WeldTime = 5f;

    /// <summary>i
    /// Время выполнения операции отверткой в секундах
    /// </summary>
    [DataField("screwdriverTime")]
    public float ScrewdriverTime = 2f;
}
