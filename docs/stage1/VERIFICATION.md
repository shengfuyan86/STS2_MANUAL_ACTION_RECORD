# Stage 1 Verification

## Static verification

- [ ] `dotnet build mod/STS2ManualActionRecorder/STS2ManualActionRecorder.csproj -c Release` succeeds.
- [ ] `STS2ManualActionRecorder.dll` is produced.
- [ ] `STS2ManualActionRecorder.json` parses.
- [ ] Manifest has `has_dll=true` and `has_pck=false`.
- [ ] Manifest has `affects_gameplay=false`.
- [ ] No gameplay patch classes are present.

## Deployment verification

Run only when ready to modify the game `mods` directory:

```powershell
scripts/deploy-stage1-mod.ps1
```

Then verify:

- [ ] `mods/STS2ManualActionRecorder/STS2ManualActionRecorder.dll` exists.
- [ ] `mods/STS2ManualActionRecorder/STS2ManualActionRecorder.json` exists.
- [ ] No `sts2.dll`, `GodotSharp.dll`, or `0Harmony.dll` was copied into the mod folder.

## Runtime smoke test

- [ ] Start Slay the Spire 2.
- [ ] Reach main menu.
- [ ] Confirm `%APPDATA%/STS2ManualActionRecorder/runs/` exists.
- [ ] Confirm latest session has `metadata.json`.
- [ ] Confirm latest session has `events.ndjson`.
- [ ] Confirm `events.ndjson` contains `recorder_loaded`.
- [ ] Exit game normally.
- [ ] Run `scripts/verify-stage1-output.ps1`.
- [ ] Validator reports `ok=true`.

## Notes

Record any runtime loader errors here before moving to Stage 2.
