using Content.Shared._LP.Mining.Components;
using Content.Shared._LP.Mining;
using Content.Server._Wega.Mining;
using Content.Shared._Wega.Mining;
using Content.Shared._Wega.Mining.Components;
using Content.Shared.Interaction;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Server.Stack;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Random;

namespace Content.Server._LP.Mining;

public sealed class MiningServerCircuitboardSystem : EntitySystem
{
    [Dependency] private readonly SharedToolSystem _toolSystem = default!;
    [Dependency] private readonly MiningServerSystem _miningServerSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StackSystem _stackSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MiningServerCircuitboardComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<MiningServerCircuitboardComponent, WeldFinishedEvent>(OnWeldFinished);
        SubscribeLocalEvent<MiningServerCircuitboardComponent, ScrewdriverFinishedEvent>(OnScrewdriverFinished);
        SubscribeLocalEvent<MiningServerCircuitboardComponent, ExaminedEvent>(OnExamined);
    }

    /// <summary>
    /// Генерирует случайные этапы ремонта платы
    /// </summary>
    public void GenerateRepairSteps(MiningServerCircuitboardRepairComponent repair)
    {
        repair.Steps.Clear();
        repair.CurrentStep = 0;
        repair.IsScanned = false;

        // возможные шаги для ремонта платы
        var possibleSteps = new List<RepairStep>
        {
            new RepairStep(RepairType.Screwdriver, "mining-circuitboard-repair-step-screwdriver"),
            new RepairStep(RepairType.Welder, "mining-circuitboard-repair-step-welder"),
            new RepairStep(RepairType.Cable, "mining-circuitboard-repair-step-cable")
        };

        // перемешивание и выбор этапов (2-3 этапов)
        var stepCount = _random.Next(2, 4);

        for (var i = 0; i < stepCount; i++)
        {
            var index = _random.Next(possibleSteps.Count);
            repair.Steps.Add(possibleSteps[index]);
            possibleSteps.RemoveAt(index);
        }
    }

    /// <summary>
    /// Обновляет визуальное состояние платы на основе ее состояния
    /// </summary>
    private void UpdateAppearance(EntityUid uid, MiningServerCircuitboardComponent board)
    {
        if (TryComp<Robust.Shared.GameObjects.AppearanceComponent>(uid, out var appearance))
        {
            _appearanceSystem.SetData(uid, MiningServerCircuitboardVisuals.IsBroken, board.IsBroken, appearance);
        }
    }

    /// <summary>
    /// Обработчик события при осмотре платы
    /// </summary>
    private void OnExamined(Entity<MiningServerCircuitboardComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("mining-server-circuitboard-examined", ("condition", ent.Comp.Condition.ToString("F0"))));

        if (ent.Comp.IsBroken)
        {
            args.PushMarkup("\n");
            args.PushMarkup(Loc.GetString("mining-server-circuitboard-examined-broken"));
        }
    }

    /// <summary>
    /// Обработчик взаимодействия с платой с помощью инструмента
    /// </summary>
    private void OnInteractUsing(Entity<MiningServerCircuitboardComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (TryComp<ToolComponent>(args.Used, out var toolComp) && _toolSystem.HasQuality(args.Used, "Pulsing"))
        {
            args.Handled = TryScanCircuitboard(ent.Owner, args.User);
            return;
        }
        args.Handled = TryRepairCircuitboard(ent.Owner, args.Used, args.User, ent.Comp);
    }

    /// <summary>
    /// Попытка сканирования платы с помощью мультитула
    /// </summary>
    private bool TryScanCircuitboard(EntityUid uid, EntityUid user)
    {
        if (!TryComp<MiningServerCircuitboardRepairComponent>(uid, out var repair))
        {
            repair = EntityManager.AddComponent<MiningServerCircuitboardRepairComponent>(uid);
        }

        if (!repair.IsScanned)
        {
            GenerateRepairSteps(repair);
            repair.IsScanned = true;
        }

        var message = Loc.GetString("mining-circuitboard-repair-scanned");
        for (var i = 0; i < repair.Steps.Count; i++)
        {
            var step = repair.Steps[i];
            message += $"\n{i + 1}. {Loc.GetString(step.Description)}";
        }
        _popup.PopupEntity(message, uid, user);

        if (TryComp<MiningServerCircuitboardComponent>(uid, out var board))
        {
            var state = new MiningCircuitboardRepairBoundInterfaceState(board.Condition, repair.CurrentStep, repair.Steps, repair.IsScanned);
            _uiSystem.SetUiState(uid, MiningCircuitboardRepairUiKey.Key, state);
        }
        _uiSystem.TryToggleUi(uid, MiningCircuitboardRepairUiKey.Key, user);

        return true;
    }

    /// <summary>
    /// Попытка починки платы
    /// </summary>
    private bool TryRepairCircuitboard(EntityUid uid, EntityUid tool, EntityUid user, MiningServerCircuitboardComponent board)
    {
        if (board.Condition >= MiningServerCircuitboardComponent.MaxCondition)
            return false;

        if (!TryComp<MiningServerCircuitboardRepairComponent>(uid, out var repair) || !repair.IsScanned)
        {
            _popup.PopupEntity(Loc.GetString("mining-circuitboard-repair-not-scanned"), uid, user);
            return false;
        }

        if (!IsToolForCurrentStep(tool, repair))
        {
            _popup.PopupEntity(Loc.GetString("mining-circuitboard-repair-wrong-tool"), uid, user);
            return false;
        }

        // код кабеля такой, потому что он на тайлы пола ставится
        // и по другому это говно не сделать, я пыталась Т_Т
        if (repair.IsCurrentStep(RepairType.Cable))
        {
            if (!TryComp<StackComponent>(tool, out var stack))
            {
                _popup.PopupEntity(Loc.GetString("mining-circuitboard-repair-no-cable"), uid, user);
                return false;
            }

            if (!TryComp(tool, out MetaDataComponent? meta))
            {
                _popup.PopupEntity(Loc.GetString("mining-circuitboard-repair-no-cable"), uid, user);
                return false;
            }

            var prototypeId = meta.EntityPrototype?.ID ?? "";
            if (!prototypeId.Contains("Cable"))
            {
                _popup.PopupEntity(Loc.GetString("mining-circuitboard-repair-no-cable"), uid, user);
                return false;
            }

            if (!_stackSystem.TryUse((tool, stack), 1))
            {
                _popup.PopupEntity(Loc.GetString("mining-circuitboard-repair-no-cable"), uid, user);
                return false;
            }

            HandleRepairStepComplete(uid, board);
            return true;
        }

        if (repair.IsCurrentStep(RepairType.Screwdriver))
        {
            return _toolSystem.UseTool(tool, user, uid, board.ScrewdriverTime, "Screwing", new ScrewdriverFinishedEvent());
        }

        if (repair.IsCurrentStep(RepairType.Welder))
        {
            return _toolSystem.UseTool(tool, user, uid, board.WeldTime, "Welding", new WeldFinishedEvent());
        }

        return false;
    }

    /// <summary>
    /// Проверяет, соответствует ли инструмент текущему шагу
    /// </summary>
    private bool IsToolForCurrentStep(EntityUid tool, MiningServerCircuitboardRepairComponent repair)
    {
        // потому что кабель может быть в стеке
        if (repair.IsCurrentStep(RepairType.Cable))
        {
            if (!TryComp<StackComponent>(tool, out var stack))
                return false;

            if (!TryComp(tool, out MetaDataComponent? meta))
                return false;

            var prototypeId = meta.EntityPrototype?.ID ?? "";
            return prototypeId.Contains("Cable");
        }

        if (!TryComp<ToolComponent>(tool, out var toolComp))
            return false;

        if (repair.IsCurrentStep(RepairType.Screwdriver) && !_toolSystem.HasQuality(tool, "Screwing"))
            return false;

        if (repair.IsCurrentStep(RepairType.Welder) && !_toolSystem.HasQuality(tool, "Welding"))
            return false;

        return true;
    }

    /// <summary>
    /// Обработчик события завершения сварки
    /// </summary>
    private void OnWeldFinished(Entity<MiningServerCircuitboardComponent> ent, ref WeldFinishedEvent args)
    {
        if (args.Cancelled || args.Used == null)
            return;

        if (TryComp<MiningServerCircuitboardRepairComponent>(ent.Owner, out var repair) &&
            repair.IsCurrentStep(RepairType.Welder))
        {
            HandleRepairStepComplete(ent.Owner, ent.Comp);
        }
    }

    /// <summary>
    /// Обработчик события завершения работы с отверткой
    /// </summary>
    private void OnScrewdriverFinished(Entity<MiningServerCircuitboardComponent> ent, ref ScrewdriverFinishedEvent args)
    {
        if (args.Cancelled || args.Used == null)
            return;

        if (TryComp<MiningServerCircuitboardRepairComponent>(ent.Owner, out var repair) &&
            repair.IsCurrentStep(RepairType.Screwdriver))
        {
            HandleRepairStepComplete(ent.Owner, ent.Comp);
        }
    }

    /// <summary>
    /// Обрабатывает завершение шага починки
    /// </summary>
    private void HandleRepairStepComplete(EntityUid uid, MiningServerCircuitboardComponent board)
    {
        if (!TryComp<MiningServerCircuitboardRepairComponent>(uid, out var repair))
            return;

        var isComplete = repair.AdvanceStep();

        if (isComplete)
        {
            _popup.PopupEntity(Loc.GetString("mining-circuitboard-repair-complete"), uid, uid);
        }
        else
        {
            var currentStep = repair.Steps[repair.CurrentStep];
            _popup.PopupEntity(Loc.GetString("mining-circuitboard-repair-step-done", ("step", Loc.GetString(currentStep.Description))), uid, uid);
        }

        if (isComplete)
        {
            board.Condition = MiningServerCircuitboardComponent.MaxCondition;

            // Сброс состояния ремонта для возможности повторного ремонта
            repair.CurrentStep = 0;
            repair.IsScanned = false;

            UpdateAppearance(uid, board);

            var query = EntityQueryEnumerator<MiningServerComponent>();
            while (query.MoveNext(out var serverUid, out var server))
            {
                if (server.CircuitboardUid == uid)
                {
                    _miningServerSystem.UpdateBrokenState(serverUid, server);
                }
            }
        }

        var state = new MiningCircuitboardRepairBoundInterfaceState(board.Condition, repair.CurrentStep, repair.Steps, repair.IsScanned);
        _uiSystem.SetUiState(uid, MiningCircuitboardRepairUiKey.Key, state);
    }
}
