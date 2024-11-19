using Content.Server._Lust.Traits.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;

namespace Content.Server._Lust.Traits.Systems;

public sealed class BadThresholdsSystem : EntitySystem
{
    [Dependency] private readonly MobThresholdSystem _threshold = default!;
    [Dependency] private readonly StaminaSystem _stamina = default!;
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<HighPainThresholdTraitComponent, ComponentInit>(OhHighPainInit);
        SubscribeLocalEvent<FragilityTraitComponent, ComponentInit>(OnFragilityInit);
        SubscribeLocalEvent<LowStaminaTraitComponent, ComponentInit>(OnStaminaInit);
    }

    private void OhHighPainInit(EntityUid uid, HighPainThresholdTraitComponent component, ComponentInit args)
    {
        if (!TryComp(uid, out MobThresholdsComponent? thresholdsComponent))
            return;
        var critDmg = _threshold.GetThresholdForState(uid, MobState.Critical, thresholdsComponent);
        _threshold.SetMobStateThreshold(uid, critDmg - component.Decrease, MobState.Critical, thresholdsComponent);
    }

    private void OnFragilityInit(EntityUid uid, FragilityTraitComponent component, ComponentInit args)
    {
        if (!TryComp(uid, out MobThresholdsComponent? thresholdsComponent))
            return;
        var critDmg = _threshold.GetThresholdForState(uid, MobState.Critical, thresholdsComponent);
        var deadDmg = _threshold.GetThresholdForState(uid, MobState.Dead, thresholdsComponent);
        _threshold.SetMobStateThreshold(uid, critDmg - component.Decrease, MobState.Critical, thresholdsComponent);
        _threshold.SetMobStateThreshold(uid, deadDmg - component.Decrease, MobState.Dead, thresholdsComponent);
    }

    private void OnStaminaInit(EntityUid uid, LowStaminaTraitComponent component, ComponentInit args)
    {
        if (!TryComp(uid, out StaminaComponent? staminaComponent))
            return;
        staminaComponent.CritThreshold -= component.Decrease;
        Dirty(uid, staminaComponent); // Дабы на клиенте обновить
    }
}
