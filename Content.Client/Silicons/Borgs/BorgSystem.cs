using Content.Client.Toggleable;
using Content.Shared.Alert;
using Content.Shared.Mobs;
using Content.Shared.Power.EntitySystems;
using Content.Shared.PowerCell;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Silicons.Borgs.Components;
// Lust edit start - borg rest visuals
using Content.Shared._Lust.Borgs;
// Lust edit end
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Client.Silicons.Borgs;

/// <inheritdoc/>
public sealed partial class BorgSystem : SharedBorgSystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly SharedBatterySystem _battery = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    public override void Initialize()
    {
        base.Initialize();

        InitializeBattery();

        // Lust edit - must run after ToggleableVisualsSystem to override LightStatus when resting
        // Both subscriptions to AppearanceChangeEvent must use the same ordering constraints (engine requirement)
        SubscribeLocalEvent<BorgChassisComponent, AppearanceChangeEvent>(OnBorgAppearanceChanged, after: [typeof(ToggleableVisualsSystem)]);
        SubscribeLocalEvent<MMIComponent, AppearanceChangeEvent>(OnMMIAppearanceChanged, after: [typeof(ToggleableVisualsSystem)]);
        // Lust edit start - borg rest visuals
        SubscribeLocalEvent<BorgRestComponent, AfterAutoHandleStateEvent>(OnBorgRestStateChanged);
        // Lust edit end
    }

    public override void UpdateUI(Entity<BorgChassisComponent?> chassis)
    {
        if (_ui.TryGetOpenUi(chassis.Owner, BorgUiKey.Key, out var bui))
            bui.Update();
    }

    private void OnBorgAppearanceChanged(Entity<BorgChassisComponent> chassis, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        UpdateBorgAppearance((chassis.Owner, chassis.Comp, args.Component, args.Sprite));
    }

    protected override void OnInserted(Entity<BorgChassisComponent> chassis, ref EntInsertedIntoContainerMessage args)
    {
        if (!chassis.Comp.Initialized)
            return;

        base.OnInserted(chassis, ref args);
        UpdateUI(chassis.AsNullable());
        UpdateBorgAppearance((chassis, chassis.Comp));
        UpdateBatteryAlert((chassis.Owner, chassis.Comp, null));
    }

    protected override void OnRemoved(Entity<BorgChassisComponent> chassis, ref EntRemovedFromContainerMessage args)
    {
        if (!chassis.Comp.Initialized)
            return;

        base.OnRemoved(chassis, ref args);
        UpdateUI(chassis.AsNullable());
        UpdateBorgAppearance((chassis, chassis.Comp));
        UpdateBatteryAlert((chassis.Owner, chassis.Comp, null));
    }

    private void UpdateBorgAppearance(Entity<BorgChassisComponent?, AppearanceComponent?, SpriteComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp1, ref ent.Comp2, ref ent.Comp3))
            return;

        // Lust edit start - borg rest/wrecked visual states
        var isResting = TryComp<BorgRestComponent>(ent, out var borgRest) && borgRest.IsResting;

        if (_appearance.TryGetData<MobState>(ent.Owner, MobStateVisuals.State, out var state, ent.Comp2))
        {
            if (state != MobState.Alive)
            {
                _sprite.LayerSetVisible((ent.Owner, ent.Comp3), BorgVisualLayers.Light, false);
                SetRestLayerSafe(ent, BorgVisualLayers.Body, false);
                SetRestLayerSafe(ent, BorgVisualLayers.LightStatus, false);
                SetRestLayerSafe(ent, BorgVisualLayers.Resting, false);
                SetRestLayerSafe(ent, BorgVisualLayers.Wrecked, true);
                return;
            }
        }

        SetRestLayerSafe(ent, BorgVisualLayers.Wrecked, false);
        SetRestLayerSafe(ent, BorgVisualLayers.Body, !isResting);
        SetRestLayerSafe(ent, BorgVisualLayers.Resting, isResting);
        // Hide flashlight sprite when resting (ToggleableVisualsSystem already ran before us)
        if (isResting)
            SetRestLayerSafe(ent, BorgVisualLayers.LightStatus, false);
        // Lust edit end

        if (!_appearance.TryGetData<bool>(ent.Owner, BorgVisuals.HasPlayer, out var hasPlayer, ent.Comp2))
            hasPlayer = false;

        // Lust edit start - hide mind-state light when resting
        if (isResting)
        {
            _sprite.LayerSetVisible((ent.Owner, ent.Comp3), BorgVisualLayers.Light, false);
            return;
        }
        // Lust edit end

        _sprite.LayerSetVisible((ent.Owner, ent.Comp3), BorgVisualLayers.Light, ent.Comp1.BrainEntity != null || hasPlayer);
        _sprite.LayerSetRsiState((ent.Owner, ent.Comp3), BorgVisualLayers.Light, hasPlayer ? ent.Comp1.HasMindState : ent.Comp1.NoMindState);
    }

    // Lust edit start - handler and helper for borg rest visuals
    private void OnBorgRestStateChanged(Entity<BorgRestComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (TryComp<AppearanceComponent>(ent, out var appearance))
            _appearance.QueueUpdate(ent, appearance);
    }

    private void SetRestLayerSafe(Entity<BorgChassisComponent?, AppearanceComponent?, SpriteComponent?> ent, Enum layer, bool visible)
    {
        if (ent.Comp3 is not { } sprite || !_sprite.LayerExists((ent.Owner, sprite), layer))
            return;

        _sprite.LayerSetVisible((ent.Owner, sprite), layer, visible);
    }
    // Lust edit end

    private void OnMMIAppearanceChanged(EntityUid uid, MMIComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;
        var sprite = args.Sprite;

        if (!_appearance.TryGetData(uid, MMIVisuals.BrainPresent, out bool brain))
            brain = false;
        if (!_appearance.TryGetData(uid, MMIVisuals.HasMind, out bool hasMind))
            hasMind = false;

        _sprite.LayerSetVisible((uid, sprite), MMIVisualLayers.Brain, brain);
        if (!brain)
        {
            _sprite.LayerSetRsiState((uid, sprite), MMIVisualLayers.Base, component.NoBrainState);
        }
        else
        {
            var state = hasMind
                ? component.HasMindState
                : component.NoMindState;
            _sprite.LayerSetRsiState((uid, sprite), MMIVisualLayers.Base, state);
        }
    }

    /// <summary>
    /// Sets the sprite states used for the borg "is there a mind or not" indication.
    /// </summary>
    /// <param name="borg">The entity and component to modify.</param>
    /// <param name="hasMindState">The state to use if the borg has a mind.</param>
    /// <param name="noMindState">The state to use if the borg has no mind.</param>
    /// <seealso cref="BorgChassisComponent.HasMindState"/>
    /// <seealso cref="BorgChassisComponent.NoMindState"/>
    public void SetMindStates(Entity<BorgChassisComponent> borg, string hasMindState, string noMindState)
    {
        borg.Comp.HasMindState = hasMindState;
        borg.Comp.NoMindState = noMindState;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        UpdateBattery(frameTime);
    }
}
