using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Lust.Cards.Stack;

/// <summary>
/// Holds the cards contained in a stack, deck or hand.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CardStackComponent : Component
{
    /// <summary>
    /// Card prototypes spawned into the stack when it is created.
    /// </summary>
    [DataField]
    public List<EntProtoId> InitialContent = [];

    /// <summary>
    /// Sound played when the stack is shuffled.
    /// </summary>
    [DataField]
    public SoundSpecifier ShuffleSound = new SoundCollectionSpecifier("cardFan");

    /// <summary>
    /// Sound played when the stack is picked up.
    /// </summary>
    [DataField]
    public SoundSpecifier PickUpSound = new SoundCollectionSpecifier("cardSlide");

    /// <summary>
    /// Sound played when the stack is placed down.
    /// </summary>
    [DataField]
    public SoundSpecifier PlaceDownSound = new SoundCollectionSpecifier("cardShove");


    /// <summary>
    /// The containers that contain the items held in the stack
    /// </summary>
    [ViewVariables]
    public Container ItemContainer = default!;

    /// <summary>
    /// The list EntityUIds of Cards
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<EntityUid> Cards = [];
}

[Serializable, NetSerializable]
public sealed class CardStackInitiatedEvent(NetEntity cardStack) : EntityEventArgs
{
    public NetEntity CardStack = cardStack;
}

/// <summary>
/// This gets Updated when new cards are added or removed from the stack
/// </summary>
[Serializable, NetSerializable]
public sealed class CardStackQuantityChangeEvent(NetEntity stack, NetEntity? card, StackQuantityChangeType type) : EntityEventArgs
{
    public NetEntity Stack = stack;
    public NetEntity? Card = card;
    public StackQuantityChangeType Type = type;
}

[Serializable, NetSerializable]
public enum StackQuantityChangeType : sbyte
{
    Added,
    Removed,
    Joined,
    Split
}



[Serializable, NetSerializable]
public sealed class CardStackReorderedEvent(NetEntity stack) : EntityEventArgs
{
    public NetEntity Stack = stack;
}

[Serializable, NetSerializable]
public sealed class CardStackFlippedEvent(NetEntity cardStack) : EntityEventArgs
{
    public NetEntity CardStack = cardStack;
}
