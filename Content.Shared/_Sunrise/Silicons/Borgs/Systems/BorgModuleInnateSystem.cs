using Content.Shared.Actions;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.UserInterface;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Content.Shared._Sunrise.InnateItem;
using Content.Shared.Interaction.Events;

namespace Content.Server._Sunrise.Silicons.Borgs.Components;

public sealed class BorgModuleInnateSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly MetaDataSystem _metadata = default!;

    private const string InnateItemsContainerId = "module_innate_items";

    private static readonly EntProtoId InnateUseItemAction = "ModuleInnateUseItemAction";


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BorgModuleInnateComponent, BorgModuleInstalledEvent>(OnInstalled);
        SubscribeLocalEvent<BorgModuleInnateComponent, BorgModuleUninstalledEvent>(OnUninstalled);

        SubscribeLocalEvent<BorgModuleInnateComponent, ModuleInnateUseItem>(OnInnateUseItem);

        // SubscribeLocalEvent<BorgModuleInnateComponent, ComponentShutdown>(OnShutdown);
    }

    // TODO: figure out if this really needed (does the borg raise BorgModuleUninstalledEvent when destroyed?)
    // private void OnShutdown(Entity<BorgModuleInnateComponent> ent, ref ComponentShutdown args)
    // {
    //     foreach (var actionEntity in ent.Comp.Actions)
    //         _actions.RemoveAction(actionEntity);

    //     if (_containers.TryGetContainer(ent.Owner, InnateItemsContainerId, out var container))
    //         _containers.CleanContainer(container);

    //     ent.Comp.Actions.Clear();
    // }

    private void OnInstalled(Entity<BorgModuleInnateComponent> module, ref BorgModuleInstalledEvent args)
    {
        var containerManager = EnsureComp<ContainerManagerComponent>(args.ChassisEnt);
        _containers.EnsureContainer<Container>(args.ChassisEnt, InnateItemsContainerId, containerManager);
        AddComponents(args.ChassisEnt, module.Comp.InnateComponents);
        AddInnateItems(args.ChassisEnt, module);
    }

    private void OnUninstalled(Entity<BorgModuleInnateComponent> module, ref BorgModuleUninstalledEvent args)
    {
        foreach (var action in module.Comp.Actions)
            _actions.RemoveAction(action);

        if (_containers.TryGetContainer(args.ChassisEnt, InnateItemsContainerId, out var container))
            _containers.CleanContainer(container);

        module.Comp.Actions.Clear();

        RemoveComponents(args.ChassisEnt, module.Comp.InnateComponents);
    }

    private void AddComponents(EntityUid chassisEnt, ComponentRegistry addedComponents)
    {
        foreach (var (compName, comp) in addedComponents)
        {
            AddComp(chassisEnt, comp.Component);
        }
    }

    private void RemoveComponents(EntityUid chassisEnt, ComponentRegistry addedComponents)
    {
        foreach (var (compName, comp) in addedComponents)
        {
            RemComp(chassisEnt, comp.Component);
        }
    }

    private void AddInnateItems(EntityUid chassis, Entity<BorgModuleInnateComponent> module)
    {
        if (!_containers.TryGetContainer(chassis, InnateItemsContainerId, out var container))
            return;

        AddUseItems(chassis, module, container);
    }

    private void AddUseItems(EntityUid chassis, Entity<BorgModuleInnateComponent> module, BaseContainer container)
    {
        foreach (var itemProto in module.Comp.UseItems)
        {
            if (itemProto is null)
                continue;

            var item = Spawn(itemProto);

            // Модифицируем компач юай, чтобы борг наверняка мог его использовать
            if (TryComp<ActivatableUIComponent>(item, out var activatableUIComponent))
            {
                activatableUIComponent.RequiresComplex = false;
                activatableUIComponent.InHandsOnly = false;
                activatableUIComponent.RequireActiveHand = false;
                Dirty(item, activatableUIComponent);
            }

            _containers.Insert(item, container);

            var actionEnt = Spawn(InnateUseItemAction);
            // Подгружаем спрайт для экшена из прото предмета
            _actions.SetIcon(actionEnt, new SpriteSpecifier.EntityPrototype(MetaData(item).EntityPrototype!.ID));
            // Заготовка события для экшена
            _actions.SetEvent(actionEnt, new ModuleInnateUseItem(item));

            // Даем экшену название и описание предмета
            _metadata.SetEntityName(actionEnt, MetaData(item).EntityName);
            _metadata.SetEntityDescription(actionEnt, MetaData(item).EntityDescription);

            // Добавляем экшн в список экшенов и в список компача
            _actionContainer.AddAction(module.Owner, actionEnt);
            _actions.AddAction(chassis, actionEnt, module.Owner);
            module.Comp.Actions.Add(actionEnt);
        }
    }

    private void OnInnateUseItem(Entity<BorgModuleInnateComponent> ent, ref ModuleInnateUseItem args)
    {
        var ev = new UseInHandEvent(args.Performer);
        RaiseLocalEvent(args.Item, ev);
    }
}

public sealed partial class ModuleInnateUseItem : InstantActionEvent
{
    public readonly EntityUid Item;

    public ModuleInnateUseItem(EntityUid item)
        : this()
    {
        Item = item;
    }
}
