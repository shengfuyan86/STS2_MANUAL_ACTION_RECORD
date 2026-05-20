from __future__ import annotations

import json
from pathlib import Path

from sts2_record.report import render_stage0_report
from sts2_record.stage0 import collect_baseline, collect_mods, collect_runtime_info, scan_static_hints


def write_json(path: Path, payload: dict) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload), encoding="utf-8")


def make_game_dir(tmp_path: Path) -> Path:
    game_dir = tmp_path / "Slay the Spire 2"
    data_dir = game_dir / "data_sts2_windows_x86_64"
    mods_dir = game_dir / "mods"
    data_dir.mkdir(parents=True)
    mods_dir.mkdir()

    write_json(
        game_dir / "release_info.json",
        {
            "version": "v0.105.1",
            "commit": "d5e30a22",
            "branch": "v0.105.1",
            "main_assembly_hash": 1363691567,
        },
    )
    (game_dir / "steam_appid.txt").write_text("2868840", encoding="utf-8")
    write_json(
        data_dir / "sts2.runtimeconfig.json",
        {"runtimeOptions": {"tfm": "net9.0", "includedFrameworks": [{"name": "Microsoft.NETCore.App", "version": "9.0.7"}]}},
    )
    write_json(
        data_dir / "sts2.deps.json",
        {
            "libraries": {
                "0Harmony/2.4.2.0": {},
                "GodotSharp/4.5.1": {},
                "Steamworks.NET/1.0.0.0": {},
            }
        },
    )
    for name in ("sts2.dll", "GodotSharp.dll", "0Harmony.dll"):
        (data_dir / name).write_bytes(b"CardPlayAction\x00EndTurnAction\x00RewardScreen\x00ShopPurchase\x00")

    (mods_dir / "STS2_MCP.dll").write_bytes(b"")
    write_json(mods_dir / "STS2_MCP.conf", {"port": 15526})
    write_json(
        mods_dir / "STS2_MCP.json",
        {"id": "STS2_MCP", "name": "STS2 MCP", "version": "0.4.0", "affects_gameplay": False},
    )

    damage_meter = mods_dir / "DamageMeter"
    damage_meter.mkdir()
    (damage_meter / "DamageMeter.dll").write_bytes(b"")
    (damage_meter / "DamageMeter.pck").write_bytes(b"")
    write_json(damage_meter / "mod_manifest.json", {"name": "Skada: Damage Meter", "version": "1.7.5"})

    typo_manifest = mods_dir / "sts2-heybox-support"
    typo_manifest.mkdir()
    (typo_manifest / "sts2-heybox-support.dll").write_bytes(b"")
    write_json(typo_manifest / "mod_mainfest.json", {"id": "sts2-heybox-support", "affects_gameplay": False})
    write_json(typo_manifest / "settings.json", {"EnableProfileSkip": True})

    return game_dir


def test_runtime_info_parses_dependency_versions(tmp_path: Path) -> None:
    game_dir = make_game_dir(tmp_path)

    runtime = collect_runtime_info(game_dir)

    assert runtime["runtime_config"]["runtimeOptions"]["tfm"] == "net9.0"
    assert runtime["dependency_versions"]["0Harmony"] == "2.4.2.0"
    assert runtime["dependency_versions"]["GodotSharp"] == "4.5.1"
    assert runtime["required_files"]["sts2.dll"]["exists"] is True


def test_collect_mods_handles_root_mcp_and_manifest_variants(tmp_path: Path) -> None:
    game_dir = make_game_dir(tmp_path)

    mods = collect_mods(game_dir)
    names = {mod["name"] for mod in mods["mods"]}

    assert mods["sts2_mcp"]["conf"]["port"] == 15526
    assert mods["sts2_mcp"]["dll_exists"] is True
    assert "STS2_MCP" in names
    assert "Skada: Damage Meter" in names
    assert "sts2-heybox-support" in names


def test_scan_static_hints_marks_unvalidated(tmp_path: Path) -> None:
    game_dir = make_game_dir(tmp_path)

    hints = scan_static_hints(game_dir)

    assert hints["assembly_exists"] is True
    assert any(group["status"] == "unvalidated_static_hint" for group in hints["candidate_groups"])


def test_baseline_report_contains_mcp_and_game_version(tmp_path: Path) -> None:
    game_dir = make_game_dir(tmp_path)

    baseline = collect_baseline(game_dir, mcp_port=15526)
    report = render_stage0_report(baseline)

    assert "v0.105.1" in report
    assert "STS2_MCP" in report
    assert "Static patch-point hints" in report
