using Robust.Shared.Serialization;
using Content.Shared.Humanoid;

namespace Content.Shared._PANEL.Ui;

public abstract class SharedInteractionSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
    }

}

[Serializable, NetSerializable]
public sealed class InteractionMessage : BoundUserInterfaceMessage
{
    public InteractionInfo? Info;

    public InteractionMessage(InteractionInfo? info)
    {
        Info = info;
    }
}

[Serializable, NetSerializable]
public sealed class OnPanelOpen : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public enum InteractionUiKey
{
    Key,
}