param(
    [string]$GameDir = "D:/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2"
)

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent $PSScriptRoot
$DataDir = Join-Path $GameDir "data_sts2_windows_x86_64"
$Required = @("sts2.dll", "GodotSharp.dll", "0Harmony.dll")
foreach ($Name in $Required) {
    $Path = Join-Path $DataDir $Name
    if (-not (Test-Path $Path)) {
        throw "Missing required game assembly: $Path"
    }
}

$ProjectPath = Join-Path $RepoRoot "mod/STS2ManualActionRecorder/STS2ManualActionRecorder.csproj"
dotnet build $ProjectPath -c Release /p:STS2GameDir="$GameDir"
