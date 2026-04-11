# Local Loki Setup for SS14 Admin Logs

This guide describes how to run Loki locally and route SS14 `adminlogs` into it for local testing.

## 1. Download Loki

Use the official Grafana Loki local install page:

- https://grafana.com/docs/loki/latest/setup/install/local/

For Windows, the page includes the same startup command format:

```powershell
.\loki-windows-amd64.exe --config.file=loki-local-config.yaml
```

## 2. Prepare `loki-local-config.yaml` for local Windows use

If you use the default local config, prefer local relative paths over `/tmp`:

```yaml
auth_enabled: false

server:
  http_listen_port: 3100
  grpc_listen_port: 9096
  log_level: info

common:
  instance_addr: 127.0.0.1
  path_prefix: ./loki-data
  storage:
    filesystem:
      chunks_directory: ./loki-data/chunks
      rules_directory: ./loki-data/rules
  replication_factor: 1
  ring:
    kvstore:
      store: inmemory
```

`auth_enabled: false` is acceptable for local-only testing. Do not use this as production configuration.

## 3. Enable admin log shipping to Loki in server config

In your server `server_config.toml`, set:

```toml
[adminlogs]
enabled = true
to_loki = true
loki_url = "http://127.0.0.1:3100"
loki_name = "sunrise-local"
loki_username = ""
loki_password = ""
```

Notes:

- `loki_url` should not include a trailing slash.
- This affects admin logs only (`adminlogs`), not the engine-wide `loki.*` sink.

## 4. Start services

1. Start Loki.
2. Start the SS14 server with the `server_config.toml` above.
3. Generate any admin-log-producing action in game (spawn, delete, VV edits, admin command usage).

## 5. Validate ingestion

Check effective CVars in server console:

```text
cvar adminlogs.to_loki
cvar adminlogs.loki_url
```

Query Loki directly from PowerShell:

```powershell
$q = [uri]::EscapeDataString('{app="sunrise-local", category="admin_log"} | json')
Invoke-RestMethod "http://127.0.0.1:3100/loki/api/v1/query_range?query=$q&limit=20&direction=backward"
```

Open in-game admin logs UI with:

```text
adminlogs
```

If `adminlogs.to_loki=true`, the UI reads from Loki.

## 6. Quick troubleshooting

- No logs in Loki:
  - Confirm `adminlogs.enabled=true`.
  - Confirm `adminlogs.to_loki=true`.
  - Confirm Loki is reachable on `http://127.0.0.1:3100`.
- Logs appear in Loki query but not in UI:
  - Verify your account has `AdminFlags.Logs`.
  - Verify you are viewing the correct round and filters in the `adminlogs` window.
