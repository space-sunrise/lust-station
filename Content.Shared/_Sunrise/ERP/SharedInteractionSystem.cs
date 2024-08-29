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
        public HashSet<string>? UserTags;
        public HashSet<string>? TargetTags;
    }


    [NetSerializable, Serializable]
    public sealed class AddLoveMessage : EuiMessageBase
    {
        public string? InteractionPrototype;

        public AddLoveMessage(string? interactionPrototype)
        {
            InteractionPrototype = interactionPrototype;
        }
    }

    [NetSerializable, Serializable]
    public sealed class SendInteractionToServer : EuiMessageBase
    {
        public string? InteractionPrototype;

        public SendInteractionToServer(string? interactionPrototype)
        {
            InteractionPrototype = interactionPrototype;
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
    }

    [Serializable, NetSerializable]
    public sealed class ResponseInteractionState : EuiMessageBase
    {
        public Sex UserSex;
        public Sex TargetSex;
        public bool UserHasClothing;
        public bool TargetHasClothing;
        public bool ErpAllowed;
        public HashSet<string> UserTags;
        public HashSet<string> TargetTags;
        public float UserLovePercent;

        public ResponseInteractionState(Sex userSex, Sex targetSex, bool userHasClothing, bool targetHasClothing, bool erp, HashSet<string> userTags, HashSet<string> targetTags, float userLovePercent)
        {
            UserSex = userSex;
            TargetSex = targetSex;
            UserHasClothing = userHasClothing;
            TargetHasClothing = targetHasClothing;
            ErpAllowed = erp;
            UserTags = userTags;
            TargetTags = targetTags;
            UserLovePercent = userLovePercent;
        }
    }

}


