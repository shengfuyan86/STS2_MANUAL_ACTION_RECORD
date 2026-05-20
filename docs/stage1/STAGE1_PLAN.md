# Stage 1 Plan

## 目标

实现最小 DLL-only recorder mod，验证游戏能加载它，并且它能写入可校验的 NDJSON。

## 范围

包含：

- C# mod 项目。
- 现代 STS2 manifest。
- `[ModInitializer(nameof(Initialize))]` 入口。
- `recorder_loaded` 事件。
- append-only NDJSON writer。
- Python validator。
- 构建、部署、验证脚本。

不包含：

- 玩家出牌、目标、结束回合等动作记录。
- 未验证 gameplay Harmony patch。
- BaseLib 依赖。
- 自动修改游戏目录。

## 关键约束

- manifest 标记 `affects_gameplay=false`。
- Harmony 只初始化，不 `PatchAll()`。
- writer 异常不得影响游戏。
- 所有事件写 `game_version` 和 `main_assembly_hash`。
- 静态字符串扫描只作为后续发现线索，不作为 patch 目标。

## 验收

- `dotnet build` 成功。
- 部署后游戏可启动。
- 加载 mod 后生成 `metadata.json` 和 `events.ndjson`。
- `events.ndjson` 至少包含 `recorder_loaded`。
- Python validator 通过。