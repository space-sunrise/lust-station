﻿using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Ranged.Components;

/// <summary>
/// Allows battery weapons to fire different types of projectiles
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(BatteryWeaponFireModesSystem))]
[AutoGenerateComponentState(true)]
public sealed partial class BatteryWeaponFireModesComponent : Component
{
    /// <summary>
    /// A list of the different firing modes the weapon can switch between
    /// </summary>
    [DataField(required: true)]
    [AutoNetworkedField]
    public List<BatteryWeaponFireMode> FireModes = new();

    /// <summary>
    /// The currently selected firing mode
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public int CurrentFireMode;
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class BatteryWeaponFireMode
{
    /// <summary>
    /// The projectile prototype associated with this firing mode
    /// </summary>
    [DataField("proto", required: true)]
    public string Prototype = default!; // 🌟Starlight🌟  entity & hitscan

    /// <summary>
    /// The battery cost to fire the projectile associated with this firing mode
    /// </summary>
    [DataField]
    public float FireCost = 100;

    /// <summary>
    /// Conditions that must be satisfied to activate this firing mode
    /// </summary>
    [DataField("conditions", serverOnly: true)]
    [NonSerialized]
    public List<FireModeCondition>? Conditions;

    [DataField("heldPrefix")]
    public string? HeldPrefix;

    [DataField("magState")]
    public string? MagState;

    [DataField("visualState")]
    public string? VisualState;

    [DataField]
    public string Name = string.Empty;
}

[Serializable, NetSerializable]
public enum BatteryWeaponFireModeVisuals : byte
{
    State
}
