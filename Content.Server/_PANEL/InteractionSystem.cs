using System.Linq;
using Content.Server.Administration.Commands;
using Content.Server.Chat.Managers;
using Content.Server.EUI;
using Content.Shared.Database;
using Content.Shared.Verbs;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using Robust.Server.GameObjects;
using Content.Shared.Humanoid;
using Content.Shared._PANEL.Ui;
using Content.Shared._PANEL.Components;
using Content.Shared._PANEL.Prototypes;
using Content.Shared._PANEL;
using Robust.Shared.Prototypes;
using Content.Shared.Hands.Components;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Shared.Audio.Systems;
using Content.Server.NPC.HTN.Preconditions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Robust.Shared.Timing;
using Content.Server.Chat.Systems;


namespace Content.Server._PANEL;

public sealed class InteractionSystem : EntitySystem
{
    [Dependency] private readonly IConsoleHost _console = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly EuiManager _euis = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly INetManager _netMan = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<InteractionComponent, GetVerbsEvent<InteractionVerb>>(AddVerbs);
        SubscribeLocalEvent<InteractionComponent, OnPanelOpen>(UpdateAllData);
        SubscribeLocalEvent<InteractionMessage>(OnItemPressed);
    }

    private void AddVerbs(EntityUid uid, InteractionComponent comp, GetVerbsEvent<InteractionVerb> args)
    {

        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!TryComp<ActorComponent>(args.User, out var actor))
        {
            return;
        }

        if (!TryComp<InteractionComponent>(args.User, out var component))
        {
            return;
        }

        InteractionVerb verb = new();
        verb.Text = "Modify markings";
        verb.Icon = new SpriteSpecifier.Rsi(new("/Textures/Mobs/Customization/reptilian_parts.rsi"), "tail_smooth");
        verb.Act = () =>
        {
            var netTarget = _entMan.GetNetEntity(args.Target);
            _uiSystem.OpenUi(uid, InteractionUiKey.Key, actor.PlayerSession);
            component.Target = netTarget;
            component.User = args.User;
            Dirty(args.User, component);
        };
        args.Verbs.Add(verb);
    }
    /// <summary>
    /// Эта функция используется для снижения значения Love.
    /// </summary>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<InteractionComponent, UserInterfaceComponent>();
        while (query.MoveNext(out var uid, out var interaction, out var bui))
        {
            float loveChange = (interaction.Love - interaction.ActualLove) * frameTime;

            interaction.Love -= loveChange;

            if (_gameTiming.CurTime - interaction.TimeFromLastErp > TimeSpan.FromSeconds(15) && interaction.Love > 0)
            {
                interaction.ActualLove = MathF.Max(0, interaction.ActualLove - 0.001f);
            }

            if (interaction.Love < 0.00001)
                interaction.Love = 0;

            if (interaction.ActualLove < 0)
                interaction.ActualLove = 0;

            if (interaction.Love > 0)
                UpdateUi(interaction, uid);
        }
    }

    private void UpdateUi(InteractionComponent? comp, EntityUid uid)
    {

        if (!Resolve(uid, ref comp))
                return;

        var state = new InteractionBoundUserInterfaceState(comp.Love);
        _uiSystem.SetUiState(uid, InteractionUiKey.Key, state);        
    }
    public void AddLove(EntityUid user, NetEntity target, int percentUser, int percentTarget)
    {
        var Target = GetEntity(target);

        if (!TryComp<InteractionComponent>(user, out var compUser))
            return;

        if (!TryComp<InteractionComponent>(Target, out var compTarget))
            return;

        UpdateLove(ref compUser, percentUser, user);
        UpdateLove(ref compTarget, percentTarget, Target);
    }

    private void UpdateLove(ref InteractionComponent comp, int percent, EntityUid entity)
    {
        if (percent == 0)
            return;

        if (_gameTiming.CurTime > comp.LoveDelay)
        {
            comp.ActualLove += (percent + _random.Next(-percent / 2, percent / 2)) / 100f;
            comp.TimeFromLastErp = _gameTiming.CurTime;
        }

        if (comp.Love >= 1)
        {
            comp.ActualLove = 0;
            comp.Love = 0.95f;
            comp.LoveDelay = _gameTiming.CurTime + TimeSpan.FromMinutes(1);
            _chat.TrySendInGameICMessage(entity, "кончает!", InGameICChatType.Emote, false);

            // if (TryComp<HumanoidAppearanceComponent>(entity, out var humanoid) && humanoid.Sex == Sex.Male)
            // {
            //     Spawn("PuddleSemen", Transform(entity).Coordinates);
            // }
        }
    }

    private void OnItemPressed(InteractionMessage msg)
    {
        if (msg.Info == null)
            return;

        var target = _entMan.GetEntity(msg.Entity);

        if (msg.Info.Sounds.Count != 0)
            _audio.PlayPvs(_random.Pick(msg.Info.Sounds), msg.Actor);

        if (msg.Info.Erp == true)
        {
            Spawn("EffectHearts", Transform(msg.Actor).Coordinates);
            Spawn("EffectHearts", Transform(target).Coordinates);
        }
        AddLove(msg.Actor, msg.Entity, msg.Info.UserPercent, msg.Info.TargetPercent);
    }
    private HashSet<string> GetTags(EntityUid? uid)
    {
        var tags = new HashSet<string>();

        if (TryComp<ContainerManagerComponent>(uid, out var containerManager))
        {
            foreach (var container in containerManager.Containers)
            {
                tags.Add(container.Key);
                foreach (var entity in container.Value.ContainedEntities)
                {
                    if (TryComp<MetaDataComponent>(entity, out var meta) && meta.EntityPrototype != null)
                    {
                        var proto = meta.EntityPrototype.ID;
                        tags.Add(proto);

                        if (meta.EntityPrototype.Parents != null)
                        {
                            foreach (var parent in meta.EntityPrototype.Parents)
                            {
                                tags.Add(parent);
                            }
                        }
                    }
                }
            }
        }
        return tags;
    }

    private void MarkingTags(HumanoidAppearanceComponent humanoid, HashSet<string> tags)
    {
        foreach (var spec in humanoid.MarkingSet.Markings)
        {
            tags.Add(spec.Key.ToString());
            foreach (var val in spec.Value)
            {
                tags.Add(val.MarkingId);
            }
        }
    }

    private (HashSet<string> userTags, HashSet<string> targetTags) GetInteractionData(EntityUid uid, InteractionComponent comp)
    {
        var userTags = new HashSet<string>();
        var targetTags = new HashSet<string>();

        if (!_entMan.TryGetComponent<InteractionComponent>(uid, out var userInteraction))
            return (userTags, targetTags);

        var target = GetEntity(userInteraction.Target);

        if (!_entMan.TryGetComponent<InteractionComponent>(target, out var targetInteraction))
            return (userTags, targetTags);

        // Заполняем userTags
        var userClothing  = GetTags(uid);
        foreach (var clothing in userClothing)
        {
            userTags.Add(clothing);
        }

        if (TryComp<HumanoidAppearanceComponent>(uid, out var userHumanoid))
        {
            MarkingTags(userHumanoid, userTags);
            userTags.Add(userHumanoid.Species.Id);
        }
        // Заполняем targetTags
        var targetClothing  = GetTags(target);
        foreach (var clothing in targetClothing)
        {
            targetTags.Add(clothing);
        }
        if (TryComp<HumanoidAppearanceComponent>(target, out var targetHumanoid))
        {
            MarkingTags(targetHumanoid, targetTags);
            targetTags.Add(targetHumanoid.Species.Id);
        }

        return (userTags, targetTags);
    }

    private InteractionInfo GetInteractionInfo(EntityUid uid, InteractionPrototype proto, InteractionComponent comp)
    {
        var (userTags, targetTags) = GetInteractionData(uid, comp);
        return new InteractionInfo(proto.Name, proto.Description, proto.ID, proto.SelfUse, proto.Erp,
                    proto.InhandObject, proto.Emotes, proto.Icon,
                    proto.UserSex, proto.TargetSex, proto.Sounds,
                    proto.UserTagBlacklist, proto.UserTagWhitelist,
                    proto.TargetTagBlacklist, proto.TargetTagWhitelist,
                    userTags, targetTags, proto.UserPercent, proto.TargetPercent);
    }

    public void UpdateInteractionList(EntityUid uid, InteractionPrototype proto, InteractionComponent comp)
    {
        var interactionInfoChangedEvent = new InteractionInfoChangedEvent
        {
            InteractionInfo = GetInteractionInfo(uid, proto, comp)
        };

        RaiseNetworkEvent(interactionInfoChangedEvent);
    }

    private void UpdateAllData(EntityUid uid, InteractionComponent comp, OnPanelOpen msg)
    {
        var allProtos = _proto.EnumeratePrototypes<InteractionPrototype>();

        foreach (var proto in allProtos)
        {
            UpdateInteractionList(msg.Actor, proto, comp);
        }
    }
}
