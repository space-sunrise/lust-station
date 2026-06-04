using Robust.Shared.Audio;

namespace Content.Shared._Lust.Cards.Deck;

/// <summary>
/// Marks a card stack as a deck and holds its visual stacking configuration.
/// </summary>
[RegisterComponent]
public sealed partial class CardDeckComponent : Component
{
    /// <summary>
    /// Sound played when the deck is shuffled.
    /// </summary>
    [DataField]
    public SoundSpecifier ShuffleSound = new SoundCollectionSpecifier("cardFan");

    /// <summary>
    /// Sound played when the deck is picked up.
    /// </summary>
    [DataField]
    public SoundSpecifier PickUpSound = new SoundCollectionSpecifier("cardSlide");

    /// <summary>
    /// Sound played when the deck is placed down.
    /// </summary>
    [DataField]
    public SoundSpecifier PlaceDownSound = new SoundCollectionSpecifier("cardShove");

    /// <summary>
    /// Vertical offset (in metres) between each stacked card layer.
    /// </summary>
    [DataField]
    public float YOffset = 0.02f;

    /// <summary>
    /// Render scale applied to each card layer.
    /// </summary>
    [DataField]
    public float Scale = 1;

    /// <summary>
    /// Maximum number of card layers rendered for the deck.
    /// </summary>
    [DataField]
    public int CardLimit = 5;
}
