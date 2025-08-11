using Content.Shared.Verbs;
using Content.Shared.Mind;
using Content.Shared.Silicons.StationAi;
using Content.Shared._Lust.Inowe;
using Robust.Shared.Map;
using Robust.Shared.Containers;
using Content.Shared.Mind.Components;
namespace Content.Server._Lust.Inowe;

public sealed class AiShellSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AiShellVerbComponent, GetVerbsEvent<Verb>>(OnGetCoreVerbs);
        SubscribeLocalEvent<AiShellComponent, GetVerbsEvent<Verb>>(OnGetShellVerbs);
    }

    private void OnGetCoreVerbs(EntityUid uid, AiShellVerbComponent comp, GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString("go-to-shell"),
            Act = () => EnterOrSpawnShell(uid, Transform(uid).Coordinates)
        });
    }

    public void EnterOrSpawnShell(EntityUid coreEntity, EntityCoordinates spawnCoords)
    {
        var mindId = FindAiMind(coreEntity);
        if (mindId == null)
            return;

        var enumerator = EntityManager.EntityQueryEnumerator<AiShellComponent>();
        while (enumerator.MoveNext(out var shellUid, out var comp))
        {
            if (comp.CoreEntity == coreEntity && !comp.IsTaken)
            {
                TryTransferMindToShell(shellUid, mindId.Value);
                return;
            }
        }

        var newShell = DeployShell(coreEntity, spawnCoords);
        TryTransferMindToShell(newShell, mindId.Value);
    }

    public EntityUid DeployShell(EntityUid coreEntity, EntityCoordinates spawnCoords)
    {
        var shell = EntityManager.SpawnEntity("AiShell", spawnCoords);
        var comp = EnsureComp<AiShellComponent>(shell);
        comp.CoreEntity = coreEntity;
        comp.IsTaken = false;
        Dirty(shell, comp);
        return shell;
    }

    public bool TryTransferMindToShell(EntityUid shell, EntityUid mindId)
    {
        if (!TryComp<AiShellComponent>(shell, out var shellComp))
            return false;

        if (shellComp.IsTaken)
            return false;

        _mind.TransferTo(mindId, shell);
        shellComp.IsTaken = true;
        Dirty(shell, shellComp);
        return true;
    }

    private void OnGetShellVerbs(EntityUid uid, AiShellComponent comp, GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString("return-to-core"),
            Act = () => ReturnToCore(uid, comp.CoreEntity)
        });
    }

    private void ReturnToCore(EntityUid shell, EntityUid coreEntity)
    {
        var mindId = FindAiMind(shell);
        if (mindId == null)
            return;

        EntityUid? target = null;

        if (TryComp<StationAiHolderComponent>(coreEntity, out var holder) &&
            holder.Slot?.ContainerSlot?.ContainedEntity is EntityUid contained)
        {
            target = contained;
        }

        _mind.TransferTo(mindId.Value, target ?? coreEntity);

        if (TryComp<AiShellComponent>(shell, out var shellComp))
        {
            shellComp.IsTaken = false;
            Dirty(shell, shellComp);
        }
    }

    public EntityUid? FindAiMind(EntityUid entity)
    {
        var mind = GetMindFromEntityOrContents(entity);
        if (mind != null)
            return mind;

        if (TryComp<StationAiCoreComponent>(entity, out var coreComp) &&
            coreComp.RemoteEntity is { } remote)
        {
            return GetMindFromEntityOrContents(remote);
        }

        return null;
    }

    private EntityUid? GetMindFromEntityOrContents(EntityUid entity)
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
            {
                foreach (var ent in cont.ContainedEntities)
                {
                    if (!HasComp<MindContainerComponent>(ent))
                        continue;

                    var mc = _mind.GetMind(ent);

                    if (mc != null)
                        return mc.Value;
                }
            }
        }
        return null;
    }
}

