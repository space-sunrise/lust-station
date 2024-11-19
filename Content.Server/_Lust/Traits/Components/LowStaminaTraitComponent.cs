namespace Content.Server._Lust.Traits.Components;

[RegisterComponent]
public sealed partial class LowStaminaTraitComponent : Component
{
    [DataField]
    public float Decrease = 25;
}
