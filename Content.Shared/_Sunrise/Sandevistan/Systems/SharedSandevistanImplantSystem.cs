using Content.Shared.Actions;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared._Sunrise.Sandevistan.Components;
using Robust.Shared.Containers;

namespace Content.Shared._Sunrise.Sandevistan.Systems;

public sealed class SandevistanImplantSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SubdermalImplantComponent, SandevistanToggleEvent>(OnFakeMindShieldToggle);
        SubscribeLocalEvent<SandevistanImplantComponent, ImplantImplantedEvent>(ImplantCheck);
        SubscribeLocalEvent<SandevistanImplantComponent, EntGotRemovedFromContainerMessage>(ImplantDraw);
    }

    /// <summary>
    /// Raise the Action of a Implanted user toggling their implant to the FakeMindshieldComponent on their entity
    /// </summary>
    private void OnFakeMindShieldToggle(Entity<SubdermalImplantComponent> entity, ref SandevistanToggleEvent ev)
    {
        ev.Handled = true;
        if (entity.Comp.ImplantedEntity is not { } ent)
            return;

        if (!TryComp<SandevistanComponent>(ent, out var comp))
            return;
        // TODO: is there a reason this cant set ev.Toggle = true;
        _actionsSystem.SetToggled((ev.Action, ev.Action), !comp.IsEnabled); // Set it to what the Mindshield component WILL be after this
        RaiseLocalEvent(ent, ev); //this reraises the action event to support an eventual future Changeling Antag which will also be using this component for it's "mindshield" ability
    }
    private void ImplantCheck(EntityUid uid, SandevistanImplantComponent component ,ref ImplantImplantedEvent ev)
    {
        if (ev.Implanted != null)
            EnsureComp<SandevistanComponent>(ev.Implanted.Value);
    }

    private void ImplantDraw(Entity<SandevistanImplantComponent> ent, ref EntGotRemovedFromContainerMessage ev)
    {
        RemComp<SandevistanComponent>(ev.Container.Owner);
    }
}
