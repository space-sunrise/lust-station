using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._PANEL.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class InteractionComponent : Component
{
    [DataField, AutoNetworkedField]
    public NetEntity? Target;

    [DataField, AutoNetworkedField]
    public EntityUid User;

    [DataField, AutoNetworkedField]
    public float ActualLove = 0;

    [DataField, AutoNetworkedField]
    public float Love = 0;

    [DataField, AutoNetworkedField]
    public TimeSpan LoveDelay;

    [DataField, AutoNetworkedField]
    public TimeSpan TimeFromLastErp;

    [DataField, AutoNetworkedField]
    public bool Erp;
}

[Serializable, NetSerializable]
public sealed class InteractionBoundUserInterfaceState : BoundUserInterfaceState
{
    public float Love;

    public InteractionBoundUserInterfaceState(float love)
    {
        Love = love;
    }
}