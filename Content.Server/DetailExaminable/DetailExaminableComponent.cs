using Content.Shared.Humanoid;

namespace Content.Server.DetailExaminable
{
    [RegisterComponent]
    public sealed partial class DetailExaminableComponent : Component
    {
        [DataField("content", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public string Content = "";

        [DataField("erp", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public Erp Erp = Erp.Ask;
    }
}
