using Content.Shared._Sunrise.ERP.Components;
using Content.Shared.Database;
using Content.Shared.Verbs;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using Robust.Server.GameObjects;
using Robust.Shared.Random;
using Content.Server.EUI;
using Content.Shared.Humanoid;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Containers;
using Robust.Shared.Timing;
using Content.Server.Chat.Systems;
namespace Content.Server._Sunrise.ERP.Systems
{
    public sealed class InteractionSystem : EntitySystem
    {
        [Dependency] private readonly EuiManager _eui = default!;
        [Dependency] protected readonly ItemSlotsSystem ItemSlotsSystem = default!;
        [Dependency] protected readonly IGameTiming _gameTiming = default!;
        [Dependency] protected readonly ChatSystem _chat = default!;
        [Dependency] protected readonly IRobustRandom _random = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<InteractionComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<GetVerbsEvent<Verb>>(AddVerbs);
        }

        public void AddLove(NetEntity entity, NetEntity target, int percent)
        {
            List<EntityUid> ents = new();
            ents.Add(GetEntity(entity));
            ents.Add(GetEntity(target));
            foreach (var ent in ents)
            {
                if (!TryComp<HumanoidAppearanceComponent>(ent, out var humanoid)) continue;
                if (TryComp<InteractionComponent>(ent, out var comp))
                {
                    if (percent != 0)
                    {
                        if (_gameTiming.CurTime > comp.LoveDelay && humanoid.Sex == Sex.Male)
                        {
                            comp.ActualLove += (percent + _random.Next(-percent / 2, percent / 2)) / 100f;
                            comp.TimeFromLastErp = _gameTiming.CurTime;
                        }
                        Spawn("EffectHearts", Transform(ent).Coordinates);
                    }
                    if (comp.Love >= 1 && humanoid.Sex == Sex.Male)
                    {
                        comp.ActualLove = 0;
                        comp.Love = 0;
                        comp.LoveDelay = _gameTiming.CurTime + TimeSpan.FromMinutes(1);
                        _chat.TrySendInGameICMessage(ent, "кончает!", InGameICChatType.Emote, false);
                    }
                }
            }
        }
        private void AddVerbs(GetVerbsEvent<Verb> args)
        {
            if (!EntityManager.TryGetComponent(args.User, out ActorComponent? actor))
                return;


            var player = actor.PlayerSession;
            if (args.User != args.Target)
            {
                if (!args.CanInteract || !args.CanAccess) return;
                args.Verbs.Add(new Verb
                {
                    Priority = -1,
                    Text = "Взаимодействовать с...",
                    Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/snow.svg.192dpi.png")), //Не знаю, какую иконку вставить
                    Act = () =>
                    {
                        if (!args.CanInteract || !args.CanAccess) return;
                        if (TryComp<InteractionComponent>(args.Target, out var targetInteraction) && TryComp<InteractionComponent>(args.User, out var userInteraction))
                        {
                            if (TryComp<HumanoidAppearanceComponent>(args.Target, out var targetHumanoid) && TryComp<HumanoidAppearanceComponent>(args.User, out var userHumanoid))
                            {
                                bool erp = true;
                                bool userClothing = false;
                                bool targetClothing = false;
                                if (!targetInteraction.Erp || !userInteraction.Erp) erp = false;
                                if (TryComp<ContainerManagerComponent>(args.User, out var container))
                                {
                                    if (container.Containers["jumpsuit"].ContainedEntities.Count != 0) userClothing = true;
                                    if (container.Containers["outerClothing"].ContainedEntities.Count != 0) userClothing = true;
                                }

                                if (TryComp<ContainerManagerComponent>(args.Target, out var targetContainer))
                                {
                                    if (targetContainer.Containers["jumpsuit"].ContainedEntities.Count != 0) targetClothing = true;
                                    if (targetContainer.Containers["outerClothing"].ContainedEntities.Count != 0) targetClothing = true;
                                }

                                _eui.OpenEui(new InteractionEui(GetNetEntity(args.Target), userHumanoid.Sex, userClothing, targetHumanoid.Sex, targetClothing, erp), player);
                            }
                        }
                    },
                    Impact = LogImpact.Low,
                });
            }
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
