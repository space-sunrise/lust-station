using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Content.Shared._Sunrise.SunriseCCVars;
using Robust.Server.ServerStatus;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

namespace Content.Server._Sunrise.MapperSync;

public sealed class MapperSyncSystem : EntitySystem
{
    [Dependency] private readonly IStatusHost _statusHost = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IResourceManager _res = default!;

    // Client state
    private readonly HttpClient _httpClient = new();
    private List<string> _cachedRemoteMaps = new();
    private DateTime _lastFetchTime = DateTime.MinValue;

    public override void Initialize()
    {
        base.Initialize();

        _statusHost.AddHandler(HandleMapRequest);
    }

    /// <summary>
    /// Gets the list of available remote maps. Refreshes the cache if older than 1 minute.
    /// </summary>
    public IReadOnlyList<string> GetRemoteMaps()
    {
        // Fire and forget fetch if we need a refresh
        if (DateTime.UtcNow - _lastFetchTime > TimeSpan.FromMinutes(1))
        {
            _lastFetchTime = DateTime.UtcNow; // Prevent spamming
            _ = FetchMapsAsync();
        }

        return _cachedRemoteMaps;
    }

    /// <summary>
    /// Manually refresh the remote maps list.
    /// </summary>
    public async Task<bool> FetchMapsAsync()
    {
        var url = _cfg.GetCVar(SunriseCCVars.MapperSyncServerUrl);
        var token = _cfg.GetCVar(SunriseCCVars.MapperSyncApiToken);

        if (string.IsNullOrWhiteSpace(url))
            return false;

        try
        {
            var req = new HttpRequestMessage(HttpMethod.Get, $"{url.TrimEnd('/')}/mappersync/list");
            if (!string.IsNullOrWhiteSpace(token))
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var res = await _httpClient.SendAsync(req);

            if (res.IsSuccessStatusCode)
            {
                var json = await res.Content.ReadAsStringAsync();
                var list = JsonSerializer.Deserialize<List<string>>(json);
                if (list != null)
                {
                    _cachedRemoteMaps = list;
                    return true;
                }
            }
            else
            {
                Log.Warning($"Failed to fetch remote maps. Status Code: {res.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Exception while fetching remote maps: {ex}");
        }

        return false;
    }

    /// <summary>
    /// Downloads a remote map and saves it locally.
    /// </summary>
    public async Task<(bool Success, string Error)> DownloadMapAsync(string mapPath)
    {
        var url = _cfg.GetCVar(SunriseCCVars.MapperSyncServerUrl);
        var token = _cfg.GetCVar(SunriseCCVars.MapperSyncApiToken);

        if (string.IsNullOrWhiteSpace(url))
            return (false, "Mapper Sync Server URL is not configured (sunrise.mapper_sync.server_url).");

        try
        {
            var queryPath = Uri.EscapeDataString(mapPath);
            var req = new HttpRequestMessage(HttpMethod.Get, $"{url.TrimEnd('/')}/mappersync/download?path={queryPath}");
            if (!string.IsNullOrWhiteSpace(token))
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var res = await _httpClient.SendAsync(req);

            if (res.IsSuccessStatusCode)
            {
                var contentBytes = await res.Content.ReadAsByteArrayAsync();

                // Ensure it ends with .yml
                if (!mapPath.EndsWith(".yml", StringComparison.OrdinalIgnoreCase))
                    mapPath += ".yml";

                // Construct target respath
                var targetPath = new ResPath(mapPath);

                // Needs to be rooted to /Maps/
                if (!targetPath.ToString().StartsWith("/Maps/", StringComparison.OrdinalIgnoreCase))
                {
                    targetPath = new ResPath("/Maps") / targetPath;
                }

                // Ensure directory exists
                var dir = targetPath.Directory;
                _res.UserData.CreateDir(dir);

                // Write file
                using var stream = _res.UserData.OpenWrite(targetPath);
                await stream.WriteAsync(contentBytes, 0, contentBytes.Length);
                await stream.FlushAsync();

                return (true, string.Empty);
            }

            return (false, $"HTTP Error: {res.StatusCode}");
        }
        catch (Exception ex)
        {
            Log.Error($"Exception while downloading remote map: {ex}");
            return (false, $"Exception: {ex.Message}");
        }
    }

    // --- SERVER SIDE HANDLERS ---

    private async Task<bool> HandleMapRequest(IStatusHandlerContext context)
    {
        if (!context.Url.AbsolutePath.StartsWith("/mappersync/"))
            return false;

        // Ensure expose is enabled
        if (!_cfg.GetCVar(SunriseCCVars.MapperSyncExposeMaps))
        {
            await context.RespondErrorAsync(HttpStatusCode.Forbidden);
            return true;
        }

        // Authenticate
        var token = _cfg.GetCVar(SunriseCCVars.MapperSyncApiToken);
        if (string.IsNullOrWhiteSpace(token) ||
            !context.RequestHeaders.TryGetValue("Authorization", out var authValues) ||
            authValues.Count == 0 ||
            authValues[0] != $"Bearer {token}")
        {
            Log.Warning($"Unauthorized mappersync request from {context.RemoteEndPoint}");
            await context.RespondErrorAsync(HttpStatusCode.Unauthorized);
            return true;
        }

        if (context.Url.AbsolutePath == "/mappersync/list" && context.IsGetLike)
        {
            await HandleList(context);
            return true;
        }

        if (context.Url.AbsolutePath == "/mappersync/download" && context.IsGetLike)
        {
            await HandleDownload(context);
            return true;
        }

        return false;
    }

    private async Task HandleList(IStatusHandlerContext context)
    {
        var mapDir = new ResPath("/Maps/");
        var maps = new List<string>();

        if (_res.UserData.Exists(mapDir))
        {
            // Recursively find .yml files
            FindMaps(_res.UserData, mapDir, maps);
        }

        await context.RespondJsonAsync(maps);
    }

    private void FindMaps(IWritableDirProvider dirProvider, ResPath currentPath, List<string> maps)
    {
        if (!dirProvider.Exists(currentPath))
            return;

        foreach (var filename in dirProvider.DirectoryEntries(currentPath))
        {
            var path = currentPath / filename;
            if (dirProvider.IsDir(path))
            {
                FindMaps(dirProvider, path, maps);
            }
            else if (filename.EndsWith(".yml", StringComparison.OrdinalIgnoreCase))
            {
                // Remove leading / to make it e.g. "Maps/test.yml"
                var strPath = path.ToString();
                if (strPath.StartsWith("/"))
                    strPath = strPath.Substring(1);
                maps.Add(strPath);
            }
        }
    }

    private async Task HandleDownload(IStatusHandlerContext context)
    {
        var mapPathStr = "";
        var query = context.Url.Query;
        if (query.StartsWith("?")) query = query.Substring(1);
        var args = query.Split('&');
        foreach (var arg in args)
        {
            if (arg.StartsWith("path="))
                mapPathStr = Uri.UnescapeDataString(arg.Substring(5));
        }

        if (string.IsNullOrWhiteSpace(mapPathStr))
        {
            await context.RespondErrorAsync(HttpStatusCode.BadRequest);
            return;
        }

        // Validate that it ends with .yml
        if (!mapPathStr.EndsWith(".yml", StringComparison.OrdinalIgnoreCase))
        {
            await context.RespondErrorAsync(HttpStatusCode.Forbidden);
            return;
        }

        // Security check for directory traversal
        if (mapPathStr.Contains(".."))
        {
            await context.RespondErrorAsync(HttpStatusCode.Forbidden);
            return;
        }

        var resPath = new ResPath(mapPathStr).ToRootedPath();

        // Extra check: must start with /Maps/
        if (!resPath.ToString().StartsWith("/Maps/", StringComparison.OrdinalIgnoreCase))
        {
            await context.RespondErrorAsync(HttpStatusCode.Forbidden);
            return;
        }

        if (!_res.UserData.Exists(resPath))
        {
            await context.RespondErrorAsync(HttpStatusCode.NotFound);
            return;
        }

        try
        {
            // Read fully into memory to avoid blocking stream reading if necessary,
            // or just use respond stream. Note: RespondStreamAsync is better.
            using var fileStream = _res.UserData.OpenRead(resPath);
            var responseStream = await context.RespondStreamAsync(HttpStatusCode.OK);
            await fileStream.CopyToAsync(responseStream);
            await responseStream.FlushAsync();
        }
        catch (Exception ex)
        {
            Log.Error($"Error downloading map {resPath}: {ex}");
            await context.RespondErrorAsync(HttpStatusCode.InternalServerError);
        }
    }
}
