using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Lust.LockableEquipment;

[Serializable, NetSerializable]
public sealed class EquipmentVisualData(bool visible, string? layer, string? rsiPath, string? state)
    : IRobustCloneable<EquipmentVisualData>
{
    public readonly bool Visible = visible;
    public readonly string? Layer = layer;
    public readonly string? RsiPath = rsiPath;
    public readonly string? State = state;

    public EquipmentVisualData Clone() => new(Visible, Layer, RsiPath, State);
}
