using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs;
using Robust.Shared.Random;
using Content.Shared.Popups;
using Robust.Shared.Network;

namespace Content.Shared._LP.Mobs.Events;

public sealed class TryCatchBreathSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly INetManager _net = default!;

    private const float DoAfterTime = 3f;

    public override void Initialize()
    {
        SubscribeLocalEvent<TryCatchBreathAlertEvent>(OnAlertClicked);
        SubscribeLocalEvent<TryCatchBreathDoAfterEvent>(OnDoAfter);
    }

    private void OnAlertClicked(TryCatchBreathAlertEvent ev)
    {
        if (!_net.IsServer)
            return;

        var uid = ev.User;

        Logger.Info($"[CatchBreath] CLICK {uid}");

        if (!TryComp<MobStateComponent>(uid, out var mob))
            return;

        if (mob.CurrentState != MobState.SoftCritical)
            return;

        var args = new DoAfterArgs(
            EntityManager,
            uid,
            DoAfterTime,
            new TryCatchBreathDoAfterEvent(),
            uid)
        {
            Broadcast = true,
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = false,
            RequireCanInteract = false,

            CancelDuplicate = true,
            BlockDuplicate = true,
        };

        _doAfter.TryStartDoAfter(args);

        _popup.PopupEntity(Loc.GetString("catch-breath-try"), uid);

    }

    private void OnDoAfter(TryCatchBreathDoAfterEvent ev)
    {
        if (!_net.IsServer)
            return;

        var uid = ev.User;

        if (ev.Cancelled)
            return;

        if (!TryComp<MobStateComponent>(uid, out var mob))
            return;

        if (mob.CurrentState != MobState.SoftCritical)
            return;

        Logger.Info($"[CatchBreath] DOAFTER {uid}");

        var roll = _random.NextFloat();

        Logger.Info($"[CatchBreath] ROOL {roll}");

        var damage = new DamageSpecifier();

        if (roll < 0.01f)
        {
            damage.DamageDict.Add("Blunt", -1);
            Logger.Info($"[CatchBreath] HEALED BLUNT {uid}");
            _popup.PopupEntity(Loc.GetString("catch-breath-blunt-success"), uid);
        }
        else if (roll < 0.51f)
        {
            damage.DamageDict.Add("Asphyxiation", -3);
            Logger.Info($"[CatchBreath] HEALED {uid}");
            _popup.PopupEntity(Loc.GetString("catch-breath-success"), uid);
        }
        else if (roll < 0.76f)
        {
            damage.DamageDict.Add("Asphyxiation", 4);
            Logger.Info($"[CatchBreath] DAMAGED {uid}");
            _popup.PopupEntity(Loc.GetString("catch-breath-failure"), uid);
        }
        else
        {
            Logger.Info($"[CatchBreath] NOTHING {uid}");
            _popup.PopupEntity(Loc.GetString("catch-breath-nothing"), uid);
        }

        _damage.TryChangeDamage(uid, damage);

        ev.Repeat = false;
    }
}
