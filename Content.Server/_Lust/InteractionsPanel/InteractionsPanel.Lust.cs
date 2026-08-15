using Content.Server.Fluids.EntitySystems;
using Content.Shared._Sunrise.Aphrodisiac;
using Content.Shared._Sunrise.InteractionsPanel.Data.Components;
using Content.Shared._Sunrise.InteractionsPanel.Data.Prototypes;
using Content.Shared._Sunrise.InteractionsPanel.Data.UI;
using Content.Shared.Chat;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Forensics.Components;
using Content.Shared.Humanoid;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server._Sunrise.InteractionsPanel;

public partial class InteractionsPanel
{
    [Dependency] private readonly PuddleSystem _puddle = default!;

    private const float LoveDecayRate = 0.5f;
    private const float OrgasmCooldownSeconds = 15f;

    private void OnUndressMessageReceived(Entity<InteractionsComponent> ent, ref RequestUndressMessage args)
    {
        if (_inventory.TryGetSlots(ent, out var slots))
        {
            foreach (var slot in slots)
            {
                _inventory.TryUnequip(ent, slot.Name, true, false, false);
            }
        }
    }

    private void ProcessInteractionLustEffects(EntityUid user, EntityUid target, InteractionPrototype interactionPrototype)
    {
        if (interactionPrototype.LoveUser > 0)
            ModifyLove(user, interactionPrototype.LoveUser);

        if (interactionPrototype.LoveTarget > 0)
            ModifyLove(target, interactionPrototype.LoveTarget);

        ProcessVirginityLoss(user, target, interactionPrototype);

        TryEmitMoan(user, interactionPrototype.LoveUser, interactionPrototype.UserMoanChance);
        TryEmitMoan(target, interactionPrototype.LoveTarget, interactionPrototype.TargetMoanChance);
    }

    private void TryEmitMoan(EntityUid uid, FixedPoint2 loveGain, float chance)
    {
        if (!_gameTiming.IsFirstTimePredicted)
            return;

        if (!_random.Prob(chance))
            return;

        if (!TryComp<InteractionsComponent>(uid, out var component))
            return;

        var now = _gameTiming.CurTime;
        if (now < component.LastMoanTime + TimeSpan.FromSeconds(5))
            return;

        component.LastMoanTime = now;
        Dirty(uid, component);

        _chatSystem.TryEmoteWithChat(uid, "Moan");
    }

    private void ProcessVirginityLoss(EntityUid user, EntityUid target, InteractionPrototype proto)
    {
        TryLoseVirginity(user, proto.UserVirginityLoss);
        TryLoseVirginity(target, proto.TargetVirginityLoss);
    }

    private void TryLoseVirginity(EntityUid ent, string type)
    {
        if (!TryComp<InteractionsComponent>(ent, out var comp))
            return;

        if (string.IsNullOrWhiteSpace(type) || type == "none")
            return;

        var sex = TryComp<HumanoidAppearanceComponent>(ent, out var humanoid)
            ? humanoid.Sex.ToString().ToLowerInvariant()
            : "unknown";

        switch (type.ToLowerInvariant())
        {
            case "anal":
                if (comp.AnalVirginity == Virginity.Yes)
                {
                    comp.AnalVirginity = Virginity.No;
                    Dirty(ent, comp);
                    _chatSystem.TrySendInGameICMessage(ent, "теряет анальную девственность", InGameICChatType.Emote, false);
                }
                break;

            case "vaginal":
            case "female":
                if (comp.Virginity == Virginity.Yes && sex == "female")
                {
                    comp.Virginity = Virginity.No;
                    Dirty(ent, comp);
                    _chatSystem.TrySendInGameICMessage(ent, "теряет девственность", InGameICChatType.Emote, false);
                }
                break;

            case "male":
                if (comp.Virginity == Virginity.Yes && sex == "male")
                {
                    comp.Virginity = Virginity.No;
                    Dirty(ent, comp);
                    _chatSystem.TrySendInGameICMessage(ent, "теряет девственность", InGameICChatType.Emote, false);
                }
                break;

            case "futanari":
                if (comp.Virginity == Virginity.Yes && sex == "futanari")
                {
                    comp.Virginity = Virginity.No;
                    Dirty(ent, comp);
                    _chatSystem.TrySendInGameICMessage(ent, "теряет девственность", InGameICChatType.Emote, false);
                }
                break;

            case "any":
                if (comp.Virginity == Virginity.Yes)
                {
                    comp.Virginity = Virginity.No;
                    Dirty(ent, comp);
                    _chatSystem.TrySendInGameICMessage(ent, "теряет девственность", InGameICChatType.Emote, false);
                }
                break;
        }
    }

    private void SpawnSemen(EntityUid source, string prototype, EntityCoordinates coordinates)
    {
        var solution = new Solution();
        solution.AddReagent(new ReagentId(prototype, GetSemenDnaData(source)), 4f);
        _puddle.TrySpillAt(coordinates, solution, out _, false);
    }

    private List<ReagentData> GetSemenDnaData(EntityUid source)
    {
        var dnaData = new DnaData();

        if (TryComp<DnaComponent>(source, out var dnaComp) && dnaComp.DNA != null)
            dnaData.DNA = dnaComp.DNA;
        else
            dnaData.DNA = Loc.GetString("forensics-dna-unknown");

        return [dnaData];
    }

    private void UpdateLove(EntityUid uid, InteractionsComponent comp, float frameTime)
    {
        if (comp.LoveAmount <= 0)
        {
            if (TryComp<LoveVisionComponent>(uid, out var loveVisionComp) && loveVisionComp.FromLoveSystem)
            {
                RemComp<LoveVisionComponent>(uid);
            }
            return;
        }

        comp.LoveAmount -= LoveDecayRate * frameTime;
        if (comp.LoveAmount < 0)
            comp.LoveAmount = 0;

        Dirty(uid, comp);

        var ratio = (float)(comp.LoveAmount / comp.MaxLoveAmount).Float();
        var hasEffect = HasComp<LoveVisionComponent>(uid);

        if (ratio >= 0.33f && !hasEffect)
        {
            var newComp = AddComp<LoveVisionComponent>(uid);
            newComp.FromLoveSystem = true;
            Dirty(uid, newComp);
        }
        else if (ratio < 0.33f && TryComp<LoveVisionComponent>(uid, out var loveVisionComp) && loveVisionComp.FromLoveSystem)
        {
            RemComp<LoveVisionComponent>(uid);
        }
    }

    private void TryOrgasm(EntityUid uid)
    {
        if (!TryComp<InteractionsComponent>(uid, out var comp))
            return;

        if (IsOnCooldown(uid, "orgasm"))
            return;

        comp.LoveAmount = 0;

        _chatSystem.TrySendInGameICMessage(uid, "кончает", InGameICChatType.Emote, false);
        _chatSystem.TryEmoteWithChat(uid, "Moan");

        if (TryComp<HumanoidAppearanceComponent>(uid, out var humanoidAppearanceComponent) && humanoidAppearanceComponent.Sex == Sex.Male)
            SpawnSemen(uid, "Semen", Transform(uid).Coordinates);

        SetCooldown(uid, "orgasm", TimeSpan.FromSeconds(OrgasmCooldownSeconds));
        Dirty(uid, comp);
    }

    public void ModifyLove(EntityUid uid, FixedPoint2 amount)
    {
        if (!TryComp<InteractionsComponent>(uid, out var comp))
            return;

        if (IsOnCooldown(uid, "orgasm"))
            return;

        comp.LoveAmount += amount;

        if (comp.LoveAmount >= comp.MaxLoveAmount)
        {
            TryOrgasm(uid);
        }
        else if (comp.LoveAmount > comp.MaxLoveAmount)
        {
            comp.LoveAmount = comp.MaxLoveAmount;
        }

        Dirty(uid, comp);

        var ratio = (float)(comp.LoveAmount / comp.MaxLoveAmount).Float();

        if (ratio >= 0.33f && !HasComp<LoveVisionComponent>(uid))
        {
            var newComp = AddComp<LoveVisionComponent>(uid);
            newComp.FromLoveSystem = true;
            Dirty(uid, newComp);
        }
        else if (ratio < 0.33f && TryComp<LoveVisionComponent>(uid, out var loveVision) && loveVision.FromLoveSystem)
        {
            RemComp<LoveVisionComponent>(uid);
        }
    }
}
