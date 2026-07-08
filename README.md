# CombatManager

CombatManager is a From The Depths mod based on the working structure of
EndlessShapes Unlimited. V1.1 adds a standalone AI movement sandbox for
previewing what common combat behaviours look like around a target.

- `plugin.json` tells From The Depths which assembly to load.
- `CombatManager.dll` contains a `GamePlugin_PostLoad` implementation and the
  in-game AI overlay.
- `CombatManager/Source` contains the C# project and solution.
- `build.ps1` builds, verifies, stages, zips, and can install the runtime mod
  folder into the local From The Depths `Mods` directory.

## Requirements

- From The Depths installed locally.
- .NET SDK.

The build script uses `$env:FTD_DIR` when set. If it is not set, it falls back
to the standard Steam install path:

```powershell
C:\Program Files (x86)\Steam\steamapps\common\From The Depths
```

## Build

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1
```

## Build and Install

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -Install
```

This installs the staged runtime folder to:

```powershell
C:\Users\<you>\Documents\From The Depths\Mods\CombatManager
```

## AI Sandbox

Press `Ctrl+Shift+C` to open/close the sandbox. The hotkey is guarded against
text input, but it no longer depends on a focused craft or build mode.

The V1.1 overlay is read-only toward the real game. It shows:

- A large target-centered top-down X/Z grid.
- Circle, point-at, and broadside behaviour presets.
- Simulated craft marker movement, heading, tangent travel direction, range
  ring, range label, and ghost trail.
- Playback controls for play, pause, step, and reset.
- Optional one-shot `Import Current AI` seeding from the focused craft.

Imported AI settings are copied once into the sandbox. The mod does not keep
scanning the craft, change targets, or write card/mainframe values.
