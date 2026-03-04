using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using System.Collections.Generic;
using Robust.Shared.Random;
using Robust.Shared.IoC;

namespace Content.Shared._Wega.Mining.Components
{
    /// <summary>
    /// Component to track the repair state of a mining server circuit board
    /// </summary>
    [RegisterComponent]
    public sealed partial class MiningServerCircuitboardRepairComponent : Component
    {
        /// <summary>
        /// Current repair step index
        /// </summary>
        public int CurrentStep = 0;

        /// <summary>
        /// List of repair steps to complete
        /// </summary>
        public List<RepairStep> Steps = new();

        /// <summary>
        /// Whether the circuit board has been scanned with a multitool
        /// </summary>
        public bool IsScanned = false;

        /// <summary>
        /// Generates random repair steps for the circuit board
        /// </summary>
        public void GenerateRepairSteps()
        {
            Steps.Clear();
            CurrentStep = 0;
            IsScanned = false;

            // Possible repair steps
            var possibleSteps = new List<RepairStep>
            {
                new RepairStep(RepairType.Screwdriver, "mining-circuitboard-repair-step-screwdriver"),
                new RepairStep(RepairType.Welder, "mining-circuitboard-repair-step-welder"),
                new RepairStep(RepairType.Cable, "mining-circuitboard-repair-step-cable")
            };

            // Shuffle and select 2-3 unique steps
            var random = IoCManager.Resolve<IRobustRandom>();
            var stepCount = random.Next(2, 4);

            for (var i = 0; i < stepCount; i++)
            {
                var index = random.Next(possibleSteps.Count);
                Steps.Add(possibleSteps[index]);
                possibleSteps.RemoveAt(index);
            }
        }

        /// <summary>
        /// Advances to the next repair step if the current step is completed
        /// </summary>
        /// <returns>True if all steps are completed</returns>
        public bool AdvanceStep()
        {
            CurrentStep++;
            return CurrentStep >= Steps.Count;
        }

        /// <summary>
        /// Checks if the current step matches the given type
        /// </summary>
        public bool IsCurrentStep(RepairType type)
        {
            if (CurrentStep >= Steps.Count)
                return false;

            return Steps[CurrentStep].Type == type;
        }
    }

    /// <summary>
    /// Type of repair step
    /// </summary>
    public enum RepairType
    {
        Screwdriver,
        Welder,
        Cable
    }

    /// <summary>
    /// Represents a single repair step
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class RepairStep
    {
        public RepairStep(RepairType type, string description)
        {
            Type = type;
            Description = description;
        }

        public RepairType Type { get; set; }
        public string Description { get; set; }
    }
}
