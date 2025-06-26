using Content.Shared._PANEL;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Shared.IdentityManagement;
using Content.Client.UserInterface.Controls;
using Content.Client.Chat.Managers;
using Robust.Client.Player;
using Content.Shared._PANEL.Ui;
using Content.Shared.Ame.Components;
using Content.Shared._PANEL.Components;

namespace Content.Client._PANEL;

public sealed class InteractionBoundUserInterface : BoundUserInterface
{
    private InteractionWindow? _window;
    [Dependency] private readonly IEntityManager _entManager = default!;
    private readonly InteractionSystem _interactionSystem;

    public InteractionBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
        _interactionSystem = _entManager.EntitySysManager.GetEntitySystem<InteractionSystem>();
    }

    protected override void Open()
    {
        base.Open();

        _window = new InteractionWindow();
        _window.OnClose += Close;
        _window.OpenCentered();
        _window.InteractionListContainer.ItemPressed += SendInteractionMessage;
        SendOnPanelOpen();
    }

    private void SendInteractionMessage(BaseButton.ButtonEventArgs? args, ListData? data)
    {
        if (args == null || data is not InteractionListData { Info: var info })
            return;

        SendMessage(new InteractionMessage(info));
    }

    private void SendOnPanelOpen()
    {
        SendMessage(new OnPanelOpen());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_window == null)
            return;
    
        if (state is not InteractionBoundUserInterfaceState cast)
            return;

        _window.LoveBar.Value = cast.Love;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

        if (_window != null)
        {
            _window.OnClose -= Close;
            _interactionSystem.InteractionListChanged -= _window.PopulateList;
        }

        _window?.Close();
    }

}
