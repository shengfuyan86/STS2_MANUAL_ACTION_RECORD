from __future__ import annotations

import json
import re
import socket
from collections import defaultdict
from pathlib import Path
from typing import Any

from sts2_record.config import DEFAULT_MCP_HOST, DEFAULT_MCP_PORT

KEYWORDS = (
    "card",
    "play",
    "turn",
    "reward",
    "map",
    "shop",
    "event",
    "campfire",
    "combat",
    "player",
    "enemy",
    "potion",
    "relic",
)

PATCH_AREAS = {
    "card_play": ("card", "play"),
    "turn": ("turn",),
    "reward": ("reward",),
    "map": ("map",),
    "shop": ("shop",),
    "event": ("event",),
    "campfire": ("campfire",),
    "combat": ("combat",),
    "potion": ("potion",),
    "relic": ("relic",),
}


def read_json(path: Path) -> dict[str, Any] | None:
    if not path.exists() or not path.is_file():
        return None
    try:
        return json.loads(path.read_text(encoding="utf-8-sig"))
    except (OSError, json.JSONDecodeError, UnicodeDecodeError) as exc:
        return {"_error": str(exc), "_path": str(path)}


def read_text(path: Path) -> str | None:
    if not path.exists() or not path.is_file():
        return None
    try:
        return path.read_text(encoding="utf-8-sig").strip()
    except (OSError, UnicodeDecodeError):
        try:
            return path.read_text(encoding="utf-8", errors="replace").strip()
        except OSError:
            return None


def collect_release_info(game_dir: Path) -> dict[str, Any]:
    release_info = read_json(game_dir / "release_info.json") or {}
    steam_appid = read_text(game_dir / "steam_appid.txt")
    return {
        "game_dir": str(game_dir),
        "exists": game_dir.exists(),
        "release_info": release_info,
        "steam_appid": steam_appid,
    }


def collect_runtime_info(game_dir: Path) -> dict[str, Any]:
    data_dir = game_dir / "data_sts2_windows_x86_64"
    runtime_config = read_json(data_dir / "sts2.runtimeconfig.json") or {}
    deps = read_json(data_dir / "sts2.deps.json") or {}
    libraries = deps.get("libraries", {}) if isinstance(deps, dict) else {}
    dependency_versions: dict[str, Any] = {}
    for name_version in libraries:
        name, _, version = name_version.partition("/")
        if name in {"0Harmony", "GodotSharp", "Steamworks.NET", "Sentry", "SmartFormat", "MonoMod.Backports"}:
            dependency_versions[name] = version

    required_files = {
        "sts2.dll": data_dir / "sts2.dll",
        "GodotSharp.dll": data_dir / "GodotSharp.dll",
        "0Harmony.dll": data_dir / "0Harmony.dll",
        "sts2.runtimeconfig.json": data_dir / "sts2.runtimeconfig.json",
        "sts2.deps.json": data_dir / "sts2.deps.json",
    }
    return {
        "data_dir": str(data_dir),
        "runtime_config": runtime_config,
        "dependency_versions": dependency_versions,
        "required_files": {
            name: {"path": str(path), "exists": path.exists()} for name, path in required_files.items()
        },
    }


def manifest_candidate(path: Path) -> bool:
    if path.suffix.lower() != ".json":
        return False
    name = path.name.lower()
    return name in {"mod_manifest.json", "mod_mainfest.json"} or name not in {"settings.json"}


def collect_mods(game_dir: Path) -> dict[str, Any]:
    mods_dir = game_dir / "mods"
    mcp_conf = read_json(mods_dir / "STS2_MCP.conf") or {}
    mcp_manifest = read_json(mods_dir / "STS2_MCP.json") or {}
    mods: list[dict[str, Any]] = []

    if not mods_dir.exists():
        return {
            "mods_dir": str(mods_dir),
            "exists": False,
            "sts2_mcp": {"conf": mcp_conf, "manifest": mcp_manifest},
            "mods": mods,
        }

    root_files = {child.name for child in mods_dir.iterdir() if child.is_file()}
    if "STS2_MCP.dll" in root_files or "STS2_MCP.json" in root_files or "STS2_MCP.conf" in root_files:
        mods.append(
            {
                "name": "STS2_MCP",
                "path": str(mods_dir),
                "layout": "root_files",
                "dlls": ["STS2_MCP.dll"] if "STS2_MCP.dll" in root_files else [],
                "pcks": [],
                "manifests": [{"path": str(mods_dir / "STS2_MCP.json"), "data": mcp_manifest}] if mcp_manifest else [],
                "configs": [{"path": str(mods_dir / "STS2_MCP.conf"), "data": mcp_conf}] if mcp_conf else [],
                "affects_gameplay": mcp_manifest.get("affects_gameplay"),
            }
        )

    for child in sorted(mods_dir.iterdir(), key=lambda p: p.name.lower()):
        if not child.is_dir():
            continue
        dlls = sorted(p.name for p in child.glob("*.dll"))
        pcks = sorted(p.name for p in child.glob("*.pck"))
        manifests = []
        configs = []
        for json_path in sorted(child.glob("*.json")):
            payload = read_json(json_path) or {}
            item = {"path": str(json_path), "data": payload}
            if manifest_candidate(json_path):
                manifests.append(item)
            else:
                configs.append(item)
        primary_manifest = next((m["data"] for m in manifests if isinstance(m.get("data"), dict)), {})
        mods.append(
            {
                "name": primary_manifest.get("id") or primary_manifest.get("name") or child.name,
                "path": str(child),
                "layout": "directory",
                "dlls": dlls,
                "pcks": pcks,
                "manifests": manifests,
                "configs": configs,
                "affects_gameplay": primary_manifest.get("affects_gameplay"),
            }
        )

    return {
        "mods_dir": str(mods_dir),
        "exists": True,
        "sts2_mcp": {
            "dll_exists": (mods_dir / "STS2_MCP.dll").exists(),
            "conf_path": str(mods_dir / "STS2_MCP.conf"),
            "conf": mcp_conf,
            "manifest_path": str(mods_dir / "STS2_MCP.json"),
            "manifest": mcp_manifest,
        },
        "mods": mods,
    }


def probe_mcp(host: str = DEFAULT_MCP_HOST, port: int = DEFAULT_MCP_PORT, timeout: float = 1.0) -> dict[str, Any]:
    result: dict[str, Any] = {"host": host, "port": port, "status": "not_reachable"}
    try:
        with socket.create_connection((host, port), timeout=timeout):
            result["status"] = "tcp_reachable"
    except OSError as exc:
        result["error"] = str(exc)
    return result


def extract_binary_strings(path: Path, min_length: int = 5) -> list[str]:
    if not path.exists() or not path.is_file():
        return []
    data = path.read_bytes()
    ascii_strings = re.findall(rb"[ -~]{%d,}" % min_length, data)
    utf16_strings = re.findall((rb"(?:[ -~]\x00){%d,}" % min_length), data)
    values = {item.decode("ascii", errors="ignore") for item in ascii_strings}
    values.update(item.decode("utf-16le", errors="ignore") for item in utf16_strings)
    return sorted(value.strip() for value in values if value.strip())


def classify_static_hints(strings: list[str], limit_per_area: int = 25) -> list[dict[str, Any]]:
    grouped: dict[str, list[str]] = defaultdict(list)
    lowered = [(value, value.lower()) for value in strings]
    for area, required_keywords in PATCH_AREAS.items():
        for value, lower in lowered:
            if all(keyword in lower for keyword in required_keywords):
                grouped[area].append(value)
                if len(grouped[area]) >= limit_per_area:
                    break
    return [
        {
            "area": area,
            "source": "sts2.dll binary string scan",
            "status": "unvalidated_static_hint",
            "hints": hints,
        }
        for area, hints in sorted(grouped.items())
        if hints
    ]


def scan_static_hints(game_dir: Path, limit_per_area: int = 25) -> dict[str, Any]:
    assembly = game_dir / "data_sts2_windows_x86_64" / "sts2.dll"
    strings = extract_binary_strings(assembly)
    return {
        "assembly": str(assembly),
        "assembly_exists": assembly.exists(),
        "keyword_count": len(KEYWORDS),
        "candidate_groups": classify_static_hints(strings, limit_per_area=limit_per_area),
    }


def collect_baseline(game_dir: Path, mcp_host: str = DEFAULT_MCP_HOST, mcp_port: int | None = None) -> dict[str, Any]:
    mods = collect_mods(game_dir)
    configured_port = mods.get("sts2_mcp", {}).get("conf", {}).get("port")
    port = mcp_port or configured_port or DEFAULT_MCP_PORT
    return {
        "release": collect_release_info(game_dir),
        "runtime": collect_runtime_info(game_dir),
        "mods": mods,
        "mcp_probe": probe_mcp(mcp_host, int(port)),
        "static_hints": scan_static_hints(game_dir),
    }


def write_json(path: Path, payload: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")
