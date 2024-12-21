using Content.Shared.Inventory.Events;
using Content.Shared._Lust.Toys.Components;

namespace Content.Shared._Lust.Toys.Systems;

public abstract class SharedToySystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VibratingToyComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<VibratingToyComponent, GotUnequippedEvent>(OnGotUnequipped);
    }

    protected virtual void OnGotEquipped(EntityUid uid, VibratingToyComponent component, GotEquippedEvent args)
    {

        component.IsEquipped = args.SlotFlags.HasFlag(component.RequiredSlot);
    }

    protected virtual void OnGotUnequipped(EntityUid uid, VibratingToyComponent component, GotUnequippedEvent args)
    {
        component.IsEquipped = false;
    }
}
