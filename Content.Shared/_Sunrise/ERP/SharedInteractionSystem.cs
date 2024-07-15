// © SUNRISE, An EULA/CLA with a hosting restriction, full text: https://github.com/space-sunrise/lust-station/blob/master/CLA.txt
using Content.Shared._Sunrise.ERP.Components;
using Robust.Shared.Serialization;
using Content.Shared.Eui;
using Content.Shared.Humanoid;
using Robust.Shared.Audio;
namespace Content.Shared._Sunrise.ERP
{
    public abstract class SharedInteractionSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
        }

    }



    [Serializable, NetSerializable]
    public sealed class SetInteractionEuiState : EuiStateBase
    {
        public NetEntity TargetNetEntity;
        public Sex UserSex;
        public Sex TargetSex;
        public bool UserHasClothing;
        public bool TargetHasClothing;
        public bool ErpAllowed;
    }


    [NetSerializable, Serializable]
    public sealed class AddLoveMessage : EuiMessageBase
    {
        public NetEntity User;
        public NetEntity Target;
        public int PercentUser;
        public int PercentTarget;

        public AddLoveMessage(NetEntity user, NetEntity target, int percentUser, int percentTarget)
        {
            User = user;
            Target = target;
            PercentUser = percentUser;
            PercentTarget = percentTarget;
        }
    }


    [NetSerializable, Serializable]
    public sealed class ResponseLoveMessage : EuiMessageBase
    {
        public float Percent;

        public ResponseLoveMessage(float percent)
        {
            Percent = percent;
        }
    }

    [NetSerializable, Serializable]
    public sealed class PlaySoundMessage : EuiMessageBase
    {
        public NetEntity User;
        public List<SoundSpecifier> Audios;

        public PlaySoundMessage(NetEntity user, List<SoundSpecifier> audios)
        {
            User = user;
            Audios = audios;
        }
    }

    [NetSerializable, Serializable]
    public sealed class RequestInteractionState : EuiMessageBase
    {
        public NetEntity User;
        public NetEntity Target;
        public RequestInteractionState(NetEntity user, NetEntity target)
        {
            User = user;
            Target = target;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ResponseInteractionState : EuiMessageBase
    {
        public Sex UserSex;
        public Sex TargetSex;
        public bool UserHasClothing;
        public bool TargetHasClothing;
        public bool ErpAllowed;

        public ResponseInteractionState(Sex userSex, Sex targetSex, bool userHasClothing, bool targetHasClothing, bool erp)
        {
            UserSex = userSex;
            TargetSex = targetSex;
            UserHasClothing = userHasClothing;
            TargetHasClothing = targetHasClothing;
            ErpAllowed = erp;
        }
    }

}


