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
using Content.Shared.FixedPoint;
using Content.Shared.Hands.Components;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;

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
                {"Slime", "Slime"},
                {"Dwarf", DefaultLactationSolution},
                {"Diona", "Sap"},
                {"Demon", DefaultLactationSolution},
                {"Tajaran", DefaultLactationSolution},
                {"Felinid", DefaultLactationSolution},
                {"Vulpkanin", DefaultLactationSolution},
            };

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<InteractionComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<InteractionComponent, GetVerbsEvent<Verb>>(AddVerbs);
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


        public bool GetInteractionData(EntityUid user, EntityUid target, out (Sex, bool, Sex, bool, bool, HashSet<string>, HashSet<string>, float)? data)
        {
            if (TryComp<InteractionComponent>(target, out var targetInteraction) && TryComp<InteractionComponent>(user, out var userInteraction))
            {

                bool erp = true;
                bool userClothing = false;
                bool targetClothing = false;
                if (!targetInteraction.Erp || !userInteraction.Erp) erp = false;

                HashSet<string> userTags = new();
                HashSet<string> targetTags = new();

                if (TryComp<ContainerManagerComponent>(user, out var container))
                {
                    if (container.Containers.TryGetValue("jumpsuit", out var userJumpsuit))
                        if (userJumpsuit.ContainedEntities.Count != 0) userClothing = true;
                    if (container.Containers.TryGetValue("outerClothing", out var userOuterClothing))
                        if (userOuterClothing.ContainedEntities.Count != 0) userClothing = true;

                    foreach (var c in container.Containers)
                    {
                        if (c.Value.ContainedEntities.Count != 0) userTags.Add(c.Key);
                        foreach (var value in c.Value.ContainedEntities)
                        {
                            var m = MetaData(value);
                            if (m.EntityPrototype != null)
                            {
                                var s = m.EntityPrototype.ID;
                                userTags.Add(s);
                                userTags.Add(s + "Unstrict");
                                var parents = m.EntityPrototype.Parents;
                                if (parents != null)
                                {
                                    foreach (var parent in parents)
                                    {
                                        userTags.Add(parent + "Unstrict");
                                    }
                                }
                            }
                        }
                    }
                }

                if (TryComp<ContainerManagerComponent>(target, out var targetContainer))
                {
                    if (targetContainer.Containers.TryGetValue("jumpsuit", out var targetJumpsuit))
                        if (targetJumpsuit.ContainedEntities.Count != 0) targetClothing = true;
                    if (targetContainer.Containers.TryGetValue("outerClothing", out var targetOuterClothing))
                        if (targetOuterClothing.ContainedEntities.Count != 0) targetClothing = true;

                    foreach (var c in targetContainer.Containers)
                    {
                        if (c.Value.ContainedEntities.Count != 0) targetTags.Add(c.Key);
                        foreach (var value in c.Value.ContainedEntities)
                        {
                            var m = MetaData(value);
                            if (m.EntityPrototype != null)
                            {
                                var s = m.EntityPrototype.ID;
                                targetTags.Add(s);
                                targetTags.Add(s + "Unstrict");
                                var parents = m.EntityPrototype.Parents;
                                if (parents != null)
                                {
                                    foreach (var parent in parents)
                                    {
                                        targetTags.Add(parent + "Unstrict");
                                    }
                                }
                            }
                        }
                    }
                }

                var userSex = Sex.Unsexed;
                var targetSex = Sex.Unsexed;

                if (TryComp<HumanoidAppearanceComponent>(target, out var targetHumanoid) && TryComp<HumanoidAppearanceComponent>(user, out var userHumanoid))
                {
                    foreach (var spec in userHumanoid.MarkingSet.Markings)
                    {
                        userTags.Add(spec.Key.ToString());
                        foreach (var val in spec.Value)
                        {
                            userTags.Add(val.MarkingId);
                        }
                    }

                    foreach (var spec in targetHumanoid.MarkingSet.Markings)
                    {
                        targetTags.Add(spec.Key.ToString());
                        foreach (var val in spec.Value)
                        {
                            targetTags.Add(val.MarkingId);
                        }
                    }

                    userTags.Add(userHumanoid.Species.Id);
                    targetTags.Add(targetHumanoid.Species.Id);
                    targetSex = targetHumanoid.Sex;
                    userSex = userHumanoid.Sex;
                }

                if (TryComp<SexComponent>(target, out var targetSexComp)) targetSex = targetSexComp.Sex;
                if (TryComp<SexComponent>(user, out var userSexComp)) userSex = userSexComp.Sex;

                data = (userSex, userClothing, targetSex, targetClothing, erp, userTags, targetTags, userInteraction.Love);
                return true;
            }
            data = null;
            return false;
        }

        public void ProcessInteraction(NetEntity user, NetEntity target, InteractionPrototype prototype)
        {
            var User = GetEntity(user);
            var Target = GetEntity(target);

            foreach(var entity in new List<EntityUid> {User, Target})
            {
                if (!TryComp<InteractionComponent>(entity, out var interaction)) continue;
                //Virginity check

                if((entity == User && prototype.UserVirginityLoss == VirginityLoss.anal ||
                    entity == Target && prototype.TargetVirginityLoss == VirginityLoss.anal) &&
                    interaction.AnalVirginity == Virginity.Yes)
                {
                    interaction.AnalVirginity = Virginity.No;
                    _chat.TrySendInGameICMessage(entity, "лишается анальной девственности", InGameICChatType.Emote, false);
                }
                if (entity == User && _random.Prob(prototype.UserMoanChance) ||
                   entity == Target && _random.Prob(prototype.TargetMoanChance)) _chat.TryEmoteWithChat(entity, "Moan", ChatTransmitRange.Normal);

                if (TryComp<HumanoidAppearanceComponent>(entity, out var humanoid))
                {
                    switch(humanoid.Sex)
                    {
                        case Sex.Male:
                            if ((entity == User && prototype.UserVirginityLoss == VirginityLoss.male ||
                                entity == Target && prototype.TargetVirginityLoss == VirginityLoss.male) &&
                                interaction.Virginity == Virginity.Yes)
                            {
                                interaction.Virginity = Virginity.No;
                                _chat.TrySendInGameICMessage(entity, "лишается девственности", InGameICChatType.Emote, false);
                            }
                            break;
                        case Sex.Female:
                            if ((entity == User && prototype.UserVirginityLoss == VirginityLoss.female ||
                                entity == Target && prototype.TargetVirginityLoss == VirginityLoss.female) &&
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
                HandleLactation(ref User, ref Target, ref prototype);
            }
        }

        public void HandleLactation(ref EntityUid userUid, ref EntityUid targetUid, ref InteractionPrototype prototype)
        {
            if (!TryComp<HumanoidAppearanceComponent>(targetUid, out var humanoid))
                return;

            if (!GetMilkSolution((targetUid, humanoid), out var speciesMilk))
                return;
            var targetPrototype = speciesMilk;

            if (targetUid == userUid)
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
                if (TryComp<HandsComponent>(targetUid, out var handsComponent) &&
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
            if (!TryComp<InteractionComponent>(User, out var compUser)) return;
            if (!TryComp<InteractionComponent>(Target, out var compTarget)) return;

            if (percentUser != 0)
            {
                if (_gameTiming.CurTime > compUser.LoveDelay)
                {
                    compUser.ActualLove += (percentUser + _random.Next(-percentUser / 2, percentUser / 2)) / 100f;
                    compUser.TimeFromLastErp = _gameTiming.CurTime;
                }
                Spawn("EffectHearts", Transform(User).Coordinates);
            }
            if (compUser.Love >= 1)
            {
                compUser.ActualLove = 0;
                compUser.Love = 0.95f;
                compUser.LoveDelay = _gameTiming.CurTime + TimeSpan.FromMinutes(1);
                _chat.TrySendInGameICMessage(User, "кончает!", InGameICChatType.Emote, false);
                if(TryComp<HumanoidAppearanceComponent>(User, out var humuser))
                {
                    if(humuser.Sex == Sex.Male)
                    {
                        Spawn("PuddleSemen", Transform(User).Coordinates);
                    }
                }
            }

            if (percentTarget != 0)
            {
                if (_gameTiming.CurTime > compTarget.LoveDelay)
                {
                    compTarget.ActualLove += (percentTarget + _random.Next(-percentTarget / 2, percentTarget / 2)) / 100f;
                    compTarget.TimeFromLastErp = _gameTiming.CurTime;
                }
                Spawn("EffectHearts", Transform(Target).Coordinates);
            }
            if (compTarget.Love >= 1)
            {
                compTarget.ActualLove = 0;
                compTarget.Love = 0.95f;
                compTarget.LoveDelay = _gameTiming.CurTime + TimeSpan.FromMinutes(1);
                _chat.TrySendInGameICMessage(Target, "кончает!", InGameICChatType.Emote, false);
                if (TryComp<HumanoidAppearanceComponent>(Target, out var taruser))
                {
                    if (taruser.Sex == Sex.Male)
                    {
                        Spawn("PuddleSemen", Transform(Target).Coordinates);
                    }
                }
            }
        }
        private void AddVerbs(EntityUid uid, InteractionComponent comp, GetVerbsEvent<Verb> args)
        {
            if (!EntityManager.TryGetComponent(args.User, out ActorComponent? actor))
                return;

            var player = actor.PlayerSession;
            if (!args.CanInteract || !args.CanAccess) return;
            args.Verbs.Add(new Verb
            {
                Priority = 9,
                Text = "Взаимодействовать с...",
                Icon = new SpriteSpecifier.Texture(new("/Textures/_Sunrise/Interface/ERP/heart.png")),
                Act = () =>
                {
                    if (!args.CanInteract || !args.CanAccess) return;
                    if (GetInteractionData(args.User, args.Target, out var dataNullable))
                    {
                        if (dataNullable.HasValue)
                        {
                            var data = dataNullable.Value;
                            _eui.OpenEui(new InteractionEui(GetNetEntity(args.User), GetNetEntity(args.Target), data.Item1, data.Item2, data.Item3, data.Item4, data.Item5, data.Item6, data.Item7), player);
                        }
                    }
                },
                Impact = LogImpact.Low,
            });
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var query = EntityQueryEnumerator<InteractionComponent>();
            while (query.MoveNext(out var uid, out var comp))
            {
                comp.Love -= ((comp.Love - comp.ActualLove) / 1) * frameTime;
                if (_gameTiming.CurTime - comp.TimeFromLastErp > TimeSpan.FromSeconds(15) && comp.Love > 0)
                {
                    comp.ActualLove -= 0.001f;
                }
                if (comp.Love < 0) comp.Love = 0;
                if (comp.ActualLove < 0) comp.ActualLove = 0;
            }
        }

        private void OnComponentInit(EntityUid uid, InteractionComponent component, ComponentInit args)
        {
        }
    }
}
