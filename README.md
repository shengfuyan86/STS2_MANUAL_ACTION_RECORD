# STS2 Manual Action Record

本项目用于实现 Slay the Spire 2 真人游玩时的动作与状态变化记录。核心方向是游戏内触发式记录：后续通过 C# mod、游戏回调或 Harmony patch 捕获动作提交点，而不是依赖 MCP 轮询当前界面来推断玩家操作。

## 当前阶段

当前实现阶段 2A：verified hook 发现与证据校验，同时已有一个 2B 诊断桥用于 end_turn 请求 smoke test。阶段 0 的环境基线工具和阶段 1 的最小 recorder mod 仍保留。

阶段 1 文档见 [docs/stage1/README.md](docs/stage1/README.md)。
阶段 2 文档见 [docs/stage2/README.md](docs/stage2/README.md)。


阶段 0 会读取本地游戏目录，生成：

- 游戏版本、commit、主程序集 hash。
- .NET/Godot/Harmony 相关运行时信息。
- 已安装 mod 和 STS2_MCP 配置。
- STS2_MCP 端口可达性探测结果。
- 从 `sts2.dll` 静态字符串扫描得到的未验证 patch 点线索。

静态线索不是最终 patch 点，必须用 STS2_MCP 或反编译验证后才能进入阶段 1。

## 使用

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
.venv/Scripts/python.exe -m pytest
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

## 阶段 2A 产出

- [docs/stage2/](docs/stage2/)：阶段 2 计划、hook 证据表、schema 和验证门槛。
- [docs/stage2/hook-candidates.json](docs/stage2/hook-candidates.json)：从本地 `sts2.dll` 生成的候选表，所有条目默认 `unverified`。
- [src/sts2_record/stage2.py](src/sts2_record/stage2.py)：候选表和 Stage 2 事件校验逻辑。

## 下一阶段入口

只有当 [docs/stage2/HOOK_DISCOVERY.md](docs/stage2/HOOK_DISCOVERY.md) 或候选 JSON 中至少一个候选被证据证明并标记为 `verified` 后，才进入 Stage 2B，为该具体提交点实现第一个 gameplay hook。