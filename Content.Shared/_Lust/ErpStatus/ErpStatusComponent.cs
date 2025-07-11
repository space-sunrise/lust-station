using Robust.Shared.GameStates;
using Content.Shared.Humanoid;

namespace Content.Shared._Lust.ErpStatus
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class ErpStatusComponent : Component
    {
        [DataField(required: true)]
        public Erp Erp = Erp.Ask;
    }
}
