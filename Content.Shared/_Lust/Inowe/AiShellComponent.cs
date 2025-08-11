using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
namespace Content.Shared._Lust.Inowe;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AiShellComponent : Component
{
    [DataField, AutoNetworkedField, ViewVariables]
    public NetEntity CoreEntity;

    [DataField, AutoNetworkedField, ViewVariables]
    public bool IsTaken;
}
