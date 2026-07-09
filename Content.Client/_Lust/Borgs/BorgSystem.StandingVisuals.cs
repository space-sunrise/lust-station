using Content.Shared.Mobs;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Standing;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;

// Lust edit start - визуал сидения боргов в отдельном файле
#pragma warning disable IDE0130
namespace Content.Client.Silicons.Borgs;

public sealed partial class BorgSystem
{
    partial void UpdateLustBorgStandingVisuals(
        Entity<BorgChassisComponent?, AppearanceComponent?, SpriteComponent?> ent,
        ref bool handled)
    {
        if (!TryGetLayerState(ent, BorgVisualLayers.Body, out var bodyState))
            return;

        var alive = !_appearance.TryGetData<MobState>(ent.Owner, MobStateVisuals.State, out var mobState, ent.Comp2)
                    || mobState == MobState.Alive;
        var resting = alive
                      && TryComp<StandingStateComponent>(ent, out var standing)
                      && !standing.Standing;

        if (TrySetDerivedLayerState(ent, BorgVisualLayers.Resting, bodyState, "rest"))
            _sprite.LayerSetVisible((ent.Owner, ent.Comp3), BorgVisualLayers.Resting, resting);

        if (TrySetDerivedLayerState(ent, BorgVisualLayers.Wrecked, bodyState, "wreck"))
            _sprite.LayerSetVisible((ent.Owner, ent.Comp3), BorgVisualLayers.Wrecked, !alive);

        _sprite.LayerSetVisible((ent.Owner, ent.Comp3), BorgVisualLayers.Body, alive && !resting);

        if (alive && !resting)
            return;

        _sprite.LayerSetVisible((ent.Owner, ent.Comp3), BorgVisualLayers.Light, false);

        if (_sprite.LayerExists((ent.Owner, ent.Comp3), BorgVisualLayers.LightStatus))
            _sprite.LayerSetVisible((ent.Owner, ent.Comp3), BorgVisualLayers.LightStatus, false);

        handled = true;
    }

    private bool TryGetLayerState(
        Entity<BorgChassisComponent?, AppearanceComponent?, SpriteComponent?> ent,
        BorgVisualLayers layer,
        out string state)
    {
        state = string.Empty;

        if (!_sprite.LayerExists((ent.Owner, ent.Comp3), layer))
            return false;

        var stateId = _sprite.LayerGetRsiState((ent.Owner, ent.Comp3), layer, RSI.StateId.Invalid);
        if (!stateId.IsValid || stateId.Name == null)
            return false;

        state = stateId.Name;
        return true;
    }

    private bool TrySetDerivedLayerState(
        Entity<BorgChassisComponent?, AppearanceComponent?, SpriteComponent?> ent,
        BorgVisualLayers layer,
        string bodyState,
        string suffix)
    {
        if (!_sprite.LayerExists((ent.Owner, ent.Comp3), layer))
            return false;

        var state = $"{bodyState}_{suffix}";
        var rsi = _sprite.LayerGetEffectiveRsi((ent.Owner, ent.Comp3), layer, RSI.StateId.Invalid);
        if (rsi == null || !rsi.TryGetState(state, out _))
            return false;

        _sprite.LayerSetRsiState((ent.Owner, ent.Comp3), layer, state);
        return true;
    }
}
#pragma warning restore IDE0130
// Lust edit end
