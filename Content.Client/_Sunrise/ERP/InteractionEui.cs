// © SUNRISE, An EULA/CLA with a hosting restriction, full text: https://github.com/space-sunrise/lust-station/blob/master/CLA.txt
using Content.Client.Eui;
using Content.Shared.Eui;
using Content.Shared._Sunrise.ERP;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Random;
using Content.Client.Chat.Managers;
using Robust.Client.Player;
using Robust.Shared.Timing;
using Content.Shared.IdentityManagement;
namespace Content.Client._Sunrise.ERP
{
    [UsedImplicitly]
    public sealed class InteractionEui : BaseEui
    {
        private readonly InteractionWindow _window;
        public IEntityManager _entManager;

        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IChatManager _chat = default!;
        [Dependency] private readonly IPlayerManager _player = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        public InteractionEui()
        {
            _entManager = IoCManager.Resolve<IEntityManager>();
            _window = new InteractionWindow(this);
            _window.OnClose += OnClosed;
        }

        public override void HandleMessage(EuiMessageBase msg)
        {
            base.HandleMessage(msg);

            switch (msg)
            {
                case ResponseInteractionState req:
                    _window.LoveBar.Value = req.UserLovePercent;
                    _window.UserHasClothing = req.UserHasClothing;
                    _window.TargetHasClothing = req.TargetHasClothing;
                    _window.UserSex = req.UserSex;
                    _window.TargetSex = req.TargetSex;
                    _window.Erp = req.ErpAllowed;
                    _window.UserTags = req.UserTags;
                    _window.TargetTags = req.TargetTags;
                    _window.Populate();
                    break;
            }
        }

        public void RequestLove()
        {
            if (!_player.LocalEntity.HasValue) return;
            if (!_window.TargetEntityId.HasValue) return;
            SendMessage(new AddLoveMessage(null));
        }

        public void RequestState()
        {
            if (!_window.TargetEntityId.HasValue) return;
            if (!_player.LocalEntity.HasValue) return;
            if (!_player.LocalEntity.Value.IsValid()) return;
            if (!_window.TargetEntityId.Value.IsValid()) return;
            if (!_window.IsOpen) return;
            SendMessage(new RequestInteractionState());
        }

        private void OnClosed()
        {
            SendMessage(new CloseEuiMessage());
        }

        public override void Opened()
        {
            _window.OpenCenteredLeft();
        }

        public override void Closed()
        {
            base.Closed();
            _window.Close();
        }

        public override void HandleState(EuiStateBase state)
        {
            base.HandleState(state);
            var euiState = (SetInteractionEuiState) state;
            _window.TargetEntityId = euiState.TargetNetEntity;
            _window.UserSex = euiState.UserSex;
            _window.TargetSex = euiState.TargetSex;
            _window.UserHasClothing = euiState.UserHasClothing;
            _window.TargetHasClothing = euiState.TargetHasClothing;
            _window.Erp = euiState.ErpAllowed;
            if (euiState.UserTags != null) { _window.UserTags = euiState.UserTags; } else { _window.UserTags = new(); }
            if (euiState.TargetTags != null) { _window.TargetTags = euiState.TargetTags; } else { _window.TargetTags = new(); }
            _window.Populate();
        }

        private List<(ItemList.Item, TimeSpan, string?)> _disabledItems = new();

        public void FrameUpdate(FrameEventArgs args)
        {
            foreach((var item, var time, var text) in _disabledItems)
            {
                item.Text = text + $" ({(time - _gameTiming.CurTime).Seconds} сек.)";
                if(_gameTiming.CurTime >= time)
                {
                    item.Text = text;
                    item.Disabled = false;
                    _disabledItems.Remove((item, time, text));
                    break;
                }
            }
        }


        public void OnItemSelect(ItemList.ItemListSelectedEventArgs args)
        {
            if (_gameTiming.CurTime >= _window.TimeUntilAllow)
            {
                if (!_player.LocalEntity.HasValue) return;
                if (!_player.LocalEntity.Value.IsValid()) return;
                var item = args.ItemList[args.ItemIndex];
                item.Disabled = true;
                _disabledItems.Add((item, _gameTiming.CurTime + TimeSpan.FromSeconds(5), item.Text));
                if (item.Metadata == null) return;
                InteractionPrototype interaction = (InteractionPrototype) item.Metadata;
                if (interaction.Emotes.Count > 0)
                {
                    if (_window.TargetEntityId == null) return;
                    if (!_window.TargetEntityId.Value.IsValid()) return;
                    string emote = _random.Pick(interaction.Emotes);
                    emote = emote.Replace("%user", Identity.Name(_player.LocalEntity.Value, _entManager));
                    emote = emote.Replace("%target", Identity.Name(_entManager.GetEntity(_window.TargetEntityId.Value), _entManager));
                    _chat.SendMessage(emote, Shared.Chat.ChatSelectChannel.Emotes);
                }
                if (interaction.Sounds.Count > 0)
                {
                    SendMessage(new PlaySoundMessage(_entManager.GetNetEntity(_player.LocalEntity.Value), interaction.Sounds));

                }
                if (!_window.TargetEntityId.HasValue) return;
                SendMessage(new AddLoveMessage(interaction.ID));
                SendMessage(new SendInteractionToServer(interaction.ID));
                _window.TimeUntilAllow = _gameTiming.CurTime + TimeSpan.FromSeconds(0.5);
            }
        }
    }
}
