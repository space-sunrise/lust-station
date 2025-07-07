﻿using Content.Shared.StatusIcon;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared.Damage;
using Content.Shared._Sunrise.Sandevistan.Trail;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Utility;
using Robust.Shared.Timing;
using Robust.Shared.Audio;

namespace Content.Shared._Sunrise.Sandevistan.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SandevistanComponent : Component
{

    [DataField, AutoNetworkedField]
    public bool IsEnabled { get; set; } = false;

    [DataField]
    public float JitteringPower = 124f;

    [DataField]
    public FixedPoint2? DisableThreshold;

    [DataField]
    public float CurrentMovementSpeedModifier;

    [DataField]
    public float MovementSpeedModifier = 1.7f;

    [DataField]
    public float AttackSpeedModifier = 2f;

    [DataField]
    public int layer;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextUpdate;

    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(0.5);

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan? DisableAt;

    [DataField]
    public TimeSpan StatusEffectTime = TimeSpan.FromSeconds(0.2);

    [DataField]
    public DamageSpecifier Damage = new()
    {
        DamageDict = new Dictionary<string, FixedPoint2>
        {
            { "Blunt", 0.5 },
            { "Poison", 0.25},
        },
    };

    [DataField]
    public SoundSpecifier? StartSound = new SoundPathSpecifier("/Audio/_Sunrise/Sandevistan/sande_start.ogg");

    [DataField]
    public SoundSpecifier? EndSound = new SoundPathSpecifier("/Audio/_Sunrise/Sandevistan/sande_end.ogg");

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? RunningSound;

    [ViewVariables(VVAccess.ReadOnly)]
    public TrailComponent? Trail;

    [ViewVariables(VVAccess.ReadWrite)]
    public int ColorAccumulator = 0;

    /// <summary>
    /// The Security status icon displayed to the security officer. Should be a duplicate of the one the mindshield uses since it's spoofing that
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<SecurityIconPrototype> SandevistanStatusIcon = "MindShieldIcon";
}