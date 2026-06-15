using Content.Shared._Lust.Borgs;
using Content.Shared.ActionBlocker;
using Content.Shared.DoAfter;
using Content.Shared.Mobs;
using Content.Shared.Movement.Events;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Standing;
using Content.Server.DoAfter;

namespace Content.Server._Lust.Borgs;

/// <summary>
/// Handles the borg resting mechanic: toggles lying-down state and
/// automatically stops resting when the borg dies.
/// The rest action is granted per borg type via ActionGrant in BorgTypePrototype.
/// </summary>
public sealed class BorgRestSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BorgRestComponent, BorgRestActionEvent>(OnRestAction);
        SubscribeLocalEvent<BorgRestComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<BorgRestComponent, UpdateCanMoveEvent>(OnUpdateCanMove);
        SubscribeLocalEvent<BorgRestComponent, BorgRestDoAfterEvent>(OnRestDoAfter);
        // Lust edit - borgs are machines, they don't topple over when destroyed
        SubscribeLocalEvent<BorgChassisComponent, DownAttemptEvent>(OnBorgDownAttempt);
    }

    private static void OnBorgDownAttempt(Entity<BorgChassisComponent> ent, ref DownAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnRestAction(Entity<BorgRestComponent> ent, ref BorgRestActionEvent args)
    {
        if (args.Handled)
            return;

        ToggleResting(ent);
        args.Handled = true;
    }

    private void OnMobStateChanged(Entity<BorgRestComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Alive && ent.Comp.IsResting)
            StopResting(ent);
    }

    private void OnUpdateCanMove(Entity<BorgRestComponent> ent, ref UpdateCanMoveEvent args)
    {
        if (ent.Comp.IsResting)
            args.Cancel();
    }

    private void OnRestDoAfter(Entity<BorgRestComponent> ent, ref BorgRestDoAfterEvent args)
    {
        if (args.Cancelled || ent.Comp.IsResting)
            return;

        ent.Comp.IsResting = true;
        Dirty(ent);
        _actionBlocker.UpdateCanMove(ent);
    }

    private void ToggleResting(Entity<BorgRestComponent> ent)
    {
        if (ent.Comp.IsResting)
            StopResting(ent);
        else
            StartResting(ent);
    }

    private void StartResting(Entity<BorgRestComponent> ent)
    {
        var doAfterArgs = new DoAfterArgs(EntityManager, ent.Owner, TimeSpan.FromSeconds(1), new BorgRestDoAfterEvent(), eventTarget: ent.Owner)
        {
            BreakOnMove = true,
            BreakOnDamage = false,
        };
        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void StopResting(Entity<BorgRestComponent> ent)
    {
        ent.Comp.IsResting = false;
        Dirty(ent);
        _actionBlocker.UpdateCanMove(ent);
    }
}
