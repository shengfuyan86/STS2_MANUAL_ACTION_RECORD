from __future__ import annotations

import json
from pathlib import Path
from typing import Any

REQUIRED_EVENT_FIELDS = {
    "schema_version",
    "recorder_version",
    "game_version",
    "main_assembly_hash",
    "session_id",
    "seq",
    "time_utc",
    "event_type",
    "source",
    "confidence",
    "payload",
}


def read_ndjson(path: Path) -> list[dict[str, Any]]:
    events: list[dict[str, Any]] = []
    with path.open("r", encoding="utf-8") as handle:
        for line_number, line in enumerate(handle, start=1):
            stripped = line.strip()
            if not stripped:
                continue
            try:
                item = json.loads(stripped)
            except json.JSONDecodeError as exc:
                raise ValueError(f"line {line_number}: invalid JSON: {exc.msg}") from exc
            if not isinstance(item, dict):
                raise ValueError(f"line {line_number}: expected JSON object")
            events.append(item)
    return events


def validate_events(events: list[dict[str, Any]], require_recorder_loaded: bool = True) -> dict[str, Any]:
    errors: list[str] = []
    previous_seq: int | None = None
    event_types: list[str] = []

    for index, event in enumerate(events, start=1):
        missing = sorted(REQUIRED_EVENT_FIELDS - set(event))
        if missing:
            errors.append(f"event {index}: missing fields {missing}")

        seq = event.get("seq")
        if not isinstance(seq, int):
            errors.append(f"event {index}: seq must be an integer")
        elif previous_seq is not None and seq <= previous_seq:
            errors.append(f"event {index}: seq {seq} is not greater than previous seq {previous_seq}")
        if isinstance(seq, int):
            previous_seq = seq

        event_type = event.get("event_type")
        if isinstance(event_type, str):
            event_types.append(event_type)
        else:
            errors.append(f"event {index}: event_type must be a string")

        if "payload" in event and not isinstance(event.get("payload"), dict):
            errors.append(f"event {index}: payload must be an object")

    if require_recorder_loaded and "recorder_loaded" not in event_types:
        errors.append("missing recorder_loaded event")

    return {
        "ok": not errors,
        "event_count": len(events),
        "event_types": event_types,
        "errors": errors,
    }


def validate_event_file(path: Path) -> dict[str, Any]:
    if not path.exists():
        return {"ok": False, "event_count": 0, "event_types": [], "errors": [f"file not found: {path}"]}
    try:
        events = read_ndjson(path)
    except ValueError as exc:
        return {"ok": False, "event_count": 0, "event_types": [], "errors": [str(exc)]}
    return validate_events(events)


def summarize_run(run_dir: Path) -> dict[str, Any]:
    metadata_path = run_dir / "metadata.json"
    events_path = run_dir / "events.ndjson"
    metadata: dict[str, Any] = {}
    if metadata_path.exists():
        metadata = json.loads(metadata_path.read_text(encoding="utf-8"))
    validation = validate_event_file(events_path)
    return {
        "run_dir": str(run_dir),
        "metadata_path": str(metadata_path),
        "events_path": str(events_path),
        "metadata": metadata,
        "validation": validation,
    }
