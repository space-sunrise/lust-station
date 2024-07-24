// © SUNRISE, An EULA/CLA with a hosting restriction, full text: https://github.com/space-sunrise/lust-station/blob/master/CLA.txt
using Content.Server.EUI;
using Content.Shared.Eui;
using JetBrains.Annotations;
using Content.Shared._Sunrise.ERP;
using Content.Shared.Humanoid;
using Content.Shared._Sunrise.ERP.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
namespace Content.Server._Sunrise.ERP.Systems
{
    [UsedImplicitly]
    public sealed class InteractionEui : BaseEui
    {
        private readonly NetEntity _target;
        private readonly Sex _userSex;
        private readonly Sex _targetSex;
        private readonly bool _userHasClothing;
        private readonly bool _targetHasClothing;
        private readonly bool _erpAllowed;

        [Dependency] private readonly IRobustRandom _random = default!;

        private readonly InteractionSystem _interaction;
        private readonly TransformSystem _transform;
        private readonly SharedAudioSystem _audio;
        public IEntityManager _entManager;
        
        public InteractionEui(NetEntity target, Sex userSex, bool userHasClothing, Sex targetSex, bool targetHasClothing, bool erpAllowed)
        {
            _target = target;
            _userSex = userSex;
            _userHasClothing = userHasClothing;
            _targetSex = targetSex;
            _targetHasClothing = targetHasClothing;
            _erpAllowed = erpAllowed;
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
                    if (!_entManager.GetEntity(req.User).Valid) return;
                    if (!_entManager.GetEntity(req.Target).Valid) return;
                    _interaction.AddLove(req.User, req.Target, req.PercentUser, req.PercentTarget);
                    if(_entManager.TryGetComponent<InteractionComponent>(_entManager.GetEntity(req.User), out var usComp))
                    {
                        SendMessage(new ResponseLoveMessage(usComp.Love));
                    }
                    if(!_transform.InRange(_transform.GetMoverCoordinates(_entManager.GetEntity(req.User)), _transform.GetMoverCoordinates(_entManager.GetEntity(req.Target)), 2))
                    {
                        Close();
                    }
                    break;
                case RequestInteractionState req:
                    if (!_entManager.GetEntity(req.User).Valid) return;
                    if (!_entManager.GetEntity(req.Target).Valid) return;
                    var res = _interaction.RequestMenu(_entManager.GetEntity(req.User), _entManager.GetEntity(req.Target));
                    if (!res.HasValue) return;
                    var resVal = res.Value;
                    SendMessage(new ResponseInteractionState(resVal.Item1, resVal.Item3, resVal.Item2, resVal.Item4, resVal.Item5));
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
            };
        }

    }
}
