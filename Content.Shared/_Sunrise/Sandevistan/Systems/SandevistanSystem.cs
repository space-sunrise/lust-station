using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Implants;
using Content.Shared._Sunrise.Sandevistan.Components;
using Content.Shared._Sunrise.Sandevistan.Trail;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Abilities;
using Content.Shared.Physics;
using Robust.Shared.Map;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Containers;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using System.Linq;
using Content.Shared.Damage;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Audio.Systems;
using Content.Shared.Jittering;

namespace Content.Shared._Sunrise.Sandevistan.Systems;

public sealed class SharedSandevistanSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speed = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedJitteringSystem _jittering = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SandevistanComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<SandevistanComponent, SandevistanToggleEvent>(OnToggle);
        SubscribeLocalEvent<SandevistanComponent, MeleeAttackEvent>(OnMeleeAttack);
        SubscribeLocalEvent<SandevistanComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
        SubscribeLocalEvent<SandevistanComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    
    public override void Update(float frameTime)
    {

        base.Update(frameTime);
        var query = EntityQueryEnumerator<SandevistanComponent>();
        while (query.MoveNext(out var uid, out var comp))
        { 
            if (comp.IsEnabled)
                {

                if (comp.Trail != null)
                {
                    comp.Trail.Color = Color.FromHsv(new Vector4(comp.ColorAccumulator % 100f / 100f, 1, 1, 1));
                    comp.ColorAccumulator++;
                    Dirty(uid, comp.Trail);
                }

                if (comp.NextUpdate > _timing.CurTime)
                    continue;
                comp.NextUpdate = _timing.CurTime + comp.UpdateInterval;

                _jittering.DoJitter(uid, comp.StatusEffectTime, true, comp.JitteringPower, 1f);

                _damageable.TryChangeDamage(uid, comp.Damage, ignoreResistances: true);
                
                if (_mobThreshold.TryGetThresholdForState(uid, MobState.Critical, out var threshold) &&
                TryComp<DamageableComponent>(uid, out var damageableComponent) &&
                damageableComponent.TotalDamage > comp.DisableThreshold)
                    {
                        Disable(uid, comp);
                    }
                }
            if (comp.DisableAt != null
                && _timing.CurTime > comp.DisableAt)
                Disable(uid, comp);
        }


    }

        private void OnStartup(EntityUid uid, SandevistanComponent comp, ref ComponentStartup args)
    {
        if (_mobThreshold.TryGetThresholdForState(uid, MobState.Critical, out var threshold))
        comp.DisableThreshold = threshold;
    }

    private void OnToggle(EntityUid uid, SandevistanComponent comp, SandevistanToggleEvent toggleEvent)
    {
        if (!comp.IsEnabled)
        {
            Enable(uid, comp);
        }
        else
        {
            comp.DisableAt = _timing.CurTime + TimeSpan.FromSeconds(2);
            _audio.PlayEntity(comp.EndSound, uid, uid);
        }
    }

     private void Enable(EntityUid uid, SandevistanComponent comp)
    {
        if (!comp.IsEnabled)
            comp.IsEnabled = true;
        if (!HasComp<DogVisionComponent>(uid))
            AddComp<DogVisionComponent>(uid);
        comp.CurrentMovementSpeedModifier = comp.MovementSpeedModifier;
        _speed.RefreshMovementSpeedModifiers(uid);
        if (TryComp<FixturesComponent>(uid, out var fixtures))
            {
                var fixture = fixtures.Fixtures.First();
                comp.layer = fixture.Value.CollisionLayer;
                _physics.SetCollisionLayer(uid, fixture.Key, fixture.Value, 0, fixtures);
            }

        
        if (!HasComp<TrailComponent>(uid))
        {
            var trail = AddComp<TrailComponent>(uid);
            trail.RenderedEntity = uid;
            trail.LerpTime = 0.1f;
            trail.LerpDelay = TimeSpan.FromSeconds(4);
            trail.Lifetime = 10;
            trail.Frequency = 0.07f;
            trail.AlphaLerpAmount = 0.2f;
            trail.MaxParticleAmount = 25;
            comp.Trail = trail;
        }


        var audio = _audio.PlayEntity(comp.StartSound, uid, uid);
        if (!audio.HasValue)
            return;

        comp.RunningSound = audio.Value.Entity;


    }

     private void Disable(EntityUid uid, SandevistanComponent comp)
    {
        comp.DisableAt = null;
        if (comp.IsEnabled)
            comp.IsEnabled = false;

        if (HasComp<DogVisionComponent>(uid))
            RemComp<DogVisionComponent>(uid);
        comp.CurrentMovementSpeedModifier = 1f;
        _speed.RefreshMovementSpeedModifiers(uid);
        if (TryComp<FixturesComponent>(uid, out var fixtures))
            {
                var fixture = fixtures.Fixtures.First();
                _physics.SetCollisionLayer(uid, fixture.Key, fixture.Value, comp.layer, fixtures);
            }

        comp.ColorAccumulator = 0;

        if (comp.Trail != null)
        {
            RemComp(uid, comp.Trail);
            comp.Trail = null;
        }


        _audio.Stop(comp.RunningSound);  

    }

    private void OnRefreshSpeed(Entity<SandevistanComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (ent.Comp.IsEnabled != null)
            args.ModifySpeed(ent.Comp.CurrentMovementSpeedModifier, ent.Comp.CurrentMovementSpeedModifier);
    }
    
    private void OnMeleeAttack(Entity<SandevistanComponent> ent, ref MeleeAttackEvent args)
    {
        if (ent.Comp.IsEnabled == false
            || !TryComp<MeleeWeaponComponent>(args.Weapon, out var weapon))
            return;

        var rate = weapon.NextAttack - _timing.CurTime;
        weapon.NextAttack -= rate - rate / ent.Comp.AttackSpeedModifier;
    }

    private void OnMobStateChanged(Entity<SandevistanComponent> ent, ref MobStateChangedEvent args)
    {
        Disable(ent, ent.Comp);
    }

}

public sealed partial class SandevistanToggleEvent : InstantActionEvent;
