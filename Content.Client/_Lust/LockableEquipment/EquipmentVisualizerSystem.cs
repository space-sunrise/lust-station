using Content.Shared._Lust.LockableEquipment;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client._Lust.LockableEquipment;

public sealed class EquipmentVisualizerSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private static readonly string[] LockableLayers =
    {
        "lockable_under",
        "lockable_normal",
        "lockable_over",
        "lockable_chest",
        "lockable_underpants",
    };

    public override void Initialize()
    {
        SubscribeLocalEvent<EquipmentContainerComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(EntityUid uid, EquipmentContainerComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        var sprite = args.Sprite;

        if (!_appearance.TryGetData<EquipmentVisualData>(uid, EquipmentVisuals.VisualData, out var visualData, args.Component) ||
            visualData == null ||
            string.IsNullOrEmpty(visualData.Layer))
        {
            foreach (var key in LockableLayers)
            {
                if (!_sprite.LayerMapTryGet((uid, sprite), key, out var idx, false))
                    continue;

                _sprite.LayerSetVisible((uid, sprite), idx, false);
            }

            return;
        }

        if (!visualData.Visible ||
            string.IsNullOrEmpty(visualData.RsiPath) ||
            string.IsNullOrEmpty(visualData.State))
        {
            if (_sprite.LayerMapTryGet((uid, sprite), visualData.Layer, out var hiddenIdx, false))
                _sprite.LayerSetVisible((uid, sprite), hiddenIdx, false);
            return;
        }

        var layerIdx = _sprite.LayerMapTryGet((uid, sprite), visualData.Layer, out var existingIdx, false)
            ? existingIdx
            : _sprite.LayerMapReserve((uid, sprite), visualData.Layer);

        _sprite.LayerSetRsi((uid, sprite), layerIdx, new ResPath(visualData.RsiPath));
        _sprite.LayerSetRsiState((uid, sprite), layerIdx, visualData.State);
        _sprite.LayerSetVisible((uid, sprite), layerIdx, true);
    }
}
