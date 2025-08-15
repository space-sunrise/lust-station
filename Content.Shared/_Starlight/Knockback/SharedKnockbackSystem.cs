using System.Linq;
using System.Numerics;
using Content.Shared._Starlight.Weapon.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Inventory;
using Content.Shared.Slippery;
using Content.Shared.Tag;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Map;
//linq

namespace Content.Shared._Starlight.Knockback;
public abstract partial class SharedKnockbackSystem : EntitySystem
{
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] protected readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<KnockbackByUserTagComponent, OnNonEmptyGunShotEvent>(OnGunShot);
    }

    private void OnGunShot(Entity<KnockbackByUserTagComponent> ent, ref OnNonEmptyGunShotEvent args)
    {
        //make sure the ammo is shootable
        foreach (var ammo in args.Ammo)
        {
            if (TryComp<HitScanCartridgeAmmoComponent>(ammo.Uid, out var hitscanCartridge))
            {
                //check if its spent
                if (hitscanCartridge.Spent)
                {
                    return;
                }
            }

            if (TryComp<CartridgeAmmoComponent>(ammo.Uid, out var cartridge))
            {
                //check if its spent
                if (cartridge.Spent)
                {
                    return;
                }
            }
        }

        EntityUid user = args.User;

        //check for tags
        if (_tagSystem.HasAnyTag(user, ent.Comp.DoestContain.Keys))
        {
            //get the specific knockback data for this tag
            KnockbackData data = ent.Comp.DoestContain.FirstOrDefault(x => _tagSystem.HasTag(user, x.Key)).Value;
            //get the gun component
            if (TryComp<GunComponent>(ent, out var gunComponent))
            {
                var toCoordinates = gunComponent.ShootCoordinates;

                if (toCoordinates == null)
                    return;

                float knockback = data.Knockback;
                //If we have no slips, cut the knockback in half
                if (CheckForNoSlips(user))
                {
                    knockback *= 0.5f;
                }

                if (knockback == 0.0f)
                    return;

                //make a clone, not a reference
                Vector2 modifiedCoords = toCoordinates.Value.Position;
                //flip the direction
                if (knockback > 0)
                    modifiedCoords = -modifiedCoords;

                //absolute knockback now
                knockback = Math.Abs(knockback);
                //normalize them
                modifiedCoords = Vector2.Normalize(modifiedCoords);
                //multiply by the knockback value
                modifiedCoords *= knockback;
                //set the new coordinates
                var flippedDirection = new EntityCoordinates(user, modifiedCoords);

                _throwing.TryThrow(user, flippedDirection, knockback * 5, user, 0, doSpin: false, compensateFriction: true);

                //deal stamina damage
                if (TryComp<StaminaComponent>(user, out var stamina))
                {
                    _stamina.TakeStaminaDamage(user, knockback * data.StaminaMultiplier, component: stamina);
                }
            }
        }
    }

    private bool CheckForNoSlips(EntityUid uid)
    {
        if (EntityManager.TryGetComponent(uid, out NoSlipComponent? flashImmunityComponent))
        {
            return true;
        }

        if (TryComp<InventoryComponent>(uid, out var inventoryComp))
        {
            //get all worn items
            var slots = _inventory.GetSlotEnumerator((uid, inventoryComp), SlotFlags.WITHOUT_POCKET);
            while (slots.MoveNext(out var slot))
            {
                if (slot.ContainedEntity != null && EntityManager.TryGetComponent(slot.ContainedEntity, out NoSlipComponent? wornNoSlipComponent))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
