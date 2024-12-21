using Content.Server.Speech.Components;
using Content.Server._Lust.Toys.Components;
using Content.Shared.Inventory.Events;
using Content.Shared._Lust.Toys.Systems;
using Content.Shared._Lust.Toys.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Jittering;

namespace Content.Server._Lust.Toys.Systems;

public sealed class VibratingToySystem : SharedToySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    protected override void OnGotEquipped(EntityUid uid, VibratingToyComponent component, GotEquippedEvent args)
    {
        base.OnGotEquipped(uid, component, args);
        if (component.IsEquipped == true && component.Enabled == true)
        {
            EnsureComp<VibratingComponent>(args.Equipee).Toy = uid;
            EnsureComp<StutteringAccentComponent>(args.Equipee);

            if (!TryComp<MovementSpeedModifierComponent>(args.Equipee, out var modifierComponent))
                return;

            if (component.BaseWalkSpeed != null || component.BaseSprintSpeed != null || component.BaseAcceleration != null)
                return;

            component.BaseSprintSpeed = modifierComponent.BaseSprintSpeed;
            component.BaseWalkSpeed = modifierComponent.BaseWalkSpeed;
            component.BaseAcceleration = modifierComponent.Acceleration;

            _movementSpeedModifier.ChangeBaseSpeed(
                args.Equipee,
                component.TargetWalkSpeed,
                component.TargetSprintSpeed,
                component.TargetAcceleration);
            Dirty(uid, component);
        }
    }

    protected override void OnGotUnequipped(EntityUid uid, VibratingToyComponent component, GotUnequippedEvent args)
    {
        base.OnGotUnequipped(uid, component, args);
        if (component.IsEquipped == false)
        {
            RemComp<VibratingComponent>(args.Equipee);
            RemComp<StutteringAccentComponent>(args.Equipee);
            RemComp<JitteringComponent>(args.Equipee);

            if (!TryComp<MovementSpeedModifierComponent>(args.Equipee, out var modifierComponent))
                return;

            if (component.BaseWalkSpeed == null || component.BaseSprintSpeed == null || component.BaseAcceleration == null)
                return;

            _movementSpeedModifier.ChangeBaseSpeed(args.Equipee, component.BaseWalkSpeed.Value, component.BaseSprintSpeed.Value, component.BaseAcceleration.Value);
            component.BaseWalkSpeed = null;
            component.BaseSprintSpeed = null;
            component.BaseAcceleration = null;
            Dirty(uid, component);
        }
    }
}
