using Content.Shared._Lust.Rest;
using Robust.Client.GameObjects;

namespace Content.Client._Lust.Rest;

public sealed class ClientRestSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RestAbilityComponent, RestChangeSpriteEvent>(OnSuccess);
    }

    private void OnSuccess(EntityUid uid, RestAbilityComponent ability, RestChangeSpriteEvent args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        ToggleBaseLayers(sprite, !ability.IsResting, ability.DisableableStringLayers, ability.DisableableEnumLayers);

        sprite.LayerSetVisible(RestVisuals.Resting, ability.IsResting);
    }

    /// <summary>
    /// Переключает видимость основных спрайтов, чтобы заменить сидячим
    /// </summary>
    /// <param name="sprite">Спрайт компонент</param>
    /// <param name="visible">Выключаем или включаем</param>
    /// <param name="stringLayers">Список переключаемых слоев в строках</param>
    /// <param name="enumLayers">Список переключаемых слоев в енумах</param>
    private void ToggleBaseLayers(SpriteComponent sprite, bool visible, HashSet<string>? stringLayers = null, HashSet<Enum>? enumLayers = null )
    {
        // Переключаем базовый спрайт
        sprite.LayerSetVisible(RestVisuals.Base, visible);

        // Все, что ниже, лютое говно, но зато работает без вопросов.

        if (stringLayers == null || stringLayers.Count == 0)
            return;

        // Переключаем все слои на спрайте, переданные как лишние
        foreach (var layer in stringLayers)
        {
            sprite.LayerSetVisible(layer, visible);
        }

        if (enumLayers == null || enumLayers.Count == 0)
            return;

        // Переключаем все слои на спрайте, переданные как лишние
        foreach (var layer in enumLayers)
        {
            sprite.LayerSetVisible(layer, visible);
        }
    }
}
