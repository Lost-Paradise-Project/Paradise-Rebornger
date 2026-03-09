using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using System.Collections.Generic;

namespace Content.Shared._LP.Mining.Components
{
    /// <summary>
    /// Компонент для отслеживания состояния ремонта печатной платы сервера для майнинга
    /// </summary>
    [RegisterComponent]
    public sealed partial class MiningServerCircuitboardRepairComponent : Component
    {
        /// <summary>
        /// индекс текущего шага ремонта
        /// </summary>
        public int CurrentStep = 0;

        /// <summary>
        /// список необходимых шагов по ремонту, которые надо выполнить
        /// </summary>
        public List<RepairStep> Steps = new();

        /// <summary>
        /// проверка платы на сканирование мультитулом
        /// </summary>
        public bool IsScanned = false;

        /// <summary>
        /// Переход на следующий этап, если текущий завершен
        /// </summary>
        /// <returns>True - если все завершено)</returns>
        public bool AdvanceStep()
        {
            CurrentStep++;
            return CurrentStep >= Steps.Count;
        }

        /// <summary>
        /// проверка: правильность применения типа ремонта к инструкции
        /// </summary>
        public bool IsCurrentStep(RepairType type)
        {
            if (CurrentStep >= Steps.Count)
                return false;

            return Steps[CurrentStep].Type == type;
        }
    }

    /// <summary>
    /// Тип этапа ремонта платы
    /// </summary>
    public enum RepairType
    {
        Screwdriver,
        Welder,
        Cable
    }

    /// <summary>
    /// Дает этап ремонта платы
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
