using Content.Shared._Wega.Mining.Components;
using Robust.Shared.Serialization;

namespace Content.Shared._Wega.Mining.Components
{
    /// <summary>
    /// Data class to hold repair state information
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
    /// Message sent from client to server when scan button is pressed
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class MiningCircuitboardRepairScanMessage : BoundUserInterfaceMessage
    {
    }
}

