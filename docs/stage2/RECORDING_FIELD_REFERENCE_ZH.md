# Stage 2 记录字段中文说明

本文档用于说明当前记录器写入的所有事件和字段含义。之后只要新增、删除、重命名记录字段，或修改字段语义，都必须在同一次修改中同步维护本文档。

## 事件外层结构

`events.ndjson` 中每一行都是一个独立 JSON 事件对象。所有事件都使用 Stage 1 的外层结构。

| 字段 | 含义 |
| --- | --- |
| `schema_version` | 记录器事件 schema 版本。它描述日志格式，不是游戏版本。 |
| `recorder_version` | 当前记录器 mod / 工具版本。 |
| `game_version` | 记录器观察到的 Slay the Spire 2 游戏版本。 |
| `main_assembly_hash` | 游戏主程序集的 hash / 指纹，用于判断当前 hook 证据是否仍匹配本地游戏版本。 |
| `session_id` | 一次记录器会话的唯一 id，也对应一次日志目录。 |
| `run_id` | 可选的更高层运行 id；当前可能为 `null`。 |
| `seq` | 当前 session 内单调递增的事件序号。判断事件先后顺序时优先看它。 |
| `time_utc` | 记录器写入事件时的 UTC 时间。 |
| `event_type` | 事件类型，例如 `recorder_loaded`、`diagnostic_card_play_state_diff`。 |
| `source` | 产生事件的 hook、子系统或记录器组件。 |
| `confidence` | 当前事件可信级别。`diagnostic` 表示诊断 / smoke test / 证据事件，不等于最终 verified gameplay event。 |
| `payload` | 事件自己的具体数据。不同事件类型有不同结构。 |

## `recorder_loaded`

记录器 mod 初始化成功时写入。

| payload 字段 | 含义 |
| --- | --- |
| `diagnostic_hooks` | 当前 mod 构建尝试注册的诊断 hook 列表。这里出现某个 hook，只说明 mod 知道并尝试注册它，不代表真实游戏过程中这个 hook 已经触发。 |

当前可能包含的诊断 hook 有：`NEndTurnButton.CallReleaseLogic`、`CombatManager.AfterAllPlayersReadyToEndTurn`、`Hook.BeforeCardPlayed`、`Hook.AfterCardPlayed` 等。

## `diagnostic_end_turn_request`

从结束回合按钮请求路径写入。它表示“玩家发起了结束回合请求”的诊断事件，不是最终 verified 的 `turn_ended` gameplay event。

| payload 字段 | 含义 |
| --- | --- |
| `action` | 诊断动作名。当前为 `end_turn_requested`。 |
| `player_net_id` | 与本次请求关联的玩家 net id。 |
| `round_number` | hook 触发时观察到的战斗轮数。 |
| `current_side` | hook 触发时当前行动方，例如 `Player`。 |
| `ready_before` | 本次请求前，该玩家是否已经处于 ready 状态。 |
| `can_turn_be_ended` | 游戏逻辑在该时刻是否认为可以结束回合。 |
| `has_playable_cards` | 游戏逻辑在该时刻是否认为还有可打出的牌。 |
| `source_method` | 产生该事件的具体来源方法。 |

## `diagnostic_end_turn_phase_one_requested`

从 `CombatManager.AfterAllPlayersReadyToEndTurn` 写入。它是更深一层的逻辑层诊断事件，说明流程已经到达“所有玩家 ready 后”的逻辑门。

| payload 字段 | 含义 |
| --- | --- |
| `hook` | 触发该事件的 hook / 方法名。 |
| `round_number` | hook 触发时的战斗轮数。 |
| `current_side` | hook 触发时当前行动方。 |
| `is_in_progress` | hook 触发时战斗是否仍在进行。 |
| `ending_player_turn_phase_one_before` | 方法体执行前，内部 phase one 结束标记的值。 |
| `ending_player_turn_phase_two_before` | 方法体执行前，内部 phase two 结束标记的值。 |
| `action_during_enemy_turn_present` | 是否存在敌方回合行动标记。 |
| `source_role` | 记录器对该 hook 作用的分类。当前表示“所有玩家 ready 之后的逻辑门”。 |

## `diagnostic_card_after_played`

从 `Hook.AfterCardPlayed` 写入。它证明一次卡牌打出已经进入成功的出牌 hook dispatcher。这个事件记录的是“提交语义”：谁打了哪张牌、目标是谁、花了多少资源、是否自动打出。它本身不保证完整记录了卡牌造成的所有状态变化。

| payload 字段 | 含义 |
| --- | --- |
| `hook` | hook 名。当前为 `Hook.AfterCardPlayed`。 |
| `card_instance_id` | 记录器给运行时 `CardModel` 对象引用分配的稳定 id。只在当前记录 session 内稳定。 |
| `card_id` | 游戏卡牌 id，例如 `MANIFEST_AUTHORITY`。 |
| `card_model_id` | 游戏完整 model id，例如 `CARD.MANIFEST_AUTHORITY`。 |
| `card_title` | 游戏显示的本地化卡牌名称。 |
| `card_target_type` | 游戏卡牌模型报告的目标类型。 |
| `player_net_id` | 卡牌拥有者 / 玩家 net id。 |
| `target_present` | 本次 `CardPlay` 是否存在目标。 |
| `target_combat_id` | 目标 creature 的 combat id。无目标时为 `null`。 |
| `target_index` | 目标 creature 在当前 creature 列表中的索引。无法解析或无目标时为 `null`。 |
| `target_log_name` | 目标 creature 的日志 / 显示名称。 |
| `result_pile` | `CardPlay.ResultPile` 报告的预期最终牌堆，例如 `Discard`、`Exhaust`、`None`。注意当前 `state_after` 捕获时，最终入弃牌堆 / 消耗堆清理还没有发生。 |
| `is_auto_play` | 是否由游戏逻辑自动打出，而不是玩家直接手动打出。 |
| `play_index` | 如果当前存在连续出牌序列，它表示本次出牌在序列中的索引。 |
| `play_count` | 如果当前存在连续出牌序列，它表示序列总数。 |
| `is_first_in_series` | 本次是否是连续出牌序列中的第一张。 |
| `is_last_in_series` | 本次是否是连续出牌序列中的最后一张。 |
| `energy_spent` | 本次出牌消耗的能量。 |
| `stars_spent` | 本次出牌消耗的星。 |

## `diagnostic_card_play_state_diff`

通过关联同一个 `CardPlay` 对象在 `Hook.BeforeCardPlayed` 和 `Hook.AfterCardPlayed` 两个 hook 中的信息写入。它会记录出牌前后可观察状态快照，并计算通用差分。

它仍然是诊断事件，不是最终 `card_played` gameplay event。

| payload 字段 | 含义 |
| --- | --- |
| `source_card_play_seq` | 记录器为这次关联捕获分配的出牌序号。之后可用于关联自动出牌子事件等。 |
| `capture_status` | 捕获结果，例如 `complete` 或 partial / unavailable 状态。 |
| `hook_window` | 说明 `state_before` 和 `state_after` 分别在什么 hook、什么语义边界捕获。 |
| `card_play` | 本次出牌的身份和动作信息。字段含义与 `diagnostic_card_after_played` 基本一致。 |
| `capture_capabilities` | 当前诊断声称能够捕获哪些信息。不能捕获的能力会明确标为 `false`。 |
| `state_before` | 在 `Hook.BeforeCardPlayed` prefix 捕获的状态快照。 |
| `state_after` | 在 `Hook.AfterCardPlayed` prefix 捕获的状态快照。 |
| `diff` | 根据 `state_before` 和 `state_after` 计算出的通用状态差分。 |

### `hook_window`

| 字段 | 含义 |
| --- | --- |
| `state_before_hook` | 捕获 before 快照的 hook。当前为 `Hook.BeforeCardPlayed prefix`。 |
| `state_after_hook` | 捕获 after 快照的 hook。当前为 `Hook.AfterCardPlayed prefix`。 |
| `state_before_semantics` | `state_before` 的语义：校验和资源消耗之后，打出的牌已经进入 Play pile，但卡牌效果还没执行。 |
| `state_after_semantics` | `state_after` 的语义：卡牌 `OnPlay` / enchantment / affliction 效果和 `History.CardPlayFinished` 已完成，但 `AfterCardPlayed` listener 和最终 result-pile 清理还没执行。 |

### `card_play`

`card_play` 对象记录本次出牌本身的信息，包括：卡牌实例 id、卡牌身份、目标、目标索引、目标名称、预期最终牌堆、是否自动出牌、连续出牌序列信息、资源消耗等。字段含义与 `diagnostic_card_after_played` 相同。

### `capture_capabilities`

| 字段 | 含义 |
| --- | --- |
| `card_piles` | 当前诊断会尝试捕获卡牌牌堆。 |
| `card_instance_ids` | 当前诊断会给卡牌分配记录器侧实例 id，用于 before / after 匹配。 |
| `creature_identities` | 当前诊断会捕获 creature 的最小身份信息。 |
| `creature_hp_block_powers` | 是否完整捕获 HP / Block / power。当前为 `false`。 |
| `orbs` | 是否完整捕获机器人 orb / channel / evoke 状态。当前为 `false`。 |
| `summons` | 是否完整捕获召唤物 / Osty / minion 状态。当前为 `false`。 |
| `choices` | 是否完整捕获玩家选择候选项和结果。当前为 `false`。 |
| `random_results` | 是否完整捕获随机结果和 RNG 来源。当前为 `false`。 |

## 状态快照

`state_before` 和 `state_after` 结构相同。

| 字段 | 含义 |
| --- | --- |
| `players` | 捕获到的玩家状态列表。 |
| `creatures` | 捕获到的 creature 身份列表。 |
| `snapshot_warnings` | 反射读取或 fallback 捕获时产生的警告。存在警告时，说明快照可能不完整。 |

### 玩家快照

| 字段 | 含义 |
| --- | --- |
| `player_net_id` | 玩家对象的运行时 net id。 |
| `hand` | 当前手牌中的卡牌列表，保留顺序。 |
| `draw_pile` | 当前抽牌堆中的卡牌列表，保留顺序。 |
| `discard_pile` | 当前弃牌堆中的卡牌列表，保留顺序。 |
| `exhaust_pile` | 当前消耗堆中的卡牌列表，保留顺序。 |
| `play_pile` | 当前 Play pile 中的卡牌列表，保留顺序。在当前捕获窗口中，正在打出的牌通常会在这里。 |
| `snapshot_warnings` | 针对这个玩家快照产生的警告。 |

### 卡牌快照项

| 字段 | 含义 |
| --- | --- |
| `recorder_card_instance_id` | 记录器给这张运行时卡牌对象引用分配的 id，例如 `card-25`。 |
| `card_id` | 游戏卡牌 id。 |
| `card_model_id` | 游戏完整 model id。 |
| `card_title` | 游戏显示的本地化卡牌名。 |
| `owner_net_id` | 拥有这张牌的玩家 net id。 |
| `zone` | 记录器快照中这张牌所在区域：`hand`、`draw_pile`、`discard_pile`、`exhaust_pile`、`play_pile`。 |
| `zone_index` | 这张牌在该区域中的 0 基索引。 |
| `current_pile` | 游戏模型自己报告的当前 pile 类型。 |
| `target_type` | 卡牌模型报告的目标类型。 |
| `current_upgrade_level` | 当前升级等级。 |
| `is_upgraded` | 是否已经升级。 |
| `is_clone` | 游戏模型是否把它标记为 clone。 |
| `is_dupe` | 游戏模型是否把它标记为 duplicate / dupe。 |
| `exhaust_on_next_play` | 是否被标记为下次打出后消耗。 |

### Creature 快照项

| 字段 | 含义 |
| --- | --- |
| `recorder_creature_instance_id` | 记录器给这个运行时 creature 对象引用分配的 id。 |
| `combat_id` | 游戏中该 creature 的 combat id。 |
| `index` | 该 creature 在战斗 creature 列表中的索引。 |
| `log_name` | 游戏日志 / 显示名称。 |
| `side` | 所属阵营，例如玩家侧或敌人侧。 |
| `is_alive` | 是否存活。 |
| `is_dead` | 是否死亡。 |

## 差分字段

`diff` 通过 `recorder_card_instance_id` 匹配 before / after 中的卡牌对象。

| 字段 | 含义 |
| --- | --- |
| `cards_moved` | before 和 after 都存在，但 zone 或 zone_index 发生变化的卡牌。 |
| `cards_created` | before 不存在、after 存在的卡牌。通常表示生成牌，也可能表示 transform 的结果。 |
| `cards_removed` | before 存在、after 不存在的卡牌。可能表示被消耗、被 transform、或在当前可见范围内消失。 |
| `cards_drawn` | 从 `draw_pile` 移动到 `hand` 的派生结果。 |
| `cards_discarded` | 移动到 `discard_pile` 的派生结果。 |
| `cards_exhausted` | 移动到 `exhaust_pile` 的派生结果。 |
| `cards_upgraded` | 升级等级或升级标记发生变化的卡牌。 |
| `order_changed` | 某个 zone 的卡牌实例顺序发生变化。`same_membership` 表示是否只是顺序变化，还是成员也变了。 |
| `draw_pile_order_changed` | 抽牌堆顺序是否变化的快捷布尔值。 |
| `choices` | 预留字段，用于未来记录选择项和选择结果。当前为空。 |
| `random_results` | 预留字段，用于未来记录随机结果。当前为空。 |
| `hp_changes` | 预留字段，用于未来记录 HP 变化。当前为空。 |
| `block_changes` | 预留字段，用于未来记录 Block 变化。当前为空。 |
| `power_changes` | 预留字段，用于未来记录 power 变化。当前为空。 |
| `orb_changes` | 预留字段，用于未来记录 orb / channel / evoke 变化。当前为空。 |
| `summon_changes` | 预留字段，用于未来记录召唤物 / Osty / minion 变化。当前为空。 |
| `auto_play_children` | 预留字段，用于未来记录自动出牌父子关系。当前为空。 |
| `unresolved` | 当前诊断明确知道自己还不能完整分类的效果说明。 |

### 常见嵌套差分字段

移动类差分通常包含：

| 字段 | 含义 |
| --- | --- |
| `recorder_card_instance_id` | 发生移动或变化的卡牌实例。 |
| `card_id` | 游戏卡牌 id。 |
| `card_model_id` | 游戏完整 model id。 |
| `card_title` | 本地化卡牌名称。 |
| `owner_net_id` | 拥有者玩家 net id。 |
| `from` | 变化前位置，通常包含 `zone` 和 `zone_index`。 |
| `to` | 变化后位置，通常包含 `zone` 和 `zone_index`。 |

生成牌一般只有 `to`，没有 `from`。消失的牌一般只有 `from`，没有 `to`。

## 当前解释规则

- 所有 `diagnostic_*` 事件都只是诊断 / smoke test / 证据事件，不能当作最终 verified gameplay event。
- 如果某次出牌的 card pile diff 为空，不代表这张牌没有效果。它可能改变了 HP、Block、power、orb、召唤物等当前还未完整捕获的系统。
- 当前 `state_after` 发生在最终 result-pile 清理之前，所以判断打出的牌最终会去哪里时，要看 `card_play.result_pile`。
- Transform 效果在当前阶段可能表现为 `cards_removed` 加 `cards_created`，直到之后验证出稳定的 transform hook。
- `recorder_card_instance_id` 只在同一个记录 session 内对同一个运行时对象引用稳定，不跨 session 稳定。
