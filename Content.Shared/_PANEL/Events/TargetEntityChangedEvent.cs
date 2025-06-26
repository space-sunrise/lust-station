using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared._PANEL;

[Serializable, NetSerializable]
public sealed class TargetEntityChangedEvent : EntityEventArgs
{
    public NetEntity? Target;

    public TargetEntityChangedEvent(NetEntity? target)
    {
        Target = target;
    }
}

