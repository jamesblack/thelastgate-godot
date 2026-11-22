# Opens the sprite viewer on the game's sprites and the overrides in data/.
#
# Usage: powershell -ExecutionPolicy Bypass -File tools/view-sprites.ps1 [-GodotPath <path to the Godot executable>]
# The Godot path can also come from the GODOT environment variable.
param([string]$GodotPath)

$defaultGodot = Join-Path $env:LOCALAPPDATA 'Microsoft\WinGet\Packages\GodotEngine.GodotEngine.Mono_Microsoft.Winget.Source_8wekyb3d8bbwe\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64.exe'

$godot = $GodotPath
if (-not $godot) { $godot = $env:GODOT }
if (-not $godot) { $godot = $defaultGodot }

if (-not (Test-Path -LiteralPath $godot -PathType Leaf)) {
    Write-Error ("Godot wasn't found at '$godot'. Pass -GodotPath, set the GODOT environment variable, " +
        "or install Godot 4.7.2 .NET with winget so it is at '$defaultGodot'.")
    exit 1
}

$gameFolder = Join-Path (Split-Path $PSScriptRoot -Parent) 'game'

# Godot only builds the C# code when run from the editor, so a scene started from the command line
# would use whatever was built last.
& dotnet build (Join-Path $gameFolder 'TheLastGate.csproj') --nologo -v quiet
if ($LASTEXITCODE -ne 0) {
    Write-Error 'The game failed to build, so the sprite viewer was not opened.'
    exit $LASTEXITCODE
}

& $godot --path $gameFolder 'res://Tools/sprite_viewer.tscn'
