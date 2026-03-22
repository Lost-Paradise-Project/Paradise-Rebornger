using Content.Shared._Wega.Mining.Components;
using Robust.Shared.Serialization;

namespace Content.Shared._LP.Mining.Components
{
    /// <summary>
    /// хранение информации о состоянии починки
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class MiningCircuitboardRepairBoundInterfaceState : BoundUserInterfaceState
    {
        public float Condition { get; set; }
        public int CurrentStep { get; set; }
        public List<RepairStep> Steps { get; set; }
        public bool IsScanned { get; set; }

        public MiningCircuitboardRepairBoundInterfaceState(float condition, int currentStep, List<RepairStep> steps, bool isScanned)
        {
            Condition = condition;
            CurrentStep = currentStep;
            Steps = steps;
            IsScanned = isScanned;
        }
    }

    /// <summary>
    /// Client - System прикол. Сообщение при нажатии кнопки сканирования
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class MiningCircuitboardRepairScanMessage : BoundUserInterfaceMessage
    {
    }
}
