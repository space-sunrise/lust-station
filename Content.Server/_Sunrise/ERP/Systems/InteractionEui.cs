// © SUNRISE, An EULA/CLA with a hosting restriction, full text: https://github.com/space-sunrise/lust-station/blob/master/CLA.txt
using Content.Server.EUI;
using Content.Shared.Eui;
using JetBrains.Annotations;
using Content.Shared._Sunrise.ERP;
using Content.Shared.Humanoid;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Server._Sunrise.ERP.Systems
{
    [UsedImplicitly]
    public sealed class InteractionEui : BaseEui
    {
        private readonly NetEntity _target;
        private readonly NetEntity _user;
        private readonly Sex _userSex;
        private readonly Sex _targetSex;
        private readonly bool _userHasClothing;
        private readonly bool _targetHasClothing;
        private readonly bool _erpAllowed;
        private readonly HashSet<string> _userTags;
        private readonly HashSet<string> _targetTags;

        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IGameTiming _timing = default!;

        private readonly InteractionSystem _interaction;
        private readonly TransformSystem _transform;
        private readonly SharedAudioSystem _audio;
        public IEntityManager _entManager;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        private Dictionary<string, InteractionPrototype> _prototypes = new();
        public InteractionEui(NetEntity user, NetEntity target, Sex userSex, bool userHasClothing, Sex targetSex, bool targetHasClothing, bool erpAllowed, HashSet<string> userTags, HashSet<string> targetTags)
        {
            _user = user;
            _target = target;
            _userSex = userSex;
            _userHasClothing = userHasClothing;
            _targetSex = targetSex;
            _targetHasClothing = targetHasClothing;
            _erpAllowed = erpAllowed;
            _userTags = userTags;
            _targetTags = targetTags;
            _interaction = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<InteractionSystem>();
            _transform = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<TransformSystem>();
            _entManager = IoCManager.Resolve<IEntityManager>();
            _audio = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<SharedAudioSystem>();
            IoCManager.InjectDependencies(this);
        }

        public override void HandleMessage(EuiMessageBase msg)
        {
            base.HandleMessage(msg);
            switch (msg)
            {
                case AddLoveMessage req:
                    var percentUser = 0;
                    var percentTarget = 0;
                    if (req.InteractionPrototype != null)
                    {
                        if (_prototypes.ContainsKey(req.InteractionPrototype))
                        {
                            var proto = _prototypes[req.InteractionPrototype];
                            percentUser = proto.LovePercentUser;
                            percentTarget = proto.LovePercentTarget;
                            if (proto.TargetWithoutCloth && _targetHasClothing) return;
                            if (proto.UserWithoutCloth && _userHasClothing) return;
                            if (proto.Erp && !_erpAllowed) return;
                        }
                        else return;
                    }
                    if (!_transform.InRange(_transform.GetMoverCoordinates(_entManager.GetEntity(_user)), _transform.GetMoverCoordinates(_entManager.GetEntity(_target)), 2))
                    {
                        Close();
                        return;
                    }
                    if (!_entManager.GetEntity(_user).Valid) return;
                    if (!_entManager.GetEntity(_target).Valid) return;
                    _interaction.AddLove(_user, _target, percentUser, percentTarget);
                    break;
                case SendInteractionToServer req:
                    if (!_transform.InRange(_transform.GetMoverCoordinates(_entManager.GetEntity(_user)), _transform.GetMoverCoordinates(_entManager.GetEntity(_target)), 2))
                    {
                        Close();
                        return;
                    }
                    if (!_entManager.GetEntity(_user).Valid) return;
                    if (!_entManager.GetEntity(_target).Valid) return;
                    if (req.InteractionPrototype != null)
                    {
                        if (_prototypes.ContainsKey(req.InteractionPrototype))
                        {
                            var proto = _prototypes[req.InteractionPrototype];
                            _interaction.ProcessInteraction(_user, _target, proto);
                        }
                    }
                    break;
                case RequestInteractionState req:
                    if (!_entManager.GetEntity(_user).Valid) return;
                    if (!_entManager.GetEntity(_target).Valid) return;
                    var res = _interaction.RequestMenu(_entManager.GetEntity(_user), _entManager.GetEntity(_target));
                    if (!res.HasValue) return;
                    var resVal = res.Value;
                    SendMessage(new ResponseInteractionState(resVal.Item1,
                        resVal.Item3,
                        resVal.Item2,
                        resVal.Item4,
                        resVal.Item5,
                        resVal.Item6,
                        resVal.Item7,
                        resVal.Item8));
                    break;
                case PlaySoundMessage req:
                    if (!_entManager.GetEntity(req.User).Valid) return;
                    _audio.PlayPvs(_random.Pick(req.Audios), _entManager.GetEntity(req.User));
                    break;
            }
        }


        public override void Opened()
        {
            base.Opened();
            _prototypes.Clear();
            _prototypeManager.EnumeratePrototypes<InteractionPrototype>().ToList().ForEach(x => _prototypes.Add(x.ID, x));
            StateDirty();
        }

        public override EuiStateBase GetNewState()
        {
            return new SetInteractionEuiState
            {
                TargetNetEntity = _target,
                UserSex = _userSex,
                UserHasClothing = _userHasClothing,
                TargetSex = _targetSex,
                TargetHasClothing = _targetHasClothing,
                ErpAllowed = _erpAllowed,
                UserTags = _userTags,
                TargetTags = _targetTags,
            };
        }

    }
}
