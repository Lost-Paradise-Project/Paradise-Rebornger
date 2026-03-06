using System.Linq;
using System.Diagnostics.CodeAnalysis;
using Content.Server.Power.Components;
using Content.Shared._Wega.Mining.Components;
using Content.Shared.Power;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Audio;
using Content.Shared.Examine;
// LP edit start
using Content.Shared._LP.Mining.Components;
using Robust.Shared.GameObjects;
using Content.Shared.Construction.Components;
using Content.Server.Construction;
using Content.Server.Construction.Components;
using Robust.Shared.Containers;
using Robust.Shared.Timing;
// LP edit end

namespace Content.Server._Wega.Mining;

public sealed class MiningServerSystem : EntitySystem
{
    [Dependency] private readonly SharedAmbientSoundSystem _ambient = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MiningServerComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<MiningServerComponent, MapInitEvent>(OnMapInit, after: [typeof(ConstructionSystem)]); // LP edit
        SubscribeLocalEvent<MiningServerComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<MiningServerComponent, ExaminedEvent>(OnExamined);
        // LP edit start
        SubscribeLocalEvent<MiningServerComponent, EntGotInsertedIntoContainerMessage>(OnBoardInserted);
        SubscribeLocalEvent<MiningServerComponent, EntGotRemovedFromContainerMessage>(OnBoardRemoved);
        // LP edit end
    }

    // LP edit start

    /// <summary>
    /// Обработчик события при вставке платы в контейнер машины
    /// </summary>
    private void OnBoardInserted(Entity<MiningServerComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        if (TryComp<MachineComponent>(ent.Owner, out var machine) && args.Container.ID == MachineFrameComponent.BoardContainerName)
        {
            ent.Comp.CircuitboardUid = args.Entity;
            UpdateBrokenState(ent.Owner, ent.Comp);
        }
    }

    /// <summary>
    /// Обработчик события при извлечении платы из контейнера машины
    /// </summary>
    private void OnBoardRemoved(Entity<MiningServerComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        if (TryComp<MachineComponent>(ent.Owner, out var machine) && args.Container.ID == MachineFrameComponent.BoardContainerName)
        {
            ent.Comp.CircuitboardUid = null;
            ent.Comp.IsBroken = true; // Машина не работает без платы потому что так завещала я
            UpdateAppearance(ent.Owner, ent.Comp);
        }
    }

    /// <summary>
    /// Обновляет состояние IsBroken в зависимости от состояния платы
    /// </summary>
    public void UpdateBrokenState(EntityUid uid, MiningServerComponent? server = null)
    {
        if (!Resolve(uid, ref server))
            return;

        if (server.CircuitboardUid.HasValue && TryComp<MiningServerCircuitboardComponent>(server.CircuitboardUid.Value, out var board))
        {
            server.IsBroken = board.IsBroken;
        }
        else
        {
            server.IsBroken = true;
        }

        UpdateAppearance(uid, server);
    }
    // LP edit end


    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<MiningServerComponent, PowerConsumerComponent>();

        while (query.MoveNext(out var uid, out var server, out var consumer))
        {
            if (server.IsBroken || consumer.ReceivedPower < server.ActualPowerConsumption)
            {
                if (server.IsBroken)
                {
                    if (consumer.DrawRate != 0f)
                        consumer.DrawRate = 0f;

                    if (server.IsActive)
                    {
                        server.IsActive = false;
                        UpdateAppearance(uid, server);
                        _ambient.SetAmbience(uid, false);
                    }
                }

                server.CurrentTemperature = Math.Max(server.CurrentTemperature - 0.145f * frameTime, 293f);
                continue;
            }

            float ambientTemperature = 293f;
            if (_atmosphereSystem.GetContainingMixture(uid, excite: true) is { } atmosphere)
                ambientTemperature = atmosphere.Temperature;

            var baseHeatGeneration = server.ActualHeatGeneration * frameTime;

            float ambientHeatMultiplier = 1f;
            if (ambientTemperature > server.BreakdownTemperature * 0.7f)
            {
                ambientHeatMultiplier = 1f + (ambientTemperature - server.BreakdownTemperature * 0.7f) / (server.BreakdownTemperature * 0.3f);
            }

            var heatGeneration = baseHeatGeneration * ambientHeatMultiplier;
            if (consumer.DrawRate != server.ActualPowerConsumption)
                consumer.DrawRate = server.ActualPowerConsumption;

            if (server.IsActive)
            {
                server.CurrentTemperature += heatGeneration;
                HeatSurroundingAtmosphere(uid, heatGeneration);

                if (TryGetAccount(out var account))
                {
                    var efficiency = GetEfficiency(server.Mode, server.MiningStage);
                    if (server.Mode == MiningMode.Credits)
                    {
                        account.Credits += efficiency * frameTime;
                    }
                    else
                    {
                        account.ResearchPoints += efficiency * frameTime;
                    }
                }
            }
            else
            {
                server.CurrentTemperature += heatGeneration * 0.2f;
            }

            server.CurrentTemperature -= 0.145f * frameTime;
            server.CurrentTemperature = Math.Max(server.CurrentTemperature, 293f);

            if (server.CurrentTemperature >= server.BreakdownTemperature && !server.IsBroken)
            {
                // LP edit start
                if (server.CircuitboardUid.HasValue && TryComp<MiningServerCircuitboardComponent>(server.CircuitboardUid.Value, out var board))
                {
                    board.Condition = MiningServerCircuitboardComponent.MinCondition;

                    // Обновляем визуальное состояние платы (показываем анимацию сломанной платы)
                    if (TryComp<Robust.Shared.GameObjects.AppearanceComponent>(server.CircuitboardUid.Value, out var boardAppearance))
                    {
                        _appearance.SetData(server.CircuitboardUid.Value, MiningServerCircuitboardVisuals.IsBroken, true, boardAppearance);
                    }
                }
                // LP edit end
                server.IsActive = false;

                if (consumer.DrawRate != 0f)
                    consumer.DrawRate = 0f;
                UpdateBrokenState(uid, server); // LP edit
                _ambient.SetAmbience(uid, false);
            }
        }
    }

    private void OnInit(Entity<MiningServerComponent> ent, ref ComponentInit args)
    {
        var account = EntityQuery<MiningAccountComponent>().FirstOrDefault();
        if (account == default)
            return;

        ent.Comp.Mode = account.GlobalMode;
    }

    private void OnMapInit(Entity<MiningServerComponent> ent, ref MapInitEvent args)
    {
        // LP edit start
        ent.Comp.MiningStage = 1;

        var uid = ent.Owner;

        // Синхронная инициализация - теперь выполняется сразу после ConstructionSystem
        // благодаря параметру after: [typeof(ConstructionSystem)] в подписке
        if (TryComp<MachineComponent>(uid, out var machine))
        {
            if (machine.BoardContainer.ContainedEntities.Count > 0)
            {
                ent.Comp.CircuitboardUid = machine.BoardContainer.ContainedEntities.First();
                UpdateBrokenState(uid, ent.Comp);
            }
            else
            {
                ent.Comp.IsBroken = true;
                UpdateAppearance(uid, ent.Comp);
            }
        }
        else
        {
            ent.Comp.IsBroken = true;
            UpdateAppearance(uid, ent.Comp);
        }
        // LP edit end
    }

    private void OnPowerChanged(Entity<MiningServerComponent> ent, ref PowerChangedEvent args)
    {
        if (args.Powered == false)
        {
            ent.Comp.IsActive = args.Powered;
            UpdateAppearance(ent.Owner, ent.Comp);
            _ambient.SetAmbience(ent, args.Powered);
        }
    }

    private void OnExamined(Entity<MiningServerComponent> entity, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange || !TryComp<PowerConsumerComponent>(entity, out var consumer))
            return;

        float chargePercent = 0f;
        if (entity.Comp.ActualPowerConsumption > 0f)
        {
            chargePercent = (consumer.ReceivedPower / entity.Comp.ActualPowerConsumption) * 100f;
            chargePercent = Math.Clamp(chargePercent, 0f, 100f);
        }

        args.PushMarkup(Loc.GetString("mining-server-examined", ("percent", chargePercent.ToString("F0"))));
    }

    private bool TryGetAccount([NotNullWhen(true)] out MiningAccountComponent? account)
    {
        account = null;
        var station = EntityQuery<MiningAccountComponent>().FirstOrDefault();
        if (station == default)
            return false;

        account = station;
        return true;
    }

    private float GetEfficiency(MiningMode mode, int stage)
    {
        return mode switch
        {
            MiningMode.Credits => stage * 0.35f, // ~500к за 2 часа для 50 серверов
            MiningMode.Research => stage * 0.17f, // ~250к за 2 часа
            _ => 0f
        };
    }

    private void HeatSurroundingAtmosphere(EntityUid uid, float heatEnergy)
    {
        if (_atmosphereSystem.GetContainingMixture(uid, excite: true) is { } atmosphere)
            _atmosphereSystem.AddHeat(atmosphere, heatEnergy * 500f);
    }

    private void UpdateAppearance(EntityUid uid, MiningServerComponent? server = null)
    {
        if (!Resolve(uid, ref server))
            return;

        if (TryComp<AppearanceComponent>(uid, out var appearance))
        {
            _appearance.SetData(uid, MiningServerVisuals.MiningStage, server.MiningStage, appearance);
            _appearance.SetData(uid, MiningServerVisuals.IsActive, server.IsActive, appearance);
        }
    }
}
