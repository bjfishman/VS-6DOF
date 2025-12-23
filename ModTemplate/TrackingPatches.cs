using System;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;

namespace VSDOF
{
    [HarmonyPatch(typeof(PlayerCamera), nameof(PlayerCamera.OnBeforeRenderFrame3D))]
    public class PlayerCameraTrackingPatch
    {
        [HarmonyPostfix]
        public static void Postfix(PlayerCamera __instance)
        {
            var capi = VsdofModSystem.Capi;
            var config = VsdofModSystem.Config;
            if (capi?.Render == null || config == null)
            {
                return;
            }

            if (capi.World?.Player?.Entity is not EntityPlayer player)
            {
                return;
            }

            if (IsTrackingSuspended(capi))
            {
                ResetOffsets(__instance, player, config);
                return;
            }

            if (capi.Render.CameraType != EnumCameraMode.FirstPerson)
            {
                ResetOffsets(__instance, player, config);
                return;
            }

            if (!config.EnableTracking || !VsdofModSystem.TryGetTracking(out FTData data))
            {
                ResetOffsets(__instance, player, config);
                return;
            }

            ApplyRotation(data, config);
            ApplyViewOffsets(data, config);
            HandleCrouch(player, data, config);
            HandleLeanToZoom(data, config);
        }

        private static void ApplyRotation(FTData data, HeadTrackingConfig config)
        {
            var capi = VsdofModSystem.Capi;
            var state = VsdofModSystem.State;
            var cameraOffset = capi?.Render?.CameraOffset;
            if (cameraOffset == null)
            {
                return;
            }

            if (!config.EnableRotation)
            {
                ResetRotation(cameraOffset, state);
                return;
            }

            float yaw = -data.Yaw * config.YawGain;
            float pitch = data.Pitch * config.PitchGain;

            cameraOffset.EnsureDefaultValues();
            cameraOffset.Rotation.Set(pitch, yaw, 0f);
            state.AppliedRotation = true;
        }

        private static void ApplyViewOffsets(FTData data, HeadTrackingConfig config)
        {
            var capi = VsdofModSystem.Capi;
            if (capi?.Render == null)
            {
                return;
            }

            bool applyTranslation = config.EnableTranslation;
            bool applyRoll = config.EnableRotation && config.EnableRoll;
            if (!applyTranslation && !applyRoll)
            {
                return;
            }

            double[] result = (double[])capi.Render.CameraMatrixOrigin.Clone();

            if (applyTranslation)
            {
                double tx = -(data.X / 100.0) * config.TranslationGainX;
                double ty = (data.Y / 100.0) * config.TranslationGainY;
                double tz = (data.Z / 100.0) * config.TranslationGainZ;

                tx = GameMath.Clamp(tx, -config.MaxTranslationX, config.MaxTranslationX);
                ty = GameMath.Clamp(ty, -config.MaxTranslationY, config.MaxTranslationY);
                tz = GameMath.Clamp(tz, -config.MaxTranslationZ, config.MaxTranslationZ);

                tx += config.BaselineOffsetX;
                ty += config.BaselineOffsetY;
                tz += config.BaselineOffsetZ;

                double[] translateMatrix = Mat4d.Create();
                Mat4d.Translate(translateMatrix, translateMatrix, -tx, -ty, -tz);
                result = Mat4d.Mul(Mat4d.Create(), translateMatrix, result);
            }

            if (applyRoll)
            {
                float roll = -data.Roll * config.RollGain;
                if (Math.Abs(roll) > 0.0001f)
                {
                    double rollRad = roll * Math.PI / 180.0;
                    double[] rollMatrix = Mat4d.Create();
                    Mat4d.Rotate(rollMatrix, rollMatrix, rollRad, new double[] { 0, 0, 1 });
                    result = Mat4d.Mul(Mat4d.Create(), rollMatrix, result);
                }
            }

            for (int i = 0; i < 16; i++)
            {
                capi.Render.CameraMatrixOrigin[i] = result[i];
                capi.Render.CameraMatrixOriginf[i] = (float)result[i];
            }
        }

        private static void HandleCrouch(EntityPlayer player, FTData data, HeadTrackingConfig config)
        {
            if (!config.EnableCrouchToggle)
            {
                return;
            }

            float headAxis = GetCrouchAxis(data, config);
            var state = VsdofModSystem.State;

            if (IsToggleMode(config))
            {
                if (headAxis < config.CrouchThreshold && state.CrouchReady)
                {
                    state.CrouchToggled = !state.CrouchToggled;
                    state.CrouchReady = false;
                }
                else if (headAxis > config.CrouchThreshold + config.CrouchHysteresis)
                {
                    state.CrouchReady = true;
                }
            }
            else
            {
                if (state.CrouchToggled)
                {
                    if (headAxis > config.CrouchThreshold + config.CrouchHysteresis)
                    {
                        state.CrouchToggled = false;
                    }
                }
                else
                {
                    if (headAxis < config.CrouchThreshold)
                    {
                        state.CrouchToggled = true;
                    }
                }
            }

            player.Controls.Sneak = state.CrouchToggled;
        }

        private static void HandleLeanToZoom(FTData data, HeadTrackingConfig config)
        {
            if (!config.EnableLeanToZoom || !VsdofModSystem.IsZoomButtonAvailable)
            {
                ReleaseLeanToZoom();
                ReleaseLeanToZoomAxis();
                return;
            }

            if (config.EnableLeanToZoomAxis)
            {
                ReleaseLeanToZoom();
                HandleLeanToZoomAxis(data, config);
                return;
            }

            ReleaseLeanToZoomAxis();

            float thresholdAbs = Math.Abs(config.LeanToZoomThreshold);
            float hysteresis = Math.Abs(config.LeanToZoomHysteresis);
            float headZ = -data.Z / 100.0f;
            bool forwardPositive = config.LeanToZoomThreshold >= 0f;
            float headAxis = forwardPositive ? headZ : -headZ;
            var state = VsdofModSystem.State;

            if (!state.LeanZoomPressed)
            {
                bool shouldPress = headAxis > thresholdAbs;
                if (shouldPress && VsdofModSystem.TrySetZoomHotkeyState(true))
                {
                    state.LeanZoomPressed = true;
                }
            }
            else
            {
                bool shouldRelease = headAxis < thresholdAbs - hysteresis;

                if (shouldRelease && VsdofModSystem.TrySetZoomHotkeyState(false))
                {
                    state.LeanZoomPressed = false;
                }
            }
        }

        private static void HandleLeanToZoomAxis(FTData data, HeadTrackingConfig config)
        {
            var capi = VsdofModSystem.Capi;
            if (capi?.Settings == null)
            {
                ReleaseLeanToZoomAxis();
                return;
            }

            if (!VsdofModSystem.TryGetZoomButtonConfig(out ZoomButtonConfig zoomConfig))
            {
                ReleaseLeanToZoomAxis();
                return;
            }

            float thresholdAbs = Math.Abs(config.LeanToZoomThreshold);
            float range = Math.Max(0.001f, Math.Abs(config.LeanToZoomAxisRange));
            float maxFactor = GameMath.Clamp(config.LeanToZoomAxisMax, 0f, 1f);
            float headZ = -data.Z / 100.0f;
            bool forwardPositive = config.LeanToZoomThreshold >= 0f;
            float headAxis = forwardPositive ? headZ : -headZ;

            float delta = headAxis - thresholdAbs;
            float t = GameMath.Clamp(delta / range, 0f, 1f) * maxFactor;

            if (t <= 0f)
            {
                ReleaseLeanToZoomAxis();
                return;
            }

            var state = VsdofModSystem.State;
            if (!state.LeanZoomAxisActive)
            {
                state.LeanZoomAxisActive = true;
                state.LeanZoomAxisFieldOfView = capi.Settings.Int[ZoomButtonConfig.FieldOfViewSettingName];
                state.LeanZoomAxisMouseSensitivity = capi.Settings.Int[ZoomButtonConfig.MouseSensitivitySettingName];
                state.LeanZoomAxisMouseSmoothing = capi.Settings.Int[ZoomButtonConfig.MouseSmoothingSettingName];
            }
            float zoomT = t;

            var overlay = VsdofModSystem.ZoomOverlay;
            if (overlay != null)
            {
                overlay.PercentZoomed = zoomConfig.vignetteShaderEnabled ? zoomT : 0f;
            }

            capi.Settings.Int[ZoomButtonConfig.FieldOfViewSettingName] =
                LerpInt(state.LeanZoomAxisFieldOfView, zoomConfig.fieldOfView, zoomT);
            capi.Settings.Int[ZoomButtonConfig.MouseSensitivitySettingName] =
                LerpInt(state.LeanZoomAxisMouseSensitivity, state.LeanZoomAxisMouseSensitivity * zoomConfig.mouseSensitivityFactor, zoomT);

            if (zoomConfig.changeMouseSmoothing)
            {
                capi.Settings.Int[ZoomButtonConfig.MouseSmoothingSettingName] =
                    LerpInt(state.LeanZoomAxisMouseSmoothing, zoomConfig.mouseSmoothing, zoomT);
            }
        }

        private static void ResetOffsets(PlayerCamera camera, EntityPlayer player, HeadTrackingConfig config)
        {
            var cameraOffset = VsdofModSystem.Capi?.Render?.CameraOffset;
            if (cameraOffset != null)
            {
                ResetRotation(cameraOffset, VsdofModSystem.State);
            }

            if (config.EnableCrouchToggle && VsdofModSystem.State.CrouchToggled)
            {
                VsdofModSystem.State.CrouchToggled = false;
                player.Controls.Sneak = false;
            }

            if (config.EnableCrouchToggle)
            {
                VsdofModSystem.State.CrouchReady = true;
            }

            ReleaseLeanToZoom();
            ReleaseLeanToZoomAxis();
        }

        private static void ResetRotation(ModelTransform cameraOffset, TrackingState state)
        {
            if (state.AppliedRotation)
            {
                cameraOffset.EnsureDefaultValues();
                cameraOffset.Rotation.Set(0f, 0f, 0f);
                state.AppliedRotation = false;
            }

        }

        private static float GetCrouchAxis(FTData data, HeadTrackingConfig config)
        {
            string axis = config.CrouchAxis ?? "Y";
            switch (axis.Trim().ToUpperInvariant())
            {
                case "X":
                    return data.X / 100.0f;
                case "Z":
                    return data.Z / 100.0f;
                default:
                    return data.Y / 100.0f;
            }
        }

        private static bool IsToggleMode(HeadTrackingConfig config)
        {
            return string.Equals(config.CrouchMode, "toggle", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsTrackingSuspended(ICoreClientAPI capi)
        {
            if (capi.IsGamePaused)
            {
                return true;
            }

            if (capi.OpenedGuis == null)
            {
                return false;
            }

            foreach (var gui in capi.OpenedGuis)
            {
                if (gui == null)
                {
                    continue;
                }

                string name = gui.GetType().FullName ?? string.Empty;
                if (name.Contains("GuiDialogEscapeMenu", StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static void ReleaseLeanToZoom()
        {
            var state = VsdofModSystem.State;
            if (state.LeanZoomPressed)
            {
                VsdofModSystem.TrySetZoomHotkeyState(false);
                state.LeanZoomPressed = false;
            }
        }

        private static void ReleaseLeanToZoomAxis()
        {
            var state = VsdofModSystem.State;
            if (!state.LeanZoomAxisActive)
            {
                return;
            }

            var capi = VsdofModSystem.Capi;
            if (capi?.Settings != null)
            {
                capi.Settings.Int[ZoomButtonConfig.FieldOfViewSettingName] = state.LeanZoomAxisFieldOfView;
                capi.Settings.Int[ZoomButtonConfig.MouseSensitivitySettingName] = state.LeanZoomAxisMouseSensitivity;
                capi.Settings.Int[ZoomButtonConfig.MouseSmoothingSettingName] = state.LeanZoomAxisMouseSmoothing;
            }

            var overlay = VsdofModSystem.ZoomOverlay;
            if (overlay != null)
            {
                overlay.PercentZoomed = 0f;
            }

            state.LeanZoomAxisActive = false;
        }

        private static int LerpInt(float a, float b, float t)
        {
            return (int)Math.Round(a + ((b - a) * t));
        }

    }

}
