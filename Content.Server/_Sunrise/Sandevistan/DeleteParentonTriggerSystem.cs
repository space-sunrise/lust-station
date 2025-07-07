using Content.Server.Explosion.EntitySystems;
using Content.Shared._Sunrise.Sandevistan;

namespace Content.Server._Sunrise.Sandevistan;

public sealed class DeleteParentOnTriggerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DeleteParentOnTriggerComponent, TriggerEvent>(HandleDeleteParentTrigger);
    }

    private void HandleDeleteParentTrigger(Entity<DeleteParentOnTriggerComponent> uid, ref TriggerEvent args)
    {EntityManager.QueueDeleteEntity(Transform(uid).ParentUid);
        args.Handled = true;}

}