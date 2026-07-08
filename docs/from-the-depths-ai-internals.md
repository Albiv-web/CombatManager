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

## Common Card Mapping

Observed card-to-routine mapping:

| Card | Behaviour | Manoeuvre | Notes |
| --- | --- | --- | --- |
| `AICirclingShipCard` | `BehaviourCircleAtDistance` | `ManoeuvreHover` | On-water adjuster, altitude ignored |
| `AICirclingTankCard` | `BehaviourCircleAtDistance` | `ManoeuvreHover` | On-land adjuster, altitude ignored |
| `AICirclingPlaneCard` | `BehaviourCircleAtDistance` | `ManoeuvreAirplane` | Above-ground/sea adjuster |
| `AICirclingHoverCard` | `BehaviourCircleAtDistance` | `ManoeuvreHover` | Above-ground/sea adjuster |
| `AIFrontalHoverCard` | `BehaviourPointAndMaintainDistance` | `ManoeuvreHover` | Point at and hold range |
| `AIBombingPlaneCard` | `BehaviourBombingRun` | `ManoeuvreAirplane` | Out of V1.3 scope |
| `AIBombingHoverCard` | `BehaviourBombingRun` | `ManoeuvreHover` | Out of V1.3 scope |
| `AINavalMovementCard` | `FtdNaval` | `FtdNavalAndLandManoeuvre` | Deprecated card migration |
| `AILandControLCard` | `FtdNaval` | `FtdNavalAndLandManoeuvre` | Deprecated card migration, land adjuster |
| `AIAerialMovementCard` | `FtdAerial` | `FtdAerialMovement` | Deprecated card migration, aerial behaviour |

This means the phrase "ship card" is not enough for simulation. CombatManager
must read the selected manoeuvre or movement card result, not infer movement
from the card's display name.

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

## Next Research Targets

To improve fidelity, decompile and summarize these next:

- `FiringAngleCalculator`: exact broadside 2.0 firing-angle point selection.
- `Adjustment` and `WaypointRelocation`: terrain, water, bearing, and altitude
  point adjustment.
- `SeaSurfacePathfinding`: water/terrain path validity.
- `AiVehicleManoeuvreCommonVariables`: PID defaults and helper methods.
- `FtdAiWrapper`: how the game maps `IPlatformInterface` requests into craft
  control requests.
- `AiTargetManager`: target selection, target velocity choice, and engagement
  target lifecycle.

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
