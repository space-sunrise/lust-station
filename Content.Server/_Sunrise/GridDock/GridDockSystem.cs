using System.Numerics;
using Content.Server._Sunrise.RoundStartFtl;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Components;
using Content.Server.Station.Events;
using Content.Server.Station.Systems;
using Robust.Shared.EntitySerialization.Systems;

namespace Content.Server._Sunrise.GridDock;

public sealed class GridDockSystem : EntitySystem
{
    [Dependency] private readonly MapLoaderSystem _loader = default!;
    [Dependency] private readonly ShuttleSystem _shuttles = default!;
    [Dependency] private readonly StationSystem _station = default!;
    private Vector2 _nextSpawnOffset;
    private const float GridSeparation = 300f;
    // Fish-end

    public override void Initialize()
    {
        _nextSpawnOffset = new Vector2(500, 500); // Fish-edit
        SubscribeLocalEvent<SpawnGridAndDockToStationComponent, StationPostInitEvent>(OnStationPostInit);
        SubscribeLocalEvent<SpawnAdditionalGridsAndDockToStationComponent, StationPostInitEvent>(OnStationPostInitMultiple); // Fish-edit
    }

    private void OnStationPostInit(EntityUid uid, SpawnGridAndDockToStationComponent component, StationPostInitEvent args)
    {
        if (component.GridPath is null)
            return;

        var ftlMap = _shuttles.EnsureFTLMap();
        var xformMap = Transform(ftlMap);
        // Fish-start
        var spawnPosition = _nextSpawnOffset;
        _nextSpawnOffset.X += GridSeparation;
        // Fish-end
        if (!_loader.TryLoadGrid(xformMap.MapID,
                component.GridPath.Value,
                out var rootUid,
                offset: spawnPosition)) // Fish-edit
            return;

        // Fish-start
        if (rootUid == null)
            return;

        if (!TryComp<ShuttleComponent>(rootUid.Value, out var shuttleComp))
            // Fish-end
            return;

        var target = _station.GetLargestGrid(Comp<StationDataComponent>(uid));

        if (target == null)
        {
            Log.Error($"GridDockSystem: No target station grid found for {ToPrettyString(uid)}"); // Fish-edit
            return;
        }

        _shuttles.FTLToDock(
            rootUid.Value, // Fish-edit
            shuttleComp,
            target.Value,
            5f,
            5f,
            priorityTag: component.PriorityTag,
            ignored: true);
    }

    // Fish-start
    private void OnStationPostInitMultiple(EntityUid uid, SpawnAdditionalGridsAndDockToStationComponent component, StationPostInitEvent args)
    {
        var target = _station.GetLargestGrid(Comp<StationDataComponent>(uid));
        if (target == null)
        {
            Log.Error($"GridDockSystem: No target station grid found for {ToPrettyString(uid)}. Aborting.");
            return;
        }

        var ftlMap = _shuttles.EnsureFTLMap();
        var xformMap = Transform(ftlMap);

        foreach (var spawnEntry in component.Spawns)
        {
            var spawnPosition = _nextSpawnOffset;
            _nextSpawnOffset.X += GridSeparation;

            if (!_loader.TryLoadGrid(xformMap.MapID,
                    spawnEntry.GridPath,
                    out var gridUid,
                    offset: spawnPosition))
            {
                Log.Warning($"Failed to load grid from path: {spawnEntry.GridPath}");
                continue;
            }

            if (gridUid == null)
            {
                Log.Warning($"Loaded grid from {spawnEntry.GridPath}, but it was empty.");
                continue;
            }

            if (!TryComp<ShuttleComponent>(gridUid.Value, out var shuttleComp))
            {
                Log.Warning($"Spawned grid {ToPrettyString(gridUid.Value)} from {spawnEntry.GridPath} has no ShuttleComponent. Skipping docking.");
                continue;
            }

            _shuttles.FTLToDock(
                gridUid.Value,
                shuttleComp,
                target.Value,
                5f,
                5f,
                priorityTag: spawnEntry.PriorityTag,
                ignored: true);
        }
    }
    // Fish-end
}
