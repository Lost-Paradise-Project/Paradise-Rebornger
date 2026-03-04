using Content.Shared._Wega.Mining.Components;
using Content.Shared._Wega.Mining;
using Content.Shared.Interaction;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Server.Stack;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Random;

namespace Content.Server._Wega.Mining;

public sealed class MiningServerCircuitboardSystem : EntitySystem
{
    [Dependency] private readonly SharedToolSystem _toolSystem = default!;
    [Dependency] private readonly MiningServerSystem _miningServerSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StackSystem _stackSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;

    // Время для разных инструментов
    private const float ScrewdriverTime = 2f;
    private const float WelderTime = 5f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MiningServerCircuitboardComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<MiningServerCircuitboardComponent, WeldFinishedEvent>(OnWeldFinished);
        SubscribeLocalEvent<MiningServerCircuitboardComponent, ScrewdriverFinishedEvent>(OnScrewdriverFinished);
        SubscribeLocalEvent<MiningServerCircuitboardComponent, CableFinishedEvent>(OnCableFinished);
        SubscribeLocalEvent<MiningServerCircuitboardComponent, ExaminedEvent>(OnExamined);
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

        // Проверяем, если это мультитул, то сканируем плату
        if (TryComp<ToolComponent>(args.Used, out var toolComp) && _toolSystem.HasQuality(args.Used, "Pulsing"))
        {
            args.Handled = TryScanCircuitboard(ent.Owner, args.User);
            return;
        }

        // Проверяем инструменты для починки
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

        // Generate repair steps if not already generated
        if (!repair.IsScanned)
        {
            repair.GenerateRepairSteps();
            repair.IsScanned = true;
        }

        // Send popup message to user
        var message = Loc.GetString("mining-circuitboard-repair-scanned");
        for (var i = 0; i < repair.Steps.Count; i++)
        {
            var step = repair.Steps[i];
            message += $"\n{i + 1}. {Loc.GetString(step.Description)}";
        }

        _popup.PopupEntity(message, uid, user);

        // Send state update to client and open UI
        if (TryComp<MiningServerCircuitboardComponent>(uid, out var board))
        {
            var state = new MiningCircuitboardRepairBoundInterfaceState(board.Condition, repair.CurrentStep, repair.Steps, repair.IsScanned);
            _uiSystem.SetUiState(uid, MiningCircuitboardRepairUiKey.Key, state);
        }

        // Open the repair UI for the user
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

        // Проверяем, если плата не сканирована, то не позволяем починять
        if (!TryComp<MiningServerCircuitboardRepairComponent>(uid, out var repair) || !repair.IsScanned)
        {
            _popup.PopupEntity(Loc.GetString("mining-circuitboard-repair-not-scanned"), uid, user);
            return false;
        }

        // Проверяем, что инструмент соответствует текущему шагу
        if (!IsToolForCurrentStep(tool, repair))
        {
            _popup.PopupEntity(Loc.GetString("mining-circuitboard-repair-wrong-tool"), uid, user);
            return false;
        }

        // Для шага с кабелем - просто потребляем кабель и завершаем шаг сразу
        if (repair.IsCurrentStep(RepairType.Cable))
        {
            // Проверяем, что в руке есть кабель (стек)
            if (!TryComp<StackComponent>(tool, out var stack))
            {
                _popup.PopupEntity(Loc.GetString("mining-circuitboard-repair-no-cable"), uid, user);
                return false;
            }

            // Проверяем, что это именно кабель (не другой стек)
            // Используем прототип для проверки - кабели имеют ID: Cable, CableMV, CableHV
            if (!TryComp(tool, out MetaDataComponent? meta) || meta == null)
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

            // Потребляем один кабель из стека
            if (!_stackSystem.TryUse((tool, stack), 1))
            {
                _popup.PopupEntity(Loc.GetString("mining-circuitboard-repair-no-cable"), uid, user);
                return false;
            }

            // Сразу завершаем шаг починки (без анимации и задержки)
            HandleRepairStepComplete(uid, board);
            return true;
        }

        // Для отвертки и сварочного аппарата используем стандартный ToolSystem с DoAfter
        if (repair.IsCurrentStep(RepairType.Screwdriver))
        {
            return _toolSystem.UseTool(tool, user, uid, ScrewdriverTime, "Screwing", new ScrewdriverFinishedEvent());
        }

        if (repair.IsCurrentStep(RepairType.Welder))
        {
            return _toolSystem.UseTool(tool, user, uid, WelderTime, "Welding", new WeldFinishedEvent());
        }

        return false;
    }

    /// <summary>
    /// Проверяет, соответствует ли инструмент текущему шагу
    /// </summary>
    private bool IsToolForCurrentStep(EntityUid tool, MiningServerCircuitboardRepairComponent repair)
    {
        // Для кабеля проверяем, что это стек с кабелем
        if (repair.IsCurrentStep(RepairType.Cable))
        {
            if (!TryComp<StackComponent>(tool, out var stack))
                return false;

            if (!TryComp(tool, out MetaDataComponent? meta) || meta == null)
                return false;

            var prototypeId = meta.EntityPrototype?.ID ?? "";
            return prototypeId.Contains("Cable");
        }

        // Для остальных инструментов проверяем наличие ToolComponent и качества
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

        HandleRepairStepComplete(ent.Owner, ent.Comp);
    }

    /// <summary>
    /// Обработчик события завершения работы с отверткой
    /// </summary>
    private void OnScrewdriverFinished(Entity<MiningServerCircuitboardComponent> ent, ref ScrewdriverFinishedEvent args)
    {
        if (args.Cancelled || args.Used == null)
            return;

        HandleRepairStepComplete(ent.Owner, ent.Comp);
    }

    /// <summary>
    /// Обработчик события завершения работы с кабелем (не используется, но нужен для подписки)
    /// </summary>
    private void OnCableFinished(Entity<MiningServerCircuitboardComponent> ent, ref CableFinishedEvent args)
    {
        if (args.Cancelled || args.Used == null)
            return;

        HandleRepairStepComplete(ent.Owner, ent.Comp);
    }

    /// <summary>
    /// Обрабатывает завершение шага починки
    /// </summary>
    private void HandleRepairStepComplete(EntityUid uid, MiningServerCircuitboardComponent board)
    {
        if (!TryComp<MiningServerCircuitboardRepairComponent>(uid, out var repair))
            return;

        // Advance to next repair step
        var isComplete = repair.AdvanceStep();

        // Send popup message
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
            // Восстанавливаем состояние платы
            board.Condition = MiningServerCircuitboardComponent.MaxCondition;

            // Обновляем визуальное состояние платы (убираем анимацию сломанной платы)
            UpdateAppearance(uid, board);

            // Обновляем состояние всех майнинг серверов, которые используют эту плату
            var query = EntityQueryEnumerator<MiningServerComponent>();
            while (query.MoveNext(out var serverUid, out var server))
            {
                if (server.CircuitboardUid == uid)
                {
                    _miningServerSystem.UpdateBrokenState(serverUid, server);
                }
            }
        }

        // Update UI state
        var state = new MiningCircuitboardRepairBoundInterfaceState(board.Condition, repair.CurrentStep, repair.Steps, repair.IsScanned);
        _uiSystem.SetUiState(uid, MiningCircuitboardRepairUiKey.Key, state);
    }
}

