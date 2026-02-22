using System.Linq;
using Content.Server.Power.EntitySystems;
using Content.Shared._Wega.Medical;
using Content.Shared._Wega.Medical.Ui;
using Content.Shared.Audio;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.FixedPoint;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;

namespace Content.Server._Wega.Medical;

public sealed class InjectorFabricatorSystem : EntitySystem
{
    [Dependency] private readonly SharedAmbientSoundSystem _ambient = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionSystem = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InjectorFabricatorComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<InjectorFabricatorComponent, EntInsertedIntoContainerMessage>(OnContainerModified);
        SubscribeLocalEvent<InjectorFabricatorComponent, EntRemovedFromContainerMessage>(OnContainerModified);
        SubscribeLocalEvent<InjectorFabricatorComponent, BoundUIOpenedEvent>(OnUIOpened);

        SubscribeLocalEvent<InjectorFabricatorComponent, InjectorFabricatorTransferBeakerToBufferMessage>(OnTransferBeakerToBufferMessage);
        SubscribeLocalEvent<InjectorFabricatorComponent, InjectorFabricatorTransferBufferToBeakerMessage>(OnTransferBufferToBeakerMessage);
        SubscribeLocalEvent<InjectorFabricatorComponent, InjectorFabricatorSetReagentMessage>(OnSetReagentMessage);
        SubscribeLocalEvent<InjectorFabricatorComponent, InjectorFabricatorRemoveReagentMessage>(OnRemoveReagentMessage);
        SubscribeLocalEvent<InjectorFabricatorComponent, InjectorFabricatorProduceMessage>(OnProduceMessage);
        SubscribeLocalEvent<InjectorFabricatorComponent, InjectorFabricatorEjectMessage>(OnEjectMessage);
        SubscribeLocalEvent<InjectorFabricatorComponent, InjectorFabricatorSyncRecipeMessage>(OnSyncRecipeMessage);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<InjectorFabricatorComponent>();
        while (query.MoveNext(out var uid, out var InjectorFabricator))
        {
            if (!InjectorFabricator.IsProducing || !this.IsPowered(uid, EntityManager))
                return;

            InjectorFabricator.ProductionTimer += frameTime;
            if (InjectorFabricator.ProductionTimer >= InjectorFabricator.ProductionTime)
            {
                InjectorFabricator.ProductionTimer = 0f;
                ProduceInjector(uid, InjectorFabricator);
                InjectorFabricator.InjectorsProduced++;

                if (InjectorFabricator.InjectorsProduced >= InjectorFabricator.InjectorsToProduce)
                {
                    InjectorFabricator.IsProducing = false;
                    InjectorFabricator.InjectorsToProduce = 0;
                    InjectorFabricator.InjectorsProduced = 0;
                    InjectorFabricator.Recipe = null;

                    _ambient.SetAmbience(uid, false);
                }

                UpdateAppearance(uid, InjectorFabricator);
                UpdateUiState(uid, InjectorFabricator);
            }
        }
    }

    private void OnMapInit(EntityUid uid, InjectorFabricatorComponent component, MapInitEvent args)
    {
        _solutionSystem.EnsureSolution(uid, InjectorFabricatorComponent.BufferSolutionName, out _, component.BufferMaxVolume);
    }

    private void OnContainerModified(EntityUid uid, InjectorFabricatorComponent component, ContainerModifiedMessage args)
    {
        if (args.Container.ID == InjectorFabricatorComponent.BeakerSlotId)
            UpdateUiState(uid, component);
    }

    private void OnUIOpened(EntityUid uid, InjectorFabricatorComponent component, BoundUIOpenedEvent args)
    {
        UpdateUiState(uid, component);
    }

    private void OnTransferBeakerToBufferMessage(EntityUid uid, InjectorFabricatorComponent component, InjectorFabricatorTransferBeakerToBufferMessage args)
    {
        if (component.IsProducing || component.BeakerSlot.Item is not { } beaker)
            return;

        if (!_solutionSystem.TryGetSolution(beaker, "beaker", out var beakerSolution, out var solution) ||
            !_solutionSystem.TryGetSolution(uid, InjectorFabricatorComponent.BufferSolutionName, out var bufferSolution, out _))
            return;

        if (solution.GetReagentQuantity(args.ReagentId) < args.Amount)
            return;

        var quantity = new ReagentQuantity(args.ReagentId, args.Amount);
        _solutionSystem.RemoveReagent(beakerSolution.Value, quantity);
        _solutionSystem.TryAddReagent(bufferSolution.Value, quantity, out _);

        UpdateUiState(uid, component);
    }

    private void OnTransferBufferToBeakerMessage(EntityUid uid, InjectorFabricatorComponent component, InjectorFabricatorTransferBufferToBeakerMessage args)
    {
        if (component.IsProducing)
            return;

        if (component.BeakerSlot.Item is not { } beaker)
            return;

        if (!_solutionSystem.TryGetSolution(beaker, "beaker", out var beakerSolution, out _) ||
            !_solutionSystem.TryGetSolution(uid, InjectorFabricatorComponent.BufferSolutionName, out var bufferSolution, out var solution))
            return;

        if (solution.GetReagentQuantity(args.ReagentId) < args.Amount)
            return;

        var quantity = new ReagentQuantity(args.ReagentId, args.Amount);
        _solutionSystem.RemoveReagent(bufferSolution.Value, quantity);
        _solutionSystem.TryAddReagent(beakerSolution.Value, quantity, out _);

        UpdateUiState(uid, component);
    }

    private void OnSetReagentMessage(EntityUid uid, InjectorFabricatorComponent component, InjectorFabricatorSetReagentMessage args)
    {
        if (component.IsProducing)
            return;

        if (component.Recipe == null)
            component.Recipe = new Dictionary<ReagentId, FixedPoint2>();

        var exactKey = component.Recipe.Keys.FirstOrDefault(k =>
            k.Prototype == args.ReagentId.Prototype);
        if (exactKey != default)
        {
            component.Recipe[exactKey] += args.Amount;
        }
        else
        {
            component.Recipe[args.ReagentId] = args.Amount;
        }

        UpdateUiState(uid, component);
    }

    private void OnRemoveReagentMessage(EntityUid uid, InjectorFabricatorComponent component, InjectorFabricatorRemoveReagentMessage args)
    {
        if (component.IsProducing || component.Recipe == null)
            return;

        var exactKey = component.Recipe.Keys.FirstOrDefault(k =>
            k.Prototype == args.ReagentId.Prototype);
        if (exactKey != default)
            component.Recipe.Remove(exactKey);

        UpdateUiState(uid, component);
    }

    private void OnProduceMessage(EntityUid uid, InjectorFabricatorComponent component, InjectorFabricatorProduceMessage args)
    {
        if (component.IsProducing)
            return;

        if (component.Recipe == null || component.Recipe.Sum(r => (long)r.Value) > 30)
            return;

        var totalRequired = new Dictionary<ReagentId, FixedPoint2>();
        foreach (var (reagent, amountPerInjector) in component.Recipe)
        {
            totalRequired[reagent] = amountPerInjector * args.Amount;
        }

        if (!_solutionSystem.TryGetSolution(uid, InjectorFabricatorComponent.BufferSolutionName, out var bufferSolution, out var buffer))
            return;

        foreach (var (reagentId, requiredAmount) in totalRequired)
        {
            var availableAmount = buffer.GetReagentQuantity(reagentId);
            if (availableAmount < requiredAmount)
                return;
        }

        component.CustomName = args.CustomName;
        component.InjectorsToProduce = args.Amount;
        component.InjectorsProduced = 0;
        component.IsProducing = true;
        component.ProductionTimer = 0f;

        _ambient.SetAmbience(uid, true);

        UpdateAppearance(uid, component);
        UpdateUiState(uid, component);
    }

    private void OnEjectMessage(EntityUid uid, InjectorFabricatorComponent component, InjectorFabricatorEjectMessage args)
    {
        if (component.IsProducing)
            return;

        _itemSlotsSystem.TryEject(uid, component.BeakerSlot, null, out var _, true);
    }

    private void OnSyncRecipeMessage(EntityUid uid, InjectorFabricatorComponent component, InjectorFabricatorSyncRecipeMessage args)
    {
        if (component.IsProducing)
            return;

        component.Recipe = args.Recipe;
        UpdateUiState(uid, component);
    }

    private void ProduceInjector(EntityUid uid, InjectorFabricatorComponent component)
    {
        if (component.Recipe == null)
            return;

        var injector = Spawn(component.Injector, Transform(uid).Coordinates);
        if (!HasComp<SolutionContainerManagerComponent>(injector))
            return;

        if (!_solutionSystem.TryGetSolution(injector, "pen", out var solution, out _))
            return;

        if (!_solutionSystem.TryGetSolution(uid, InjectorFabricatorComponent.BufferSolutionName, out var bufferSolution, out _))
            return;

        foreach (var (reagent, amount) in component.Recipe)
        {
            var addQuantity = new ReagentQuantity(reagent, amount);
            _solutionSystem.TryAddReagent(solution.Value, addQuantity, out _);

            var remQuantity = new ReagentQuantity(reagent, amount);
            _solutionSystem.RemoveReagent(bufferSolution.Value, remQuantity);
        }

        if (!string.IsNullOrWhiteSpace(component.CustomName))
            _metaData.SetEntityName(injector, component.CustomName);
    }

    private void UpdateAppearance(EntityUid uid, InjectorFabricatorComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        _appearance.SetData(uid, InjectorFabricatorVisuals.IsRunning, component.IsProducing);
    }

    private void UpdateUiState(EntityUid uid, InjectorFabricatorComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var state = GetUserInterfaceState(uid, component);
        _uiSystem.SetUiState(uid, InjectorFabricatorUiKey.Key, state);
    }

    private InjectorFabricatorBoundUserInterfaceState GetUserInterfaceState(EntityUid uid, InjectorFabricatorComponent component)
    {
        NetEntity? beakerNetEntity = null;
        ContainerInfo? beakerContainerInfo = null;

        if (component.BeakerSlot.Item != null)
        {
            beakerNetEntity = GetNetEntity(component.BeakerSlot.Item);
            beakerContainerInfo = BuildBeakerContainerInfo(component.BeakerSlot.Item.Value);
        }

        _solutionSystem.TryGetSolution(uid, InjectorFabricatorComponent.BufferSolutionName, out _, out var buffer);

        bool canProduce = false;
        if (component.Recipe != null && component.Recipe.Sum(r => (long)r.Value) <= 30 && buffer != null)
        {
            canProduce = true;
            foreach (var (reagentId, amount) in component.Recipe)
            {
                var availableAmount = buffer.GetReagentQuantity(reagentId);
                if (availableAmount < amount)
                {
                    canProduce = false;
                    break;
                }
            }
        }

        return new InjectorFabricatorBoundUserInterfaceState(
            component.IsProducing,
            canProduce,
            beakerNetEntity,
            beakerContainerInfo,
            buffer,
            buffer?.Volume ?? FixedPoint2.Zero,
            component.BufferMaxVolume,
            component.Recipe,
            component.CustomName,
            component.InjectorsToProduce,
            component.InjectorsProduced
        );
    }

    private ContainerInfo? BuildBeakerContainerInfo(EntityUid beaker)
    {
        if (!HasComp<SolutionContainerManagerComponent>(beaker)
            || !_solutionSystem.TryGetSolution(beaker, "beaker", out _, out var solution))
            return null;

        return new ContainerInfo(
            Name(beaker),
            solution.Volume,
            solution.MaxVolume)
        {
            Reagents = solution.Contents.ToList()
        };
    }
}
