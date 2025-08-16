using Content.Shared.Verbs;
using Content.Shared.Mind;
using Content.Shared.Silicons.StationAi;
using Content.Shared._Lust.Inowe;
using Robust.Shared.Map;
using Robust.Shared.Containers;
using Content.Shared.Mind.Components;
using Content.Server.Actions;
using Content.Shared.Actions.Components;
namespace Content.Server._Lust.Inowe;

public sealed class AiShellSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    private bool _transferInProgress;

    public override void Initialize()
    {
        SubscribeLocalEvent<AiShellVerbComponent, GetVerbsEvent<Verb>>(AddCoreVerb);
        SubscribeLocalEvent<AiShellComponent, GetVerbsEvent<Verb>>(AddShellVerb);
        SubscribeLocalEvent<AiCoreActionsComponent, GoToShellActionEvent>(HandleGoToShell);
        SubscribeLocalEvent<AiShellComponent, ReturnToCoreActionEvent>(HandleReturnToCore);
    }

    private void AddCoreVerb(EntityUid uid, AiShellVerbComponent comp, GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var core = GetCoreEntity(uid);
        var spawn = Transform(core).Coordinates;

        if (TryComp<AiCoreActionsComponent>(core, out var coreAi) && coreAi.GoToShellActionId != null)
            return;

        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString("activate-body"),
            Act = () => EnterOrCreateShell(core, spawn)
        });
    }

    private void AddShellVerb(EntityUid uid, AiShellComponent comp, GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString("return-to-core"),
            Act = () => MoveMindToCore(uid, comp.CoreEntity)
        });
    }

    private void HandleGoToShell(EntityUid uid, AiCoreActionsComponent comp, GoToShellActionEvent args)
    {
        if (args.Handled)
            return;

        var core = GetCoreEntity(uid);
        EnterOrCreateShell(core, Transform(core).Coordinates);
        args.Handled = true;
    }

    private void HandleReturnToCore(EntityUid uid, AiShellComponent comp, ReturnToCoreActionEvent args)
    {
        if (args.Handled)
            return;

        MoveMindToCore(uid, comp.CoreEntity);
        args.Handled = true;
    }

    public void EnterOrCreateShell(EntityUid coreEntity, EntityCoordinates spawnCoords)
    {
        if (_transferInProgress)
            return;

        try
        {
            _transferInProgress = true;
            var mindId = GetMind(coreEntity);
            if (mindId == null)
                return;

            var shells = EntityManager.EntityQueryEnumerator<AiShellComponent>();
            while (shells.MoveNext(out var shellUid, out var comp))
            {
                if (comp.CoreEntity == coreEntity && !comp.IsTaken)
                {
                    TransferMind(shellUid, mindId.Value);
                    return;
                }
            }

            var newShell = SpawnShell(coreEntity, spawnCoords);
            TransferMind(newShell, mindId.Value);
        }
        finally
        {
            _transferInProgress = false;
        }
    }

    public EntityUid SpawnShell(EntityUid coreEntity, EntityCoordinates spawnCoords)
    {
        var shell = EntityManager.SpawnEntity("AiShell", spawnCoords);
        var comp = EnsureComp<AiShellComponent>(shell);
        comp.CoreEntity = coreEntity;
        comp.IsTaken = false;
        Dirty(shell, comp);
        return shell;
    }

    public bool TransferMind(EntityUid shell, EntityUid mindId)
    {
        if (!TryComp<AiShellComponent>(shell, out var shellComp) || shellComp.IsTaken)
            return false;

        var core = shellComp.CoreEntity;
        if (TryComp<AiCoreActionsComponent>(core, out var coreActions) && coreActions.GoToShellActionId is { } goId &&
            TryComp<ActionsComponent>(core, out var coreActComp))
        {
            _actions.RemoveAction((core, coreActComp), goId);
            coreActions.GoToShellActionId = null;
        }

        _mind.TransferTo(mindId, shell);
        shellComp.IsTaken = true;
        Dirty(shell, shellComp);

        var returnAction = _actions.AddAction(shell, "ReturnToCoreAction");
        if (returnAction != null)
            shellComp.ReturnToCoreActionId = returnAction.Value;

        return true;
    }

    private void MoveMindToCore(EntityUid shell, EntityUid coreEntity)
    {
        var mindId = GetMind(shell);
        if (mindId == null)
            return;

        var actualCore = GetCoreEntity(coreEntity);
        _mind.TransferTo(mindId.Value, actualCore);

        if (TryComp<AiShellComponent>(shell, out var sc))
        {
            sc.IsTaken = false;
            Dirty(shell, sc);
        }

        if (TryComp<AiShellComponent>(shell, out var shellComp) && shellComp.ReturnToCoreActionId is { } returnId &&
            TryComp<ActionsComponent>(shell, out var shellActionsComp))
        {
            _actions.RemoveAction((shell, shellActionsComp), returnId);
            shellComp.ReturnToCoreActionId = null;
        }

        if (!TryComp<AiCoreActionsComponent>(actualCore, out var coreActions))
            coreActions = EnsureComp<AiCoreActionsComponent>(actualCore);

        if (coreActions.GoToShellActionId == null && TryComp<ActionsComponent>(actualCore, out var coreActComp))
        {
            var goAction = _actions.AddAction(actualCore, "GoToShellAction");
            if (goAction != null)
                coreActions.GoToShellActionId = goAction.Value;
        }
    }

    public EntityUid? GetMind(EntityUid entity)
    {
        var mind = GetMindFromEntity(entity);
        if (mind != null)
            return mind;

        if (TryComp<StationAiCoreComponent>(entity, out var coreComp) && coreComp.RemoteEntity is { } remote)
            return GetMindFromEntity(remote);

        return null;
    }

    private EntityUid? GetMindFromEntity(EntityUid entity)
    {
        if (HasComp<MindContainerComponent>(entity))
        {
            var m = _mind.GetMind(entity);
            if (m != null)
                return m.Value;
        }

        if (TryComp<ContainerManagerComponent>(entity, out var contMan))
        {
            foreach (var cont in contMan.Containers.Values)
                foreach (var ent in cont.ContainedEntities)
                    if (HasComp<MindContainerComponent>(ent))
                    {
                        var mc = _mind.GetMind(ent);
                        if (mc != null)
                            return mc.Value;
                    }
        }
        return null;
    }

    private EntityUid GetCoreEntity(EntityUid coreEntity)
    {
        if (TryComp<StationAiHolderComponent>(coreEntity, out var holder) &&
            holder.Slot?.ContainerSlot?.ContainedEntity is EntityUid contained)
            return contained;

        return coreEntity;
    }
}
