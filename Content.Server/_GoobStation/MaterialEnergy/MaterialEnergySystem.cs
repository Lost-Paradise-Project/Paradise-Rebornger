using Content.Server.Power.EntitySystems;
using Content.Server.Stack;
using Content.Shared.Interaction;
using Content.Shared.Materials;
using Content.Shared.Stacks;
using Robust.Shared.Timing; // LP Edit

namespace Content.Server._GoobStation.MaterialEnergy
{
    public sealed class MaterialEnergySystem : EntitySystem
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly BatterySystem _batterySystem = default!;
        [Dependency] private readonly StackSystem _stack = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<MaterialEnergyComponent, InteractUsingEvent>(OnInteract);
        }

        private void OnInteract(EntityUid uid, MaterialEnergyComponent component, InteractUsingEvent args)
        {
            args.Handled = true; // LP Edit

            if (component.MaterialWhiteList == null || component.LowQualityMaterialWhiteList == null) // LP Edit
                return;

            _entityManager.TryGetComponent<PhysicalCompositionComponent>(args.Used, out var _composition);
            if (_composition == null)
                return;

            _entityManager.TryGetComponent<StackComponent>(args.Used, out var materialStack);
            if (materialStack == null)
                return;

            foreach (var fueltype in component.MaterialWhiteList)
            {
                if (_composition.MaterialComposition.ContainsKey(fueltype))
                    AddBatteryCharge(
                        uid,
                        args.Used,
                        _composition.MaterialComposition[fueltype],
            // LP Edit Start, player can now use low quality materials to charge the plasma cutter, but at half efficiency
                        materialStack.Count,
                        isLowQuality: false);
            }

            foreach (var fueltype in component.LowQualityMaterialWhiteList)
            {
                if (_composition.MaterialComposition.ContainsKey(fueltype))
                    AddBatteryCharge(
                        uid,
                        args.Used,
                        _composition.MaterialComposition[fueltype],
                        materialStack.Count,
                        isLowQuality: true);
            }
            // LP Edit End, player can now use low quality materials to charge the plasma cutter, but at half efficiency
        }

        private void AddBatteryCharge(
            EntityUid cutter,
            EntityUid _material,
            int materialPerSheet,
            int sheetsInStack, // LP Edit
            bool isLowQuality = false) // LP Edit
        {
            var chargeDiff = _batterySystem.GetChargeDifference(cutter);

            // LP Edit Start, player can now use low quality materials to charge the plasma cutter, but at half efficiency

            if (chargeDiff <= 0)
                return;

            var totalEnergy = materialPerSheet * sheetsInStack;
            var efficiency = isLowQuality ? 0.5f : 1f;
            var energy = Math.Min(chargeDiff, (int)(totalEnergy * efficiency));

            if (energy <= 0)
                return;

            _batterySystem.AddCharge(cutter, energy);

            var sheetsToRemove = (int)Math.Ceiling(energy / (materialPerSheet * efficiency));
            var stackCount = Math.Min(sheetsToRemove, sheetsInStack);

            if (stackCount > 0)
            {
                var removed = _stack.Split(_material, stackCount, Transform(_material).Coordinates);

                Timer.Spawn(0, () =>
                {
                    if (Exists(removed))QueueDel(removed);
                });
            }
            // LP Edit End, player can now use low quality materials to charge the plasma cutter, but at half efficiency
        }
    }
}
