using Content.Shared._Lust.Rest;
using Content.Shared.Interaction.Components;

namespace Content.Server._Lust.Rest;

public sealed class RestSystem : SharedRestSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RestAbilityComponent, RestDoAfterEvent>(OnSuccess);
    }

    /// <summary>
    /// Метод, вызываемый после успешного срабатывнаия дуафтера
    /// </summary>
    private void OnSuccess(EntityUid uid, RestAbilityComponent ability, RestDoAfterEvent args)
    {
        if (args.Handled)
            return;

        if (args.Cancelled)
            return;

        // Переключаем маркер сидения
        ability.IsResting = !ability.IsResting;
        Dirty(uid, ability);

        // Переключаем возможность ходить и запрашиваем смену спрайта
        ToggleRestLogic(uid, ability.IsResting);
        RaiseNetworkEvent(new RestChangeSpriteEvent{Entity = GetNetEntity(uid)});

        args.Handled = true;
    }

    /// <summary>
    /// Запрещает двигаться пока цель сидит
    /// </summary>
    /// <param name="uid">Цель</param>
    /// <param name="isResting">Сидим ли мы</param>
    private void ToggleRestLogic(EntityUid uid, bool isResting)
    {
        if (isResting)
        {
            var block = EnsureComp<BlockMovementComponent>(uid);

            // Позволяет вертеться и делать всякие другие вещи, отличные от передвижения
            block.BlockInteraction = false;
            Dirty(uid, block);
        }
        else
        {
            RemComp<BlockMovementComponent>(uid);
        }
    }
}
