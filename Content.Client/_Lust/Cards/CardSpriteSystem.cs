using System.Linq;
using Content.Shared._Lust.Cards.Stack;
using Robust.Client.Graphics;
using Robust.Client.GameObjects;

namespace Content.Client._Lust.Cards;

/// <summary>
/// Lays out the member-card sprites of a stack/deck/hand.
/// </summary>
public sealed class CardSpriteSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize() { }

    public bool TryAdjustLayerQuantity(Entity<SpriteComponent, CardStackComponent> uid, int? cardLimit = null)
    {
        var sprite = uid.Comp1;
        var stack = uid.Comp2;
        var cardCount = cardLimit == null ? stack.Cards.Count : Math.Min(stack.Cards.Count, cardLimit.Value);

        var layerCount = 0;
        //Gets the quantity of layers
        var relevantCards = stack.Cards.TakeLast(cardCount).ToList();
        foreach (var card in relevantCards)
        {
            if (!TryComp(card, out SpriteComponent? cardSprite))
                return false;

            layerCount += cardSprite.AllLayers.Count();
        }
        layerCount = int.Max(1, layerCount); // you need one layer.
        //inserts Missing Layers
        if (sprite.AllLayers.Count() < layerCount)
            for (var i = sprite.AllLayers.Count(); i < layerCount; i++)
                sprite.AddBlankLayer(i);

        //Removes extra layers
        else if (sprite.AllLayers.Count() > layerCount)
            for (var i = sprite.AllLayers.Count() - 1; i >= layerCount; i--)
                sprite.RemoveLayer(i);

        return true;
    }

    public bool TryHandleLayerConfiguration(Entity<SpriteComponent, CardStackComponent> uid, int cardCount, Func<Entity<SpriteComponent>, int, int, bool> layerFunc)
    {
        var sprite = uid.Comp1;
        var stack = uid.Comp2;

        // int = index of what card it is from; RSI = the source card's rsi (Lust-Edit).
        List<(int, ISpriteLayer, RSI?)> layers = [];

        var i = 0;
        var cards = stack.Cards.TakeLast(cardCount).ToList();
        foreach (var card in cards)
        {
            if (!TryComp(card, out SpriteComponent? cardSprite))
                return false;
            // Lust-Edit: remember each card's own RSI so a stack entity whose base RSI differs from the
            // cards (e.g. deck icon in _Lust/cards.rsi while faces live in _Sunrise/poker_cards.rsi) still renders.
            var cardRsi = cardSprite.BaseRSI;
            layers.AddRange(cardSprite.AllLayers.Select(layer => (i, layer, cardRsi)));
            i++;
        }

        var j = 0;
        foreach (var obj in layers)
        {
            var (cardIndex, layer, cardRsi) = obj;
            sprite.LayerSetVisible(j, true);
            // Lust-Edit: set the source card's state and RSI together so cards from a different RSI
            // than the stack entity render (and no stale-state refresh against the wrong RSI).
            if (cardRsi != null && layer.RsiState.Name != null)
                sprite.LayerSetState(j, layer.RsiState.Name, cardRsi);
            else
                sprite.LayerSetTexture(j, layer.Texture);
            layerFunc.Invoke((uid, sprite), cardIndex, j);
            j++;
        }

        return true;
    }
}
