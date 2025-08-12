﻿using Content.Server.Disposal.Unit;
using Content.Shared._Sunrise.VentCraw;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Humanoid;

namespace Content.Server._Sunrise.Misc.BlindInDisposals;

/// <summary>
/// Простая система, делающая персонажа слепым, пока он находится в трубах.
/// Из труб не должно быть видно реальный мир!!
/// </summary>
public sealed class BlindInDisposalsSystem : EntitySystem
{
    [Dependency] private readonly BlindableSystem _blindable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BeingDisposedComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<BeingDisposedComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<BeingDisposedComponent, CanSeeAttemptEvent>(OnCanSee);
    }

    private void OnStartup(Entity<BeingDisposedComponent> ent, ref ComponentStartup args)
    {
        // Это здесь, так как АПИ тела вызывает NRE при отсутствии глаз у выбранного ентити. Крутая система
        // TODO: Портировать фикс NRE с фаера
        if (!HasComp<BlindableComponent>(ent))
            return;

        _blindable.UpdateIsBlind(ent.Owner);
    }

    private void OnRemove(Entity<BeingDisposedComponent> ent, ref ComponentRemove args)
    {
        if (!HasComp<BlindableComponent>(ent))
            return;

        _blindable.UpdateIsBlind(ent.Owner);
    }

    private void OnCanSee(Entity<BeingDisposedComponent> ent, ref CanSeeAttemptEvent args)
    {
        if (!HasComp<BlindableComponent>(ent))
            return;

        if (CanSeeInTube(ent))
            return;

        args.Cancel();
    }

    private bool CanSeeInTube(EntityUid uid)
    {
        if (!HasComp<VentCrawlerComponent>(uid))
            return false;

        if (HasComp<HumanoidAppearanceComponent>(uid))
            return false;

        return true;
    }
}
