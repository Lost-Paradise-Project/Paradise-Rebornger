using System.Linq;
using Content.Shared._EstacaoPirata.Cards.Stack;
using Robust.Client.GameObjects;

namespace Content.Client._EstacaoPirata.Cards;

/// <summary>
/// This handles...
/// </summary>
public sealed class CardSpriteSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _spriteSystem = default!;
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
        layerCount = int.Max(1, layerCount); // Frontier: you need one layer.
        //inserts Missing Layers
        if (sprite.AllLayers.Count() < layerCount)
            for (var i = sprite.AllLayers.Count(); i < layerCount; i++)
                _spriteSystem.AddBlankLayer((uid.Owner, sprite));

        //Removes extra layers
        else if (sprite.AllLayers.Count() > layerCount)
            for (var i = sprite.AllLayers.Count() - 1; i >= layerCount; i--)
                _spriteSystem.RemoveLayer((uid.Owner, sprite), i);

        return true;
    }

    public bool TryHandleLayerConfiguration(Entity<SpriteComponent, CardStackComponent> uid, int cardCount, Func<Entity<SpriteComponent>, int, int, bool> layerFunc)
    {
        var sprite = uid.Comp1;
        var stack = uid.Comp2;

        // int = index of what card it is from
        List<(int, ISpriteLayer)> layers = [];

        var i = 0;
        var cards = stack.Cards.TakeLast(cardCount).ToList();
        foreach (var card in cards)
        {
            if (!TryComp(card, out SpriteComponent? cardSprite))
                return false;
            layers.AddRange(cardSprite.AllLayers.Select(layer => (i, layer)));
            i++;
        }

        var j = 0;
        foreach (var obj in layers)
        {
            var (cardIndex, layer) = obj;
            _spriteSystem.LayerSetVisible((uid.Owner, sprite), j, true);
            _spriteSystem.LayerSetTexture((uid.Owner, sprite), j, layer.Texture);
            _spriteSystem.LayerSetRsiState((uid.Owner, sprite), j, layer.RsiState.Name);
            layerFunc.Invoke((uid, sprite), cardIndex, j);
            j++;
        }

        return true;
    }
}
