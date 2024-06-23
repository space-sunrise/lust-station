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
        public NetEntity User;
        public NetEntity Target;
        public int Percent;

        public AddLoveMessage(NetEntity user, NetEntity target, int percent)
        {
            User = user;
            Target = target;
            Percent = percent;
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

}


