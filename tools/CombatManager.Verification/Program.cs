using System;
using CombatManager.Ai;
using CombatManager.Ui;
using UnityEngine;

namespace CombatManager.Verification
{
    internal static class Program
    {
        private static int Main()
        {
            try
            {
                Fix180WrapsAngles();
                SafePlanarDirectionIgnoresY();
                RotateYawMatchesUnityTopDownConvention();
                GroundDistanceUsesOnlyXZ();
                RedProjectionStaysCenteredWhileBothMove();
                SimultaneousDuelStepIsMirrorStable();
                BothEntitiesPlanIndependently();
                CirclePlannerSeparatesRawSteerFromMotionPoint();
                ShipTurnRateConstrainsHeadingChange();
                HoverAzimuthLimitReducesMovement();
                SixAxisMovesAndFacesWithLookAhead();
                AirplaneMaintainsForwardSpeed();
                ScenarioPresetsApplyBothMainframes();
                BuildDuelFrameDoesNotMutateNavalState();
                BlueImportNullDoesNotChangeRed();
                FullscreenLayoutKeepsPanelsInsideScreen();
                FullscreenLayoutKeepsGraphPositiveAt1280();
                FullscreenLayoutAssignsSeparateColumns();
                Console.WriteLine("CombatManager verification passed.");
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception);
                return 1;
            }
        }

        private static void Fix180WrapsAngles()
        {
            AssertNear(-179f, PlanarMath.Fix180(181f), "181 wraps to -179");
            AssertNear(179f, PlanarMath.Fix180(-181f), "-181 wraps to 179");
        }

        private static void SafePlanarDirectionIgnoresY()
        {
            Vector3 direction = PlanarMath.SafePlanarDirection(
                new Vector3(0f, 20f, 0f),
                new Vector3(0f, -10f, 10f),
                Vector3.right);
            AssertNear(0f, direction.x, "planar direction x");
            AssertNear(0f, direction.y, "planar direction y");
            AssertNear(1f, direction.z, "planar direction z");
        }

        private static void RotateYawMatchesUnityTopDownConvention()
        {
            Vector3 rotated = PlanarMath.RotateYaw(Vector3.forward, 90f);
            AssertNear(1f, rotated.x, "yaw right x");
            AssertNear(0f, rotated.y, "yaw right y");
            AssertNear(0f, rotated.z, "yaw right z");
        }

        private static void GroundDistanceUsesOnlyXZ()
        {
            float distance = PlanarMath.GroundDistance(new Vector3(0f, 100f, 0f), new Vector3(3f, -100f, 4f));
            AssertNear(5f, distance, "3-4-5 ground distance");
        }

        private static void RedProjectionStaysCenteredWhileBothMove()
        {
            var state = new AiSimulationState();
            state.ApplyScenarioPreset(AiScenarioPreset.ShipDuel);
            state.Step(3f);

            Rect rect = new Rect(20f, 10f, 900f, 500f);
            AiSimulationGridProjection projection = AiSimulationGridProjection.For(rect, state);
            Vector2 red = projection.WorldToScreen(state.Red.Position);
            AssertNear(rect.center.x, red.x, "red centered x");
            AssertNear(rect.center.y, red.y, "red centered y");
        }

        private static void SimultaneousDuelStepIsMirrorStable()
        {
            var state = new AiSimulationState();
            ConfigureSymmetricPointAt(state.Blue);
            ConfigureSymmetricPointAt(state.Red);
            state.ResetScenario();

            float beforeMidpoint = (state.Blue.Position.x + state.Red.Position.x) * 0.5f;
            state.Step(0.5f);
            float afterMidpoint = (state.Blue.Position.x + state.Red.Position.x) * 0.5f;
            float blueTravel = PlanarMath.GroundDistance(new Vector3(380f, 0f, 0f), state.Blue.Position);
            float redTravel = PlanarMath.GroundDistance(Vector3.zero, state.Red.Position);

            if (Math.Abs(afterMidpoint - beforeMidpoint) > 0.01f)
                throw new InvalidOperationException($"symmetric simultaneous step drifted midpoint: {beforeMidpoint} -> {afterMidpoint}");
            if (Math.Abs(blueTravel - redTravel) > 0.01f)
                throw new InvalidOperationException($"symmetric simultaneous step moved entities unequally: blue {blueTravel}, red {redTravel}");
        }

        private static void BothEntitiesPlanIndependently()
        {
            var state = new AiSimulationState();
            state.Blue.Preset = AiSimulationPreset.Circle;
            state.Blue.Side = AiSimulationSide.Left;
            state.Red.Preset = AiSimulationPreset.Broadside;
            state.Red.Side = AiSimulationSide.Right;
            state.ResetScenario();

            AiDuelFrame frame = state.BuildDuelFrame();
            if (frame.Blue.Kind != "Circle")
                throw new InvalidOperationException($"blue did not plan circle: {frame.Blue.Kind}");
            if (frame.Red.Kind != "Broadside")
                throw new InvalidOperationException($"red did not plan broadside: {frame.Red.Kind}");
            AssertNear(-state.Red.BroadsideAngle, frame.Red.BroadsideAngle, "red right broadside angle");
        }

        private static void CirclePlannerSeparatesRawSteerFromMotionPoint()
        {
            var state = new AiSimulationState();
            state.Blue.Preset = AiSimulationPreset.Circle;
            state.Blue.Side = AiSimulationSide.Left;
            state.Blue.Radius = 200f;
            state.ResetScenario();

            AiSimulationFrame frame = state.BuildDuelFrame().Blue;
            float rawDistance = PlanarMath.GroundDistance(frame.CraftPosition, frame.RawSteerPoint);
            float motionDistance = PlanarMath.GroundDistance(frame.CraftPosition, frame.MotionPoint);
            if (rawDistance <= motionDistance * 3f)
                throw new InvalidOperationException($"raw steer and motion point too close: raw {rawDistance}, motion {motionDistance}");
        }

        private static void ShipTurnRateConstrainsHeadingChange()
        {
            AiSimulationState slow = CreateShipTurnState(5f);
            AiSimulationState fast = CreateShipTurnState(180f);

            slow.Step(1f);
            fast.Step(1f);

            float slowTurned = Mathf.Abs(PlanarMath.SignedPlanarAngle(Vector3.forward, slow.Blue.Heading));
            float fastTurned = Mathf.Abs(PlanarMath.SignedPlanarAngle(Vector3.forward, fast.Blue.Heading));
            if (fastTurned <= slowTurned + 3f)
                throw new InvalidOperationException($"turn rate did not affect heading: slow {slowTurned}, fast {fastTurned}");
        }

        private static void HoverAzimuthLimitReducesMovement()
        {
            AiSimulationState strict = CreateHoverAzimuthState(1f);
            AiSimulationState permissive = CreateHoverAzimuthState(180f);

            strict.Step(1f);
            permissive.Step(1f);

            if (permissive.Blue.CraftCurrentSpeed <= strict.Blue.CraftCurrentSpeed)
                throw new InvalidOperationException($"hover azimuth limit did not reduce speed: strict {strict.Blue.CraftCurrentSpeed}, permissive {permissive.Blue.CraftCurrentSpeed}");
        }

        private static void SixAxisMovesAndFacesWithLookAhead()
        {
            var state = new AiSimulationState();
            state.ApplyScenarioPreset(AiScenarioPreset.HoverDuel);
            state.Blue.ApplyCraftProfile(AiCraftProfile.SixAxisDrone);
            state.Blue.SixAxisLookAheadDistance = 500f;
            state.ResetScenario();

            Vector3 before = state.Blue.Position;
            state.Blue.Radius += 120f;
            state.Step(1f);

            if (PlanarMath.GroundDistance(before, state.Blue.Position) <= 0.1f)
                throw new InvalidOperationException("six-axis model did not translate");
            if (float.IsNaN(state.Blue.Heading.x) || float.IsNaN(state.Blue.Heading.z))
                throw new InvalidOperationException("six-axis model produced NaN heading");
        }

        private static void AirplaneMaintainsForwardSpeed()
        {
            var state = new AiSimulationState();
            state.ApplyScenarioPreset(AiScenarioPreset.PlaneIntercept);
            for (int i = 0; i < 8; i++)
                state.Step(0.5f);

            if (state.Blue.CraftCurrentSpeed < state.Blue.AirplaneMinimumSpeed - 0.01f)
                throw new InvalidOperationException($"blue airplane fell below minimum speed: {state.Blue.CraftCurrentSpeed}");
            if (state.Red.CraftCurrentSpeed < state.Red.AirplaneMinimumSpeed - 0.01f)
                throw new InvalidOperationException($"red airplane fell below minimum speed: {state.Red.CraftCurrentSpeed}");
        }

        private static void ScenarioPresetsApplyBothMainframes()
        {
            var state = new AiSimulationState();
            state.ApplyScenarioPreset(AiScenarioPreset.BroadsideDuel);
            if (state.Blue.Preset != AiSimulationPreset.NavalBroadside || state.Red.Preset != AiSimulationPreset.NavalBroadside)
                throw new InvalidOperationException("broadside duel did not set both entities to Naval 2.0");

            state.ApplyScenarioPreset(AiScenarioPreset.PlaneIntercept);
            if (state.Blue.CraftMovementModel != AiCraftMovementModel.Airplane || state.Red.CraftMovementModel != AiCraftMovementModel.Airplane)
                throw new InvalidOperationException("plane intercept did not set both entities to airplane movement");
        }

        private static void BuildDuelFrameDoesNotMutateNavalState()
        {
            var state = new AiSimulationState();
            state.ApplyScenarioPreset(AiScenarioPreset.BroadsideDuel);
            state.Step(0.1f);
            AiSimulationNavalState blue = state.Blue.NavalState;
            AiSimulationNavalState red = state.Red.NavalState;

            for (int i = 0; i < 10; i++)
                state.BuildDuelFrame();

            if (state.Blue.NavalState != blue || state.Red.NavalState != red)
                throw new InvalidOperationException("BuildDuelFrame mutated naval state");
        }

        private static void BlueImportNullDoesNotChangeRed()
        {
            var state = new AiSimulationState();
            AiSimulationPreset redPreset = state.Red.Preset;
            AiCraftMovementModel redMovement = state.Red.CraftMovementModel;

            AiSimulationImporter.TryImport(null, state, out _);

            if (state.Red.Preset != redPreset || state.Red.CraftMovementModel != redMovement)
                throw new InvalidOperationException("null import changed Red configuration");
        }

        private static void FullscreenLayoutKeepsPanelsInsideScreen()
        {
            CombatManagerEditorLayout layout = CombatManagerEditorLayout.For(1366f, 768f);
            if (layout.BluePanel.x < layout.Root.x || layout.BluePanel.y < layout.Root.y)
                throw new InvalidOperationException("blue panel escaped the screen");
            if (layout.RedPanel.xMax > layout.Root.xMax || layout.RedPanel.yMax > layout.Root.yMax)
                throw new InvalidOperationException("red panel escaped the screen");
            if (layout.Grid.y != layout.BluePanel.y || layout.Grid.height != layout.RedPanel.height)
                throw new InvalidOperationException("graph and side panels are not aligned");
        }

        private static void FullscreenLayoutKeepsGraphPositiveAt1280()
        {
            CombatManagerEditorLayout layout = CombatManagerEditorLayout.For(1280f, 720f);
            AssertNear(260f, layout.SidePanelWidth, "1280 side panel width");
            if (layout.Grid.width <= 420f)
                throw new InvalidOperationException($"1280 graph width too small: {layout.Grid.width}");
            if (layout.Grid.height <= 500f)
                throw new InvalidOperationException($"1280 graph height too small: {layout.Grid.height}");
        }

        private static void FullscreenLayoutAssignsSeparateColumns()
        {
            CombatManagerEditorLayout layout = CombatManagerEditorLayout.For(1920f, 1080f);
            AssertNear(320f, layout.SidePanelWidth, "wide side panel width");
            if (!(layout.BluePanel.xMax < layout.Grid.x && layout.Grid.xMax < layout.RedPanel.x))
                throw new InvalidOperationException("Blue, graph, and Red columns overlap or are out of order");
            if (Math.Abs(layout.BluePanel.width - layout.RedPanel.width) > 0.001f)
                throw new InvalidOperationException("side panel widths diverged");
        }

        private static void ConfigureSymmetricPointAt(AiSimEntity entity)
        {
            entity.ApplyCraftProfile(AiCraftProfile.SurfaceShip);
            entity.Preset = AiSimulationPreset.PointAt;
            entity.Radius = 300f;
            entity.CraftSpeed = 40f;
            entity.CraftAcceleration = 40f;
            entity.CraftTurnRate = 180f;
            entity.ShipTarryDistance = 0f;
        }

        private static AiSimulationState CreateShipTurnState(float turnRate)
        {
            var state = new AiSimulationState();
            state.Blue.ApplyCraftProfile(AiCraftProfile.SurfaceShip);
            state.Blue.Preset = AiSimulationPreset.Circle;
            state.Blue.Side = AiSimulationSide.Left;
            state.Blue.Radius = 260f;
            state.Blue.CraftSpeed = 50f;
            state.Blue.CraftAcceleration = 50f;
            state.Blue.CraftTurnRate = turnRate;
            state.ResetScenario();
            state.Blue.Radius = 320f;
            return state;
        }

        private static AiSimulationState CreateHoverAzimuthState(float moveWithinAzimuth)
        {
            var state = new AiSimulationState();
            state.Blue.ApplyCraftProfile(AiCraftProfile.Hovercraft);
            state.Blue.Preset = AiSimulationPreset.PointAt;
            state.Blue.Side = AiSimulationSide.Left;
            state.Blue.Radius = 260f;
            state.Blue.CraftSpeed = 40f;
            state.Blue.CraftAcceleration = 40f;
            state.Blue.HoverMoveWithinAzimuth = moveWithinAzimuth;
            state.ResetScenario();
            state.Blue.Radius = 460f;
            return state;
        }

        private static void AssertNear(float expected, float actual, string name)
        {
            if (Math.Abs(expected - actual) > 0.001f)
                throw new InvalidOperationException($"{name}: expected {expected}, got {actual}");
        }
    }
}
