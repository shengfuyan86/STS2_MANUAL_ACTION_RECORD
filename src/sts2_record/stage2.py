from __future__ import annotations

import json
from pathlib import Path
from typing import Any

from sts2_record.stage0 import scan_static_hints
from sts2_record.stage1 import read_ndjson, validate_events as validate_stage1_events

CANDIDATE_AREAS = ("combat_lifecycle", "end_turn", "card_play", "potion_use")
VERIFIED_REQUIRED_FIELDS = (
    "target_type",
    "method",
    "signature",
    "source_evidence",
    "layer",
    "commit_point_reasoning",
    "positive_test",
    "negative_test",
    "payload_fields",
)
STAGE2_GAMEPLAY_EVENTS = {
    "combat_started",
    "combat_ended",
    "turn_started",
    "turn_ended",
    "card_played",
    "potion_used",
}
STAGE2_REQUIRED_PAYLOAD_FIELDS = {"hook", "action", "state_before", "state_after"}
DIAGNOSTIC_CARD_PLAY_STATE_DIFF_REQUIRED_FIELDS = {
    "source_card_play_seq",
    "hook_window",
    "card_play",
    "state_before",
    "state_after",
    "diff",
}
AREA_HINT_MAP = {
    "combat_lifecycle": ("combat", "turn"),
    "end_turn": ("turn",),
    "card_play": ("card_play",),
    "potion_use": ("potion",),
}


def build_candidate_table(game_dir: Path) -> dict[str, Any]:
    static_hints = scan_static_hints(game_dir, limit_per_area=12)
    hints_by_area = {group["area"]: group for group in static_hints["candidate_groups"]}
    candidates = []
    for area in CANDIDATE_AREAS:
        hints: list[str] = []
        for hint_area in AREA_HINT_MAP[area]:
            hints.extend(hints_by_area.get(hint_area, {}).get("hints", []))
        candidates.append(
            {
                "area": area,
                "target_type": "",
                "method": "",
                "signature": "",
                "source_evidence": "static_hint_only",
                "layer": "",
                "commit_point_reasoning": "",
                "positive_test": "",
                "negative_test": "",
                "payload_fields": [],
                "status": "unverified",
                "static_hints": hints[:20],
                "notes": "Static hints are search leads only; do not implement a hook from this row until status is verified.",
            }
        )
    return {
        "schema_version": "stage2.candidates.v1",
        "game_dir": str(game_dir),
        "assembly": static_hints["assembly"],
        "assembly_exists": static_hints["assembly_exists"],
        "candidates": candidates,
    }


def write_candidate_table(path: Path, table: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(table, ensure_ascii=False, indent=2), encoding="utf-8")


def load_candidate_table(path: Path) -> dict[str, Any]:
    return json.loads(path.read_text(encoding="utf-8"))


def validate_candidate_table(table: dict[str, Any]) -> dict[str, Any]:
    errors: list[str] = []
    candidates = table.get("candidates")
    if not isinstance(candidates, list):
        return {"ok": False, "candidate_count": 0, "verified_count": 0, "errors": ["candidates must be a list"]}

    verified_count = 0
    for index, candidate in enumerate(candidates, start=1):
        if not isinstance(candidate, dict):
            errors.append(f"candidate {index}: must be an object")
            continue
        status = candidate.get("status")
        if status not in {"unverified", "candidate", "verified", "rejected"}:
            errors.append(f"candidate {index}: invalid status {status!r}")
            continue
        if status == "verified":
            verified_count += 1
            for field in VERIFIED_REQUIRED_FIELDS:
                value = candidate.get(field)
                if value in (None, "", []):
                    errors.append(f"candidate {index}: verified candidate missing {field}")
            if candidate.get("layer") == "ui":
                errors.append(f"candidate {index}: UI-layer candidate cannot be verified for gameplay action recording")
    return {
        "ok": not errors,
        "candidate_count": len(candidates),
        "verified_count": verified_count,
        "errors": errors,
    }


def validate_stage2_events_from_file(path: Path) -> dict[str, Any]:
    try:
        events = read_ndjson(path)
    except ValueError as exc:
        return {"ok": False, "event_count": 0, "event_types": [], "errors": [str(exc)]}
    base = validate_stage1_events(events, require_recorder_loaded=False)
    errors = list(base["errors"])
    for index, event in enumerate(events, start=1):
        event_type = event.get("event_type")
        if event_type == "diagnostic_card_play_state_diff":
            payload = event.get("payload")
            if not isinstance(payload, dict):
                errors.append(f"event {index}: diagnostic card-play state diff payload must be an object")
                continue
            missing = sorted(DIAGNOSTIC_CARD_PLAY_STATE_DIFF_REQUIRED_FIELDS - set(payload))
            if missing:
                errors.append(f"event {index}: diagnostic card-play state diff missing payload fields {missing}")
            continue
        if event_type not in STAGE2_GAMEPLAY_EVENTS:
            continue
        payload = event.get("payload")
        if not isinstance(payload, dict):
            errors.append(f"event {index}: Stage 2 gameplay payload must be an object")
            continue
        missing = sorted(STAGE2_REQUIRED_PAYLOAD_FIELDS - set(payload))
        if missing:
            errors.append(f"event {index}: Stage 2 gameplay event missing payload fields {missing}")
        hook = payload.get("hook")
        if isinstance(hook, dict):
            for field in ("target_type", "method", "signature", "evidence_id"):
                if hook.get(field) in (None, ""):
                    errors.append(f"event {index}: hook missing {field}")
        elif "hook" in payload:
            errors.append(f"event {index}: hook must be an object")
    return {
        "ok": not errors,
        "event_count": base["event_count"],
        "event_types": base["event_types"],
        "errors": errors,
    }
