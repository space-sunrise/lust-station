using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Lust.Cards.Hand;

/// <summary>
/// Marks a card stack as a hand (fanned out) and holds its visual configuration.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CardHandComponent : Component
{
    /// <summary>
    /// Total spread angle (in degrees) across the fanned cards.
    /// </summary>
    [DataField]
    public float Angle = 120f;

    /// <summary>
    /// Horizontal spread (in metres) across the fanned cards.
    /// </summary>
    [DataField]
    public float XOffset = 0.5f;

    /// <summary>
    /// Render scale applied to each card layer.
    /// </summary>
    [DataField]
    public float Scale = 1;

    /// <summary>
    /// Maximum number of cards rendered in the fan.
    /// </summary>
    [DataField]
    public int CardLimit = 10;

    /// <summary>
    /// Whether the hand is currently flipped face-down.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Flipped = false;
}


[Serializable, NetSerializable]
public enum CardUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class CardHandDrawMessage(NetEntity card) : BoundUserInterfaceMessage
{
    public NetEntity Card = card;
}
