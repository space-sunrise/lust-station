using Content.Shared.Electrocution;
using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.Containers;
using Robust.Shared.Timing; // Lust-add

namespace Content.Shared.Trigger.Systems;

public sealed class ShockOnTriggerSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedElectrocutionSystem _electrocution = default!;
    [Dependency] private readonly IGameTiming _timing = default!; // Lust-add

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShockOnTriggerComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(Entity<ShockOnTriggerComponent> ent, ref TriggerEvent args)
    {
        // Lust-start
        if (ent.Comp.PreviousActivation + ent.Comp.Cooldown > _timing.CurTime)
            return; // кулдаун ещё не прошёл
        // Lust-end

        if (args.Key != null && !ent.Comp.KeysIn.Contains(args.Key))
            return;

        EntityUid? target;
        if (ent.Comp.TargetContainer)
        {
            // shock whoever is wearing this clothing item
            if (!_container.TryGetContainingContainer(ent.Owner, out var container))
                return;
            target = container.Owner;
        }
        else
        {
            target = ent.Comp.TargetUser ? args.User : ent.Owner;
        }

        if (target == null)
            return;

        // Lust-edit-start
        if (_electrocution.TryDoElectrocution(target.Value, ent.Owner, ent.Comp.Damage, ent.Comp.Duration, true, ignoreInsulation: true))
        {
            ent.Comp.PreviousActivation = _timing.CurTime;
            Dirty(ent);
            args.Handled = true;
        }
        // Lust-edit-end
    }

}
