using Content.Shared._Lust.Cards.Hand;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.GameObjects;

namespace Content.Client._Lust.Cards.Hand.UI;

[UsedImplicitly]
public sealed class CardHandMenuBoundUserInterface : BoundUserInterface
{
    private CardHandMenu? _menu;

    public CardHandMenuBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = new CardHandMenu(Owner);
        _menu.OnClose += Close;
        _menu.OnCardSelected += SendCardHandDrawMessage;
        _menu.OpenCentered();
    }

    public void SendCardHandDrawMessage(NetEntity e) => SendMessage(new CardHandDrawMessage(e));

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        _menu?.Dispose();
    }
}
