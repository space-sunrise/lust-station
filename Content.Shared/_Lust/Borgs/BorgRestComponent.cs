using Robust.Shared.GameStates;

namespace Content.Shared._Lust.Borgs;

/// <summary>
/// Allows a borg to voluntarily enter a resting/lying-down state via an action.
/// The rest action is granted per borg type via ActionGrant in BorgTypePrototype.AddComponents.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class BorgRestComponent : Component
{
    /// <summary>
    /// Whether the borg is currently resting.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsResting;
}
