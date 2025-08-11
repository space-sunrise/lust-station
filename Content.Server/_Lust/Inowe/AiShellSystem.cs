using Content.Shared._Lust.Inowe;
using Content.Shared.Mind;
using Robust.Shared.Map;
namespace Content.Server._Lust.Inowe;

public sealed class AiShellSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    public EntityUid DeployShell(EntityUid coreEntity, EntityCoordinates spawnCoords)
    {
        var shell = Spawn("AiShell", spawnCoords);
        var comp = EnsureComp<AiShellComponent>(shell);
        comp.CoreEntity = GetNetEntity(coreEntity);
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
}
