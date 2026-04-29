using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.InteractionsPanel.Data.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class InteractionsComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public EntityUid? CurrentTarget;

    [ViewVariables, AutoNetworkedField]
    public FixedPoint2 LoveAmount = 0;

    [ViewVariables, AutoNetworkedField]
    public FixedPoint2 MaxLoveAmount = 100;

    [ViewVariables, AutoNetworkedField]
    public Virginity Virginity;

    [ViewVariables, AutoNetworkedField]
    public Virginity AnalVirginity;

    [ViewVariables, AutoNetworkedField]
    public bool Erp = true;

    [ViewVariables, AutoNetworkedField]
    public TimeSpan LastMoanTime = TimeSpan.Zero;

    [ViewVariables, AutoNetworkedField]
    public Dictionary<string, TimeSpan> InteractionCooldowns = new();
}
