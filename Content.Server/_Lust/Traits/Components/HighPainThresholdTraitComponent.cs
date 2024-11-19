using Content.Shared.FixedPoint;

namespace Content.Server._Lust.Traits.Components;

[RegisterComponent]
public sealed partial class HighPainThresholdTraitComponent : Component
{
    [DataField]
    public FixedPoint2 Decrease = 25;
}
