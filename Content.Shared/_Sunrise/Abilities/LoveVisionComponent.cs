using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Aphrodesiac;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LoveVisionComponent : Component
{
    [AutoNetworkedField]
    public bool FromLoveSystem = false;
}
