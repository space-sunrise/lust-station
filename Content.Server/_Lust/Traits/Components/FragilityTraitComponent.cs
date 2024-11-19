using Content.Shared.FixedPoint;

namespace Content.Server._Lust.Traits.Components;

[RegisterComponent]
public sealed partial class FragilityTraitComponent : Component
{
    [DataField]
    public FixedPoint2 Decrease = 25;
}
