# CombatManager

CombatManager is a From The Depths mod based on the working structure of
EndlessShapes Unlimited. V1.6 presents the symmetric Blue-vs-Red duel sandbox
as a fullscreen editor: Blue/player controls are fixed on the left, Red/enemy
controls are fixed on the right, and the Red-centered tactical graph fills the
middle.

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

The V1.6 editor is read-only toward the real game. It shows:

- An opaque fullscreen Red-centered top-down X/Z tactical grid.
- Fixed Blue/player and Red/enemy panels with independent scrolling.
- Blue and Red mainframe controls for circle, point-at, broadside, and Naval 2.0.
- Ship/tank, hover, six-axis, and airplane manoeuvre simulation for both sides.
- Ship duel, broadside duel, hover duel, and plane intercept presets.
- Raw steer bearing, finite motion point, desired facing, trails, range labels,
  and optional legend for both simulated craft.
- Top-toolbar controls for scenario presets, play, pause, step, reset, zoom,
  fit duel, trail visibility, and tactical overlays.
- Optional one-shot `Import Blue AI` seeding from a selected mainframe on the
  focused craft. Red remains manually configured.

Imported AI settings are copied once into Blue. The mod does not keep scanning
the craft, change targets, or write card/mainframe values.

## Research Notes

- [From The Depths AI internals](docs/from-the-depths-ai-internals.md)
  summarizes how mainframes, cards, behaviours, manoeuvres, target snapshots,
  and control requests work. Use it as the source map for future simulator
  fidelity work.
