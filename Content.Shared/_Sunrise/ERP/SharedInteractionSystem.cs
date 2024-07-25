// © SUNRISE, An EULA/CLA with a hosting restriction, full text: https://github.com/space-sunrise/lust-station/blob/master/CLA.txt
using Content.Shared._Sunrise.ERP.Components;
using Robust.Shared.Serialization;
using Content.Shared.Eui;
using Content.Shared.Humanoid;
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
        public string? InteractionPrototype;

        public AddLoveMessage(string? interactionPrototype)
        {
            InteractionPrototype = interactionPrototype;
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


