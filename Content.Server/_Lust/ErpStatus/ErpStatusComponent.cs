namespace Content.Server._Lust.ErpStatus
{
    [RegisterComponent]
    public sealed partial class ErpStatusComponent : Component
    {
        [DataField("erp", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public Shared.Humanoid.Erp Erp = Shared.Humanoid.Erp.Ask;
    }
}
