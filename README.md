# CombatManager

CombatManager is a From The Depths mod based on the working structure of
EndlessShapes Unlimited. V2.2 presents a symmetric Blue-vs-Red AI duel sandbox
with editable AI blueprints: Blue/player controls are tabbed on the left,
Red/enemy controls are tabbed on the right, and the tactical graph fills the
middle with top-down 2D plus Unity-rendered 3D view modes. Blueprints
mirror the vanilla mainframe, behaviour, manoeuvre, and adjustment setup so
presets, import, simulation, and the future write-to-craft flow all share one
data shape. The Blue Import tab also has a read-only Live Parity harness that
compares observed focused-craft AI control requests against CombatManager's
vanilla-mapped prediction.

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

The V2.2 editor is read-only toward the real game. It shows:

- An opaque fullscreen tactical graph with top-down 2D and drag-to-3D altitude
  view modes, plus Red, Blue, and Freecam camera modes.
- A Unity-rendered 3D graph scene with altitude pillars, climb/dive trails,
  range rings, intent markers, and fixed-size IMGUI labels.
- Mouse-wheel graph zoom from 0.25x to 8x, with marker/text size fixed in screen
  pixels while world geometry scales.
- Freecam drag-pan and Fit Duel recentering.
- Clean, Tactical, and Debug graph detail modes with label collision avoidance
  and a scale bar.
- Tabbed Blue/player and Red/enemy panels with independent scrolling.
- Blue and Red mainframe controls for circle, point-at, broadside, and Naval 2.0.
- Aerial Attack Run 1.0, Attack Run 2.0, and Attack Run 3.0 sandbox behaviours
  with vanilla state-machine defaults and approximation labels.
- Ship/tank, hover, six-axis, and airplane manoeuvre simulation for both sides.
- Ship duel, broadside duel, hover duel, plane intercept, and aerial attack
  presets.
- Raw steer bearing, finite motion point, desired facing, trails, range labels,
  and optional legend for both simulated craft.
- Top-toolbar controls for scenario presets, play, pause, step, reset, graph
  view mode, detail mode, zoom, and fit duel.
- Larger, clearer HUD text and graph labels with opaque backplates.
- Optional one-shot `Import Blue AI` seeding from a selected mainframe on the
  focused craft. Red remains manually configured.
- Preset AI blueprints for slow ship broadsiders, fast point-at planes, hover
  snipers, circle ships, aircraft interceptors, and preview-only close rammers.
- A Blue export preview that lists the vanilla mainframe/card mutations a later
  guarded writer would apply, while explicitly performing no real writes.
- Live Parity, default off, for observed-vs-predicted `AiControlType` request
  comparison against the focused craft's current selected mainframe.
- Stable Auto/Both broadside side selection so Naval 2.0 does not flicker
  between left/right intent when the target bearing is tied around 0/180 degrees.

Imported AI settings are copied once into Blue. The mod does not keep scanning
the craft, change targets, or write card/mainframe values. Live Parity may read
continuously while enabled, but it never syncs sandbox settings or mutates the
real craft.

## Research Notes

- [From The Depths AI internals](docs/from-the-depths-ai-internals.md)
  summarizes how mainframes, cards, behaviours, manoeuvres, target snapshots,
  and control requests work. Use it as the source map for future simulator
  fidelity work.
