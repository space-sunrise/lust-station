// © SUNRISE, An EULA/CLA with a hosting restriction, full text: https://github.com/space-sunrise/lust-station/blob/master/CLA.txt
using System.Diagnostics.CodeAnalysis;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared._Sunrise.ERP.Components;
using Content.Shared.Database;
using Content.Shared.Verbs;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using Robust.Shared.Random;
using Content.Server.EUI;
using Content.Shared.Humanoid;
using Robust.Shared.Containers;
using Robust.Shared.Timing;
using Content.Server.Chat.Systems;
using Content.Server.Fluids.EntitySystems;
using Content.Shared._Sunrise.ERP;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Hands.Components;
using Content.Shared.Ghost;
using Content.Shared.GameTicking;
using Content.Server.GameTicking;

namespace Content.Server._Sunrise.ERP.Systems
{
    public sealed class InteractionSystem : EntitySystem
    {
        [Dependency] private readonly EuiManager _eui = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly ChatSystem _chat = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly PuddleSystem _puddle = default!;
        [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
        [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
        [Dependency] private readonly GameTicker _gameTicker = default!;

        public static string DefaultBloodSolutionName = "bloodstream";
        public static string DefaultChemicalsSolutionName = "chemicals";
        public static string DefaultLactationSolution = "Milk";

        public static List<string> AcceptableSolutions = new List<string>()
        {
            "drink",
            "beaker",
        };

        private static Dictionary<string, string?> MilkConvertation =
            new Dictionary<string, string?>()
            {
                {"Human", DefaultLactationSolution},
                {"Vox", DefaultLactationSolution},
                {"Reptilian", DefaultLactationSolution},
                {"SlimePerson", "Slime"},
                {"Dwarf", DefaultLactationSolution},
                {"Diona", "Sap"},
                {"Demon", DefaultLactationSolution},
                {"Tajaran", DefaultLactationSolution},
                {"Felinid", DefaultLactationSolution},
                {"Vulpkanin", DefaultLactationSolution},
            };
        private readonly Dictionary<ICommonSession, InteractionEui> _openUis = new();
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<InteractionComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<InteractionComponent, GetVerbsEvent<Verb>>(AddVerbs);
            SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
            SubscribeLocalEvent<RoundRestartCleanupEvent>(Reset);
        }

        private void OnComponentInit(EntityUid uid, InteractionComponent component, ComponentInit args)
        {
        }
        private void AddVerbs(EntityUid uid, InteractionComponent comp, GetVerbsEvent<Verb> args)
        {
            if (!EntityManager.TryGetComponent(args.User, out ActorComponent? actor))
                return;

            if (!args.CanInteract || !args.CanAccess)
                return;

            var player = actor.PlayerSession;

            args.Verbs.Add(new Verb
            {
                Priority = 9,
                Text = "Взаимодействовать с...",
                Icon = new SpriteSpecifier.Texture(new("/Textures/_Lust/Interface/ERP/heart.png")),
                Act = () => OpenInteractionEui(player, args),
                Impact = LogImpact.Low,
            });
        }

        private void OnPlayerAttached(PlayerAttachedEvent message)
        {

            if (!_openUis.ContainsKey(message.Player))
                return;

            if (!HasComp<GhostComponent>(message.Entity))
                return;

            CloseEui(message.Player);
        }

        public void Reset(RoundRestartCleanupEvent ev)
        {
            foreach (var session in _openUis.Keys)
            {
                CloseEui(session);
            }

            _openUis.Clear();
        }

        public bool GetInteractionData(EntityUid user, EntityUid target, out (Sex, bool, Sex, bool, bool, HashSet<string>, HashSet<string>, float)? data)
        {
            data = null;

            if (!TryComp<InteractionComponent>(target, out var targetInteraction) || !TryComp<InteractionComponent>(user, out var userInteraction))
                return false;

            bool erp = true;
            bool userClothing = CheckClothing(user, out var userTags);
            bool targetClothing = CheckClothing(target, out var targetTags);

            if (!targetInteraction.Erp || !userInteraction.Erp)
                erp = false;

            var (userSex, targetSex) = SexCheck(user, target, userTags, targetTags);

            data = (userSex, userClothing, targetSex, targetClothing, erp, userTags, targetTags, userInteraction.Love);
            return true;
        }

        private bool CheckClothing(EntityUid uid, out HashSet<string> tags)
        {
            tags = new HashSet<string>();

            if (!TryComp<ContainerManagerComponent>(uid, out var containerManager))
                return false;

            bool hasClothing = false;

            foreach (var container in containerManager.Containers)
            {
                if (container.Value.ContainedEntities.Count > 0)
                {
                    tags.Add(container.Key);
                    hasClothing |= container.Key == "jumpsuit" || container.Key == "outerClothing";
                }

                foreach (var value in container.Value.ContainedEntities)
                {
                    var meta = MetaData(value);
                    if (meta.EntityPrototype != null)
                    {
                        var proto = meta.EntityPrototype.ID;
                        tags.Add(proto);
                        tags.Add(proto + "Unstrict");
                        if (meta.EntityPrototype.Parents != null)
                        {
                            foreach (var parent in meta.EntityPrototype.Parents)
                            {
                                tags.Add(parent + "Unstrict");
                            }
                        }
                    }
                }
            }

            return hasClothing;
        }

        private (Sex userSex, Sex targetSex) SexCheck(EntityUid user, EntityUid target, HashSet<string> userTags, HashSet<string> targetTags)
        {
            var userSex = Sex.Unsexed;
            var targetSex = Sex.Unsexed;

            if (TryComp<HumanoidAppearanceComponent>(user, out var userHumanoid))
            {
                AddTagsFromMarkings(userHumanoid, userTags);
                userTags.Add(userHumanoid.Species.Id);
                userSex = userHumanoid.Sex;
            }

            if (TryComp<HumanoidAppearanceComponent>(target, out var targetHumanoid))
            {
                AddTagsFromMarkings(targetHumanoid, targetTags);
                targetTags.Add(targetHumanoid.Species.Id);
                targetSex = targetHumanoid.Sex;
            }

            userSex = TryComp<SexComponent>(user, out var userSexComp) ? userSexComp.Sex : userSex;
            targetSex = TryComp<SexComponent>(target, out var targetSexComp) ? targetSexComp.Sex : targetSex;

            return (userSex, targetSex);
        }

        private void AddTagsFromMarkings(HumanoidAppearanceComponent humanoid, HashSet<string> tags)
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
        public void ProcessInteraction(NetEntity user, NetEntity target, InteractionPrototype prototype)
        {
            var netUser = GetEntity(user);
            var netTarget = GetEntity(target);

            foreach (var entity in new List<EntityUid> {netUser, netTarget})
            {
                if (!TryComp<InteractionComponent>(entity, out var interaction)) continue;
                //Virginity check

                if ((entity == netUser && prototype.UserVirginityLoss == VirginityLoss.anal ||
                    entity == netTarget && prototype.TargetVirginityLoss == VirginityLoss.anal) &&
                    interaction.AnalVirginity == Virginity.Yes)
                {
                    interaction.AnalVirginity = Virginity.No;
                    _chat.TrySendInGameICMessage(entity, "лишается анальной девственности", InGameICChatType.Emote, false);
                }
                if (entity == netUser && _random.Prob(prototype.UserMoanChance) ||
                   entity == netTarget && _random.Prob(prototype.TargetMoanChance)) _chat.TryEmoteWithChat(entity, "Moan", ChatTransmitRange.Normal);

                if (TryComp<HumanoidAppearanceComponent>(entity, out var humanoid))
                {
                    switch (humanoid.Sex)
                    {
                        case Sex.Male:
                            if ((entity == netUser && prototype.UserVirginityLoss == VirginityLoss.male ||
                                entity == netTarget && prototype.TargetVirginityLoss == VirginityLoss.male) &&
                                interaction.Virginity == Virginity.Yes)
                            {
                                interaction.Virginity = Virginity.No;
                                _chat.TrySendInGameICMessage(entity, "лишается девственности", InGameICChatType.Emote, false);
                            }
                            break;
                        case Sex.Female:
                            if ((entity == netUser && prototype.UserVirginityLoss == VirginityLoss.female ||
                                entity == netTarget && prototype.TargetVirginityLoss == VirginityLoss.female) &&
                                interaction.Virginity == Virginity.Yes)
                            {
                                interaction.Virginity = Virginity.No;
                                _chat.TrySendInGameICMessage(entity, "теряет девственность", InGameICChatType.Emote, false);
                            }
                            break;
                        default: break;
                    }
                }
            }

            // Process Lactation
            if (prototype.LactationStimulationFlag)
            {
                HandleLactation(ref netUser, ref netTarget, ref prototype);
            }
        }

        public void HandleLactation(ref EntityUid userUid, ref EntityUid targetUid, ref InteractionPrototype prototype)
        {
            if (!TryComp<HumanoidAppearanceComponent>(targetUid, out var humanoid))
                return;

            if (!GetMilkSolution((targetUid, humanoid), out var speciesMilk))
                return;
            var targetPrototype = speciesMilk;

            var forcedFlag = prototype.ID == "BoobsMilkDecant";

            if (targetUid == userUid || forcedFlag)
            {
                var targetXform = Transform(targetUid);
                // Проверка чтоб на боргов не влияли ограничения на лактацию
                if (TryComp<SolutionContainerManagerComponent>(targetUid, out var solutionComponent) &&
                    _solutionContainerSystem.TryGetSolution((targetUid, solutionComponent),
                        DefaultBloodSolutionName,
                        out var containerSolution) &&
                    TryComp<BloodstreamComponent>(targetUid, out var bloodstreamComponent))
                {
                    _solutionContainerSystem.SplitSolution(containerSolution.Value,
                        prototype.Coefficient * prototype.AmountLactate);
                }

                // Это условие проверяет что - у цели есть руки, у цели в руках есть что-то, это что-то является допустимым контейнером
                if (TryComp<HandsComponent>(forcedFlag ? userUid : targetUid, out var handsComponent) &&
                    handsComponent.ActiveHandEntity != null &&
                    TryComp<SolutionContainerManagerComponent>(handsComponent.ActiveHandEntity,
                        out var containerSolutionManager) &&
                    CheckContaining((handsComponent.ActiveHandEntity.Value, containerSolutionManager),
                        AcceptableSolutions,
                        out var containerSolutionEntity))
                {
                    _solutionContainerSystem.TryAddReagent(containerSolutionEntity.Value,
                        targetPrototype,
                        prototype.AmountLactate);
                }
                else
                {
                    _puddle.TrySplashSpillAt(targetUid,
                        targetXform.Coordinates,
                        new Solution(targetPrototype, prototype.AmountLactate, _bloodstream.GetEntityBloodData(targetUid)),
                        out _);
                }
            }
            else
            {
                if (TryComp<SolutionContainerManagerComponent>(userUid, out var userSolutionComponent) &&
                    _solutionContainerSystem.TryGetSolution((userUid, userSolutionComponent),
                        DefaultChemicalsSolutionName,
                        out var userSolution) &&
                    TryComp<SolutionContainerManagerComponent>(targetUid, out var targetSolutionComponent) &&
                    _solutionContainerSystem.TryGetSolution((targetUid, targetSolutionComponent),
                        DefaultBloodSolutionName,
                        out var targetSolution) &&
                    TryComp<BloodstreamComponent>(targetUid, out var bloodstreamComponent))
                {
                    _solutionContainerSystem.SplitSolution(targetSolution.Value,
                        prototype.Coefficient * prototype.AmountLactate);
                    _solutionContainerSystem.TryAddReagent(userSolution.Value,
                        targetPrototype,
                        prototype.AmountLactate);
                }
            }
        }

        private bool GetMilkSolution(Entity<HumanoidAppearanceComponent> entity,
            [NotNullWhen(true)] out string? speciesMilk)
        {
            speciesMilk = null;
            if (MilkConvertation.TryGetValue(entity.Comp.Species, out var milkString) && milkString != null)
            {
                speciesMilk = milkString;
                return true;
            }
            return false;
        }

        // Вспомогательная функция. Оффы сделали что стаканы и мензурки имеют разные названия Solution..
        private bool CheckContaining(Entity<SolutionContainerManagerComponent?> entity,
            List<string> acceptable,
            [NotNullWhen(true)] out Entity<SolutionComponent>? solutionEntity)
        {
            foreach (var solutionString in acceptable)
            {
                if (_solutionContainerSystem.TryGetSolution(entity, solutionString, out solutionEntity))
                {

                    return true;
                }
            }

            solutionEntity = null;
            return false;
        }

        public void AddLove(NetEntity entity, NetEntity target, int percentUser, int percentTarget)
        {
            var User = GetEntity(entity);
            var Target = GetEntity(target);

            if (!TryComp<InteractionComponent>(User, out var compUser))
                return;

            if (!TryComp<InteractionComponent>(Target, out var compTarget))
                return;

            UpdateLove(ref compUser, percentUser, User, "EffectHearts");
            UpdateLove(ref compTarget, percentTarget, Target, "EffectHearts");
        }

        private void UpdateLove(ref InteractionComponent comp, int percent, EntityUid entity, string effect)
        {
            if (percent == 0)
                return;

            if (_gameTiming.CurTime > comp.LoveDelay)
            {
                comp.ActualLove += (percent + _random.Next(-percent / 2, percent / 2)) / 100f;
                comp.TimeFromLastErp = _gameTiming.CurTime;
            }
            Spawn(effect, Transform(entity).Coordinates);

            if (comp.Love >= 1)
            {
                comp.ActualLove = 0;
                comp.Love = 0.95f;
                comp.LoveDelay = _gameTiming.CurTime + TimeSpan.FromMinutes(1);
                _chat.TrySendInGameICMessage(entity, "кончает!", InGameICChatType.Emote, false);

                if (TryComp<HumanoidAppearanceComponent>(entity, out var humanoid) && humanoid.Sex == Sex.Male)
                {
                    Spawn("PuddleSemen", Transform(entity).Coordinates);
                }
            }
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var query = EntityQueryEnumerator<InteractionComponent>();
            while (query.MoveNext(out var uid, out var comp))
            {
                float loveChange = (comp.Love - comp.ActualLove) * frameTime;
                comp.Love -= loveChange;
                if (_gameTiming.CurTime - comp.TimeFromLastErp > TimeSpan.FromSeconds(15) && comp.Love > 0)
                {
                    comp.ActualLove = MathF.Max(0, comp.ActualLove - 0.001f);
                }

                if (comp.Love < 0.00001)
                    comp.Love = 0;

                if (comp.ActualLove < 0)
                    comp.ActualLove = 0;
            }
        }

        public (Sex, bool, Sex, bool, bool, HashSet<string>, HashSet<string>, float)? RequestMenu(EntityUid User, EntityUid Target)
        {
            if (GetInteractionData(User, Target, out var dataNullable)) {
                if (dataNullable.HasValue)
                {
                    var data = dataNullable.Value;
                    return (data.Item1, data.Item2, data.Item3, data.Item4, data.Item5, data.Item6, data.Item7, data.Item8);
                }
                return null;
            }
            return null;
        }


        public void OpenInteractionEui(ICommonSession player, GetVerbsEvent<Verb> args)
        {

            if (!_gameTicker.PlayerGameStatuses.TryGetValue(player.UserId, out var status))
                return;

            if ((player.AttachedEntity is not { Valid: true } attached ||
             EntityManager.HasComponent<GhostComponent>(attached)) && status != PlayerGameStatus.NotReadyToPlay)
                CloseEui(player);

            if (!args.CanInteract || !args.CanAccess)
                return;

            if (_openUis.ContainsKey(player))
                CloseEui(player);

            if (GetInteractionData(args.User, args.Target, out var dataNullable))
            {
                if (dataNullable.HasValue)
                {
                    var data = dataNullable.Value;
                    var eui = _openUis[player] = new InteractionEui(GetNetEntity(args.User), GetNetEntity(args.Target), data.Item1, data.Item2, data.Item3, data.Item4, data.Item5, data.Item6, data.Item7);
                    _eui.OpenEui(eui, player);
                }
            }
        }

        public void CloseEui(ICommonSession session)
        {
            if (!_openUis.ContainsKey(session))
                return;

            _openUis.Remove(session, out var eui);

            eui?.Close();
        }
    }
}
