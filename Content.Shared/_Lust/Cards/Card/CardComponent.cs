using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Lust.Cards.Card;

/// <summary>
/// Marks an entity as a single playable card and stores its face/back sprite layers.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CardComponent : Component
{
    /// <summary>
    /// The back of the card (shown while flipped). Built from the entity sprite layers if empty.
    /// </summary>
    [DataField(readOnly: true)]
    public List<SpriteSpecifier> BackSprite = [];

    /// <summary>
    /// The front of the card. Captured from the entity sprite layers on startup (client-side).
    /// </summary>
    [DataField(readOnly: true)]
    public List<SpriteSpecifier> FrontSprite = [];

    /// <summary>
    /// If it is currently flipped. This is used to update sprite and name.
    /// </summary>
    [DataField(readOnly: true), AutoNetworkedField]
    public bool Flipped = false;

    /// <summary>
    /// The localization id of the card name.
    /// </summary>
    [DataField(readOnly: true), AutoNetworkedField]
    public string Name = "";
}

[Serializable, NetSerializable]
public sealed class CardFlipUpdatedEvent(NetEntity card) : EntityEventArgs
{
    public NetEntity Card = card;
}
