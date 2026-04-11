using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Server.GameTicking;
using Content.Shared.Administration;
using Content.Shared.Administration.Logs;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.Eui;
using Microsoft.Extensions.ObjectPool;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using static Content.Shared.Administration.Logs.AdminLogsEuiMsg;

namespace Content.Server.Administration.Logs;

public sealed class AdminLogsEui : BaseEui
{
    [Dependency] private readonly IAdminLogManager _adminLogs = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly IEntityManager _e = default!;

    private readonly ISawmill _sawmill;

    private int _clientBatchSize;
    private bool _isLoading = true;
    private readonly Dictionary<Guid, string> _players = new();
    private int _roundLogs;
    private CancellationTokenSource _logSendCancellation = new();
    private LogFilter _filter;
    // Sunrise added start - guard metadata loading from stale async responses
    private int _loadFromDbRequestId;
    // Sunrise added end

    private readonly DefaultObjectPool<List<SharedAdminLog>> _adminLogListPool =
        new(new ListPolicy<SharedAdminLog>());

    public AdminLogsEui()
    {
        IoCManager.InjectDependencies(this);

        _sawmill = _logManager.GetSawmill(AdminLogManager.SawmillId);

        _configuration.OnValueChanged(CCVars.AdminLogsClientBatchSize, ClientBatchSizeChanged, true);

        _filter = new LogFilter
        {
            CancellationToken = _logSendCancellation.Token,
            // Sunrise added start - request one extra record and keep cursor on visible page edge
            LokiCursorOverfetch = 1,
            Limit = _clientBatchSize + 1
            // Sunrise added end
        };
    }

    private int CurrentRoundId => _e.System<GameTicker>().RoundId;

    // Sunrise added start - remove blocking metadata await from EUI open flow
    public override void Opened()
    // Sunrise added end
    {
        base.Opened();

        _adminManager.OnPermsChanged += OnPermsChanged;

        // Sunrise added start - normalize round selection and load metadata asynchronously
        var roundId = NormalizeRoundId(_filter.Round);
        _filter.Round = roundId;
        QueueLoadFromDb(roundId);
        // Sunrise added end
    }

    private void ClientBatchSizeChanged(int value)
    {
        _clientBatchSize = value;
    }

    private void OnPermsChanged(AdminPermsChangedEventArgs args)
    {
        if (args.Player == Player && !_adminManager.HasAdminFlag(Player, AdminFlags.Logs))
        {
            Close();
        }
    }

    public override EuiStateBase GetNewState()
    {
        if (_isLoading)
        {
            return new AdminLogsEuiState(CurrentRoundId, new Dictionary<Guid, string>(), 0)
            {
                IsLoading = true
            };
        }

        var state = new AdminLogsEuiState(CurrentRoundId, _players, _roundLogs);

        return state;
    }

    // Sunrise added start - keep message handler non-blocking for first log page delivery
    public override void HandleMessage(EuiMessageBase msg)
    // Sunrise added end
    {
        base.HandleMessage(msg);

        if (!_adminManager.HasAdminFlag(Player, AdminFlags.Logs))
        {
            return;
        }

        switch (msg)
        {
            case LogsRequest request:
            {
                _sawmill.Info($"Admin log request from admin with id {Player.UserId.UserId} and name {Player.Name}");

                _logSendCancellation.Cancel();
                _logSendCancellation = new CancellationTokenSource();
                // Sunrise added start - normalize requested round before querying round metadata/count
                var roundId = NormalizeRoundId(request.RoundId);
                _filter = new LogFilter
                {
                    CancellationToken = _logSendCancellation.Token,
                    Round = roundId,
                    Search = request.Search,
                    Types = request.Types,
                    Impacts = request.Impacts,
                    Before = request.Before,
                    After = request.After,
                    IncludePlayers = request.IncludePlayers,
                    AnyPlayers = request.AnyPlayers,
                    AllPlayers = request.AllPlayers,
                    IncludeNonPlayers = request.IncludeNonPlayers,
                    LastLogId = null,
                    LastLogCursor = null,
                    // Sunrise added start - request one extra record and keep cursor on visible page edge
                    LokiCursorOverfetch = 1,
                    Limit = _clientBatchSize + 1
                    // Sunrise added end
                };
                // Sunrise added end

                // Sunrise added start - send visible logs immediately, update metadata/count in parallel
                SendLogs(true);
                QueueLoadFromDb(roundId);
                // Sunrise added end
                break;
            }
            case NextLogsRequest:
            {
                _sawmill.Info($"Admin log next batch request from admin with id {Player.UserId.UserId} and name {Player.Name}");

                SendLogs(false);
                break;
            }
        }
    }

    public void SetLogFilter(string? search = null, bool invertTypes = false, HashSet<LogType>? types = null)
    {
        var message = new SetLogFilter(
            search,
            invertTypes,
            types);

        SendMessage(message);
    }

    private async void SendLogs(bool replace)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var logs = await Task.Run(async () => await _adminLogs.All(_filter, _adminLogListPool.Get),
            _filter.CancellationToken);

        var hasNext = logs.Count > _clientBatchSize;
        if (hasNext)
            logs.RemoveRange(_clientBatchSize, logs.Count - _clientBatchSize);

        if (logs.Count > 0)
        {
            _filter.LogsSent += logs.Count;
            _filter.LastLogId = logs[^1].Id;
        }

        var message = new NewLogs(logs, replace, hasNext);

        SendMessage(message);

        _sawmill.Info($"Sent {logs.Count} logs to {Player.Name} in {stopwatch.Elapsed.TotalMilliseconds} ms");

        _adminLogListPool.Return(logs);
    }

    public override void Closed()
    {
        base.Closed();

        _configuration.UnsubValueChanged(CCVars.AdminLogsClientBatchSize, ClientBatchSizeChanged);
        _adminManager.OnPermsChanged -= OnPermsChanged;

        // Sunrise added start - invalidate pending metadata loads for closed EUI instances
        Interlocked.Increment(ref _loadFromDbRequestId);
        // Sunrise added end
        _logSendCancellation.Cancel();
        _logSendCancellation.Dispose();
    }

    // Sunrise added start - normalize invalid round ids and keep metadata loads race-safe
    private int NormalizeRoundId(int? roundId)
    {
        if (roundId is > 0)
            return roundId.Value;

        var currentRoundId = CurrentRoundId;
        return currentRoundId > 0 ? currentRoundId : 0;
    }

    private void QueueLoadFromDb(int roundId)
    {
        var requestId = Interlocked.Increment(ref _loadFromDbRequestId);
        _ = LoadFromDb(roundId, requestId);
    }

    private void ResetRoundMetadata()
    {
        _players.Clear();
        _roundLogs = 0;
    }

    private bool IsCurrentLoadRequest(int requestId)
    {
        return !IsShutDown && requestId == Volatile.Read(ref _loadFromDbRequestId);
    }

    private async Task LoadFromDb(int roundId, int requestId)
    {
        if (!IsCurrentLoadRequest(requestId))
            return;

        _isLoading = true;
        StateDirty();

        try
        {
            if (roundId <= 0)
            {
                ResetRoundMetadata();
                return;
            }

            var round = _adminLogs.Round(roundId);
            var count = _adminLogs.CountLogs(roundId);
            await Task.WhenAll(round, count);

            if (!IsCurrentLoadRequest(requestId))
                return;

            var players = (await round).Players
                .ToDictionary(player => player.UserId, player => player.LastSeenUserName);
            var roundLogs = await count;

            if (!IsCurrentLoadRequest(requestId))
                return;

            _players.Clear();

            foreach (var (id, name) in players)
            {
                _players.Add(id, name);
            }

            _roundLogs = roundLogs;
        }
        catch (Exception ex)
        {
            if (!IsCurrentLoadRequest(requestId))
                return;

            ResetRoundMetadata();
            _sawmill.Warning($"Failed loading admin log metadata for round {roundId}: {ex}");
        }
        finally
        {
            if (IsCurrentLoadRequest(requestId))
            {
                _isLoading = false;
                StateDirty();
            }
        }
    }
    // Sunrise added end
}
