using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.InteractionsPanel.Data.Conditions;

[Serializable, NetSerializable, DataDefinition]
public sealed partial class ItemCondition : IAppearCondition
{
    [DataField]
    public bool CheckInitiator { get; private set; } = true;

    [DataField]
    public bool CheckTarget { get; private set; }

    [DataField]
    public List<EntProtoId> ItemWhiteList { get; private set; } = new();

    public bool IsMet(EntityUid initiator, EntityUid target, EntityManager entityManager)
    {
        var handsSystem = entityManager.EntitySysManager.GetEntitySystem<SharedHandsSystem>();

        if (CheckInitiator)
        {
            if (!HasRequiredItem(initiator, entityManager, handsSystem))
                return false;
        }

        if (CheckTarget)
        {
            if (!HasRequiredItem(target, entityManager, handsSystem))
                return false;
        }

        return true;
    }

    private bool HasRequiredItem(EntityUid entity, EntityManager entityManager, SharedHandsSystem handsSystem)
    {
        if (!entityManager.TryGetComponent<HandsComponent>(entity, out var handsComponent))
            return false;

        foreach (var heldEntity in handsSystem.EnumerateHeld((entity, handsComponent)))
        {
            if (!entityManager.TryGetComponent<MetaDataComponent>(heldEntity, out var meta))
                return false;

            if (meta.EntityPrototype != null && ItemWhiteList.Contains(meta.EntityPrototype.ID))
                return true;
        }

        return false;
    }
}
