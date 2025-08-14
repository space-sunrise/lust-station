using Content.Shared.Inventory;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Timing;

namespace Content.Shared.Weapons.Ranged.Events;

/// <summary>
/// Raised on a gun when someone is attempting to shoot it.
/// Cancel this event to prevent it from shooting.
/// </summary>
[ByRefEvent]
public record struct ShotAttemptedEvent : IInventoryRelayEvent // Sunrise-Edit
{
    SlotFlags IInventoryRelayEvent.TargetSlots => SlotFlags.GLOVES; // Sunrise-Edit
    /// <summary>
    /// The user attempting to shoot the gun.
    /// </summary>
    public EntityUid User;

    /// <summary>
    /// The gun being shot.
    /// </summary>
    public Entity<GunComponent> Used;

    public string? Message;

    public bool Cancelled { get; private set; }

    /// </summary>
    /// Prevent the gun from shooting
    /// </summary>
    public void Cancel()
    {
        Cancelled = true;
    }

    /// </summary>
    /// Allow the gun to shoot again, only use if you know what you are doing
    /// </summary>
    public void Uncancel()
    {
        Cancelled = false;
    }
}
