using System.Linq;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Robust.Server.Audio;
using Robust.Shared.Containers;
using Content.Server.Bible.Components;

namespace Content.Server._Sunrise.BloodCult.HolyWater;

public sealed class HolyWaterSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly EntityManager _entManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BibleWaterConvertComponent, AfterInteractEvent>(OnBibleInteract);
    }

    private void OnBibleInteract(EntityUid uid, BibleWaterConvertComponent component, AfterInteractEvent args)
    {
        if (HasComp<MobStateComponent>(args.Target))
            return;

        if (!HasComp<BibleUserComponent>(args.User))
            return;

        if (!HasComp<SolutionContainerManagerComponent>(args.Target))
            return;

        if (TryComp<ContainerManagerComponent>(args.Target, out var container))
            {
                foreach (var solution in container.Containers)
                {
                    var solutions = container.Containers
                    .Where(kvp => kvp.Key.StartsWith("solution@"))
                    .Select(kvp => kvp.Key)
                    .ToList();

                    foreach(var solutionKey in solutions)
                    {
                        if (container.Containers.TryGetValue(solutionKey, out var liquidCon))
                        if (_entManager.TryGetComponent<SolutionComponent>(liquidCon.ContainedEntities[0], out var con))
                        {
                            foreach (var reagent in con.Solution.Contents)
                            {
                                if (reagent.Reagent.Prototype != component.ConvertedId)
                                    continue;

                                var amount = reagent.Quantity;

                                con.Solution.RemoveReagent(reagent.Reagent.Prototype, reagent.Quantity);
                                con.Solution.AddReagent(component.ConvertedToId, amount);

                                _popup.PopupEntity(Loc.GetString("holy-water-converted"), args.Target.Value, args.User);
                                _audio.PlayPvs("/Audio/Effects/holy.ogg", args.Target.Value);

                                return;
                            }
                        }    
                    }

                } 
            }
    }
    
}
