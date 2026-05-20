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

## 字段说明

所有当前已记录事件和字段的含义见 [RECORDING_FIELD_REFERENCE_ZH.md](RECORDING_FIELD_REFERENCE_ZH.md)（中文）和 [RECORDING_FIELD_REFERENCE.md](RECORDING_FIELD_REFERENCE.md)（英文）。以后新增、删除、重命名或修改记录字段语义时，必须同步维护字段说明文档。

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

当前仓库已经接入诊断桥，只记录真实请求或逻辑层 hook，不把 UI hover / cancel / undo 当成最终 gameplay 事件。

End-turn 诊断点：

- `NEndTurnButton.CallReleaseLogic`
- `NEndTurnButton.SecretEndTurnLogicViaFtue`
- `CombatManager.AfterAllPlayersReadyToEndTurn`

它发出的事件类型是 `diagnostic_end_turn_request` 和 `diagnostic_end_turn_phase_one_requested`，用于 smoke test 和证据补全，不代表 `turn_ended` 已经 verified。

Card-play 诊断点：

- `Hook.AfterCardPlayed`

它发出的事件类型是 `diagnostic_card_after_played`。这个事件只证明一次卡牌成功提交到了逻辑层，并记录卡牌、目标、资源、是否自动出牌、play series 等信息；它不能完整记录卡牌效果造成的实际状态变化。

## Card-play 记录边界

Stage 2B 的 card-play 记录必须拆成两层：

1. `card_played` / `diagnostic_card_after_played`：记录“谁成功打出了哪张牌，对谁打，花了多少资源，是手动还是自动”。
2. `card_play_state_diff` / `diagnostic_card_play_state_diff`：记录“这次出牌实际改变了什么”。

不能按 547 张卡逐张硬编码效果。所有职业都有会改变其他手牌、牌堆、生成牌、自动打牌、延迟触发、状态或召唤的卡牌；完整复盘必须记录出牌前后状态快照并计算差分。

最低状态快照范围：

- 每个玩家的 hand / draw pile / discard pile / exhaust pile，包含卡牌实例、顺序和 zone index。
- 当前出牌卡牌的实例、费用、升级状态、临时属性和 modifier。
- 敌我 creature 的 HP / Block / powers。
- 机器人 orb、骨系召唤 / Osty / minion 等职业状态，如果运行时可稳定读取。

最低差分范围：

- cards drawn / discarded / exhausted / moved / created / transformed / upgraded。
- draw pile top/order changes。
- hp / block / power / orb / summon changes。
- random 或 player choice 的最终结果；如果能取得候选项，也记录 choice options。
- auto-play 的父子关系，例如 `source_card_play_seq`。

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