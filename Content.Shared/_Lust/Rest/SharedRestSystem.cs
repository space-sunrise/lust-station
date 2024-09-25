using Content.Shared.DoAfter;
using Content.Shared.Light.Components;
using Content.Shared.Silicons.Borgs.Components;

namespace Content.Shared._Lust.Rest;

public abstract class SharedRestSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Сидение
        SubscribeLocalEvent<RestAbilityComponent, RestActionEvent>(OnActionToggled);

        // Совместимость
        SubscribeLocalEvent<HandheldLightComponent, ActionLightToggledSunriseEvent>(OnToggleAction);
    }

    #region Base

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

    #endregion

    #region Compability

    /// <summary>
    /// Метод, созданный для предотвращения использования фонарика в присяде.
    /// Потому что его использование у борга переключает его лампочки на спрайте и висят в воздухе
    /// Кусок большого щиткода, который покрывает недостаток спрайтов
    /// </summary>
    private void OnToggleAction(EntityUid uid, HandheldLightComponent component, ActionLightToggledSunriseEvent args)
    {
        // Только для боргов
        if (!HasComp<BorgChassisComponent>(uid))
            return;

        if (!TryComp<RestAbilityComponent>(uid, out var restComponent))
            return;

        // Перехватываем ивент, чтобы он не сработал, если мы сидим.
        if (restComponent.IsResting)
            args.Cancel();
    }

    #endregion
}
