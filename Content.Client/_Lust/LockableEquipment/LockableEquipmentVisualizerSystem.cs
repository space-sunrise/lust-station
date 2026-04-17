using Content.Shared._Lust.LockableEquipment;
using Robust.Client.GameObjects;

namespace Content.Client._Lust.LockableEquipment;

public sealed class LockableEquipmentVisualizerSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private const string BaseLayer = "base";

    public override void Initialize()
    {
        SubscribeLocalEvent<LockableEquipmentComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(EntityUid uid, LockableEquipmentComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!args.Sprite.LayerMapTryGet(BaseLayer, out var layer))
            return;

        if (!_appearance.TryGetData<string>(uid, EquipmentVisuals.IconState, out var iconState, args.Component) ||
            string.IsNullOrEmpty(iconState))
        {
            return;
        }

        _sprite.LayerSetRsiState((uid, args.Sprite), layer, iconState);
    }
}
