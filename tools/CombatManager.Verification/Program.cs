using System;
using System.Collections.Generic;
using BrilliantSkies.Ai.Interfaces;
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
                BlueProjectionCanBeCentered();
                FreecamProjectionUsesStoredOrigin();
                FreecamPanUsesMetersPerPixelWithMapDragSigns();
                ZoomScalesMetersPerPixelAndClamps();
                FitDuelResetsZoomAndFreecamOrigin();
                GraphDetailModesSetExpectedVisibility();
                SimultaneousDuelStepIsMirrorStable();
                BothEntitiesPlanIndependently();
                CirclePlannerSeparatesRawSteerFromMotionPoint();
                ShipTurnRateConstrainsHeadingChange();
                HoverAzimuthLimitReducesMovement();
                SixAxisMovesAndFacesWithLookAhead();
                AirplaneMaintainsForwardSpeed();
                ScenarioPresetsApplyBothMainframes();
                BuildDuelFrameDoesNotMutateNavalState();
                AutoBroadsideSideDoesNotFlickerNearWrap();
                BlueImportNullDoesNotChangeRed();
                FullscreenLayoutKeepsPanelsInsideScreen();
                FullscreenLayoutKeepsGraphPositiveAt1280();
                FullscreenLayoutAssignsSeparateColumns();
                FullscreenLayoutKeepsTabContentInsidePanels();
                FullscreenToolbarGroupsDoNotOverlap();
                BlueprintPresetValuesAreStable();
                BlueprintSyncsToSandboxEntities();
                BlueprintExportPreviewHandlesNoFocusedCraft();
                BlueprintExportPreviewClassifiesUnsupportedPresets();
                BlueprintCaptureDoesNotChangePlannerFrame();
                VanillaShipRequestPredictionMatchesSteerSign();
                VanillaShipTarryPredictsVelocityCancel();
                VanillaHoverAndSixAxisPredictIndependentAxes();
                VanillaAirplanePredictsForwardBankAndAltitude();
                LiveParityHandlesNoFocusedCraft();
                LiveParityNullReadDoesNotMutateSandboxState();
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

        private static void BlueProjectionCanBeCentered()
        {
            var state = new AiSimulationState();
            state.ApplyScenarioPreset(AiScenarioPreset.ShipDuel);
            state.Step(1f);
            state.SetGraphViewMode(AiGraphViewMode.BlueCentered);

            Rect rect = new Rect(20f, 10f, 900f, 500f);
            AiSimulationGridProjection projection = AiSimulationGridProjection.For(rect, state);
            Vector2 blue = projection.WorldToScreen(state.Blue.Position);
            AssertNear(rect.center.x, blue.x, "blue centered x");
            AssertNear(rect.center.y, blue.y, "blue centered y");
        }

        private static void FreecamProjectionUsesStoredOrigin()
        {
            var state = new AiSimulationState();
            state.SetGraphViewMode(AiGraphViewMode.Freecam);
            Vector3 origin = new Vector3(123f, 0f, -456f);
            state.SetFreecamOrigin(origin);

            Rect rect = new Rect(0f, 0f, 800f, 400f);
            AiSimulationGridProjection projection = AiSimulationGridProjection.For(rect, state);
            Vector2 center = projection.WorldToScreen(origin);
            AssertNear(rect.center.x, center.x, "freecam origin x");
            AssertNear(rect.center.y, center.y, "freecam origin y");
        }

        private static void FreecamPanUsesMetersPerPixelWithMapDragSigns()
        {
            var state = new AiSimulationState();
            state.SetGraphViewMode(AiGraphViewMode.Freecam);
            state.SetFreecamOrigin(Vector3.zero);
            Rect rect = new Rect(0f, 0f, 800f, 400f);
            AiSimulationGridProjection projection = AiSimulationGridProjection.For(rect, state);
            float metersPerPixel = projection.MetersPerPixel;

            state.PanFreecam(new Vector2(10f, -20f), metersPerPixel);

            AssertNear(-10f * metersPerPixel, state.FreecamOrigin.x, "freecam pan x");
            AssertNear(-20f * metersPerPixel, state.FreecamOrigin.z, "freecam pan z");
        }

        private static void ZoomScalesMetersPerPixelAndClamps()
        {
            var state = new AiSimulationState();
            Rect rect = new Rect(0f, 0f, 800f, 400f);
            state.SetGridZoom(1f);
            float baseMetersPerPixel = AiSimulationGridProjection.For(rect, state).MetersPerPixel;
            state.SetGridZoom(2f);
            float zoomedMetersPerPixel = AiSimulationGridProjection.For(rect, state).MetersPerPixel;
            AssertNear(baseMetersPerPixel * 0.5f, zoomedMetersPerPixel, "2x zoom halves meters per pixel");

            state.SetGridZoom(100f);
            AssertNear(8f, state.GridZoom, "max zoom clamp");
            state.SetGridZoom(0.01f);
            AssertNear(0.25f, state.GridZoom, "min zoom clamp");
        }

        private static void FitDuelResetsZoomAndFreecamOrigin()
        {
            var state = new AiSimulationState();
            state.ApplyScenarioPreset(AiScenarioPreset.PlaneIntercept);
            state.SetGraphViewMode(AiGraphViewMode.Freecam);
            state.SetFreecamOrigin(new Vector3(999f, 0f, -999f));
            state.SetGridZoom(4f);

            state.FitDuel();

            Vector3 midpoint = (state.Blue.Position + state.Red.Position) * 0.5f;
            AssertNear(1f, state.GridZoom, "fit resets zoom");
            AssertNear(midpoint.x, state.FreecamOrigin.x, "fit freecam x");
            AssertNear(midpoint.z, state.FreecamOrigin.z, "fit freecam z");
        }

        private static void GraphDetailModesSetExpectedVisibility()
        {
            var state = new AiSimulationState();
            state.SetGraphDetailMode(AiGraphDetailMode.Clean);
            if (state.ShowTrail || state.ShowDesiredTrail || state.ShowRawSteer || state.ShowMotionPoint || state.ShowLegend)
                throw new InvalidOperationException("clean graph detail mode left debug layers visible");

            state.SetGraphDetailMode(AiGraphDetailMode.Tactical);
            if (!state.ShowTrail || !state.ShowDesiredTrail || state.ShowRawSteer || !state.ShowMotionPoint || state.ShowLegend)
                throw new InvalidOperationException("tactical graph detail mode did not set expected layers");

            state.SetGraphDetailMode(AiGraphDetailMode.Debug);
            if (!state.ShowTrail || !state.ShowDesiredTrail || !state.ShowRawSteer || !state.ShowMotionPoint || !state.ShowLegend)
                throw new InvalidOperationException("debug graph detail mode did not enable all graph layers");
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

        private static void AutoBroadsideSideDoesNotFlickerNearWrap()
        {
            var tiedBehind = new AiPlanInput
            {
                Preset = AiSimulationPreset.NavalBroadside,
                Side = AiSimulationSide.Both,
                NavalState = AiSimulationNavalState.BroadsideLeft,
                CraftPosition = Vector3.zero,
                CraftHeading = Vector3.forward,
                TargetPosition = new Vector3(0.2f, 0f, -500f),
                TargetVelocity = Vector3.zero,
                Radius = 600f,
                BroadsideOuterRadius = 900f,
                BroadsideAngle = 75f,
                CircleMinApproachAngle = 45f,
                CraftSpeed = 45f
            };

            if (AiBehaviourPlanner.AdvanceNavalState(tiedBehind) != AiSimulationNavalState.BroadsideLeft)
                throw new InvalidOperationException("auto broadside did not keep left side near 180 degree wrap");

            tiedBehind.NavalState = AiSimulationNavalState.BroadsideRight;
            tiedBehind.TargetPosition = new Vector3(-0.2f, 0f, -500f);
            if (AiBehaviourPlanner.AdvanceNavalState(tiedBehind) != AiSimulationNavalState.BroadsideRight)
                throw new InvalidOperationException("auto broadside did not keep right side near 180 degree wrap");

            AiPlanInput clearSide = tiedBehind;
            clearSide.NavalState = AiSimulationNavalState.Closing;
            clearSide.TargetPosition = new Vector3(500f, 0f, 0f);
            AiSimulationNavalState clearCandidate = AiBehaviourPlanner.AdvanceNavalState(clearSide);
            clearSide.NavalState = clearCandidate == AiSimulationNavalState.BroadsideLeft
                ? AiSimulationNavalState.BroadsideRight
                : AiSimulationNavalState.BroadsideLeft;
            if (AiBehaviourPlanner.AdvanceNavalState(clearSide) != clearCandidate)
                throw new InvalidOperationException("auto broadside did not switch when one side was clearly favoured");
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
            AssertNear(300f, layout.SidePanelWidth, "1280 side panel width");
            if (layout.Grid.width <= 560f)
                throw new InvalidOperationException($"1280 graph width too small: {layout.Grid.width}");
            if (layout.Grid.height <= 500f)
                throw new InvalidOperationException($"1280 graph height too small: {layout.Grid.height}");
        }

        private static void FullscreenLayoutAssignsSeparateColumns()
        {
            CombatManagerEditorLayout layout = CombatManagerEditorLayout.For(1920f, 1080f);
            AssertNear(380f, layout.SidePanelWidth, "wide side panel width");
            if (!(layout.BluePanel.xMax < layout.Grid.x && layout.Grid.xMax < layout.RedPanel.x))
                throw new InvalidOperationException("Blue, graph, and Red columns overlap or are out of order");
            if (Math.Abs(layout.BluePanel.width - layout.RedPanel.width) > 0.001f)
                throw new InvalidOperationException("side panel widths diverged");
        }

        private static void FullscreenLayoutKeepsTabContentInsidePanels()
        {
            CombatManagerEditorLayout layout = CombatManagerEditorLayout.For(1920f, 1080f);
            AssertRectInside(layout.BluePanel, layout.BlueTabContent, "blue tab content");
            AssertRectInside(layout.RedPanel, layout.RedTabContent, "red tab content");
            if (layout.BlueTabContent.height <= 840f || layout.RedTabContent.height <= 840f)
                throw new InvalidOperationException("tab content lost too much vertical space");
        }

        private static void FullscreenToolbarGroupsDoNotOverlap()
        {
            CombatManagerEditorLayout layout = CombatManagerEditorLayout.For(1280f, 720f);
            AssertRectInside(layout.Toolbar, layout.ToolbarLeft, "toolbar left");
            AssertRectInside(layout.Toolbar, layout.ToolbarMiddle, "toolbar middle");
            AssertRectInside(layout.Toolbar, layout.ToolbarRight, "toolbar right");
            if (!(layout.ToolbarLeft.xMax < layout.ToolbarMiddle.x && layout.ToolbarMiddle.xMax < layout.ToolbarRight.x))
                throw new InvalidOperationException("toolbar groups overlap or are out of order");
        }

        private static void BlueprintPresetValuesAreStable()
        {
            foreach (AiBlueprintPreset preset in AiBlueprintPresetLibrary.All)
            {
                AiMainframeBlueprint blueprint = AiBlueprintPresetLibrary.Create(preset, AiEntityRole.Blue);
                if (string.IsNullOrWhiteSpace(blueprint.MainframeName))
                    throw new InvalidOperationException($"preset {preset} did not create a name");
            }

            AiMainframeBlueprint broadsider = AiBlueprintPresetLibrary.Create(AiBlueprintPreset.SlowShipBroadsider, AiEntityRole.Blue);
            if (broadsider.Behaviour != AiSimulationPreset.NavalBroadside || broadsider.Manoeuvre != AiCraftMovementModel.ShipOrTank)
                throw new InvalidOperationException("slow ship broadsider did not map to Naval 2.0 ship/tank");
            AssertNear(2500f, broadsider.Radius, "broadsider range");

            AiMainframeBlueprint hover = AiBlueprintPresetLibrary.Create(AiBlueprintPreset.HoverSniper, AiEntityRole.Red);
            if (hover.Behaviour != AiSimulationPreset.PointAt || hover.Manoeuvre != AiCraftMovementModel.SixAxis)
                throw new InvalidOperationException("hover sniper did not map to point-at six-axis");
            AssertNear(3000f, hover.Radius, "hover sniper range");
        }

        private static void BlueprintSyncsToSandboxEntities()
        {
            var state = new AiSimulationState();
            state.ApplyBlueprintPreset(AiEntityRole.Blue, AiBlueprintPreset.HoverSniper);
            if (state.Blue.Preset != AiSimulationPreset.PointAt || state.Blue.CraftMovementModel != AiCraftMovementModel.SixAxis)
                throw new InvalidOperationException("blue hover sniper did not sync to sandbox entity");
            AssertNear(3000f, state.Blue.Radius, "blue blueprint radius");

            state.ApplyBlueprintPreset(AiEntityRole.Red, AiBlueprintPreset.FastPlanePointAt);
            if (state.Red.Preset != AiSimulationPreset.PointAt || state.Red.CraftMovementModel != AiCraftMovementModel.Airplane)
                throw new InvalidOperationException("red fast plane did not sync to sandbox entity");
            AssertNear(state.Red.Radius, state.RedBlueprint.Radius, "red entity and blueprint radius");
        }

        private static void BlueprintExportPreviewHandlesNoFocusedCraft()
        {
            var state = new AiSimulationState();
            AiBlueprintExportPlan plan = AiBlueprintExportPlanner.Build(null, state.BlueBlueprint, -1);
            if (plan.HasFocusedCraft || plan.HasSelectedMainframe)
                throw new InvalidOperationException("no-craft export preview reported a selected craft/mainframe");
            if (!plan.Supported)
                throw new InvalidOperationException("default blueprint should be supported for future export");
            if (plan.Mutations.Count == 0 || plan.Warnings.Count == 0)
                throw new InvalidOperationException("export preview did not describe mutations and warnings");
        }

        private static void BlueprintExportPreviewClassifiesUnsupportedPresets()
        {
            AiMainframeBlueprint rammer = AiBlueprintPresetLibrary.Create(AiBlueprintPreset.CloseRangeRammer, AiEntityRole.Blue);
            AiBlueprintExportPlan plan = AiBlueprintExportPlanner.Build(null, rammer, -1);
            if (plan.Supported)
                throw new InvalidOperationException("close-range rammer should be preview-only until Ram is mapped");
        }

        private static void BlueprintCaptureDoesNotChangePlannerFrame()
        {
            var state = new AiSimulationState();
            AiDuelFrame before = state.BuildDuelFrame();
            state.CaptureBlueprintFromEntity(state.Blue);
            state.CaptureBlueprintFromEntity(state.Red);
            AiDuelFrame after = state.BuildDuelFrame();
            AssertNear(before.Blue.GroundRange, after.Blue.GroundRange, "blue range after blueprint capture");
            AssertNear(before.Red.GroundRange, after.Red.GroundRange, "red range after blueprint capture");
            AssertNear(before.Blue.MotionPoint.x, after.Blue.MotionPoint.x, "blue motion point x after blueprint capture");
            AssertNear(before.Blue.MotionPoint.z, after.Blue.MotionPoint.z, "blue motion point z after blueprint capture");
        }

        private static void VanillaShipRequestPredictionMatchesSteerSign()
        {
            AiMovementRequestContext context = RequestContext(AiCraftMovementModel.ShipOrTank);
            AiVanillaIntentPlan intent = IntentAt(new Vector3(100f, 0f, 100f));
            List<AiControlRequestPrediction> requests = AiVanillaPredictor.PredictRequests(context, intent);

            if (PredictionValue(requests, AiControlType.ThrustForward) <= 0f)
                throw new InvalidOperationException("ship/tank did not predict forward thrust toward a forward steer point");
            if (PredictionValue(requests, AiControlType.YawRight) <= 0f)
                throw new InvalidOperationException("ship/tank did not predict yaw right for a right-side steer point");
            if (PredictionValue(requests, AiControlType.YawLeft) > 0f)
                throw new InvalidOperationException("ship/tank predicted both yaw directions");
        }

        private static void VanillaShipTarryPredictsVelocityCancel()
        {
            AiMovementRequestContext context = RequestContext(AiCraftMovementModel.ShipOrTank);
            context.TarryDistance = 50f;
            context.CraftVelocity = Vector3.forward * 12f;
            AiVanillaIntentPlan intent = IntentAt(new Vector3(0f, 0f, 10f));
            List<AiControlRequestPrediction> requests = AiVanillaPredictor.PredictRequests(context, intent);

            if (PredictionValue(requests, AiControlType.ThrustBackward) <= 0f)
                throw new InvalidOperationException("ship/tank tarry prediction did not request reverse thrust to cancel forward velocity");
        }

        private static void VanillaHoverAndSixAxisPredictIndependentAxes()
        {
            AiMovementRequestContext hover = RequestContext(AiCraftMovementModel.Hover);
            hover.HoverMoveWithinAzimuth = 180f;
            AiVanillaIntentPlan intent = IntentAt(new Vector3(80f, 30f, 120f));
            List<AiControlRequestPrediction> hoverRequests = AiVanillaPredictor.PredictRequests(hover, intent);
            if (PredictionValue(hoverRequests, AiControlType.StrafeRight) <= 0f || PredictionValue(hoverRequests, AiControlType.HoverUp) <= 0f)
                throw new InvalidOperationException("hover prediction did not request independent strafe/hover axes");

            AiMovementRequestContext sixAxis = RequestContext(AiCraftMovementModel.SixAxis);
            List<AiControlRequestPrediction> sixAxisRequests = AiVanillaPredictor.PredictRequests(sixAxis, intent);
            if (PredictionValue(sixAxisRequests, AiControlType.ThrustForward) <= 0f || PredictionValue(sixAxisRequests, AiControlType.StrafeRight) <= 0f)
                throw new InvalidOperationException("six-axis prediction did not request independent forward/strafe axes");
        }

        private static void VanillaAirplanePredictsForwardBankAndAltitude()
        {
            AiMovementRequestContext context = RequestContext(AiCraftMovementModel.Airplane);
            context.AirplaneBankingTurnAbove = 5f;
            context.AirplaneBankingTurnRoll = 45f;
            AiVanillaIntentPlan intent = IntentAt(new Vector3(-100f, 40f, 200f));
            List<AiControlRequestPrediction> requests = AiVanillaPredictor.PredictRequests(context, intent);

            if (PredictionValue(requests, AiControlType.ThrustForward) <= 0f)
                throw new InvalidOperationException("airplane prediction did not request forward thrust");
            if (PredictionValue(requests, AiControlType.RollLeft) <= 0f)
                throw new InvalidOperationException("airplane prediction did not request left bank for a left steer point");
            if (PredictionValue(requests, AiControlType.PitchUp) <= 0f)
                throw new InvalidOperationException("airplane prediction did not request pitch up for a higher steer point");
        }

        private static void LiveParityHandlesNoFocusedCraft()
        {
            AiLiveParitySnapshot snapshot = AiLiveParityCollector.Capture(null, -1);
            if (snapshot.HasFocusedConstruct || snapshot.HasMainframe || snapshot.HasTarget)
                throw new InvalidOperationException("null live parity capture reported live craft state");
            if (snapshot.Warnings.Count == 0)
                throw new InvalidOperationException("null live parity capture did not produce a warning");
        }

        private static void LiveParityNullReadDoesNotMutateSandboxState()
        {
            var state = new AiSimulationState();
            AiSimulationPreset bluePreset = state.Blue.Preset;
            AiSimulationPreset redPreset = state.Red.Preset;
            string blueName = state.BlueBlueprint.MainframeName;
            AiLiveParitySnapshot snapshot = AiLiveParityCollector.Capture(null, state.SelectedImportIndex);
            state.LiveParity = snapshot;

            if (state.Blue.Preset != bluePreset || state.Red.Preset != redPreset || state.BlueBlueprint.MainframeName != blueName)
                throw new InvalidOperationException("live parity read mutated sandbox or blueprint state");
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

        private static AiMovementRequestContext RequestContext(AiCraftMovementModel model)
        {
            return new AiMovementRequestContext
            {
                Model = model,
                SourceManoeuvre = model.ToString(),
                CraftPosition = Vector3.zero,
                CraftHeading = Vector3.forward,
                CraftVelocity = Vector3.zero,
                CraftAltitude = 0f,
                TarryDistance = 0f,
                ReverseAllowed = true,
                HoverYawLockDistance = 150f,
                HoverMoveWithinAzimuth = 30f,
                SixAxisLookAheadDistance = 50f,
                AirplaneIdleThrust = 100f,
                AirplaneIdleDistance = 300f,
                AirplaneBankingTurnAbove = 30f,
                AirplaneBankingTurnRoll = 45f,
                AirplanePitchForAltitude = 15f
            };
        }

        private static AiVanillaIntentPlan IntentAt(Vector3 point)
        {
            Vector3 flat = PlanarMath.SafePlanarDirection(Vector3.zero, point, Vector3.forward);
            return new AiVanillaIntentPlan
            {
                Supported = true,
                BehaviourClass = "Synthetic",
                Kind = "Synthetic",
                Summary = "synthetic request prediction intent",
                State = "test",
                RawSteerPoint = point,
                MotionPoint = point,
                DesiredFacing = flat,
                DesiredTravel = flat,
                Range = point.magnitude,
                GroundRange = PlanarMath.Flatten(point).magnitude,
                Azimuth = PlanarMath.SignedPlanarAngle(Vector3.forward, flat),
                MaintainDistanceLower = point.magnitude,
                MaintainDistanceUpper = point.magnitude,
                HasRawSteerPoint = true,
                HasMotionPoint = true,
                HasDesiredFacing = true
            };
        }

        private static float PredictionValue(List<AiControlRequestPrediction> requests, AiControlType type)
        {
            float value = 0f;
            foreach (AiControlRequestPrediction request in requests)
            {
                if (request.Type == type)
                    value = Math.Max(value, request.Value);
            }

            return value;
        }

        private static void AssertNear(float expected, float actual, string name)
        {
            if (Math.Abs(expected - actual) > 0.001f)
                throw new InvalidOperationException($"{name}: expected {expected}, got {actual}");
        }

        private static void AssertRectInside(Rect outer, Rect inner, string name)
        {
            if (inner.x < outer.x || inner.y < outer.y || inner.xMax > outer.xMax || inner.yMax > outer.yMax)
                throw new InvalidOperationException($"{name} escaped its parent");
        }
    }
}
