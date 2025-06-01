using System.Linq;
using System.Numerics;
using Content.Server.Shuttles.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Threading;

namespace Content.Server.Shuttles.Systems;

public sealed partial class DockingSystem
{
    /*
     * Handles the shuttle side of FTL docking.
     */

    private const int DockRoundingDigits = 2;

    public Angle GetAngle(EntityUid uid, TransformComponent xform, EntityUid targetUid, TransformComponent targetXform)
    {
        var (shuttlePos, shuttleRot) = _transform.GetWorldPositionRotation(xform);
        var (targetPos, targetRot) = _transform.GetWorldPositionRotation(targetXform);

        var shuttleCOM = Robust.Shared.Physics.Transform.Mul(new Transform(shuttlePos, shuttleRot),
            _physicsQuery.GetComponent(uid).LocalCenter);
        var targetCOM = Robust.Shared.Physics.Transform.Mul(new Transform(targetPos, targetRot),
            _physicsQuery.GetComponent(targetUid).LocalCenter);

        var mapDiff = shuttleCOM - targetCOM;
        var angle = mapDiff.ToWorldAngle();
        angle -= targetRot;
        return angle;
    }

    /// <summary>
    /// Checks if 2 docks can be connected by moving the shuttle directly onto docks.
    /// </summary>
    private bool CanDock(
        DockingComponent shuttleDock,
        TransformComponent shuttleDockXform,
        DockingComponent gridDock,
        TransformComponent gridDockXform,
        Box2 shuttleAABB,
        Angle targetGridRotation,
        FixturesComponent shuttleFixtures,
        Entity<MapGridComponent> gridEntity,
        bool isMap,
        bool ignored,
        out Matrix3x2 matty,
        out Box2 shuttleDockedAABB,
        out Angle gridRotation)
    {
        shuttleDockedAABB = Box2.UnitCentered;
        gridRotation = Angle.Zero;
        matty = Matrix3x2.Identity;

        if (shuttleDock.Docked ||
            gridDock.Docked ||
            !shuttleDockXform.Anchored ||
            !gridDockXform.Anchored)
        {
            if (!ignored)
                return false;
        }

        // First, get the station dock's position relative to the shuttle, this is where we rotate it around
        var stationDockPos = shuttleDockXform.LocalPosition +
                             shuttleDockXform.LocalRotation.RotateVec(new Vector2(0f, -1f));

        // Need to invert the grid's angle.
        var shuttleDockAngle = shuttleDockXform.LocalRotation;
        var gridDockAngle = gridDockXform.LocalRotation.Opposite();
        var offsetAngle = gridDockAngle - shuttleDockAngle;

        var stationDockMatrix = Matrix3Helpers.CreateInverseTransform(stationDockPos, shuttleDockAngle);
        var gridXformMatrix = Matrix3Helpers.CreateTransform(gridDockXform.LocalPosition, gridDockAngle);
        matty = Matrix3x2.Multiply(stationDockMatrix, gridXformMatrix);

        if (!ignored && !ValidSpawn(gridEntity, matty, offsetAngle, shuttleFixtures, isMap))
            return false;

        shuttleDockedAABB = matty.TransformBox(shuttleAABB);
        gridRotation = offsetAngle.Reduced();
        return true;
    }

    /// <summary>
    /// Gets docking config between 2 specific docks.
    /// </summary>
    public DockingConfig? GetDockingConfig(
        EntityUid shuttleUid,
        EntityUid targetGrid,
        EntityUid shuttleDockUid,
        DockingComponent shuttleDock,
        EntityUid gridDockUid,
        DockingComponent gridDock,
        bool ignored = false)
    {
        var shuttleDocks = new List<Entity<DockingComponent>>(1)
       {
           (shuttleDockUid, shuttleDock)
       };

        var gridDocks = new List<Entity<DockingComponent>>(1)
       {
           (gridDockUid, gridDock)
       };

        return GetDockingConfigPrivate(shuttleUid, targetGrid, shuttleDocks, gridDocks, ignored: ignored);
    }

    /// <summary>
    /// Tries to get a valid docking configuration for the shuttle to the target grid.
    /// </summary>
    /// <param name="priorityTag">Priority docking tag to prefer, e.g. for emergency shuttle</param>
    public DockingConfig? GetDockingConfig(EntityUid shuttleUid, EntityUid targetGrid, string? priorityTag = null, bool ignored = false)
    {
        var gridDocks = GetDocks(targetGrid);
        var shuttleDocks = GetDocks(shuttleUid);

        return GetDockingConfigPrivate(shuttleUid, targetGrid, shuttleDocks, gridDocks, priorityTag, ignored: ignored);
    }

    /// <summary>
    /// Tries to get a docking config at the specified coordinates and angle.
    /// </summary>
    public DockingConfig? GetDockingConfigAt(EntityUid shuttleUid,
        EntityUid targetGrid,
        EntityCoordinates coordinates,
        Angle angle,
        bool fallback = true,
        string? priorityTag = null) // Sunrise-Edit
    {
        var gridDocks = GetDocks(targetGrid);
        var shuttleDocks = GetDocks(shuttleUid);

        var configs = GetDockingConfigs(shuttleUid, targetGrid, shuttleDocks, gridDocks, priorityTag); // Sunrise-Edit

        foreach (var config in configs)
        {
            if (config.Coordinates.Equals(coordinates) && config.Angle.EqualsApprox(angle, 0.15))
            {
                return config;
            }
        }

        if (fallback && configs.Count > 0)
        {
            return configs.First();
        }

        return null;
    }

    /// <summary>
    /// Gets all docking configs between the 2 grids.
    /// </summary>
    private List<DockingConfig> GetDockingConfigs(
        EntityUid shuttleUid,
        EntityUid targetGrid,
        List<Entity<DockingComponent>> shuttleDocks,
        List<Entity<DockingComponent>> gridDocks,
        string? priorityTag = null, // Sunrise-Edit
        bool ignored = false)
    {
        var stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();

        var validDockConfigs = new ConcurrentBag<DockingConfig>();
        var totalIterations = 0;
        var validIterations = 0;

        if (gridDocks.Count <= 0)
            return validDockConfigs.ToList();

        if (!string.IsNullOrEmpty(priorityTag))
        {
            var filteredGridDocks = gridDocks
                .Where(d => TryComp<PriorityDockComponent>(d.Owner, out var prio) && prio.Tag == priorityTag)
                .ToList();

            if (filteredGridDocks.Count == 0)
            {
                _logger!.Info($"No docks found with priority tag {priorityTag}, falling back to regular docks");
            }
            else
            {
                gridDocks = filteredGridDocks;
            }
        }

        var targetGridGrid = _gridQuery.GetComponent(targetGrid);
        var targetGridXform = _xformQuery.GetComponent(targetGrid);
        var targetGridAngle = _transform.GetWorldRotation(targetGridXform).Reduced();
        var shuttleFixturesComp = Comp<FixturesComponent>(shuttleUid);
        var shuttleAABB = _gridQuery.GetComponent(shuttleUid).LocalAABB;

        var isMap = HasComp<MapComponent>(targetGrid);

        var dockCache = new ConcurrentDictionary<(EntityUid, EntityUid), (bool Success, Matrix3x2 Matty, Box2 AABB, Angle Angle)>();

        if (shuttleDocks.Count > 0)
        {
            Parallel.ForEach(shuttleDocks, (shuttleDockEntity) =>
            {
                var (dockUid, shuttleDock) = shuttleDockEntity;

                if (!Exists(dockUid) || !TryComp<DockingComponent>(dockUid, out var dockComp))
                    return;

                var shuttleDockXform = _xformQuery.GetComponent(dockUid);

                foreach (var (gridDockUid, gridDock) in gridDocks)
                {
                    if (!Exists(gridDockUid) || !TryComp<DockingComponent>(gridDockUid, out var gridDockComp))
                        continue;

                    Interlocked.Increment(ref totalIterations);
                    var gridXform = _xformQuery.GetComponent(gridDockUid);

                    if (!dockCache.TryGetValue((dockUid, gridDockUid), out var cacheResult))
                    {
                        if (!CanDock(
                                dockComp, shuttleDockXform,
                                gridDockComp, gridXform,
                                shuttleAABB,
                                targetGridAngle,
                                shuttleFixturesComp,
                                (targetGrid, targetGridGrid),
                                isMap,
                                ignored,
                                out var matty,
                                out var dockedAABB,
                                out var targetAngle))
                        {
                            dockCache.TryAdd((dockUid, gridDockUid), (false, default, default, default));
                            continue;
                        }

                        cacheResult = (true, matty, dockedAABB, targetAngle);
                        dockCache.TryAdd((dockUid, gridDockUid), cacheResult);
                    }
                    else if (!cacheResult.Success)
                    {
                        continue;
                    }

                    Interlocked.Increment(ref validIterations);

                    var (_, cacheMatty, cacheDockedAABB, cacheTargetAngle) = cacheResult;

                    // Can't just use the AABB as we want to get bounds as tight as possible.
                    var gridPosition = new EntityCoordinates(targetGrid, Vector2.Transform(Vector2.Zero, cacheMatty));
                    var spawnPosition = new EntityCoordinates(targetGridXform.MapUid!.Value, _transform.ToMapCoordinates(gridPosition).Position);

                    var dockedBounds = new Box2Rotated(shuttleAABB.Translated(spawnPosition.Position), cacheTargetAngle, spawnPosition.Position);

                    var grids = new List<Entity<MapGridComponent>>();
                    _mapManager.FindGridsIntersecting(targetGridXform.MapID, dockedBounds, ref grids, includeMap: false);
                    if (grids.Any(o => o.Owner != targetGrid && o.Owner != targetGridXform.MapUid) && !ignored)
                    {
                        continue;
                    }

                    var dockedPorts = new List<(EntityUid DockAUid, EntityUid DockBUid, DockingComponent DockA, DockingComponent DockB)>()
                    {
                        (dockUid, gridDockUid, dockComp, gridDockComp),
                    };

                    cacheDockedAABB = cacheDockedAABB.Rounded(DockRoundingDigits);

                    foreach (var (otherUid, other) in shuttleDocks)
                    {
                        if (other == shuttleDock)
                            continue;

                        if (!Exists(otherUid) || !TryComp<DockingComponent>(otherUid, out var otherDockComp))
                            continue;

                        foreach (var (otherGridUid, otherGrid) in gridDocks)
                        {
                            if (otherGrid == gridDock)
                                continue;

                            if (!Exists(otherGridUid) || !TryComp<DockingComponent>(otherGridUid, out var otherGridDockComp))
                                continue;

                            Interlocked.Increment(ref totalIterations);

                            if (!dockCache.TryGetValue((otherUid, otherGridUid), out var otherCacheResult))
                            {
                                if (!CanDock(
                                        otherDockComp,
                                        _xformQuery.GetComponent(otherUid),
                                        otherGridDockComp,
                                        _xformQuery.GetComponent(otherGridUid),
                                        shuttleAABB,
                                        targetGridAngle,
                                        shuttleFixturesComp,
                                        (targetGrid, targetGridGrid),
                                        isMap,
                                        ignored,
                                        out var otherMatty,
                                        out var otherDockedAABB,
                                        out var otherTargetAngle))
                                {
                                    dockCache.TryAdd((otherUid, otherGridUid), (false, default, default, default));
                                    continue;
                                }

                                otherCacheResult = (true, otherMatty, otherDockedAABB, otherTargetAngle);
                                dockCache.TryAdd((otherUid, otherGridUid), otherCacheResult);
                            }
                            else if (!otherCacheResult.Success)
                            {
                                continue;
                            }

                            Interlocked.Increment(ref validIterations);

                            var (_, _, cacheOtherDockedAABB, cacheOtherTargetAngle) = otherCacheResult;
                            cacheOtherDockedAABB = cacheOtherDockedAABB.Rounded(DockRoundingDigits);

                            if (!cacheTargetAngle.Equals(cacheOtherTargetAngle) ||
                                !cacheDockedAABB.Equals(cacheOtherDockedAABB))
                            {
                                continue;
                            }

                            dockedPorts.Add((otherUid, otherGridUid, otherDockComp, otherGridDockComp));
                        }
                    }

                    validDockConfigs.Add(new DockingConfig()
                    {
                        Docks = dockedPorts,
                        Coordinates = gridPosition,
                        Area = cacheDockedAABB,
                        Angle = cacheTargetAngle,
                    });
                }
            });
        }

        stopwatch.Stop();
        _logger!.Info($"GetDockingConfigs completed in {stopwatch.ElapsedMilliseconds}ms. Total iterations: {totalIterations}, Valid iterations: {validIterations}, Priority tag: {priorityTag}");

        return validDockConfigs.ToList();
    }

    private DockingConfig? GetDockingConfigPrivate(
        EntityUid shuttleUid,
        EntityUid targetGrid,
        List<Entity<DockingComponent>> shuttleDocks,
        List<Entity<DockingComponent>> gridDocks,
        string? priorityTag = null,
        bool ignored = false)
    {
        var validDockConfigs = GetDockingConfigs(shuttleUid, targetGrid, shuttleDocks, gridDocks, priorityTag, ignored); // Sunrise-Edit

        if (validDockConfigs.Count <= 0)
            return null;

        var targetGridAngle = _transform.GetWorldRotation(targetGrid).Reduced();

        // Prioritise by priority docks, then by maximum connected ports, then by most similar angle.
        validDockConfigs = validDockConfigs
           .OrderByDescending(x => IsConfigPriority(x, priorityTag))
           .ThenByDescending(x => x.Docks.Count)
           .ThenBy(x => Math.Abs(Angle.ShortestDistance(x.Angle.Reduced(), targetGridAngle).Theta)).ToList();

        var location = validDockConfigs.First();
        location.TargetGrid = targetGrid;
        // TODO: Ideally do a hyperspace warpin, just have it run on like a 10 second timer.

        return location;
    }

    public bool IsConfigPriority(DockingConfig config, string? priorityTag)
    {
        return config.Docks.Any(docks =>
            TryComp<PriorityDockComponent>(docks.DockBUid, out var priority)
            && priority.Tag?.Equals(priorityTag) == true);
    }

    /// <summary>
    /// Checks whether the shuttle can warp to the specified position.
    /// </summary>
    private bool ValidSpawn(Entity<MapGridComponent> gridEntity, Matrix3x2 matty, Angle angle, FixturesComponent shuttleFixturesComp, bool isMap)
    {
        var transform = new Transform(Vector2.Transform(Vector2.Zero, matty), angle);

        // Because some docking bounds are tight af need to check each chunk individually
        foreach (var fix in shuttleFixturesComp.Fixtures.Values)
        {
            var polyShape = (PolygonShape)fix.Shape;
            var aabb = polyShape.ComputeAABB(transform, 0);
            aabb = aabb.Enlarged(-0.01f);

            // If it's a map check no hard collidable anchored entities overlap
            if (isMap)
            {
                var localTiles = _mapSystem.GetLocalTilesEnumerator(gridEntity.Owner, gridEntity.Comp, aabb);

                while (localTiles.MoveNext(out var tile))
                {
                    var anchoredEnumerator = _mapSystem.GetAnchoredEntitiesEnumerator(gridEntity.Owner, gridEntity.Comp, tile.GridIndices);

                    while (anchoredEnumerator.MoveNext(out var anc))
                    {
                        if (!_physicsQuery.TryGetComponent(anc, out var physics) ||
                            !physics.CanCollide ||
                            !physics.Hard)
                        {
                            continue;
                        }

                        return false;
                    }
                }
            }
            // If it's not a map check it doesn't overlap the grid.
            else
            {
                if (_mapSystem.GetLocalTilesIntersecting(gridEntity.Owner, gridEntity.Comp, aabb).Any())
                    return false;
            }
        }

        return true;
    }

    public List<Entity<DockingComponent>> GetDocks(EntityUid uid)
    {
        _dockingSet.Clear();
        _lookup.GetChildEntities(uid, _dockingSet);

        return _dockingSet.ToList();
    }
}
