from __future__ import annotations

import json
from pathlib import Path

from sts2_record.stage2 import build_candidate_table, validate_candidate_table, validate_stage2_events_from_file


def write_event_file(path: Path, events: list[dict]) -> None:
    path.write_text("".join(json.dumps(event) + "\n" for event in events), encoding="utf-8")


def base_event(event_type: str, payload: dict) -> dict:
    return {
        "schema_version": "0.1.0",
        "recorder_version": "0.1.0",
        "game_version": "v0.105.1",
        "main_assembly_hash": 1363691567,
        "session_id": "session",
        "run_id": None,
        "seq": 1,
        "time_utc": "2026-05-19T00:00:00+00:00",
        "event_type": event_type,
        "source": "test",
        "confidence": "safe",
        "payload": payload,
    }


def make_game_dir(tmp_path: Path) -> Path:
    data_dir = tmp_path / "game" / "data_sts2_windows_x86_64"
    data_dir.mkdir(parents=True)
    (data_dir / "sts2.dll").write_bytes(
        b"MegaCrit.Sts2.Core.Combat.CombatManager+<DoTurnEnd>d__117\x00"
        b"MegaCrit.Sts2.Core.Hooks.Hook+<AfterCardPlayed>d__15\x00"
        b"MegaCrit.Sts2.Core.Hooks.Hook+<AfterPotionUsed>d__59\x00"
    )
    return tmp_path / "game"


def test_candidate_table_defaults_to_unverified(tmp_path: Path) -> None:
    table = build_candidate_table(make_game_dir(tmp_path))

    assert table["candidates"]
    assert {candidate["status"] for candidate in table["candidates"]} == {"unverified"}
    assert validate_candidate_table(table)["ok"] is True


def test_verified_candidate_requires_complete_evidence(tmp_path: Path) -> None:
    table = build_candidate_table(make_game_dir(tmp_path))
    table["candidates"][0]["status"] = "verified"

    result = validate_candidate_table(table)

    assert result["ok"] is False
    assert any("missing signature" in error for error in result["errors"])
    assert any("missing negative_test" in error for error in result["errors"])


def test_ui_layer_candidate_cannot_be_verified(tmp_path: Path) -> None:
    table = build_candidate_table(make_game_dir(tmp_path))
    candidate = table["candidates"][0]
    candidate.update(
        {
            "status": "verified",
            "target_type": "UiType",
            "method": "OnHover",
            "signature": "void OnHover()",
            "source_evidence": "test",
            "layer": "ui",
            "commit_point_reasoning": "test",
            "positive_test": "test",
            "negative_test": "test",
            "payload_fields": ["test"],
        }
    )

    result = validate_candidate_table(table)

    assert result["ok"] is False
    assert any("UI-layer" in error for error in result["errors"])


def test_stage2_gameplay_event_requires_action_payload(tmp_path: Path) -> None:
    events = tmp_path / "events.ndjson"
    write_event_file(events, [base_event("card_played", {})])

    result = validate_stage2_events_from_file(events)

    assert result["ok"] is False
    assert "hook" in result["errors"][0]


def test_stage2_gameplay_event_accepts_verified_shape(tmp_path: Path) -> None:
    events = tmp_path / "events.ndjson"
    write_event_file(
        events,
        [
            base_event(
                "turn_ended",
                {
                    "hook": {
                        "target_type": "VerifiedType",
                        "method": "VerifiedMethod",
                        "signature": "void VerifiedMethod()",
                        "evidence_id": "manual-001",
                    },
                    "action": {},
                    "state_before": {},
                    "state_after": {},
                },
            )
        ],
    )

    result = validate_stage2_events_from_file(events)

    assert result["ok"] is True


def test_diagnostic_card_play_state_diff_accepts_diagnostic_shape(tmp_path: Path) -> None:
    events = tmp_path / "events.ndjson"
    write_event_file(
        events,
        [
            base_event(
                "diagnostic_card_play_state_diff",
                {
                    "source_card_play_seq": 1,
                    "hook_window": {},
                    "card_play": {},
                    "state_before": {"players": [], "creatures": []},
                    "state_after": {"players": [], "creatures": []},
                    "diff": {},
                },
            )
        ],
    )

    result = validate_stage2_events_from_file(events)

    assert result["ok"] is True


def test_diagnostic_card_play_state_diff_requires_minimum_payload(tmp_path: Path) -> None:
    events = tmp_path / "events.ndjson"
    write_event_file(events, [base_event("diagnostic_card_play_state_diff", {"source_card_play_seq": 1})])

    result = validate_stage2_events_from_file(events)

    assert result["ok"] is False
    assert any("diagnostic card-play state diff missing payload fields" in error for error in result["errors"])
