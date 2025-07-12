using System.Linq;
using Content.Shared.Administration;
using Content.Shared.Administration.Events;
using Content.Shared.GameTicking;
using Robust.Shared.Network;
using Content.Shared._PANEL;

namespace Content.Client._PANEL
{
    public sealed partial class InteractionSystem : EntitySystem
    {
        public event Action<List<InteractionInfo>>? InteractionListChanged;

        public event Action<NetEntity?>? TargetChanged;

        private Dictionary<string, InteractionInfo>? _interactionList;
        private NetEntity? _target;

        public IReadOnlyList<InteractionInfo> InteractionList
        {
            get
            {
                if (_interactionList != null) return _interactionList.Values.ToList();

                return new List<InteractionInfo>();
            }
        }

        public override void Initialize()
        {
            base.Initialize();
            SubscribeNetworkEvent<InteractionInfoChangedEvent>(OnInteractionInfoChanged);
            SubscribeNetworkEvent<TargetEntityChangedEvent>(OnTargetChanged);
        }

        public override void Shutdown()
        {
            base.Shutdown();
        }

        private void OnInteractionInfoChanged(InteractionInfoChangedEvent ev)
        {
            if (ev.InteractionInfo == null) return;

            if (_interactionList == null) 
                _interactionList = new();

            _interactionList[ev.InteractionInfo.Id] = ev.InteractionInfo;

            InteractionListChanged?.Invoke(_interactionList.Values.ToList());
        }
    
        public void OnTargetChanged(TargetEntityChangedEvent ev)
        {
            if (ev.Target == null) return;

            _target = ev.Target;
            TargetChanged?.Invoke(_target);
        } 
    }
}
