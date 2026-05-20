from __future__ import annotations

import argparse
import json
from pathlib import Path

from sts2_record.config import DEFAULT_GAME_DIR, DEFAULT_MCP_HOST, DEFAULT_MCP_PORT, DEFAULT_OUT_DIR
from sts2_record.report import write_stage0_report
from sts2_record.stage0 import collect_baseline, probe_mcp, scan_static_hints, write_json
from sts2_record.stage1 import summarize_run, validate_event_file
from sts2_record.stage2 import (
    build_candidate_table,
    load_candidate_table,
    validate_candidate_table,
    validate_stage2_events_from_file,
    write_candidate_table,
)


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(prog="sts2-record")
    subparsers = parser.add_subparsers(dest="command", required=True)

    stage0 = subparsers.add_parser("stage0", help="Stage 0 environment and patch-point discovery helpers")
    stage0_sub = stage0.add_subparsers(dest="stage0_command", required=True)

    for name in ("baseline", "scan-strings", "write-report"):
        cmd = stage0_sub.add_parser(name)
        cmd.add_argument("--game-dir", type=Path, default=DEFAULT_GAME_DIR)
        cmd.add_argument("--out-dir", type=Path, default=DEFAULT_OUT_DIR)
        cmd.add_argument("--mcp-host", default=DEFAULT_MCP_HOST)
        cmd.add_argument("--mcp-port", type=int, default=None)

    probe = stage0_sub.add_parser("probe-mcp")
    probe.add_argument("--mcp-host", default=DEFAULT_MCP_HOST)
    probe.add_argument("--mcp-port", type=int, default=DEFAULT_MCP_PORT)

    stage1 = subparsers.add_parser("stage1", help="Stage 1 recorder output validation helpers")
    stage1_sub = stage1.add_subparsers(dest="stage1_command", required=True)

    validate = stage1_sub.add_parser("validate-events")
    validate.add_argument("--events", type=Path, required=True)

    summarize = stage1_sub.add_parser("summarize-run")
    summarize.add_argument("--run-dir", type=Path, required=True)

    stage2 = subparsers.add_parser("stage2", help="Stage 2 verified hook discovery helpers")
    stage2_sub = stage2.add_subparsers(dest="stage2_command", required=True)

    write_candidates = stage2_sub.add_parser("write-candidate-table")
    write_candidates.add_argument("--game-dir", type=Path, default=DEFAULT_GAME_DIR)
    write_candidates.add_argument("--out", type=Path, required=True)

    validate_candidates = stage2_sub.add_parser("validate-candidates")
    validate_candidates.add_argument("--table", type=Path, required=True)

    validate_stage2 = stage2_sub.add_parser("validate-events")
    validate_stage2.add_argument("--events", type=Path, required=True)

    return parser


def main(argv: list[str] | None = None) -> int:
    args = build_parser().parse_args(argv)

    if args.command == "stage0" and args.stage0_command == "baseline":
        baseline = collect_baseline(args.game_dir, args.mcp_host, args.mcp_port)
        print(json.dumps(baseline, ensure_ascii=False, indent=2))
        return 0

    if args.command == "stage0" and args.stage0_command == "probe-mcp":
        result = probe_mcp(args.mcp_host, args.mcp_port)
        print(json.dumps(result, ensure_ascii=False, indent=2))
        return 0

    if args.command == "stage0" and args.stage0_command == "scan-strings":
        result = scan_static_hints(args.game_dir)
        print(json.dumps(result, ensure_ascii=False, indent=2))
        return 0

    if args.command == "stage0" and args.stage0_command == "write-report":
        baseline = collect_baseline(args.game_dir, args.mcp_host, args.mcp_port)
        out_dir = args.out_dir
        write_json(out_dir / "stage0-baseline.json", baseline)
        write_stage0_report(out_dir / "stage0-baseline.md", baseline)
        print(str(out_dir / "stage0-baseline.md"))
        return 0

    if args.command == "stage1" and args.stage1_command == "validate-events":
        result = validate_event_file(args.events)
        print(json.dumps(result, ensure_ascii=False, indent=2))
        return 0 if result["ok"] else 1

    if args.command == "stage1" and args.stage1_command == "summarize-run":
        result = summarize_run(args.run_dir)
        print(json.dumps(result, ensure_ascii=False, indent=2))
        return 0 if result["validation"]["ok"] else 1

    if args.command == "stage2" and args.stage2_command == "write-candidate-table":
        table = build_candidate_table(args.game_dir)
        write_candidate_table(args.out, table)
        print(str(args.out))
        return 0

    if args.command == "stage2" and args.stage2_command == "validate-candidates":
        result = validate_candidate_table(load_candidate_table(args.table))
        print(json.dumps(result, ensure_ascii=False, indent=2))
        return 0 if result["ok"] else 1

    if args.command == "stage2" and args.stage2_command == "validate-events":
        result = validate_stage2_events_from_file(args.events)
        print(json.dumps(result, ensure_ascii=False, indent=2))
        return 0 if result["ok"] else 1

    return 2


if __name__ == "__main__":
    raise SystemExit(main())
