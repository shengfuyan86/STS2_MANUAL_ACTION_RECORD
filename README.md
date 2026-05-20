# STS2 Manual Action Recorder

STS2 Manual Action Recorder 是一个 Slay the Spire 2 触发式动作记录 mod。它的目标是在真人正常游玩时，通过游戏逻辑 hook 记录玩家动作和可观察状态变化，而不是通过 MCP 轮询当前界面来推断操作。

当前版本仍处于 Stage 2 诊断阶段：已经可以写入 recorder 加载事件、结束回合诊断事件、出牌诊断事件和出牌前后牌堆状态差分。所有 `diagnostic_*` 事件都用于 smoke test 和证据确认，暂时不要把它们当作最终 verified gameplay event。

开发说明见 [DEVELOPMENT.md](DEVELOPMENT.md)。

## 目录位置

默认游戏安装目录：

```text
D:/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2
```

mod 部署目录：

```text
D:/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2/mods/STS2ManualActionRecorder
```

记录输出目录：

```text
%APPDATA%/STS2ManualActionRecorder/runs/
```

在当前 Windows 用户环境下通常等价于：

```text
C:/Users/Pupil/AppData/Roaming/STS2ManualActionRecorder/runs/
```

每次记录器启动会创建一个新的 session 目录，核心日志文件是：

```text
C:/Users/Pupil/AppData/Roaming/STS2ManualActionRecorder/runs/<session_id>/events.ndjson
```

`events.ndjson` 每一行都是一个 JSON 事件。

## 安装 / 重新部署 mod

在仓库根目录执行：

```powershell
powershell -ExecutionPolicy Bypass -File scripts/deploy-stage1-mod.ps1 -GameDir "D:/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2"
```

这个脚本会：

1. 构建 `mod/STS2ManualActionRecorder/STS2ManualActionRecorder.csproj`。
2. 复制 `STS2ManualActionRecorder.dll` 到游戏 `mods/STS2ManualActionRecorder` 目录。
3. 复制 `STS2ManualActionRecorder.json` 到同一个 mod 目录。

如果游戏路径不同，把 `-GameDir` 后面的路径替换成你的 Slay the Spire 2 安装目录。

## 使用方法

1. 关闭游戏。
2. 执行部署命令。
3. 启动 Slay the Spire 2。
4. 进入一场战斗并正常操作。
5. 退出或切回桌面，打开 `%APPDATA%/STS2ManualActionRecorder/runs/`。
6. 找到最新 session 目录中的 `events.ndjson`。

可以测试的当前诊断行为：

- 启动游戏后，应写入 `recorder_loaded`。
- 点击结束回合，应写入 end-turn 诊断事件。
- 成功打出一张牌，应写入 `diagnostic_card_after_played`。
- 成功打出一张会改变手牌 / 牌堆 / 生成卡的牌，应写入 `diagnostic_card_play_state_diff`，并在 `diff` 中看到对应变化。

取消拖牌、取消选目标、hover、preview 等不应被当作最终出牌事件。

## 常用命令

构建 mod 但不部署：

```powershell
powershell -ExecutionPolicy Bypass -File scripts/build-stage1-mod.ps1 -GameDir "D:/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2"
```

构建并部署 mod：

```powershell
powershell -ExecutionPolicy Bypass -File scripts/deploy-stage1-mod.ps1 -GameDir "D:/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2"
```

验证最新 Stage 1 / recorder 基础事件日志：

```powershell
powershell -ExecutionPolicy Bypass -File scripts/verify-stage1-output.ps1
```

验证某个 Stage 2 事件文件：

```bash
PYTHONPATH=src .venv/Scripts/python.exe -m sts2_record.cli stage2 validate-events --events "C:/Users/Pupil/AppData/Roaming/STS2ManualActionRecorder/runs/<session_id>/events.ndjson"
```

运行项目测试：

```bash
PYTHONPATH=src .venv/Scripts/python.exe -m pytest
```

## 当前会记录什么

字段详细说明见：

- 中文：[docs/stage2/RECORDING_FIELD_REFERENCE_ZH.md](docs/stage2/RECORDING_FIELD_REFERENCE_ZH.md)
- 英文：[docs/stage2/RECORDING_FIELD_REFERENCE.md](docs/stage2/RECORDING_FIELD_REFERENCE.md)

当前主要事件：

| 事件 | 含义 |
| --- | --- |
| `recorder_loaded` | mod 已成功加载，并列出当前注册的诊断 hook。 |
| `diagnostic_end_turn_request` | 玩家请求结束回合的按钮路径诊断事件。 |
| `diagnostic_end_turn_phase_one_requested` | 结束回合进入更深逻辑门的诊断事件。 |
| `diagnostic_card_after_played` | 一张牌成功打出并进入 `Hook.AfterCardPlayed` 的诊断事件。 |
| `diagnostic_card_play_state_diff` | 一次出牌前后的可观察状态快照和牌堆差分。 |

## 重要限制

- 当前是诊断阶段，不是最终完整战斗复盘格式。
- `diagnostic_card_after_played` 只能说明“成功打出了哪张牌”，不能说明这张牌造成的所有状态变化。
- `diagnostic_card_play_state_diff` 当前主要覆盖卡牌实例、手牌、抽牌堆、弃牌堆、消耗堆、Play pile 的变化。
- HP、Block、power、orb、召唤物、choice、random 等系统还没有被完整捕获。
- `state_after` 当前在最终 result-pile 清理之前捕获，所以打出的牌最终去向要参考 `card_play.result_pile`。

## 读取最新日志

PowerShell 中可以打开 runs 目录：

```powershell
explorer "$env:APPDATA\STS2ManualActionRecorder\runs"
```

也可以查看最新 session：

```powershell
$run = Get-ChildItem "$env:APPDATA\STS2ManualActionRecorder\runs" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
$run.FullName
Get-Content "$($run.FullName)\events.ndjson"
```

如果只想看事件类型：

```powershell
Get-Content "$($run.FullName)\events.ndjson" | ForEach-Object { ($_ | ConvertFrom-Json).event_type }
```
