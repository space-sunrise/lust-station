// © SUNRISE, An EULA/CLA with a hosting restriction, full text: https://github.com/space-sunrise/lust-station/blob/master/CLA.txt
using Content.Shared.Humanoid;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
namespace Content.Shared._Sunrise.ERP.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class InteractionComponent : Component
{
    [DataField, AutoNetworkedField] public bool Erp;
    [DataField, AutoNetworkedField] public Virginity Virginity;
    [DataField, AutoNetworkedField] public Virginity AnalVirginity;
    [DataField, AutoNetworkedField] public float ActualLove = 0;
    [DataField, AutoNetworkedField] public float Love = 0;
    [DataField, AutoNetworkedField] public TimeSpan LoveDelay;
    [DataField, AutoNetworkedField] public TimeSpan TimeFromLastErp;
    [DataField, AutoNetworkedField] public HashSet<string> GenitalSprites = new() { "/Textures/deprecated.rsi/deprecated.png", "/Textures/deprecated.rsi/deprecated.png" };

}

[Serializable, NetSerializable]
public enum InteractionKey
{
    Key
}
