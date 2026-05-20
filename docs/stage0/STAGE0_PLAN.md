# Stage 0 Implementation Plan

## 目标

把本机 Slay the Spire 2 环境、已装 MCP/mod、未来 recorder mod 的 patch 点发现流程固定下来。

## 步骤

### 1. 环境基线采集

运行：

```bash
.venv/Scripts/python.exe -m sts2_record.cli stage0 write-report --game-dir "D:/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2"
```

检查报告中是否包含：

- `release_info.json` 的版本、commit、branch、`main_assembly_hash`。
- `steam_appid.txt`。
- `sts2.runtimeconfig.json` 的 target framework。
- `sts2.deps.json` 中的 `0Harmony`、`GodotSharp` 等依赖版本。
- `sts2.dll`、`GodotSharp.dll`、`0Harmony.dll` 是否存在。

### 2. Mod 与 STS2_MCP 盘点

检查报告中的 `Installed mods` 与 `STS2_MCP` 区域。

重点确认：

- `STS2_MCP.dll` 存在。
- `STS2_MCP.conf` 端口是 `15526`。
- `STS2_MCP.json` 标记 `affects_gameplay=false`。
- 其他 mod 是否可能影响后续 recorder 行为。

### 3. MCP 可达性验证

先启动游戏，确认 STS2_MCP 已加载，再运行：

```bash
.venv/Scripts/python.exe -m sts2_record.cli stage0 probe-mcp --mcp-port 15526
```

结果含义：

- `tcp_reachable`：端口可连接，可以继续用 MCP/运行时工具做方法发现。
- `not_reachable`：游戏未启动、mod 未加载、端口变化或被防火墙拦截。

### 4. Patch 点发现

先用静态扫描获得关键词线索：

```bash
.venv/Scripts/python.exe -m sts2_record.cli stage0 scan-strings --game-dir "D:/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2"
```

然后用 STS2_MCP 或反编译工具验证真实类型/方法，不要直接使用静态线索。

优先验证这些区域：

1. 出牌提交点。
2. 目标选择提交点。
3. 结束回合提交点。
4. 战斗开始/结束。
5. 奖励选择/跳过。
6. 地图节点选择。
7. 商店购买/移除。
8. 事件选项。
9. 营火选项。
10. 药水使用。

### 5. 阶段 0 完成条件

在 `PATCH_POINT_DISCOVERY.md` 中补齐 5-10 个候选 patch 点。每个点必须说明：

- 来源：STS2_MCP、反编译、日志实验等。
- 层级：UI 层或游戏逻辑层。
- 验证方式：什么真人操作会触发，什么取消/预览操作不会触发。
- 是否接受进入阶段 1。