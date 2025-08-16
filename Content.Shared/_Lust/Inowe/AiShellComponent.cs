using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
namespace Content.Shared._Lust.Inowe;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AiShellComponent : Component
{
    [DataField, AutoNetworkedField, ViewVariables]
    public EntityUid CoreEntity { get; set; }

    [DataField, AutoNetworkedField, ViewVariables]
    public bool IsTaken;

    [DataField, AutoNetworkedField, ViewVariables]
    public EntityUid? ReturnToCoreActionId;
}
