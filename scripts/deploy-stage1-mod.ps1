param(
    [string]$GameDir = "D:/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2"
)

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent $PSScriptRoot
& "$PSScriptRoot/build-stage1-mod.ps1" -GameDir $GameDir

$ProjectDir = Join-Path $RepoRoot "mod/STS2ManualActionRecorder"
$BuildDll = Join-Path $ProjectDir "bin/Release/net9.0/STS2ManualActionRecorder.dll"
$Manifest = Join-Path $ProjectDir "STS2ManualActionRecorder.json"
$TargetDir = Join-Path $GameDir "mods/STS2ManualActionRecorder"

New-Item -ItemType Directory -Force -Path $TargetDir | Out-Null
Copy-Item $BuildDll (Join-Path $TargetDir "STS2ManualActionRecorder.dll") -Force
Copy-Item $Manifest (Join-Path $TargetDir "STS2ManualActionRecorder.json") -Force

Write-Host "Deployed Stage 1 mod to $TargetDir"
