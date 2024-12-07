using Content.Shared._Lust.Rest;
using Robust.Client.GameObjects;
using Content.Client.Silicons.Borgs;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Silicons.Borgs.Components;

namespace Content.Client._Lust.Rest;

public sealed class RestSystem : SharedRestSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<RestChangeSpriteEvent>(OnSuccess);
    }

    private void OnSuccess(RestChangeSpriteEvent args)
    {
        var uid = GetEntity(args.Entity);

        if (!TryComp<RestAbilityComponent>(uid, out var ability))
            return;

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
    private void ToggleBaseLayers(SpriteComponent sprite, bool visible, HashSet<string> stringLayers, HashSet<Enum> enumLayers)
    {
        // Переключаем базовый спрайт
        sprite.LayerSetVisible(BorgVisualLayers.Body, visible);

        // Все, что ниже, лютое говно, но зато работает без вопросов.

        ToggleLayers(sprite, visible, stringLayers);
        ToggleLayers(sprite, visible, enumLayers);
    }

    /// <summary>
    /// Метод переключения слоев
    /// </summary>
    /// <param name="sprite">Спрайт компонент</param>
    /// <param name="visible">Выключаем или включаем</param>
    /// <param name="layers">Список переключаемых слоев</param>
    /// <typeparam name="T">Тип передаваемых в HashSet данных</typeparam>
    private void ToggleLayers<T>(SpriteComponent sprite, bool visible, HashSet<T> layers) where T: notnull
    {
        if (layers.Count == 0)
            return;

        // Переключаем все слои на спрайте, переданные как лишние
        foreach (var layer in layers)
        {
            sprite.LayerSetVisible(layer, visible);
        }
    }
}
