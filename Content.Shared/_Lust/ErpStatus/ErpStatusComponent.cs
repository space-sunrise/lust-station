using Robust.Shared.GameStates;

namespace Content.Shared._Lust.ErpStatus
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class ErpStatusComponent : Component
    {
        [DataField(required: true)]
        public Humanoid.Erp Erp = Humanoid.Erp.Ask;
    }
}
