using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Aphrodisiac;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LoveVisionComponent : Component
{
    [AutoNetworkedField]
    public bool FromLoveSystem = false;
}
