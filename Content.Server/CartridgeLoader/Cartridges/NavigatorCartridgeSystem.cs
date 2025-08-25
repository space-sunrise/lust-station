using Content.Server.Station.Systems;
using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.Station.Components;
using Robust.Shared.Map;
using System.Linq;

namespace Content.Server.CartridgeLoader.Cartridges;

public sealed class NavigatorCartridgeSystem : EntitySystem
{
    [Dependency] private readonly CartridgeLoaderSystem _cartridgeLoader = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NavigatorCartridgeComponent, CartridgeMessageEvent>(OnUiMessage);
        SubscribeLocalEvent<NavigatorCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
    }

    /// <summary>
    /// The ui messages received here get wrapped by a CartridgeMessageEvent and are relayed from the <see cref="CartridgeLoaderSystem"/>
    /// </summary>
    /// <remarks>
    /// The cartridge specific ui message event needs to inherit from the CartridgeMessageEvent
    /// </remarks>
    private void OnUiMessage(EntityUid uid, NavigatorCartridgeComponent component, CartridgeMessageEvent args)
    {
        UpdateUiState(uid, GetEntity(args.LoaderUid), component);
    }

    /// <summary>
    /// This gets called when the ui fragment needs to be updated for the first time after activating
    /// </summary>
    private void OnUiReady(EntityUid uid, NavigatorCartridgeComponent component, CartridgeUiReadyEvent args)
    {
        UpdateUiState(uid, args.Loader, component);
    }

    private void UpdateUiState(EntityUid uid, EntityUid loaderUid, NavigatorCartridgeComponent? component)
    {
        if (!Resolve(uid, ref component))
            return;

        var owningStation = _stationSystem.GetOwningStation(loaderUid);
        var stationName = "Unknown Station";
        NetEntity? mapUid = null;

        if (owningStation != null && TryComp<MetaDataComponent>(owningStation.Value, out var metaData))
        {
            stationName = metaData.EntityName;
            
            // Try to get the station's primary grid for the map
            if (TryComp<StationDataComponent>(owningStation.Value, out var stationData) && stationData.Grids.Count > 0)
            {
                // Get the first grid as the map reference and convert to NetEntity
                mapUid = GetNetEntity(stationData.Grids.First());
            }
        }

        var state = new NavigatorUiState(mapUid, stationName);
        _cartridgeLoader.UpdateCartridgeUiState(loaderUid, state);
    }
}