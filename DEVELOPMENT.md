# STS2 Manual Action Record Development Guide

本项目用于实现 Slay the Spire 2 真人游玩时的动作与状态变化记录。核心方向是游戏内触发式记录：后续通过 C# mod、游戏回调或 Harmony patch 捕获动作提交点，而不是依赖 MCP 轮询当前界面来推断玩家操作。

## 当前阶段

当前实现阶段 2A：verified hook 发现与证据校验，同时已有 2B 诊断桥用于 end_turn 和 card-play smoke test。阶段 0 的环境基线工具和阶段 1 的最小 recorder mod 仍保留。

阶段 1 文档见 [docs/stage1/README.md](docs/stage1/README.md)。
阶段 2 开发文档见 [docs/stage2/README.md](docs/stage2/README.md)。

阶段 0 会读取本地游戏目录，生成：

- 游戏版本、commit、主程序集 hash。
- .NET/Godot/Harmony 相关运行时信息。
- 已安装 mod 和 STS2_MCP 配置。
- STS2_MCP 端口可达性探测结果。
- 从 `sts2.dll` 静态字符串扫描得到的未验证 patch 点线索。

静态线索不是最终 patch 点，必须用 STS2_MCP 或反编译验证后才能进入阶段 1。

## 开发命令

默认游戏路径：

```text
D:/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2
```

生成阶段 0 报告：

```bash
.venv/Scripts/python.exe -m sts2_record.cli stage0 write-report --game-dir "D:/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2"
```

只探测 MCP 端口：

```bash
.venv/Scripts/python.exe -m sts2_record.cli stage0 probe-mcp --mcp-port 15526
```

只扫描静态字符串线索：

```bash
.venv/Scripts/python.exe -m sts2_record.cli stage0 scan-strings --game-dir "D:/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2"
```

运行测试：

```bash
PYTHONPATH=src .venv/Scripts/python.exe -m pytest
```

构建 mod：

```powershell
powershell -ExecutionPolicy Bypass -File scripts/build-stage1-mod.ps1 -GameDir "D:/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2"
```

部署 mod：

```powershell
powershell -ExecutionPolicy Bypass -File scripts/deploy-stage1-mod.ps1 -GameDir "D:/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2"
```

验证最新事件日志：

```powershell
powershell -ExecutionPolicy Bypass -File scripts/verify-stage1-output.ps1
```

验证 Stage 2 事件文件：

```bash
PYTHONPATH=src .venv/Scripts/python.exe -m sts2_record.cli stage2 validate-events --events path/to/events.ndjson
```

## 阶段 0 产出

- `docs/stage0/reports/stage0-baseline.json`
- `docs/stage0/reports/stage0-baseline.md`
- `docs/stage0/PATCH_POINT_DISCOVERY.md` 中补全的真实候选 patch 点表格

## 阶段 1 产出

- [mod/STS2ManualActionRecorder/](mod/STS2ManualActionRecorder/)：最小 DLL-only recorder mod。
- [docs/stage1/](docs/stage1/)：阶段 1 计划、schema 和验证清单。
- `scripts/build-stage1-mod.ps1`：构建 mod。
- `scripts/deploy-stage1-mod.ps1`：部署到游戏 `mods` 目录，需手动执行。
- `scripts/verify-stage1-output.ps1`：验证 recorder 输出。

## 阶段 2A / 2B 产出

- [docs/stage2/](docs/stage2/)：阶段 2 计划、hook 证据表、schema、字段说明和验证门槛。
- [docs/stage2/hook-candidates.json](docs/stage2/hook-candidates.json)：从本地 `sts2.dll` 生成的候选表，所有条目默认 `unverified`。
- [src/sts2_record/stage2.py](src/sts2_record/stage2.py)：候选表和 Stage 2 事件校验逻辑。
- [mod/STS2ManualActionRecorder/Integration/](mod/STS2ManualActionRecorder/Integration/)：当前诊断 hook 和状态差分实现。

## 下一阶段入口

只有当 [docs/stage2/HOOK_DISCOVERY.md](docs/stage2/HOOK_DISCOVERY.md) 或候选 JSON 中至少一个候选被证据证明并标记为 `verified` 后，才进入对应 gameplay hook 的最终实现。当前 `diagnostic_*` 事件仍然是诊断 / smoke test 事件，不应当直接视为最终 gameplay event。
