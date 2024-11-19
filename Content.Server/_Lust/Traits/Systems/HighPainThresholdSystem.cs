using Content.Server._Lust.Traits.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;

namespace Content.Server._Lust.Traits.Systems;

public sealed class HighPainThresholdSystem : EntitySystem
{
    [Dependency] private readonly MobThresholdSystem _threshold = default!;
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<HighPainThresholdTraitComponent, ComponentInit>(OnInit);
    }

    private void OnInit(EntityUid uid, HighPainThresholdTraitComponent component, ComponentInit args)
    {
        if (!TryComp(uid, out MobThresholdsComponent? thresholdsComponent))
            return;
        var critDmg = _threshold.GetThresholdForState(uid, MobState.Critical, thresholdsComponent);
        _threshold.SetMobStateThreshold(uid, critDmg - component.Decrease, MobState.Critical, thresholdsComponent);
    }
}
