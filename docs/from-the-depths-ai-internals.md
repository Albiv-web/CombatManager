# From The Depths AI Internals Notes

CombatManager uses this document as the working map for AI simulation. It is
based on focused local decompilation and reflection of the From The Depths
managed assemblies, then summarized here in our own words. Do not check
decompiled game source into this repository.

Research snapshot:

- Game version in `CombatManager/plugin.json`: `4.3.3`
- Local install: `C:\Program Files (x86)\Steam\steamapps\common\From The Depths`
- Assemblies inspected: `Ai.dll`, `Ftd.dll`, `Common.dll`
- Assembly timestamp for `Ai.dll` and `Ftd.dll`: `2026-06-30 16:29:03`
- Tool: `ilspycmd 10.1.0.8386`
- Scratch output: `artifacts/research/decompiled/` and ignored by git

Refresh recipe:

```powershell
$managed = 'C:\Program Files (x86)\Steam\steamapps\common\From The Depths\From_The_Depths_Data\Managed'
ilspycmd -l c -r $managed (Join-Path $managed 'Ai.dll')
ilspycmd -l c -r $managed (Join-Path $managed 'Ftd.dll')
ilspycmd -r $managed -t 'BrilliantSkies.Ai.AiMaster' (Join-Path $managed 'Ai.dll')
ilspycmd -r $managed -t 'BrilliantSkies.Ai.Modules.Behaviour.BehaviourCircleAtDistance' (Join-Path $managed 'Ai.dll')
ilspycmd -r $managed -t 'AISimpleCard`2' (Join-Path $managed 'Ftd.dll')
```

## Mental Model

FTD separates combat AI into two layers:

1. A behaviour decides the desired combat intent: target, steer point, distance,
   facing, side, altitude, and internal behaviour state.
2. A manoeuvre turns that steer point into `AiControlType` requests such as
   thrust, yaw, pitch, roll, strafe, and hover.

That split matters for CombatManager. A circle behaviour with hover movement
does not act like the same circle behaviour with airplane movement. The
behaviour tells us where the AI wants to go; the selected movement card or
manoeuvre tells us how the craft tries to get there.

## Mainframe And Pack Flow

Important classes:

- `AIMainframe` in `Ftd.dll`
- `AiNode` in `Ftd.dll`
- `AiMaster` in `Ai.dll`
- `AiMasterPack` in `Ai.dll`
- `AICard`, `AICardSlot`, `AISimpleCard<TBehaviour,TManoeuvre>`, and
  deprecated card classes in `Ftd.dll`

`AIMainframe` owns an `AiMasterPack`. The mainframe registers itself in
`MainConstruct.iBlockTypeStorage.MainframeStore`, which is the right entry point
for CombatManager import/inspection.

`AiNode` wraps the mainframe into a runtime node. It exposes craft position,
rotation, velocity, weapon side counts, processing power, target manager, and
the `AiMaster`.

`AiMasterPack` stores:

- `Movement`: `Off`, `Manual`, `Automatic`, or `Fleet`
- `Firing`: `Off` or `On`
- `Priority`: the highest active priority controls movement
- `SelectedBehaviourId`
- `SelectedManoeuvreId`
- `Common`: shared PID/controller variables
- `Adjustments`: path/terrain/altitude/sea-surface adjustment settings
- `Packages`: behaviours, manoeuvres, and additional routines

Selection details:

- `GetSelectedBehaviour(out IBehaviour)` only returns the behaviour whose
  unique id matches `SelectedBehaviourId`.
- `GetSelectedManoeuvre(out IManoeuvre)` first tries `SelectedManoeuvreId`, then
  falls back to the first manoeuvre package if the selected id is missing.
- `EnsureSomethingSelected()` selects the first behaviour when nothing is
  selected.

Runtime movement flow:

1. `AiNode.AiControlRoutine()` calls `AiMaster.RunFixedUpdate()` if the player is
   not overriding controls and the craft is not tractored.
2. `AiMaster.RunFixedUpdate()` skips movement when movement is `Off`, the player
   overrides controls, or this mainframe is not highest priority.
3. The selected manoeuvre is required for movement.
4. In `Automatic`, the selected behaviour is asked to move against the current
   target.
5. If the behaviour returns `true`, it has created a combat `SteerPoint` and
   called `manoeuvre.GoHere()`.
6. If no behaviour handles movement, the AI falls back to waypoint/order logic.
7. Additional routines run after the main movement branch.

## Cards And Slots

AI cards are physical blocks connected through AI card slots. The connection
flow increases card capacity and installs or unlocks routines in the
mainframe's `AiMasterPack`.

`AICardSlot`:

- Adds one `CardSlots` count to the node.
- Allows cards to connect to the mainframe through slot geometry.

`AiCardDepreciated`:

- Old card base class.
- Increases `RoutineAvailability.Available`.
- Some deprecated movement cards create generated behaviour/manoeuvre routines
  when loaded from old designs.

`AISimpleCard<TBehaviour,TManoeuvre>`:

- Newer card pattern.
- Stores fixed behaviour and manoeuvre GUIDs.
- On connection, looks for existing packages with those GUIDs.
- If missing, creates the behaviour and manoeuvre, adds them to the pack,
  selects them, applies adjuster defaults, and sets the AI to automatic.
- On permanent removal, removes those packages again.

GPP cards are separate. `AiGPPCard` adds processing power for detection systems;
it is not the same as behaviour routine capacity.

## Blueprint Writer Map

V1.8 adds CombatManager blueprints as the draft shape for a later guarded
writer. The important vanilla mapping is:

- Mainframe/basic settings:
  - `AiMasterPack.Movement`: default `Automatic`
  - `AiMasterPack.Firing`: default `On`
  - `AiMasterPack.Priority`: default `0`, vanilla UI range `-500..500`
  - selected behaviour/manoeuvre are stored as `SelectedBehaviourId` and
    `SelectedManoeuvreId`
- Routine capacity:
  - `RoutineAvailability.Available` is increased by connected AI card slots.
  - `RoutineAvailability.Used` counts non-manoeuvre routines, so behaviour and
    additional routines consume capacity while manoeuvres do not.
  - `AiMasterPack.RemovePackage()` clears selected IDs when a selected routine
    is removed and decrements capacity usage for non-manoeuvres.
  - `AiMasterPack.Load()` recomputes used capacity by counting packages whose
    routine type is not manoeuvre.
- Creating/selecting:
  - `AiMasterPack.NewPackage()` creates a package and immediately calls
    `Setup(platform, Common)`.
  - `MakeSystem(Guid)` searches `AiBehaviour`, `AiManoeuvre`, then
    `AiAdditional` attributes and constructs the matching routine class.
  - Future writes should create or reuse the desired routine, set its scalar
    fields, then update the selected behaviour/manoeuvre IDs.

Supported V1.8 blueprint classes:

| Blueprint field | Vanilla class |
| --- | --- |
| Circle behaviour | `BehaviourCircleAtDistance` |
| Point At behaviour | `BehaviourPointAndMaintainDistance` |
| Broadside 1.0 behaviour | `BehaviourBroadside` |
| Broadside 2.0 / Naval behaviour | `FtdNaval` |
| Ship/Tank manoeuvre | `FtdNavalAndLandManoeuvre` |
| Hover manoeuvre | `ManoeuvreHover` |
| Six-axis manoeuvre | `ManoeuvreSixAxis` |
| Airplane manoeuvre | `ManoeuvreAirplane` |

The V1.8 export preview is deliberately dry-run only. It reports the selected
focused mainframe, routine capacity, routine classes that would be created or
selected, scalar fields that would be set, adjustment defaults, and unsupported
fields. It does not modify mainframe names, card slots, packages, selected IDs,
routine values, or real craft AI state.

## Common Card Mapping

Observed card-to-routine mapping:

| Card | Behaviour | Manoeuvre | Notes |
| --- | --- | --- | --- |
| `AICirclingShipCard` | `BehaviourCircleAtDistance` | `ManoeuvreHover` | On-water adjuster, altitude ignored |
| `AICirclingTankCard` | `BehaviourCircleAtDistance` | `ManoeuvreHover` | On-land adjuster, altitude ignored |
| `AICirclingPlaneCard` | `BehaviourCircleAtDistance` | `ManoeuvreAirplane` | Above-ground/sea adjuster |
| `AICirclingHoverCard` | `BehaviourCircleAtDistance` | `ManoeuvreHover` | Above-ground/sea adjuster |

## Aerial Attack Runs

V2.2 focused decompilation added the first aerial combat map for the 3D
sandbox. The scratch source remains under ignored `artifacts/`; this section is
the committed summary.

`FtdAerial` is vanilla "Attack run 1.0 (with flyover)". It has two runtime
states: towards target and away from target. While towards, it aims at the
target plus `FlyoverHeight` and switches away when ground distance is below the
lower bombing-run bracket. While away, it keeps the yaw it had at breakoff,
flies roughly 250m along that bearing, uses the cruise altitude from
`MinimumAndCruiseAltitude`, and reengages when ground distance exceeds the
upper bracket or `EngageOverrideTime` elapses. Defaults observed: bracket
50m-250m, wait 15s, attack altitude 0m, minimum/cruise 20m/75m.

`BehaviourBombingRun` is vanilla "Attack run 2.0 (with U-turn)". It tracks
`RunActive`, `LastRunFinishedAt`, and an abort timer. While active it attacks
until below `BreakoffDistance`, below `BreakoffAltitude`, or past `AbortTime`
after entering `AbortTimeStartDistance`. While inactive it reengages past
`ReengageDistance` or after `ReengageTime`. It points at the target during the
run, pitch-locks when close and within about 10 degrees azimuth, and otherwise
uses the behaviour altitude setting. Defaults observed: breakoff 400m,
reengage 1000m/15s, pitch-to-target 800m, abort 20s from 0m, breakoff altitude
-1000m, preferred altitude commonly 200m from the bombing plane card.

`BehaviourAircraft` is vanilla "Attack run 3.0 (combines 1.0 and 2.0)". It
shares the 2.0 breakoff/reengage/abort shape, adds `EngagementAltitude`,
`IgnoreAltitude`, `UsePrediction`, `PointDirect`, and `Flyover`, and can choose
between U-turn and flyover flee behaviour. Defaults observed: engagement
altitude 100m above target, prediction off, point-direct 400m, flyover off,
ignore altitude on.

`ManoeuvreAirplane` always requests forward thrust, uses idle thrust only near
placeholder/idling distance, controls altitude through hover output, and uses
pitch/yaw/roll differently during banking turns. `FtdAerialMovement` is the
older Airplane 1.0 manoeuvre: it always thrusts forward, yaw-turns at smaller
azimuth, bank-turns above its upper azimuth threshold, and uses hover/pitch/yaw
to change altitude.

CombatManager V2.2 mirrors the attack/flee state machines and their public
scalar defaults, then feeds the resulting altitude into the sandbox movement
model. The following remain approximate: vanilla PID outputs, exact roll/pitch
coupling, predictive interception math, waypoint relocation/path adjustment,
terrain/sea-surface safety, and propulsion/drag/block-layout physics.
| `AIFrontalHoverCard` | `BehaviourPointAndMaintainDistance` | `ManoeuvreHover` | Point at and hold range |
| `AIBombingPlaneCard` | `BehaviourBombingRun` | `ManoeuvreAirplane` | Out of V1.3 scope |
| `AIBombingHoverCard` | `BehaviourBombingRun` | `ManoeuvreHover` | Out of V1.3 scope |
| `AINavalMovementCard` | `FtdNaval` | `FtdNavalAndLandManoeuvre` | Deprecated card migration |
| `AILandControLCard` | `FtdNaval` | `FtdNavalAndLandManoeuvre` | Deprecated card migration, land adjuster |
| `AIAerialMovementCard` | `FtdAerial` | `FtdAerialMovement` | Deprecated card migration, aerial behaviour |

This means the phrase "ship card" is not enough for simulation. CombatManager
must read the selected manoeuvre or movement card result, not infer movement
from the card's display name.

Focused card defaults observed so far:

| Source | Defaults relevant to CombatManager |
| --- | --- |
| `AICirclingShipCard` | Circle + hover movement, altitude ignored, preferred altitude `0`, adjustment `OnWater`, terrain prediction `10s`, water depth `10m`, min land/water `0m`, max altitude `0m` |
| `AICirclingTankCard` | Circle + hover movement, altitude ignored, preferred altitude `0`, adjustment `OnLand`, terrain prediction `10s`, land height `5m`, min land/water `0m`, max altitude `0m` |
| `AICirclingHoverCard` | Circle + hover movement, preferred altitude `200m`, adjustment `Above`, terrain prediction `10s` |
| `AICirclingPlaneCard` | Circle + airplane movement, preferred altitude `200m`, adjustment `Above`, terrain prediction `10s`, airplane pitch-for-altitude `15deg` |
| `AIFrontalHoverCard` | Point At + hover movement, preferred altitude `200m`, adjustment `Above`, terrain prediction `10s` |
| `AINavalMovementCard` | Deprecated migration installs `FtdNaval` + `FtdNavalAndLandManoeuvre`, adjustment `OnWater`, min land/water `0m`, max altitude `0m` |

## Target Data

`TargetPositionInfo` is the key target snapshot passed to behaviours:

- `Valid`
- `Position`
- `Velocity`
- `Direction`: target position minus craft position
- `Range`: full 3D distance
- `GroundDistance`: X/Z distance
- `Azimuth`: signed yaw from craft forward to target direction
- `Elevation`
- `ElevationForAltitudeComponentOnly`
- `AltitudeAboveSeaLevel`

`AiNode.GetTargetPositionInfoForEngagementTarget()` asks the selected manoeuvre
whether it uses velocity instead of forward. If there is no manoeuvre, or if the
manoeuvre has `IsUsingVelocityInsteadOfForward` enabled, target acquisition is
requested with that velocity-aware flag.

## SteerPoint

Behaviours write into `SteerPoint`; manoeuvres read it.

Key fields:

- `GamePos`: desired world point
- `LocalPos`: desired point in craft local coordinates
- `ActualImpliedTravelDistance`: distance that manoeuvres use for idling and
  completion logic
- `Azimuth`: angle from craft forward or craft velocity to the steer point
- `Elevation`
- `AltitudeAboveMeanSeaLevel`
- `RotationToEndOn`: desired final facing
- `PitchLocked` and `RollLocked`: behaviour can force pitch/roll demands
- `Reverse` and `HappyToReverse`
- `GoalType`: combat, waypoint, or placeholder waypoint

`SteerPoint.LargeDistance` is `1000f`. Several behaviours use this as a "keep
driving in this bearing" cue. It is not always a real destination. CombatManager
therefore needs separate concepts for raw AI steer bearing and finite sandbox
motion point.

When `IsUsingVelocityInsteadOfForward` is enabled, `SteerPoint` blends current
velocity more strongly than craft forward when calculating azimuth and
elevation. This is important for aircraft and fast movers.

## Control Requests

Manoeuvres write movement intent via `IPlatformInterface.MakeRequest()` or
`SetRequest()`. The enum is:

- `ThrustForward`, `ThrustBackward`
- `StrafeRight`, `StrafeLeft`
- `HoverUp`, `HoverDown`
- `YawRight`, `YawLeft`
- `PitchUp`, `PitchDown`
- `RollRight`, `RollLeft`
- `PrimaryIncrease`, `PrimaryDecrease`, `SecondaryIncrease`,
  `SecondaryDecrease`, `TertiaryIncrease`, `TertiaryDecrease`
- `PrimaryRun`, `SecondaryRun`, `TertiaryRun`
- `A`, `B`, `C`, `D`, `E`

For live import/status, CombatManager can read `platform.GetRequest(type)`. For
standalone simulation, it should generate equivalent high-level kinematics from
the selected manoeuvre model rather than trying to drive real FTD controls.

## Behaviour Logic To Mirror

### Circle

Class: `BehaviourCircleAtDistance`

Important settings:

- `DistanceToMaintain`
- `PreferredSide`: `Both`, `Left`, `Right`
- `MinApproachAngle`
- `AltitudeType`: `Absolute`, `Relative`, `Ignore`
- `PreferredAltitude`
- optional roll/evasion settings

Planner summary:

1. Reject invalid targets.
2. Compute approach angle:
   - Base is 90 degrees.
   - Add range correction based on `(desiredDistance - groundDistance) / 200`.
   - Clamp that correction to the configured minimum approach angle envelope.
   - Flip sign for `Right`, or for `Both` when target azimuth is positive.
3. Build desired facing by looking at the target and adding the approach angle.
4. Build a point 1000m forward along that facing, at the configured altitude.
5. Run the adjustment layer.
6. Set the steer point with `LargeDistance` and the desired rotation.
7. Call the selected manoeuvre.

CombatManager implication:

- The raw 1000m point should be displayed as a steer bearing or collapsed into a
  finite sandbox motion point.
- The simulated craft should not treat the raw 1000m point as a throttle target.

### Point At And Maintain Distance

Class: `BehaviourPointAndMaintainDistance`

Important settings:

- `DistanceToMaintain`
- `UseCurrentDistanceIfLower`
- `AzimuthBeforeReverse`
- `PitchToTarget`
- `PitchWithinAzi`
- optional left/right evasion times
- altitude settings

Planner summary:

1. Reject invalid targets.
2. Compute flat direction from craft to target.
3. Desired point is target position minus flat direction times desired distance.
4. If "use current distance if lower" is enabled, desired distance is capped to
   the current ground distance.
5. If the desired distance is greater than current distance and target azimuth is
   inside the reverse cone, set `HappyToReverse`.
6. Desired facing points at the target, with pitch clamped or disabled depending
   on azimuth.
7. Pitch is locked.
8. Optional evasion shifts the point laterally by about 100m.
9. Adjustment layer can modify the point before `SteerPoint.SetGoal()`.

CombatManager implication:

- This is a finite point planner and maps cleanly to sandbox pursuit.
- Reverse behavior depends on manoeuvre support, so movement model matters.

### Broadside 1.0

Class: `BehaviourBroadside`

Important settings:

- `AngleToMaintain`
- `DistanceToMaintain` lower/upper bracket

Planner summary:

1. Reject invalid targets.
2. Compute direction from craft to target.
3. Rotate that direction by the negative broadside angle.
4. Build a point 100m from craft along that rotated direction.
5. Clamp the resulting point so its range from target is within the configured
   min/max bracket.
6. Set desired facing along the broadside vector.
7. Set steer point with `LargeDistance` and call the manoeuvre.

CombatManager implication:

- Desired facing and desired point can be approximated well in 2D.
- The movement result still depends on whether the manoeuvre can strafe, reverse,
  turn in place, or must keep forward speed.

### Broadside 2.0 / Naval

Class: `FtdNaval`, backed by `FtdLegacyCommon`

Important settings:

- `BroadsideDistance` lower/upper
- `MinimumBroadsideDistanceToMaintain`
- `NominalBroadsideAngle`
- `TurningCircle`
- `DepthRequirement`
- `DisableReverse`
- `MinimumFirepowerFractionBeforeSwitchingSide`
- `MaximumHullComShiftBeforeSwitchingSide`
- `AngleChangeFactor`
- `SwitchType`

Planner summary:

1. State begins as `closing`.
2. If closing and range is below the lower broadside distance, enter broadside.
3. If broadside and range is above the upper broadside distance, return to
   closing.
4. In broadside, choose side:
   - Enforce side from firepower imbalance if configured.
   - Enforce side from hull center of mass shift if configured.
   - Otherwise predict target position using `target.Position + target.Velocity *
     5` and choose left/right from signed angle to craft forward.
5. Adjust broadside angle based on selected side, switch type, and target motion.
6. Use `FiringAngleCalculator` plus adjustment/pathfinding to find a valid point
   that achieves the firing angle and range constraints.
7. If no point works, flip side for a cooldown, then retry.
8. Set steer point and call the manoeuvre.

CombatManager implication:

- V1 can mirror state transitions and side choice.
- Exact terrain, sea-surface, and firing-angle pathfinding require more
  decompilation and should stay labelled approximate.
- Target velocity matters more here than for simple broadside.

## Manoeuvre Logic To Mirror

### Ship Or Tank

Class: `FtdNavalAndLandManoeuvre`

Used by deprecated naval/land cards and selectable "Ship or tank" movement.

Main behavior:

- Keeps roll level.
- Optional hover control.
- Optional pitch control toward an ideal pitch.
- Thrusts backward if `SteerPoint.Reverse` is set.
- Otherwise thrusts forward.
- Forward thrust is reduced when steer azimuth is large:
  - full thrust up to roughly 50 degrees
  - scales down toward about 20 percent by roughly 135 degrees
- Yaws toward zero azimuth for forward travel.
- Yaws toward 180 degrees when reversing.
- Uses idle/tarry distance to stop at final waypoints.

CombatManager model:

- Good fit for heading-limited surface craft.
- Speed should be capped by configured max speed, acceleration, and turn rate.
- Turning and thrust should respond to target azimuth, not only distance.

### Hover Movement

Class: `ManoeuvreHover`

Main behavior:

- Uses local forward/back offset for thrust.
- Uses local lateral offset or lateral speed for strafe.
- If target yaw error is above `MoveWithinAzi`, forward is reduced to about
  30 percent and strafe is disabled.
- Yaw can target the waypoint bearing or the desired end rotation depending on
  combat/waypoint state.
- Pitch and roll can honor behaviour locks.
- Hover controls altitude to the steer point altitude.

CombatManager model:

- Good fit for 6-axis and hovercraft.
- Unlike ship/tank, it can move toward a point while facing another direction.
- Circle and point-at are much more stable with this movement model than with a
  surface-only forward-thrust model.

### Airplane 2.0

Class: `ManoeuvreAirplane`

Main behavior:

- Always requests forward thrust, except idle thrust can be reduced in placeholder
  waypoint wander.
- Uses yaw, pitch, roll, and hover controllers together.
- Rolls into banked turns when yaw error exceeds configured thresholds.
- Can use pitch to point at target altitude or to satisfy altitude controller.
- Can use strafe when heavily rolled.

CombatManager model:

- Needs an aircraft kinematic model with minimum forward speed, bank-limited turn
  rate, and altitude metadata.
- A simple "turn toward desired point" model will underrepresent overshoot and
  orbit widening.

### FTD Aerial Movement

Class: `FtdAerialMovement`

This is the deprecated/generated aerial movement used by old aerial movement
cards. It requests forward thrust, uses yawing/banking turn thresholds, and
controls altitude through pitch/yaw/hover logic. Treat it as aircraft movement
for V1.x, then split it from `ManoeuvreAirplane` if we need closer fidelity.

## CombatManager Implementation Rules

- Import through `MainConstruct.iBlockTypeStorage.MainframeStore`.
- Read routines via:
  - `mainframe.Node.Master.Pack.GetSelectedBehaviour(out IBehaviour)`
  - `mainframe.Node.Master.Pack.GetSelectedManoeuvre(out IManoeuvre)`
- Read target snapshot via:
  - `mainframe.Node.GetTargetPositionInfoForEngagementTarget()`
- Read current control requests only as status via `IPlatformInterface.GetRequest`.
- Do not call vanilla `IBehaviour.Move()` in the sandbox. It can mutate behaviour
  state, run adjustment/pathfinding, and call `manoeuvre.GoHere()`, which writes
  control requests into the real platform interface.
- Keep import one-shot. The standalone sandbox should not continuously scan or
  write craft AI state.
- Model behaviour and manoeuvre separately:
  - Behaviour planner: desired point, desired facing, range, side/state.
  - Manoeuvre model: how a craft type moves toward that intent.
- Keep raw steer points distinct from finite sandbox motion points.
- Label anything involving terrain, water depth, collision avoidance, firing
  angle pathfinding, PID tuning, or real propulsion physics as approximate.

## CombatManager Implementation Status

V1.4 moves the sandbox closer to the real FTD split:

- `AiBehaviourPlanner` is the shared planner layer for the standalone sandbox.
  It outputs raw steer point, finite sandbox motion point, desired facing,
  range, azimuth, side/state, and approximation notes.
- The sandbox now treats the selected manoeuvre/movement card as a separate
  movement model:
  - Ship/tank: yaw-limited forward/reverse movement, turn slowdown, and tarry
    distance.
  - Hover/six-axis: independent translation and desired-facing rotation with
    strafe authority.
  - Airplane: continuous forward motion, minimum speed, and turn-radius-limited
    heading changes.
- Import lists mainframes from the focused craft and requires selection when
  more than one supported mainframe is present. It imports behaviour,
  manoeuvre, movement mode, firing mode, priority, supported behaviour
  parameters, current read-only requests, and approximation context once only.
- The grid names raw steer bearing separately from the finite motion point so
  long vanilla steer points do not get confused with the sandbox chase point.

V1.5 turns that into a two-sided duel sandbox:

- The simulation now has Blue and Red `AiSimEntity` objects. Each has
  mainframe-like behaviour settings, manoeuvre model, craft profile, AI state,
  position, heading, velocity, trails, and warnings.
- Both entities build behaviour plans from the same pre-step snapshot, then both
  movement models advance. This avoids order bias where Red would react to
  Blue's already-updated position.
- The grid remains Red-centered so Red is always the target reticle, but Red
  itself also has AI intent and manoeuvre simulation.
- `AiManoeuvreSimulator` is shared by both sides and mirrors the known shape of
  FTD manoeuvres:
  - Ship/tank approximates tarry distance, reverse, yaw-to-steer, and
    azimuth-based thrust slowdown.
  - Hover approximates yaw lock, move-within-azimuth, forward reduction, and
    strafe suppression above azimuth.
  - Six-axis approximates independent forward/back, strafe, hover, yaw, and
    look-ahead facing.
  - Airplane approximates idle thrust, minimum forward motion, banked turning,
    and altitude/pitch metadata.
- We are confident about the architecture: behaviour selects intent and
  manoeuvre turns intent into requests. We are not claiming exact vanilla
  physics/PID/pathfinding yet.

V1.6 is a layout milestone on top of the same simulation model:

- The sandbox now opens as a fullscreen editor surface instead of a floating
  resizable window.
- Blue/player mainframe and movement controls live in the fixed left panel.
- Red/enemy mainframe and movement controls live in the fixed right panel.
- Scenario, playback, zoom, fit, and overlay toggles live in the top toolbar so
  the Red-centered graph can consume the entire center of the screen.
- No new live synchronization or write path was introduced; Blue import remains
  one-shot and Red remains manually configured.

V1.7 is a readability milestone on top of the same simulation model:

- Blue and Red side panels now use tabs so AI, movement, status, and import
  controls no longer compete in one long scroll.
- Toolbar controls are grouped into scenario, playback, and view controls.
- Text, buttons, sliders, and graph labels are larger, and graph labels use
  opaque backplates to remain readable over the tactical grid.
- No planner or manoeuvre simulation behaviour changed.

V1.8 adds an AI blueprint layer without adding writes:

- `AiMainframeBlueprint` is the shared draft format for Blue and Red. It stores
  mainframe fields, behaviour, manoeuvre, movement model knobs, adjustment
  defaults, and approximation warnings.
- Presets now create blueprints first, then sync the sandbox entity from that
  blueprint. Manual Blue/Red edits also capture back into the blueprint.
- `Import Blue AI` seeds the Blue blueprint once from the selected focused
  mainframe, then syncs the sandbox. It still does not continue scanning.
- The Blue import tab can build an `AiBlueprintExportPlan`. The plan is a
  dry-run list of future vanilla mutations plus warnings and routine-capacity
  status; it performs no package/card/mainframe changes.
- Built-in draft presets cover:
  - slow 2500m Naval 2.0 ship broadsider
  - fast airplane point-at
  - 3000m hover/six-axis sniper
  - circle ship
  - fast aircraft interceptor
  - close-range rammer preview, labelled unsupported until `BehaviourRam` is
    researched and mapped
- Known V1.8 writer gaps: no card-block creation/removal, no routine
  replacement prompt, no PID/common-variable writes, no additional routines,
  no breadboard writes, and no actual apply button.

V1.9 fixes an Auto/Both broadside stability bug:

- The sandbox previously re-ran broadside side selection as a stateless signed
  angle test every frame. When the target bearing hovered near `0` or the
  signed-angle wrap at `+/-180`, tiny numerical or motion changes could flip
  left/right every tick.
- Naval 2.0 and simple broadside Auto/Both now keep the existing resolved side
  inside a small deadband around those tied bearings, while still switching when
  one side is clearly favoured.
- This mirrors the important behaviour of vanilla's stateful
  `FtdLegacyCommon.state`: broadside side is part of behaviour state, not just a
  fresh UI preference calculation each rendered frame.

V2.0 adds a read-only parity harness and focused vanilla request mapping:

- `AiVanillaIntentPlan` is now the shared representation for supported
  behaviour intent. It carries the behaviour class, raw steer point, finite
  motion point, desired facing, range/azimuth, state/side, and approximation
  flags. The old live import predictor delegates through this same layer.
- `AiControlRequestPrediction` models the movement-card output layer: predicted
  `AiControlType`, value, source manoeuvre, confidence, and note.
- `AiLiveParitySnapshot` reads the focused construct, selected mainframe,
  selected behaviour/manoeuvre, target snapshot, craft pose/velocity, and the
  platform's current non-zero requests. It then predicts from the same snapshot
  and lists observed-vs-predicted deltas. It does not call `IBehaviour.Move`,
  does not call manoeuvre `Move`, and does not write requests or package state.
- The fidelity claim is now explicit: CombatManager targets vanilla behaviour
  intent plus movement-request parity. Exact propulsion layout, drag, block
  placement, terrain collision, sea state, pathfinding, and PID time history
  remain approximations unless the live parity deltas prove a mirrored mapping.

Focused V2.0 decompile findings:

- `FiringAngleCalculator` is used by broadside/Naval behaviour to test possible
  firing angles. It builds a short test point from craft-to-target direction and
  broadside angle, scales the firing angle around the midpoint between minimum
  broadside range and enter-broadside range, pushes too-close points back out to
  the minimum range, extends tiny waypoints to roughly 300m from the craft, and
  rejects points that fail adjustment/depth checks. CombatManager labels this as
  approximate because full firepower and sea-surface checks are not mirrored yet.
- `Adjustment` and `WaypointRelocation` form the common waypoint post-processing
  path. Behaviours produce candidate points, then adjustment can clamp altitude,
  avoid terrain/water, relocate around land or sea constraints, and apply
  bearing/pathfinding avoidance. CombatManager's 2D sandbox does not yet mutate
  steer points through these world-dependent passes.
- `MigratePack` stores default adjustment bundles used by manoeuvre/card
  defaults. Ship/tank defaults are water-oriented, including OnWater altitude
  reference, water depth, turning circle, and low max altitude; aerial defaults
  raise altitude-oriented constraints.
- `SeaSurfacePathfinding` is a world/path validity layer. It depends on the
  map/terrain/sea surface and therefore cannot be treated as pure sandbox math.
  V2.0 documents it but keeps sea-surface relocation marked approximate.
- `AiVehicleManoeuvreCommonVariables` owns the common PID helpers that
  manoeuvres use to emit `AiControlType` requests. Important sign mappings:
  positive roll output requests `RollRight`; negative pitch output requests
  `PitchUp`; yaw-to-zero output requests `YawLeft` when positive and `YawRight`
  when negative; positive hover output requests `HoverUp`; forward/back speed
  cancellation maps positive to `ThrustForward` and negative to
  `ThrustBackward`. Defaults include a 20m translational request scale, 30m
  terminal phase, and azimuth throttle drop-off helpers.
- `FtdAiWrapper` is the bridge from AI logic to craft controls. `MakeRequest`
  passes an `AiControlType` magnitude to `ControlsRestricted.MakeRequest`,
  `GetRequest` reads the last input for that axis, and `SetRequest` overwrites a
  request. Live Parity reads through this wrapper path only; it never writes.
- `AiTargetManager` owns target lifecycle and selection around target priority,
  validity, velocity, and engagement target data. V2.0 reads the selected
  engagement target snapshot exposed by the mainframe node; it does not mirror
  full target selection logic.

V2.0 movement-request mappings:

- Ship/tank (`FtdNavalAndLandManoeuvre`): when not idling, vanilla requests
  `ThrustForward` or `ThrustBackward` and yaws toward zero azimuth, with forward
  thrust reduced from 1.0 toward 0.2 between roughly 50 and 135 degrees of steer
  azimuth. Inside tarry distance it yaws to desired end rotation and cancels
  forward velocity. CombatManager mirrors the request signs and throttle curve,
  while PID gains and velocity history remain approximate.
- Hover (`ManoeuvreHover`): forward/back output follows local forward shift,
  strafe follows local lateral shift, yaw follows desired rotation or waypoint
  yaw, and altitude uses hover output. Above `MoveWithinAzi`, forward is reduced
  to 30% and strafe is disabled. CombatManager mirrors these request channels
  and labels exact PID magnitude approximate.
- Six-axis (`ManoeuvreSixAxis`): local lateral shift maps to
  strafe-left/right, local forward shift maps to thrust-forward/backward,
  altitude maps to hover, and yaw follows look-ahead desired facing.
  CombatManager mirrors those independent axes.
- Airplane (`ManoeuvreAirplane` / `FtdAerialMovement`): forward thrust is
  always requested, idle/placeholder distance can lower thrust, banked turns use
  roll above a configured azimuth threshold, yaw follows waypoint angle, and
  altitude feeds hover/pitch logic. CombatManager mirrors request signs and
  major thresholds, while roll/pitch coupling remains approximate.

## Next Research Targets

To improve fidelity further, decompile and summarize these next:

- exact behaviour planners for aerial, attack-run, bombing, ram/charge, and
  hover-above/below behaviours.
- more `Adjustment` and `WaypointRelocation` branches around terrain, water,
  bearing avoidance, and finite path relocation.
- more `AiTargetManager` branches for target scoring, target velocity choice,
  and target loss/retarget timing.
- PID default values and time-history effects for request magnitudes once Live
  Parity exposes stable deltas across real craft.

## Simulator Roadmap From This Research

1. Add exact planner notes for aerial, bombing, charge, and frontal behaviours.
2. Replace current approximated movement models with research-backed details
   from `AiVehicleManoeuvreCommonVariables`, `Adjustment`, and
   `FtdAiWrapper`.
3. Add Red-side or target-side import only after live target craft discovery is
   understood and can remain strictly read-only.
4. Add scenario copy/paste serialization once the scenario model has settled.
5. Add per-feature approximation badges: no pathfinding, no terrain, no PID,
   no propulsion physics, no target-priority logic.
6. Later, add a 3D/altitude view after the 2D target-centered lab is reliable.
