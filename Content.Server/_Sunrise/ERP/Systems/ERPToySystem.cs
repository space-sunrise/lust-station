// © SUNRISE, An EULA/CLA with a hosting restriction, full text: https://github.com/space-sunrise/lust-station/blob/master/CLA.txt
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
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Audio;
using Content.Shared.DoAfter;
namespace Content.Server._Sunrise.ERP.Systems
{
    public sealed class ERPToySystem : EntitySystem
    {
        [Dependency] private readonly EuiManager _eui = default!;
        [Dependency] protected readonly ItemSlotsSystem ItemSlotsSystem = default!;
        [Dependency] protected readonly IGameTiming _gameTiming = default!;
        [Dependency] protected readonly ChatSystem _chat = default!;
        [Dependency] protected readonly IRobustRandom _random = default!;
        [Dependency] protected readonly SharedPopupSystem _popup = default!;
        [Dependency] protected readonly SharedAudioSystem _audio = default!;
        [Dependency] protected readonly SharedDoAfterSystem _doafter = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ERPToyComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<ERPToyComponent, UseInHandEvent>(OnUseInHand);
            SubscribeLocalEvent<ERPToyComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<InteractionComponent, ERPToyDoAfterEvent>(OnDoAfter);
        }



        private void OnAfterInteract(EntityUid uid, ERPToyComponent component, AfterInteractEvent args)
        {
            if (args.Handled) return;
            if (args.Target != args.User) return;
            if (!TryComp<HumanoidAppearanceComponent>(args.User, out var humanoid)) return;
            if (!TryComp<InteractionComponent>(args.User, out var interaction)) return;
            if (!interaction.Erp) return;
            var sex = humanoid.Sex;
            string usingGenital = component.GenitalTagList[component.SelectedGenital]
                .Replace("penis", "пениса")
                .Replace("vagina", "вагины")
                .Replace("anal", "анала");
            if (TryComp<ContainerManagerComponent>(args.User, out var container))
            {
                if (container.Containers["jumpsuit"].ContainedEntities.Count != 0) { _popup.PopupEntity($"Сначала снимите {Identity.Name(container.Containers["jumpsuit"].ContainedEntities[0], EntityManager, args.User)}", args.User); return; }
                if (container.Containers["outerClothing"].ContainedEntities.Count != 0) { _popup.PopupEntity($"Сначала снимите {Identity.Name(container.Containers["outerClothing"].ContainedEntities[0], EntityManager, args.User)}", args.User); return; }
                if (container.Containers["pants"].ContainedEntities.Count != 0) { _popup.PopupEntity($"Сначала снимите {Identity.Name(container.Containers["pants"].ContainedEntities[0], EntityManager, args.User)}", args.User); return; }
            }
            if (sex == Sex.Male)
            {
                if (component.GenitalTagList[component.SelectedGenital] == "vagina") { _popup.PopupEntity($"Вы не имеете {usingGenital}!", args.User); return; }
            }
            else if (sex == Sex.Female)
            {
                if (component.GenitalTagList[component.SelectedGenital] == "penis") { _popup.PopupEntity($"Вы не имеете {usingGenital}!", args.User); return; }
            }
            args.Handled = true;
            var doAfterArgs = new DoAfterArgs(EntityManager, args.User, 1, new ERPToyDoAfterEvent(), args.User, args.User, uid)
            {
                BlockDuplicate = true,
                BreakOnMove = true,
                NeedHand = true,
                BreakOnHandChange = true,
                DuplicateCondition = DuplicateConditions.All,
            };
            _doafter.TryStartDoAfter(doAfterArgs);
        }

        private void OnDoAfter<T>(Entity<InteractionComponent> ent, ref T args) where T : DoAfterEvent
        {
            if (args.Handled || args.Cancelled) return;
            if (args.Target == null) return;
            if (_random.Prob(0.1f))
            {
                _chat.TryEmoteWithChat(args.Target.Value, "Moan", ChatTransmitRange.Normal);
            }
            var sound = _random.Pick(new List<SoundPathSpecifier>
            {
                new SoundPathSpecifier("/Audio/_Sunrise/ERP/hlup.ogg"),
                new SoundPathSpecifier("/Audio/_Sunrise/ERP/hlup2.ogg"),
                new SoundPathSpecifier("/Audio/_Sunrise/ERP/hlup3.ogg"),
                new SoundPathSpecifier("/Audio/_Sunrise/ERP/hlup4.ogg")
            }
            );
            _audio.PlayPvs(sound, args.Target.Value);
            args.Handled = true;
        }


        private void OnUseInHand(EntityUid uid, ERPToyComponent component, UseInHandEvent args)
        {
            if (args.Handled) return;
            if (!TryComp<HumanoidAppearanceComponent>(args.User, out var humanoid)) return;
            args.Handled = true;
            var sex = humanoid.Sex;
            if (sex == Sex.Unsexed) {
                _popup.PopupEntity("Вы не можете использовать это!", args.User, args.User, PopupType.Small);
                return;
            }
            component.SelectedGenital += 1;
            int loops = 0;
            while (loops < 10) {
                if (component.SelectedGenital > component.GenitalTagList.Count - 1)
                {
                    component.SelectedGenital = 0;
                }
                if (sex == Sex.Male)
                {
                    if (component.GenitalTagList[component.SelectedGenital] == "vagina")
                    {
                        component.SelectedGenital += 1;
                    }
                    else break;
                }
                else if (sex == Sex.Female)
                {
                    if (component.GenitalTagList[component.SelectedGenital] == "penis")
                    {
                        component.SelectedGenital += 1;
                    }
                    else break;
                }
            }
            string usingGenital = component.GenitalTagList[component.SelectedGenital]
                .Replace("penis", "пенис")
                .Replace("vagina", "вагину")
                .Replace("anal", "анал");
            _popup.PopupEntity($"{Identity.Name(uid, EntityManager, args.User)} теперь используется на {usingGenital}", args.User, args.User, PopupType.Small);
        }

        private void OnComponentInit(EntityUid uid, ERPToyComponent component, ComponentInit args)
        {
        }
    }
}
