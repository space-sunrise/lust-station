using Robust.Shared.Utility;

namespace Content.Server._Sunrise.RoundStartFtl;

/// <summary>
/// Spawns a list of grids and docks them to the parent station on round start.
/// </summary>
[RegisterComponent]
public sealed partial class SpawnAdditionalGridsAndDockToStationComponent : Component
{
    [DataField("spawns", required: true)]
    public List<GridSpawnEntry> Spawns = new();
}

/// <summary>
/// Defines a single grid to be spawned.
/// </summary>
[DataDefinition]
public sealed partial class GridSpawnEntry
{
    [DataField("gridPath", required: true)]
    public ResPath GridPath { get; private set; }

    [DataField("priorityTag", required: true)]
    public string PriorityTag { get; private set; } = string.Empty;
}
