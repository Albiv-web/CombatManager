# CombatManager

CombatManager is a From The Depths mod based on the working structure of
EndlessShapes Unlimited. V1 adds a passive AI intent visualizer for inspecting
what the focused craft's mainframe is trying to do.

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

## AI Intent Visualizer

In build mode on a craft, press `Ctrl+Shift+C` to open/close the visualizer.

The V1 overlay is read-only. It shows:

- AI mainframes on the focused craft.
- Selected behaviour and manoeuvre routines.
- A top-down X/Z grid with craft, target, desired steer point, desired facing,
  maintain-distance rings, and current movement requests.
- Passive predictions for Broadside 2.0/naval, broadside, point-at, and circle
  behaviours.
- A draggable sandbox target when the AI has no live target.

Unsupported behaviours still show routine names, warnings, and current control
requests without trying to guess their logic.
