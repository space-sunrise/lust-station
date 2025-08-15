using Robust.Shared.GameStates;
using Content.Shared.Humanoid;

namespace Content.Shared._Lust.ErpStatus
{
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    public sealed partial class ErpStatusComponent : Component
    {
        [DataField(required: true), AutoNetworkedField]
        public Erp Erp = Erp.Ask;
    }
}
