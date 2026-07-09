using Content.Shared._Lust.Borgs.Components;
using Content.Shared.Actions;
using Content.Shared.Rotation;
using Content.Shared.Stunnable;

namespace Content.Shared._Lust.Borgs;

public sealed class SharedBorgRestActionSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BorgRestActionComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<BorgRestActionComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<BorgRestActionComponent, BorgToggleRestActionEvent>(OnToggleRest);
        SubscribeLocalEvent<BorgRestingComponent, ComponentStartup>(OnRestingStartup);
        SubscribeLocalEvent<BorgRestingComponent, ComponentRemove>(OnRestingRemove);
        SubscribeLocalEvent<KnockedDownComponent, ComponentRemove>(OnKnockedDownRemove);
    }

    private void OnStartup(Entity<BorgRestActionComponent> ent, ref ComponentStartup args)
    {
        _actions.AddAction(ent, ref ent.Comp.ToggleActionEntity, ent.Comp.ToggleAction);
        UpdateActionState(ent);
    }

    private void OnShutdown(Entity<BorgRestActionComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.ToggleActionEntity);
    }

    private void OnToggleRest(Entity<BorgRestActionComponent> ent, ref BorgToggleRestActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (HasComp<BorgRestingComponent>(ent))
        {
            RemComp<BorgRestingComponent>(ent);
            RemComp<KnockedDownComponent>(ent);
        }
        else
        {
            EnsureComp<BorgRestingComponent>(ent);

            if (_stun.TryKnockdown(ent.Owner, null, refresh: true, autoStand: false, drop: false, force: true))
                SetRestingVisualRotation(ent);
            else
                RemComp<BorgRestingComponent>(ent);
        }

        UpdateActionState(ent);
    }

    private void OnRestingStartup(Entity<BorgRestingComponent> ent, ref ComponentStartup args)
    {
        if (TryComp<BorgRestActionComponent>(ent, out var borgRest))
        {
            SetRestingVisualRotation(ent.Owner);
            UpdateActionState((ent.Owner, borgRest));
        }
    }

    private void OnRestingRemove(Entity<BorgRestingComponent> ent, ref ComponentRemove args)
    {
        if (TryComp<BorgRestActionComponent>(ent, out var borgRest))
            UpdateActionState((ent.Owner, borgRest), resting: false);
    }

    private void OnKnockedDownRemove(Entity<KnockedDownComponent> ent, ref ComponentRemove args)
    {
        RemCompDeferred<BorgRestingComponent>(ent);
    }

    private void UpdateActionState(Entity<BorgRestActionComponent> ent)
    {
        UpdateActionState(ent, HasComp<BorgRestingComponent>(ent));
    }

    private void UpdateActionState(Entity<BorgRestActionComponent> ent, bool resting)
    {
        _actions.SetToggled(ent.Comp.ToggleActionEntity, resting);
    }

    private void SetRestingVisualRotation(EntityUid uid)
    {
        _appearance.SetData(uid, RotationVisuals.RotationState, RotationState.Vertical);
    }
}
