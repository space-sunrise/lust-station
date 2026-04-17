using Content.Shared._Lust.LockableEquipment;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client._Lust.LockableEquipment;

public sealed class EquipmentVisualizerSystem : VisualizerSystem<EquipmentContainerComponent>
{
    private static readonly string[] LockableLayers =
    {
        "lockable_under",
        "lockable_normal",
        "lockable_over",
        "lockable_chest",
        "lockable_underpants",
    };

    protected override void OnAppearanceChange(EntityUid uid, EquipmentContainerComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        var sprite = args.Sprite;

        // Always clear all lockable layers first to prevent stale overlaps.
        foreach (var key in LockableLayers)
        {
            if (SpriteSystem.LayerMapTryGet((uid, sprite), key, out var idx, false))
                SpriteSystem.LayerSetVisible((uid, sprite), idx, false);
        }

        if (!AppearanceSystem.TryGetData<EquipmentVisualData>(uid, EquipmentVisuals.VisualData, out var visualData, args.Component) ||
            visualData == null ||
            !visualData.Visible ||
            string.IsNullOrEmpty(visualData.Layer) ||
            string.IsNullOrEmpty(visualData.RsiPath) ||
            string.IsNullOrEmpty(visualData.State))
        {
            return;
        }

        var layerIdx = SpriteSystem.LayerMapTryGet((uid, sprite), visualData.Layer, out var existingIdx, false)
            ? existingIdx
            : SpriteSystem.LayerMapReserve((uid, sprite), visualData.Layer);

        SpriteSystem.LayerSetRsi((uid, sprite), layerIdx, new ResPath(visualData.RsiPath));
        SpriteSystem.LayerSetRsiState((uid, sprite), layerIdx, visualData.State);
        SpriteSystem.LayerSetVisible((uid, sprite), layerIdx, true);
    }
}
