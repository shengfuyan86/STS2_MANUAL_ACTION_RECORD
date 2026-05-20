# Patch Point Discovery

本文件用于记录阶段 0 发现并验证的真实候选 patch 点。

不要把 `scan-strings` 产生的静态字符串线索直接写成已接受 patch 点。静态线索只能填在 `source` 或 `notes` 里，最终必须经过 STS2_MCP、反编译或运行时实验验证。

## 层级判定

### UI 层，不作为主记录点

这些方法通常只能作为调试线索：

- hover
- drag
- preview
- cancel
- highlight
- button focus
- tooltip
- visual animation

原因：玩家可能拖动、预览或取消，不能代表动作已经提交。

### 游戏逻辑层，优先作为主记录点

这些才是 recorder 的主要 patch 目标：

- card action committed
- target selected and action accepted
- turn ended
- reward selected or skipped
- map node committed
- shop purchase/removal committed
- event option committed
- campfire option committed
- potion use committed
- combat/run lifecycle transition

原因：这些点更接近状态实际变化，能还原真人操作序列。

## 验证表格

| Area | Candidate type/method | Source | Validation step | Layer | Accepted? | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| card play | TBD | static hint / STS2_MCP / decompiler | Play a card; verify drag/cancel does not trigger | TBD | no | Fill after validation |
| target selection | TBD | static hint / STS2_MCP / decompiler | Use targeted card; verify target commit only | TBD | no | Fill after validation |
| end turn | TBD | static hint / STS2_MCP / decompiler | Click end turn; verify one event per submitted end turn | TBD | no | Fill after validation |
| combat start/end | TBD | static hint / STS2_MCP / decompiler | Enter and finish combat | TBD | no | Fill after validation |
| reward select/skip | TBD | static hint / STS2_MCP / decompiler | Select and skip rewards separately | TBD | no | Fill after validation |
| map node | TBD | static hint / STS2_MCP / decompiler | Click a map node; verify preview does not trigger | TBD | no | Fill after validation |
| shop purchase/remove | TBD | static hint / STS2_MCP / decompiler | Buy card/relic/potion and remove card | TBD | no | Fill after validation |
| event option | TBD | static hint / STS2_MCP / decompiler | Choose event option; verify hover does not trigger | TBD | no | Fill after validation |
| campfire option | TBD | static hint / STS2_MCP / decompiler | Rest/smith/etc.; verify final selection | TBD | no | Fill after validation |
| potion use | TBD | static hint / STS2_MCP / decompiler | Use potion; verify discard/cancel behavior | TBD | no | Fill after validation |

## 接受标准

候选点进入阶段 1 前必须满足：

1. 方法/类型名来自真实运行时或反编译结果。
2. 能说明触发它的真人操作。
3. 能说明不会被 hover、drag、preview、cancel 误触发，或明确标记为 UI 调试点。
4. 能解释该点能采集哪些字段：卡牌、目标、房间、奖励、状态摘要等。
5. 标记为 `Accepted? = yes`。