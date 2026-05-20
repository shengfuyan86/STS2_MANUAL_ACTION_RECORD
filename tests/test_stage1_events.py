from __future__ import annotations

import json
from pathlib import Path

from sts2_record.stage1 import validate_event_file, validate_events


def event(seq: int, event_type: str = "recorder_loaded") -> dict:
    return {
        "schema_version": "0.1.0",
        "recorder_version": "0.1.0",
        "game_version": "v0.105.1",
        "main_assembly_hash": 1363691567,
        "session_id": "session",
        "run_id": None,
        "seq": seq,
        "time_utc": "2026-05-19T00:00:00+00:00",
        "event_type": event_type,
        "source": "test",
        "confidence": "safe",
        "payload": {},
    }


def write_ndjson(path: Path, events: list[dict]) -> None:
    path.write_text("".join(json.dumps(item) + "\n" for item in events), encoding="utf-8")


def test_valid_stage1_events_pass(tmp_path: Path) -> None:
    path = tmp_path / "events.ndjson"
    write_ndjson(path, [event(1), event(2, "run_detected"), event(3, "combat_detected")])

    result = validate_event_file(path)

    assert result["ok"] is True
    assert result["event_count"] == 3


def test_malformed_line_fails_with_line_number(tmp_path: Path) -> None:
    path = tmp_path / "events.ndjson"
    path.write_text(json.dumps(event(1)) + "\n{" + "\n", encoding="utf-8")

    result = validate_event_file(path)

    assert result["ok"] is False
    assert "line 2" in result["errors"][0]


def test_missing_required_field_fails() -> None:
    item = event(1)
    del item["session_id"]

    result = validate_events([item])

    assert result["ok"] is False
    assert "session_id" in result["errors"][0]


def test_non_monotonic_seq_fails() -> None:
    result = validate_events([event(2), event(2, "run_detected")])

    assert result["ok"] is False
    assert "not greater" in result["errors"][0]


def test_missing_recorder_loaded_fails() -> None:
    result = validate_events([event(1, "run_detected")])

    assert result["ok"] is False
    assert "missing recorder_loaded" in result["errors"][-1]
