using Content.Shared.DoAfter;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Robust.Shared.Containers;

namespace Content.Shared._Sunrise.Felinid;

public abstract class SharedFelinidSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FelinidComponent, GettingPickedUpAttemptEvent>(OnGettingPickupAttempt);
        SubscribeLocalEvent<FelinidComponent, PickupAttemptEvent>(OnPickupAttempt);
        SubscribeLocalEvent<FelinidComponent, BeingEquippedAttemptEvent>(OnBeingEquippedAttempt);
        SubscribeLocalEvent<FelinidComponent, ContainerIsInsertingAttemptEvent>(OnHandEquippedAttempt);
        SubscribeLocalEvent<FelinidComponent, UseAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<FelinidComponent, ThrowAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<FelinidComponent, InteractionAttemptEvent>(OnInteractAttempt);
        SubscribeLocalEvent<FelinidComponent, PullAttemptEvent>(OnPullAttempt);
        SubscribeLocalEvent<FelinidComponent, AttackAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<FelinidComponent, FelinidPickupDoAfterEvent>(OnDoAfter);
    }

    private void OnInteractAttempt(Entity<FelinidComponent> ent, ref InteractionAttemptEvent args)
    {
        if (ent.Comp.InContainer && !HasComp<FelinidContainerComponent>(args.Target))
            args.Cancelled = true;
    }

    private void OnAttempt(EntityUid uid, FelinidComponent component, CancellableEntityEventArgs args)
    {
        if (component.InContainer)
            args.Cancel();
    }

    private void OnPullAttempt(EntityUid uid, FelinidComponent component, PullAttemptEvent args)
    {
        if (component.InContainer)
            args.Cancelled = true;
    }

    private void OnHandEquippedAttempt(EntityUid uid, FelinidComponent component, ContainerIsInsertingAttemptEvent args)
    {
        if (!HasComp<FelinidComponent>(args.EntityUid))
            return;

        args.Cancel();
    }

    private void OnBeingEquippedAttempt(Entity<FelinidComponent> ent, ref BeingEquippedAttemptEvent args)
    {
        if (!HasComp<FelinidComponent>(args.EquipTarget))
            return;

        args.Cancel();
    }

    private void OnPickupAttempt(EntityUid uid, FelinidComponent component, PickupAttemptEvent args)
    {
        if (HasComp<FelinidComponent>(args.Item) || component.InContainer)
            args.Cancel();
    }
    private void OnGettingPickupAttempt(EntityUid uid, FelinidComponent component, ref GettingPickedUpAttemptEvent args)
    {
        args.Cancel();
        StartFelinidPickupDoAfter(args.User, args.Item);
    }
    private void StartFelinidPickupDoAfter(EntityUid user, EntityUid item)
    {
        var ev = new FelinidPickupDoAfterEvent();
        var args = new DoAfterArgs(EntityManager, user, 3f, ev, item, target: item)
        {
            BreakOnMove = true,
            NeedHand = true,
            MovementThreshold = 0.5f
        };

        _doAfterSystem.TryStartDoAfter(args);
    }
    private void OnDoAfter(Entity<FelinidComponent> ent, ref FelinidPickupDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (args.Args.Target == null)
            return;

        if (!TryComp<HandsComponent>(args.Args.User, out var hands))
            return;

        if (!_hands.TryGetEmptyHand(args.Args.User, out var emptyHand, hands))
            return;

        if (TryComp<MultiHandedItemComponent>(args.Args.Target.Value, out var multiHanded)
            && hands.CountFreeHands() < multiHanded!.HandsNeeded)
        {
            _popup.PopupPredictedCursor(Loc.GetString("multi-handed-item-pick-up-fail",
                ("number", multiHanded.HandsNeeded - 1), ("item", ent.Owner)), args.Args.User);
            return;
        }

        _hands.TryPickup(args.Args.User, args.Args.Target.Value, emptyHand, checkActionBlocker: false, handsComp: hands);
        args.Handled = true;
    }
}
