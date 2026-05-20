from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path


DEFAULT_GAME_DIR = Path("D:/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2")
DEFAULT_OUT_DIR = Path("docs/stage0/reports")
DEFAULT_MCP_HOST = "127.0.0.1"
DEFAULT_MCP_PORT = 15526


@dataclass(frozen=True)
class Stage0Config:
    game_dir: Path = DEFAULT_GAME_DIR
    out_dir: Path = DEFAULT_OUT_DIR
    mcp_host: str = DEFAULT_MCP_HOST
    mcp_port: int = DEFAULT_MCP_PORT

    @property
    def data_dir(self) -> Path:
        return self.game_dir / "data_sts2_windows_x86_64"

    @property
    def mods_dir(self) -> Path:
        return self.game_dir / "mods"

    @property
    def main_assembly(self) -> Path:
        return self.data_dir / "sts2.dll"
