namespace Content.Shared._Lust.LockableEquipment;

/// <summary>
/// Stores the internal container used for installed lockable devices.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(EquipmentContainerSystem))]
public sealed partial class EquipmentContainerComponent : Component
{
    /// <summary>
    /// Container identifier holding the currently installed device.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string ContainerId = "locked-equipment";

    /// <summary>
    /// Delay before attaching a device completes.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan AttachDoAfter = TimeSpan.FromSeconds(1.5);

    /// <summary>
    /// Delay before removing a device completes.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan DetachDoAfter = TimeSpan.FromSeconds(1.5);
}
