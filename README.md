# CombatManager

CombatManager is a From The Depths mod based on the working structure of
EndlessShapes Unlimited. V1.3 adds target profiles and a standalone AI pursuit
simulation for previewing what common combat behaviours look like around moving
targets.

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

The V1.3 overlay is read-only toward the real game. It shows:

- An opaque target-centered top-down X/Z tactical grid.
- Circle, point-at, broadside, and Naval 2.0 behaviour presets.
- Static, slow mover, ship, fast mover, and plane target profiles.
- Simulated craft pursuit toward mirrored AI steer points.
- Craft trail, AI steer-point trail, target future path, range labels, and
  optional legend.
- Playback controls for play, pause, step, reset, zoom, fit orbit, trail
  visibility, target path, and target/craft motion settings.
- Optional one-shot `Import Current AI` seeding from the focused craft.

Imported AI settings are copied once into the sandbox. The mod does not keep
scanning the craft, change targets, or write card/mainframe values.

## Research Notes

- [From The Depths AI internals](docs/from-the-depths-ai-internals.md)
  summarizes how mainframes, cards, behaviours, manoeuvres, target snapshots,
  and control requests work. Use it as the source map for future simulator
  fidelity work.
