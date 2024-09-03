using Content.Shared.DoAfter;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._Lust.Rest;

public sealed class SharedRestSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RestAbilityComponent, RestActionEvent>(OnActionToggled);
        SubscribeLocalEvent<RestAbilityComponent, RestDoAfterEvent>(OnSuccess);
    }

    private void OnActionToggled(EntityUid uid, RestAbilityComponent ability, RestActionEvent args)
    {
        var doAfterEventArgs = new DoAfterArgs(EntityManager, uid, ability.Cooldown, new RestDoAfterEvent(), uid)
        {
            BreakOnMove = true,
            BreakOnWeightlessMove = false,
            BreakOnDamage = true,
        };

        _doAfter.TryStartDoAfter(doAfterEventArgs);
    }

    private void OnSuccess(EntityUid uid, RestAbilityComponent ability, RestDoAfterEvent args)
    {
        if (args.Handled)
            return;

        if (args.Cancelled)
            return;

        if (!_timing.IsFirstTimePredicted)
            return;

        ability.IsResting = !ability.IsResting;
        Dirty(uid, ability);

        RaiseLocalEvent(uid, new RestChangeSpriteEvent{Entity = uid});
        ToggleRestLogic(uid, ability);

        args.Handled = true;
    }

    /// <summary>
    /// !!SHITCODE ALERT!! Запрещает двигаться, посредством сбрасывания скорости в ноль, пока цель сидит
    /// </summary>
    /// <param name="uid">Цель</param>
    /// <param name="ability">Компонент сидения, в нем хранится бекап данных</param>
    private void ToggleRestLogic(EntityUid uid, RestAbilityComponent ability)
    {
        if (!TryComp<MovementSpeedModifierComponent>(uid, out var movementSpeed))
            return;

        var walkSpeed = 0f;
        var sprintSpeed = 0f;

        if (ability.IsResting)
        {
            ability.PreviousWalkSpeed = movementSpeed.BaseWalkSpeed;
            ability.PreviousSprintSpeed = movementSpeed.BaseSprintSpeed;

            Dirty(uid, ability);
        }
        else
        {
            walkSpeed = ability.PreviousWalkSpeed;
            sprintSpeed = ability.PreviousSprintSpeed;
        }

        _movementSpeedModifier.ChangeBaseSpeed(uid, walkSpeed, sprintSpeed, movementSpeed.Acceleration);
    }
}
