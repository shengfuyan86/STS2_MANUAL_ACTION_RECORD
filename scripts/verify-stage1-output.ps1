$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent $PSScriptRoot
$RunsDir = Join-Path $env:APPDATA "STS2ManualActionRecorder/runs"
if (-not (Test-Path $RunsDir)) {
    throw "Runs directory not found: $RunsDir"
}

$Latest = Get-ChildItem $RunsDir -Directory | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if ($null -eq $Latest) {
    throw "No recorder run directories found under $RunsDir"
}

$Events = Join-Path $Latest.FullName "events.ndjson"
$Python = Join-Path $RepoRoot ".venv/Scripts/python.exe"
$env:PYTHONPATH = Join-Path $RepoRoot "src"
& $Python -m sts2_record.cli stage1 validate-events --events $Events
