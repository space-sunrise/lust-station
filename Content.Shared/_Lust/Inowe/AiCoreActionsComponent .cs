using Robust.Shared.GameStates;

[RegisterComponent, NetworkedComponent]
public sealed partial class AiCoreActionsComponent : Component
{
    public EntityUid? GoToShellActionId;
}
