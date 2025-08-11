using Robust.Shared.GameStates;

namespace Content.Shared._Lust.Inowe;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AiShellComponent : Component
{
    [DataField, AutoNetworkedField, ViewVariables]
    public EntityUid CoreEntity { get; set; }

    [DataField, AutoNetworkedField, ViewVariables]
    public bool IsTaken;
}
