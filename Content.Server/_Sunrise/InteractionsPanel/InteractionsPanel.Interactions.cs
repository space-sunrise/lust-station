using System.Numerics;
using Content.Server._Sunrise.PlayerCache;
using Content.Server.Chat.Systems;
using Content.Server.Fluids.EntitySystems;
using Content.Shared._Sunrise.Aphrodisiac;
using Content.Shared._Sunrise.InteractionsPanel.Data.Components;
using Content.Shared._Sunrise.InteractionsPanel.Data.Prototypes;
using Content.Shared._Sunrise.InteractionsPanel.Data.UI;
using Content.Shared.Chat;
using Content.Shared.Chemistry.Components;
using Content.Shared.Clothing;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Hands;
using Content.Shared.Humanoid;
using Content.Shared.Input;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server._Sunrise.InteractionsPanel;

public partial class InteractionsPanel
{
    [Dependency] private readonly PlayerCacheManager _playerCacheManager = default!;
    [Dependency] private readonly PuddleSystem _puddle = default!;

    private const float LoveDecayRate = 0.5f;
    private const float OrgasmCooldownSeconds = 15f;

    private void InitializeInteractions()
    {
        Subs.BuiEvents<InteractionsComponent>(InteractionWindowUiKey.Key,
            subs =>
            {
                subs.Event<InteractionMessage>(OnInteractionMessageReceived);
                subs.Event<RequestUndressMessage>(OnUndressMessageReceived);
            });

        SubscribeLocalEvent<InteractionsComponent, GetVerbsEvent<AlternativeVerb>>(AddInteractionsVerb);
        SubscribeLocalEvent<InteractionsComponent, ComponentInit>(OnInteractionsComponentInit);

        SubscribeLocalEvent<InteractionsComponent, ClothingDidEquippedEvent>(ClothingDidEquipped);
        SubscribeLocalEvent<InteractionsComponent, ClothingDidUnequippedEvent>(ClothingDidUnequipped);
        SubscribeLocalEvent<InteractionsComponent, DidEquipHandEvent>(DidEquipped);
        SubscribeLocalEvent<InteractionsComponent, DidUnequipHandEvent>(DidUnequipped);

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.Interact, new PointerInputCmdHandler(HandleInteract))
            .Bind(ContentKeyFunctions.Interact, InputCmdHandler.FromDelegate(enabled: TryAutoInteraction))
            .Register<InteractionsPanel>();
    }

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

    private void TryAutoInteraction(ICommonSession? session)
    {
        if (session?.AttachedEntity is not { Valid: true } player || !Exists(player))
            return;

        if (!HasComp<InteractionsComponent>(player))
            return;

        if (_ui.IsUiOpen(player, InteractionWindowUiKey.Key))
        {
            _ui.ServerSendUiMessage(player, InteractionWindowUiKey.Key, new RequestSavePosAndCloseMessage());
            return;
        }

        var xform = Transform(player);
        var mapPos = xform.MapPosition;
        var entitiesInRange = new List<EntityUid>();

        var bounds = Box2.CenteredAround(mapPos.Position, new Vector2(3, 3));
        var query = _lookup.GetEntitiesInRange(mapPos, 1.0f, LookupFlags.Approximate | LookupFlags.Dynamic);

        foreach (var ent in query)
        {
            if (ent == player) continue;
            if (!HasComp<InteractionsComponent>(ent)) continue;
            if (!_interaction.InRangeAndAccessible(player, ent)) continue;

            entitiesInRange.Add(ent);
        }

        if (entitiesInRange.Count > 0)
        {
            var target = entitiesInRange[0];
            OpenUI(player, target);
        }
        else
        {
            OpenUI(player, player);
        }
    }

    public bool HandleInteract(ICommonSession? playerSession, EntityCoordinates coordinates, EntityUid entity)
    {
        if (playerSession?.AttachedEntity is not { Valid: true } player || !Exists(player))
            return false;
        if(!HasComp<TransformComponent>(player))
            return false;
        if(!HasComp<TransformComponent>(entity))
            return false;
        if (!_interaction.InRangeAndAccessible(player, entity))
            return false;
        if (!HasComp<InteractionsComponent>(player))
            return false;
        if (!HasComp<InteractionsComponent>(entity))
            return false;
        OpenUI(player, entity);
        return true;
    }

    private void DidEquipped(EntityUid uid, InteractionsComponent component, DidEquipHandEvent args)
    {
        UpdateUIForClothingChange(uid);
    }

    private void DidUnequipped(EntityUid uid, InteractionsComponent component, DidUnequipHandEvent args)
    {
        UpdateUIForClothingChange(uid);
    }

    private void ClothingDidEquipped(EntityUid uid, InteractionsComponent component, ClothingDidEquippedEvent args)
    {
        UpdateUIForClothingChange(uid);
    }

    private void ClothingDidUnequipped(EntityUid uid, InteractionsComponent component, ClothingDidUnequippedEvent args)
    {
        UpdateUIForClothingChange(uid);
    }

    private void UpdateUIForClothingChange(EntityUid changedEntity)
    {
        var query = EntityQueryEnumerator<InteractionsComponent>();
        while (query.MoveNext(out var observerUid, out var observerComp))
        {
            if (!_ui.IsUiOpen(observerUid, InteractionWindowUiKey.Key))
                continue;

            if (!observerComp.CurrentTarget.HasValue)
                continue;

            var needsUpdate = observerComp.CurrentTarget.Value == changedEntity || observerUid == changedEntity;
            if (!needsUpdate)
                continue;

            var state = PrepareUIState(observerUid, observerComp.CurrentTarget.Value);
            _ui.SetUiState(observerUid, InteractionWindowUiKey.Key, state);
        }
    }

    private void OnInteractionsComponentInit(EntityUid uid, InteractionsComponent component, ComponentInit args)
    {
        var interfaceData = new InterfaceData(
            clientType: "Content.Client._Sunrise.InteractionsPanel.InteractionsWindowBoundUserInterface"
        );

        _ui.SetUi(uid, InteractionWindowUiKey.Key, interfaceData);
    }

    private void OnInteractionMessageReceived(Entity<InteractionsComponent> ent, ref InteractionMessage args)
    {
        var target = ent.Comp.CurrentTarget;
        if (target == null)
            return;

        if (!_playerManager.TryGetSessionByEntity(ent.Owner, out var userSession))
            return;

        _playerManager.TryGetSessionByEntity(target.Value, out var targetSession);
        var targetIsPlayer = targetSession != null;

        if (IsOnCooldown(ent.Owner, args.InteractionId))
            return;

        if (args is { IsCustom: true, CustomData: not null })
        {
            HandleCustomInteraction(ent.Owner, target.Value, args.InteractionId, args.CustomData, userSession, targetSession, targetIsPlayer);
            return;
        }

        if (!_prototypeManager.TryIndex<InteractionPrototype>(args.InteractionId, out var interactionPrototype))
            return;

        if (!CheckAllAppearConditions(interactionPrototype, ent.Owner, target.Value))
            return;

        var userPref = _playerCacheManager.GetEmoteVisibility(userSession.UserId);
        var targetPref = _playerCacheManager.GetEmoteVisibility(targetIsPlayer ? targetSession!.UserId : null);

        var rawMsg = _random.Pick(interactionPrototype.InteractionMessages);
        var msg = FormatInteractionMessage(rawMsg, ent.Owner, target.Value);

        if (userPref && targetPref && targetIsPlayer)
        {
            _chatSystem.SendInVoiceRange(ChatChannel.Emotes, msg, msg, ent.Owner, ChatTransmitRange.Normal, color: Color.Pink);
        }
        else
        {
            var filter = Filter.Empty();
            filter.AddPlayer(userSession);

            if (targetIsPlayer)
                filter.AddPlayer(targetSession!);

            _chatManager.ChatMessageToManyFiltered(filter, ChatChannel.Emotes, msg, msg, ent.Owner, false, true, Color.Pink);
        }

        if (interactionPrototype.InteractionSounds.Count != 0)
        {
            var rngSound = _random.Pick(interactionPrototype.InteractionSounds);

            if (_prototypeManager.TryIndex(rngSound, out var soundProto))
            {
                _audio.PlayPvs(soundProto.Sound, ent.Owner, AudioParams.Default);
            }
        }

        if (interactionPrototype.SpawnsEffect)
        {
            if (interactionPrototype.EntityEffect != null
                && _prototypeManager.TryIndex(interactionPrototype.EntityEffect.Value, out var effectPrototype))
            {
                if (_random.Prob(interactionPrototype.EffectChance))
                {
                    Spawn(effectPrototype.EntityEffect, Transform(ent.Owner).Coordinates);

                    if (ent.Owner != target.Value)
                    {
                        Spawn(effectPrototype.EntityEffect, Transform(target.Value).Coordinates);
                    }
                }
            }
        }

        if (interactionPrototype.Cooldown > TimeSpan.Zero)
        {
            SetCooldown(ent.Owner, args.InteractionId, interactionPrototype.Cooldown);
        }

        if (interactionPrototype.LoveUser > 0)
            ModifyLove(ent.Owner, interactionPrototype.LoveUser);

        if (interactionPrototype.LoveTarget > 0)
            ModifyLove(target.Value, interactionPrototype.LoveTarget);

        ProcessVirginityLoss(ent.Owner, target.Value, interactionPrototype);

        TryEmitMoan(ent.Owner, interactionPrototype.LoveUser, interactionPrototype.UserMoanChance);
        TryEmitMoan(target.Value, interactionPrototype.LoveTarget, interactionPrototype.TargetMoanChance);

        _log.Add(LogType.Interactions, LogImpact.Medium,
            $"[InteractionsPanel] {ToPretty(ent.Owner)} использует \"{interactionPrototype.ID}\" на {ToPretty(target.Value)}");
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

    private void SpawnSemen(string prototype, EntityCoordinates coordinates)
    {
        _puddle.TrySpillAt(
            coordinates,
            new Solution(prototype, 4f),
            out _,
            false);
    }

    private void HandleCustomInteraction(
        EntityUid user,
        EntityUid target,
        string interactionId,
        CustomInteractionData data,
        ICommonSession userSession,
        ICommonSession? targetSession,
        bool targetIsPlayer)
    {
        var userPref = _playerCacheManager.GetEmoteVisibility(userSession.UserId);
        var targetPref = _playerCacheManager.GetEmoteVisibility(targetIsPlayer ? targetSession!.UserId : null);

        var msg = FormatInteractionMessage(data.InteractionMessage, user, target);

        if (userPref && targetPref && targetIsPlayer)
        {
            _chatSystem.SendInVoiceRange(ChatChannel.Emotes, msg, msg, user, ChatTransmitRange.Normal, color: Color.Pink);
        }
        else
        {
            var filter = Filter.Empty();
            filter.AddPlayer(userSession);

            if (targetIsPlayer)
                filter.AddPlayer(targetSession!);

            _chatManager.ChatMessageToManyFiltered(filter, ChatChannel.Emotes, msg, msg, user, false, true, Color.Pink);
        }

        if (!string.IsNullOrEmpty(data.SoundId) && _prototypeManager.TryIndex<InteractionSoundPrototype>(data.SoundId, out var soundProto))
        {
            _audio.PlayPvs(soundProto.Sound, user, AudioParams.Default);
        }

        if (data.SpawnsEffect && !string.IsNullOrEmpty(data.EntityEffectId) &&
            _prototypeManager.TryIndex<InteractionEntityEffectPrototype>(data.EntityEffectId, out var effectProto))
        {
            if (_random.Prob(data.EffectChance))
            {

                Spawn(effectProto.EntityEffect, Transform(user).Coordinates);

                if (user != target)
                {
                    Spawn(effectProto.EntityEffect, Transform(target).Coordinates);
                }
            }
        }

        if (data.Cooldown > 0)
        {
            SetCooldown(user, interactionId, TimeSpan.FromSeconds(data.Cooldown));
        }

        _log.Add(LogType.Interactions, LogImpact.Medium,
            $"[InteractionsPanel] {ToPretty(user)} кастомное взаимодействие \"{interactionId}\" с {ToPretty(target)}: \"{data.InteractionMessage}\"");
    }

    private string ToPretty(EntityUid uid)
    {
        return $"{uid} ({MetaData(uid).EntityName})";
    }

    private void UpdateInteractions(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<InteractionsComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            UpdateCooldowns(uid);
            UpdateLove(uid, comp, frameTime);
        }
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
            SpawnSemen("Semen", Transform(uid).Coordinates);

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

    private bool IsOnCooldown(EntityUid user, string interactionId)
    {
        if (!TryComp<InteractionsComponent>(user, out var component))
            return false;

        if (!component.InteractionCooldowns.TryGetValue(interactionId, out var endTime))
            return false;

        return _gameTiming.CurTime < endTime;
    }

    private void SetCooldown(EntityUid user, string interactionId, TimeSpan duration)
    {
        if (!TryComp<InteractionsComponent>(user, out var component))
            return;

        component.InteractionCooldowns[interactionId] = _gameTiming.CurTime + duration;
        Dirty(user, component);
    }

    private void UpdateCooldowns(EntityUid user)
    {
        if (!TryComp<InteractionsComponent>(user, out var component))
            return;

        var currentTime = _gameTiming.CurTime;
        var expiredCooldowns = new List<string>();

        foreach (var (interactionId, endTime) in component.InteractionCooldowns)
        {
            if (currentTime >= endTime)
                expiredCooldowns.Add(interactionId);
        }

        if (expiredCooldowns.Count == 0)
            return;

        foreach (var id in expiredCooldowns)
        {
            component.InteractionCooldowns.Remove(id);
        }

        Dirty(user, component);
    }

    private void AddInteractionsVerb(Entity<InteractionsComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!TryComp<UserInterfaceComponent>(args.User, out var interfaceComponent))
            return;

        if (_mobState.IsIncapacitated(args.Target) || _mobState.IsIncapacitated(args.User))
            return;

        if (!args.CanAccess || !args.CanInteract || !args.CanComplexInteract)
            return;

        var user = args.User;
        var target = args.Target;

        AlternativeVerb verb = new()
        {
            Act = () =>
            {
                OpenUI((user, interfaceComponent), target);
            },
            Text = "Взаимодействовать [F]",
            Priority = -1
        };

        args.Verbs.Add(verb);
    }

    private string FormatInteractionMessage(string template, EntityUid user, EntityUid target)
    {
        var userName = MetaData(user).EntityName;
        var targetName = MetaData(target).EntityName;

        var result = template
            .Replace("%user", userName)
            .Replace("%target", targetName);

        return result;
    }
}
