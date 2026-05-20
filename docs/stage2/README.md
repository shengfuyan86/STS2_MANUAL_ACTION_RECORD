# Stage 2: Verified Trigger Hooks

阶段 2 的目标是实现真正的触发式核心操作记录：

```text
游戏逻辑提交点 -> hook 被调用 -> 写事件
```

当前先执行 Stage 2A：发现与验证。Stage 2A 不写任何 gameplay patch，只生成候选证据表并验证证据完整性。

## 当前限制

- Stage 0 的字符串扫描结果只是线索。
- 未验证 target type、method、signature 前，不创建 `Patches/*.cs`。
- 不 patch UI hover、drag、preview、cancel。
- 不使用轮询或 scene-tree 观察作为最终玩家动作记录。

## Stage 2A 命令

生成候选表草稿：

```bash
.venv/Scripts/python.exe -m sts2_record.cli stage2 write-candidate-table --game-dir "D:/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2" --out docs/stage2/hook-candidates.json
```

验证候选表：

```bash
.venv/Scripts/python.exe -m sts2_record.cli stage2 validate-candidates --table docs/stage2/hook-candidates.json
```

验证 Stage 2 事件文件：

```bash
.venv/Scripts/python.exe -m sts2_record.cli stage2 validate-events --events path/to/events.ndjson
```

## 2B 诊断桥

当前仓库已经接入一个最小诊断桥，只记录真实的 end-turn 请求和下沉后的逻辑层 phase-one 请求，不把 UI hover / cancel / undo 当成最终 gameplay 事件：

- `NEndTurnButton.CallReleaseLogic`
- `NEndTurnButton.SecretEndTurnLogicViaFtue`
- `CombatManager.AfterAllPlayersReadyToEndTurn`

它发出的事件类型是 `diagnostic_end_turn_request` 和 `diagnostic_end_turn_phase_one_requested`，用于 smoke test 和证据补全，不代表 `turn_ended` 已经 verified。

## 进入 Stage 2B 的条件

至少一个候选在 [HOOK_DISCOVERY.md](HOOK_DISCOVERY.md) 或候选 JSON 中被标记为 `verified`，并具备：

- exact target type
- exact method
- exact signature
- source evidence
- layer classification
- commit-point reasoning
- positive test
- negative test
- payload fields
- 用户确认