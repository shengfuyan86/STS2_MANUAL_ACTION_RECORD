# Slay the Spire 2 真人操作记录项目计划书

## 1. 目标

建立一个本地项目，在真人游玩 Slay the Spire 2 时，以事件触发方式记录：

- 玩家所有可还原的操作：出牌、选目标、结束回合、选择奖励、跳过奖励、进入房间、商店购买/移除、事件选项、营火选择、地图选择等。
- 操作前后的角色与局内状态变化：生命、格挡、能量、金币、楼层、房间、手牌/牌堆/弃牌/消耗牌、遗物、药水、怪物状态、增益/减益等。
- 与版本绑定的元信息：游戏版本、主程序集 hash、mod 版本、记录 schema 版本。

核心原则：不依赖轮询来推断玩家动作；轮询/快照只作为校验和补漏。主记录路径必须来自游戏运行时内的事件、回调或 Harmony patch。

## 2. 已验证事实

本计划只基于当前已验证的信息展开，不假设不存在的官方 API。

### 2.1 本地游戏安装

你的 Slay the Spire 2 路径：

`D:/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2`

已确认关键文件：

- `SlayTheSpire2.exe`
- `SlayTheSpire2.pck`
- `data_sts2_windows_x86_64/sts2.dll`
- `data_sts2_windows_x86_64/sts2.runtimeconfig.json`
- `data_sts2_windows_x86_64/0Harmony.dll`
- `data_sts2_windows_x86_64/GodotSharp.dll`
- `mods/STS2_MCP.dll`
- `mods/STS2_MCP.conf`
- `mods/STS2_MCP.json`

本地版本文件 `release_info.json` 显示：

- game version: `v0.105.1`
- commit: `d5e30a22`
- date: `2026-05-08T19:47:15-07:00`
- branch: `v0.105.1`
- main_assembly_hash: `1363691567`

运行时文件 `sts2.runtimeconfig.json` 显示：

- target framework: `net9.0`
- included framework: `Microsoft.NETCore.App 9.0.7`

### 2.2 本地 MCP / mod 状态

本机已经安装 STS2 MCP：

- `mods/STS2_MCP.conf`
  - id: `STS2_MCP`
  - name: `STS2 MCP`
  - author: `kunology`
  - version: `0.4.0`
  - `has_dll: true`
  - `has_pck: false`
  - `affects_gameplay: false`
- `mods/STS2_MCP.json`
  - port: `15526`

这说明当前环境已经具备“游戏内 mod DLL + 本地 MCP bridge”的基础，不需要先从零证明 Slay the Spire 2 能加载 C# mod。

### 2.3 当前项目目录

当前项目路径：

`d:/code/pycharm/sts2_manual_action_record`

目前只发现：

- `.venv/`
- `.idea/`
- `.claude/`

没有实际业务源代码。因此本项目可以按新项目组织。

## 3. 外部资料结论

### 3.1 游戏与更新节奏

Slay the Spire 2 当前处于 Early Access。官方 Steam 页面与公告显示游戏仍在持续更新，后续计划包括 Steam Workshop、更多角色/卡牌/事件/遗物/药水等内容。结论：记录器必须把游戏版本和主程序集 hash 写入每条 run 元数据，不能假设卡牌、遗物、方法名或存档结构长期稳定。

### 3.2 Modding 路径

社区当前主流路径是 C# mod + manifest + 可选 Godot `.pck` 资源，放入游戏 `mods` 目录加载。当前本机已存在多个 mod 和 `STS2_MCP.dll`，并且游戏目录内已有 `0Harmony.dll`，所以本项目优先采用：

- C#/.NET mod DLL 作为游戏内 recorder。
- Harmony patch 作为触发式记录的主要技术手段。
- STS2_MCP 作为调试/发现工具：检查运行时对象、场景树、程序集、方法名，帮助定位 patch 点。

### 3.3 Wiki / 卡牌遗物数据

当前没有确认到可作为唯一权威 API 的官方卡牌/遗物 wiki。可用来源包括 SpireWiki、wiki.gg、StratGG、Steam 新闻、社区 datamine 项目等，但这些应作为辅助参考。

项目里的卡牌/遗物知识库应按优先级处理：

1. 优先从本地游戏数据/程序集/资源中抽取 ID、名称、描述、稀有度、类型等。
2. 社区 wiki 只作为中文/英文解释、分类、机制说明的补充。
3. 每次游戏版本变化后重新生成知识库快照。
4. 所有知识库条目都记录 `source`、`game_version`、`main_assembly_hash`、`extracted_at`。

## 4. 总体架构

```text
Slay the Spire 2 runtime
        |
        | C# mod / Harmony patches / game callbacks
        v
Recorder Mod
        |
        | append NDJSON / optional localhost stream
        v
Local Event Store
        |
        +--> Raw events: runs/<run_id>/events.ndjson
        +--> Snapshots: runs/<run_id>/snapshots.ndjson
        +--> Metadata: runs/<run_id>/metadata.json
        |
        v
Python Tooling
        |
        +--> validate event chain
        +--> diff action-before/action-after states
        +--> build card/relic wiki snapshot
        +--> export training/evaluation datasets
        +--> optional MCP server for AI clients
```

## 5. 模块设计

### 5.1 Recorder Mod（必须）

职责：运行在游戏进程内，以触发式方式记录真人操作。

技术路线：

- 目标运行时：`.NET 9`。
- 交付物：`STS2ManualActionRecorder.dll` + `STS2ManualActionRecorder.conf/json`。
- 安装位置：`D:/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2/mods/STS2ManualActionRecorder/` 或同目录下按当前 mod 规范放置。
- Patch 工具：优先复用游戏目录已有 `0Harmony.dll`。
- 发现工具：优先使用现有 `STS2_MCP`，端口 `15526`，配合 ILSpy/dnSpy 静态查看 `sts2.dll`。

最低可行记录点：

1. run 开始/结束。
2. combat 开始/结束。
3. 回合开始/结束。
4. 出牌动作：卡牌 ID、手牌 index、目标 ID/位置、能量前后、抽弃消耗变化。
5. 药水使用。
6. 奖励选择/跳过。
7. 地图节点选择。
8. 事件选项选择。
9. 商店购买/移除。
10. 营火选择。

重要约束：具体 patch 方法名不在计划书里编造。第一阶段必须通过 STS2_MCP 和 `sts2.dll` 反查真实方法，再把 patch 点写进代码和测试清单。

### 5.2 Event Store（必须）

MVP 直接写本地文件，避免先引入网络服务导致不稳定。

建议目录：

```text
%APPDATA%/STS2ManualActionRecorder/
  config.json
  runs/
    <run_id>/
      metadata.json
      events.ndjson
      snapshots.ndjson
      errors.log
```

每条事件为一行 JSON，避免游戏崩溃时整文件损坏。

基础事件结构：

```json
{
  "schema_version": "0.1.0",
  "game_version": "v0.105.1",
  "main_assembly_hash": 1363691567,
  "recorder_version": "0.1.0",
  "run_id": "...",
  "seq": 123,
  "time_utc": "2026-05-19T00:00:00Z",
  "event_type": "card_played",
  "context": {
    "floor": 7,
    "room_type": "combat",
    "combat_turn": 2
  },
  "action": {
    "card_id": "...",
    "card_instance_id": "...",
    "target_id": "..."
  },
  "state_before": {},
  "state_after": {}
}
```

`state_before/state_after` 在 MVP 中可以先记录摘要，后续再扩大到完整状态。

### 5.3 Python Tooling（必须）

Python 部分放在当前项目中，负责离线处理，不直接参与游戏内 patch。

建议目录：

```text
sts2_manual_action_record/
  pyproject.toml
  README.md
  src/sts2_record/
    __init__.py
    config.py
    schema.py
    ingest.py
    validate.py
    export.py
    wiki_extract.py
  tests/
    test_schema.py
    test_validate.py
  mod/
    STS2ManualActionRecorder/
      STS2ManualActionRecorder.csproj
      ModEntry.cs
      Patches/
      Serialization/
```

Python 职责：

- 校验 NDJSON 是否可解析。
- 校验 `seq` 连续性。
- 校验每个 action 的 `state_before -> state_after` 是否存在基本一致性。
- 合并同一 run 的事件和快照。
- 导出给 AI 使用的数据集，例如 JSONL、Parquet 或 SQLite。
- 生成/更新卡牌和遗物知识库快照。

### 5.4 可选 MCP Server（二期）

不要把 MCP server 放进 MVP 的关键路径。原因：当前痛点是“轮询式 MCP 不能记录真人操作”，因此 MVP 应先解决触发式记录。MCP 更适合作为后续读取和查询记录的接口。

二期可以提供一个独立 MCP server：

- `list_runs`
- `get_run_metadata`
- `get_events`
- `get_state_at_seq`
- `query_card_knowledge`
- `query_relic_knowledge`
- `export_run_for_ai`

它读取本项目 Event Store，不控制游戏。

## 6. 实施阶段

### 阶段 0：基线确认（0.5 天）

产出：环境报告。

任务：

1. 记录当前游戏版本：`v0.105.1`、commit、assembly hash。
2. 备份当前 `mods` 列表和 STS2_MCP 配置。
3. 启动游戏并确认 STS2_MCP 在 `15526` 端口可用。
4. 用 STS2_MCP 或静态反编译列出关键程序集类型和方法候选。

验收：

- 能明确写出 5-10 个真实候选 patch 点。
- 能区分哪些是输入 UI 层方法，哪些是游戏逻辑层方法。

### 阶段 1：最小 Recorder Mod（1-2 天）

产出：可加载但只写生命周期事件的 mod。

任务：

1. 建立 C# mod 项目。
2. 配置引用：`sts2.dll`、`GodotSharp.dll`、`0Harmony.dll` 及必要依赖。
3. 实现 mod 初始化。
4. 实现 append-only NDJSON writer。
5. 写入 `recorder_loaded`、`run_detected`、`combat_detected` 等安全事件。
6. 启动游戏验证不会影响正常游玩。

验收：

- 游戏能启动。
- mod 能加载。
- 文件能写入。
- 退出游戏后 NDJSON 完整可读。
- 未出现明显卡顿或崩溃。

### 阶段 2：核心操作记录（2-4 天）

产出：能记录一局普通战斗内关键真人操作。

任务：

1. 通过 STS2_MCP/反编译确认真实出牌调用链。
2. patch 出牌逻辑层方法，不 patch 纯 UI hover/drag 方法作为主记录点。
3. 记录卡牌 ID、实例 ID、目标、能量、抽弃消耗摘要。
4. patch 结束回合。
5. patch 药水使用。
6. 为每个操作记录 before/after snapshot。
7. 写 Python validator 检查事件链。

验收：

- 真人打一场战斗，事件序列能按顺序还原：回合开始 -> 出牌/用药/结束回合 -> 敌方行动 -> 下一回合。
- 与人工观察的视频或截图对照，关键操作没有漏记。
- 记录不依赖固定频率轮询。

### 阶段 3：非战斗选择记录（2-3 天）

产出：能覆盖从地图到奖励再到下一房间的 run 流程。

任务：

1. patch 地图节点选择。
2. patch 卡牌奖励选择/跳过。
3. patch 遗物获取。
4. patch 事件选项。
5. patch 商店购买、移除、离开。
6. patch 营火选项。

验收：

- 一局从第 1 层到至少 Boss 前，房间路径和选择可还原。
- 奖励选择、跳过、商店购买不会混淆。

### 阶段 4：知识库与版本化（2-4 天）

产出：本地卡牌/遗物/药水/事件知识库快照。

任务：

1. 确认游戏数据来源：优先从 `.pck`、本地资源或 `sts2.dll` 中提取。
2. 提取稳定 ID、显示名、描述、类型、稀有度、角色/颜色、数值字段。
3. 建立 `data/game_v0.105.1/` 快照。
4. 引入社区 wiki 作为补充字段，但不能覆盖本地抽取的 ID 和数值。
5. 写版本 diff：当游戏更新后，比较新增/删除/变更条目。

验收：

- 至少能离线查询当前版本所有已抽取卡牌和遗物。
- 每个条目有来源和版本。
- 游戏更新后可重新生成，不需要手工维护全部数据。

### 阶段 5：AI/MCP 查询接口（二期，2-3 天）

产出：给 AI 使用的只读 MCP server。

任务：

1. 用 Python 实现独立 MCP server。
2. 提供 run/event/state/wiki 查询工具。
3. 支持按 `run_id`、`seq`、`floor`、`event_type` 查询。
4. 支持导出“某一步之前 AI 应看到的信息”和“真人实际动作”。

验收：

- AI 能读取某局数据并复盘真人决策。
- MCP server 不需要连接正在运行的游戏。
- 与 STS2_MCP 职责分离：STS2_MCP 用于游戏调试/嵌入发现，本项目 MCP 用于记录数据查询。

## 7. 风险与应对

### 7.1 Early Access 更新破坏 patch 点

风险：游戏更新后方法名、类型、字段、存档结构变化。

应对：

- 每条记录写入 `game_version` 和 `main_assembly_hash`。
- patch 初始化时检查版本，不匹配则进入 safe mode，只写 `version_mismatch`，不强行 patch。
- 为每个支持版本维护 patch manifest。
- 每次更新先跑“发现脚本 + smoke test”。

### 7.2 patch UI 层导致误记

风险：拖动卡牌、取消选择、hover 都可能被误认为操作。

应对：

- 主 patch 点选择游戏逻辑提交点，而不是 UI 预览点。
- UI 层事件只作为调试信息，不进入最终 action 事件。

### 7.3 记录影响游戏性能或稳定性

风险：同步写文件过多导致卡顿。

应对：

- 事件入内存队列，后台 flush。
- 每行 NDJSON 尽量小，完整快照只在关键点记录。
- writer 异常不得影响游戏逻辑。

### 7.4 Wiki 数据不稳定或不权威

风险：社区 wiki 更新延迟或字段错误。

应对：

- 本地游戏抽取为主。
- wiki 只作为补充解释。
- 记录来源与抽取时间。

### 7.5 MCP 误用为实时轮询方案

风险：又回到“轮询当前界面”的问题。

应对：

- MVP 不依赖 MCP 记录动作。
- MCP 只用于：开发期发现 patch 点、二期查询已经记录的数据。

## 8. 第一版 MVP 范围

MVP 不追求一开始覆盖所有机制，只证明“真人动作可触发式记录”。

必须包含：

- C# recorder mod 可加载。
- 写入 NDJSON。
- 记录游戏版本和 recorder 版本。
- 记录 run/combat/turn 生命周期。
- 记录出牌、目标、结束回合。
- 记录操作前后基础状态：hp、energy、block、gold、floor、hand/draw/discard/exhaust 数量、怪物 hp 摘要。
- Python validator 能读取并检查一场战斗。

暂不包含：

- 全量卡牌/遗物 wiki。
- 完整 MCP server。
- 自动训练数据管线。
- 完整 UI 回放器。

## 9. 推荐下一步

下一步不要直接写大规模代码。先做“patch 点发现报告”：

1. 启动 Slay the Spire 2 和 STS2_MCP。
2. 连接 `localhost:15526`。
3. 列出 `sts2.dll` 中与 card/play/turn/reward/map/shop/event/campfire 相关的类型与方法。
4. 用一场手动战斗验证哪些方法只在动作提交时触发。
5. 固化第一批 patch 点后，再创建 C# recorder mod。

这样可以避免计划建立在猜测的方法名上，也能把实现风险控制在第一天内暴露。