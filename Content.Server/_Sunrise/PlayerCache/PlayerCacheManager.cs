// © SUNRISE, An EULA/CLA with a hosting restriction, full text: https://github.com/space-sunrise/space-station-14/blob/master/CLA.txt

using Robust.Shared.Network;
using System.Diagnostics.CodeAnalysis;
using Content.Shared._Sunrise.PlayerCache;

namespace Content.Server._Sunrise.PlayerCache;

public sealed class PlayerCacheManager
{
    [Dependency] private readonly IServerNetManager _netManager = default!;

    private readonly Dictionary<NetUserId, PlayerCacheData> _cache = new();

    public void Initialize()
    {
        _netManager.RegisterNetMessage<MsgPlayerCacheSync>(OnCacheSync);
        _netManager.RegisterNetMessage<MsgPlayerCacheRequest>();
        _netManager.Connected += OnConnected;
        _netManager.Disconnect += OnDisconnect;
    }

    private void OnConnected(object? sender, NetChannelArgs e)
    {
        var request = new MsgPlayerCacheRequest();
        e.Channel.SendMessage(request);
    }

    private void OnDisconnect(object? sender, NetDisconnectedArgs e)
    {
        _cache.Remove(e.Channel.UserId);
    }

    private void OnCacheSync(MsgPlayerCacheSync msg)
    {
        _cache[msg.MsgChannel.UserId] = msg.Cache;
    }

    public bool TryGetCache(NetUserId userId, [NotNullWhen(true)] out PlayerCacheData? data)
        => _cache.TryGetValue(userId, out data);

    public void SetCache(NetUserId userId, PlayerCacheData data)
        => _cache[userId] = data;

    public bool TryGetCachedGhostTheme(NetUserId userId, out string theme)
    {
        if (_cache.TryGetValue(userId, out var data) && !string.IsNullOrEmpty(data.GhostTheme))
        {
            theme = data.GhostTheme;
            return true;
        }
        theme = "";
        return false;
    }
    public bool TryGetCachedPet(NetUserId userId, out string pet)
    {
        if (_cache.TryGetValue(userId, out var data) && !string.IsNullOrEmpty(data.Pet))
        {
            pet = data.Pet;
            return true;
        }
        pet = "";
        return false;
    }
}
