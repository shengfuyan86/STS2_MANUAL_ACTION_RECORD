# Stage 1 Event Schema

Stage 1 使用 NDJSON：每行一个 JSON object。

## Event envelope

```json
{
  "schema_version": "0.1.0",
  "recorder_version": "0.1.0",
  "game_version": "v0.105.1",
  "main_assembly_hash": 1363691567,
  "session_id": "20260519T000000000Z-1234abcd",
  "run_id": null,
  "seq": 1,
  "time_utc": "2026-05-19T00:00:00.0000000+00:00",
  "event_type": "recorder_loaded",
  "source": "mod_initializer",
  "confidence": "safe",
  "payload": {}
}
```

## Required fields

- `schema_version`
- `recorder_version`
- `game_version`
- `main_assembly_hash`
- `session_id`
- `seq`
- `time_utc`
- `event_type`
- `source`
- `confidence`
- `payload`

## Stage 1 event types

### `recorder_loaded`

表示 recorder mod 初始化入口已运行。

Example payload:

```json
{
  "harmony_id": "local.sts2.manual_action_recorder",
  "patches_applied": false,
  "output_directory": "..."
}
```

### `run_detected`

阶段 1 schema 预留。只有在找到可靠生命周期入口或明确的运行时观察证据后才写入。

Example payload:

```json
{
  "detection_method": "verified_lifecycle_callback",
  "evidence": "..."
}
```

### `combat_detected`

阶段 1 schema 预留。不能用 hover、UI preview 或未验证字符串扫描作为依据。

Example payload:

```json
{
  "detection_method": "verified_lifecycle_callback",
  "evidence": "..."
}
```

## Validation rules

- 每行必须是完整 JSON object。
- `seq` 必须严格递增。
- `payload` 必须是 object。
- 至少包含一个 `recorder_loaded`。