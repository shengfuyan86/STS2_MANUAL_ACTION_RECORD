# Stage 1: Minimal Recorder Mod

阶段 1 的目标是证明 `STS2ManualActionRecorder` 可以作为 Slay the Spire 2 C# mod 加载，并安全写入 append-only NDJSON。

本阶段不记录玩家动作，不 patch 未验证 gameplay 方法，也不把 Stage 0 的静态字符串线索当作真实 patch 点。

## 产出

- `mod/STS2ManualActionRecorder/STS2ManualActionRecorder.csproj`
- `mod/STS2ManualActionRecorder/STS2ManualActionRecorder.json`
- `mod/STS2ManualActionRecorder/STS2ManualActionRecorder.dll`（构建后）
- `%APPDATA%/STS2ManualActionRecorder/runs/<session_id>/metadata.json`
- `%APPDATA%/STS2ManualActionRecorder/runs/<session_id>/events.ndjson`

## 构建

```powershell
scripts/build-stage1-mod.ps1
```

或直接运行：

```bash
dotnet build mod/STS2ManualActionRecorder/STS2ManualActionRecorder.csproj -c Release
```

## 部署

部署会写入游戏 `mods` 目录，需用户明确执行：

```powershell
scripts/deploy-stage1-mod.ps1
```

部署目标：

```text
D:/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2/mods/STS2ManualActionRecorder/
```

只复制 recorder DLL 和 manifest，不复制游戏自带 DLL。

## 验证输出

游戏启动并加载 mod 后，运行：

```powershell
scripts/verify-stage1-output.ps1
```

或手动指定文件：

```bash
.venv/Scripts/python.exe -m sts2_record.cli stage1 validate-events --events "%APPDATA%/STS2ManualActionRecorder/runs/<session_id>/events.ndjson"
```

## 当前事件

阶段 1 必须能写：

- `recorder_loaded`

`run_detected` 和 `combat_detected` 的 schema 已定义，但需要后续找到可靠生命周期入口后再写入。