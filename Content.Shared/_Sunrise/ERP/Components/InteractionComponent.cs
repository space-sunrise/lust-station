using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
namespace Content.Shared._Sunrise.ERP.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class InteractionComponent : Component
{
    [DataField, AutoNetworkedField] public bool Erp;
    [DataField, AutoNetworkedField] public float ActualLove = 0;
    [DataField, AutoNetworkedField] public float Love = 0;
    [DataField, AutoNetworkedField] public TimeSpan LoveDelay;
    [DataField, AutoNetworkedField] public TimeSpan TimeFromLastErp;
}

[Serializable, NetSerializable]
public enum InteractionKey
{
    Key
}
