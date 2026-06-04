using Robust.Shared.Serialization;

namespace Content.Shared._Lust.Cards.Hand;

/// <summary>
/// Marks a card stack as a hand (fanned out) and holds its visual configuration.
/// </summary>
[RegisterComponent]
public sealed partial class CardHandComponent : Component
{
    [DataField]
    public float Angle = 120f;

    [DataField]
    public float XOffset = 0.5f;

    [DataField]
    public float Scale = 1;

    [DataField]
    public int CardLimit = 10;

    [DataField]
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
