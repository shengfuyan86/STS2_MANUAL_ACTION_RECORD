# Stage 0: Baseline and Patch-Point Discovery

阶段 0 的目标是为后续 recorder mod 建立可重复的事实基础。

## 输入

- Slay the Spire 2 安装目录。
- 本机已安装的 `mods/` 目录。
- `release_info.json`、`sts2.runtimeconfig.json`、`sts2.deps.json`。
- 可选：运行中的 STS2_MCP，默认端口 `15526`。

## 输出

- `reports/stage0-baseline.json`
- `reports/stage0-baseline.md`
- 经人工/运行时验证后的 patch 点表格。

## 命令

```bash
.venv/Scripts/python.exe -m sts2_record.cli stage0 write-report --game-dir "D:/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2"
```

如果 STS2_MCP 没有随游戏运行，报告里的 MCP 状态会是 `not_reachable`。这不是失败，只表示还没有完成运行时验证。

## 阶段 0 验收标准

- 报告记录当前游戏版本、commit 和主程序集 hash。
- 报告记录运行时、关键 DLL、已装 mod 和 STS2_MCP 配置。
- 报告明确 MCP 当前是否可连接。
- `PATCH_POINT_DISCOVERY.md` 最终列出 5-10 个真实候选 patch 点。
- 每个候选点标明来源、验证步骤、层级判断和是否接受。

## 不做的事

- 不安装 recorder mod。
- 不修改游戏目录。
- 不启动或控制游戏。
- 不把静态字符串扫描结果当作真实 patch 点。