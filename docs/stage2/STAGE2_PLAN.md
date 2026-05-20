# Stage 2 Plan

## 原则

阶段 2 只接受真实游戏逻辑提交点 hook。实现顺序是：

1. 收集候选。
2. 验证 type/method/signature。
3. 验证它是提交点，不是 UI 预览点。
4. 用户确认 `verified`。
5. 只为该 verified 目标写 hook。
6. 部署后正负向测试。

## Stage 2A：发现与验证

可以做：

- 从 `sts2.dll` 静态字符串生成候选线索。
- 记录反编译、STS2_MCP、运行时反射证据。
- 校验候选证据是否完整。
- 定义 Stage 2 event schema。

不可以做：

- 创建 gameplay patch 类。
- 调用 `PatchAll()`。
- 使用猜测方法名。
- 把 UI 事件当作玩家动作提交。

## Stage 2B：诊断桥与已验证 hook 实现

最终 gameplay hook 只在候选被标记为 `verified` 后实现。诊断桥可以在候选阶段用于 smoke test，但必须使用 `diagnostic_*` 事件名，不能伪装成最终 gameplay event。

推荐验证顺序：

1. end turn / combat lifecycle：提交点较清晰，适合建立验证流程。
2. card play submit：验证 `Hook.AfterCardPlayed` 是否只在成功出牌后触发，并区分 manual / auto-play。
3. card play result：新增出牌前后状态快照和差分，用于记录所有职业卡牌效果。

## Card-play 过程要求

Card-play 不能只记录“打出了哪张牌”。根据 `v0.105.1` 快照，547 张卡中大量卡牌会改变其他手牌、牌堆、生成牌、状态牌、自动打出其他牌、触发延迟效果、改变 power/orb/summon/entity 状态。

因此 card-play 必须拆成：

- 提交记录：谁打出了哪张牌、目标、资源、是否自动打出、play series。
- 结果记录：出牌前状态、出牌后状态、自动计算出的状态差分。

必须优先覆盖这些差分类型：

- 手牌、抽牌堆、弃牌堆、消耗堆的卡牌实例和顺序变化。
- 抽牌、弃牌、消耗、移动、生成、复制、Transform、Upgrade、费用/临时属性变化。
- 敌我 HP、Block、power/buff/debuff 变化。
- 机器人 orb/channel/evoke 变化。
- 骨系 summon / Osty / minion 状态变化。
- 玩家选择和随机结果。
- 自动打牌的父子关系。

验证用例不能只用 Strike/Defend。至少要覆盖：

- 盗贼：`DAGGER_THROW` / `PREPARED` / `ACROBATICS` / `CALCULATED_GAMBLE` / `STORM_OF_STEEL`。
- 机器人：生成状态牌到弃牌堆或手牌的卡，如 `OVERCLOCK` / `TURBO` / `COMPACT`。
- 骨系：生成 Soul、召唤或移动弃牌堆到手牌的卡，如 `REAVE` / `SEVERANCE` / `DIRGE` / `DREDGE`。
- 储君：置顶、Transform、重复打出，如 `PHOTON_CUT` / `COSMIC_INDIFFERENCE` / `BEGONE` / `DECISIONS_DECISIONS`。
- 战士：消耗其他牌、牌顶打出或弃牌堆置顶，如 `BURNING_PACT` / `TRUE_GRIT` / `FIEND_FIRE` / `HEADBUTT` / `HAVOC`。

## 证据要求

每个 verified 候选必须记录：

- `area`
- `target_type`
- `method`
- `signature`
- `source_evidence`
- `layer`
- `commit_point_reasoning`
- `positive_test`
- `negative_test`
- `payload_fields`
- `status=verified`

## 风险控制

- 版本/hash 不匹配时不得 patch gameplay。
- hook 异常只能写 `errors.log`，不得影响游戏。
- 一次只加一个 hook。
- 每个 hook 都必须证明取消路径不会写最终 action event。