using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace VSDOF
{
    public class VsdofModSystem : ModSystem
    {
        private Harmony harmony;

        internal static ICoreClientAPI Capi;
        internal static FreeTrackReader Reader;
        internal static HeadTrackingConfig Config;
        internal static TrackingState State = new TrackingState();
        internal static GuiDialogHeadTrackingSettings SettingsDialog;
        internal static bool IsZoomButtonAvailable;
        internal static SquintOverlayRenderer ZoomOverlay;

        private const string ZoomButtonModId = "zoombuttonreborn";
        private const string ZoomButtonLegacyModId = "zoombutton";
        private const string ZoomButtonHotkeyCode = "zoombutton";

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);

            Capi = api;
            Config = api.LoadModConfig<HeadTrackingConfig>("vsdof.json") ?? new HeadTrackingConfig();
            api.StoreModConfig(Config, "vsdof.json");

            Reader = new FreeTrackReader();
            IsZoomButtonAvailable = api.ModLoader?.IsModEnabled(ZoomButtonModId) ?? false;
            if (!IsZoomButtonAvailable)
            {
                IsZoomButtonAvailable = api.ModLoader?.IsModEnabled(ZoomButtonLegacyModId) ?? false;
            }
            if (IsZoomButtonAvailable)
            {
                ZoomOverlay = new SquintOverlayRenderer(api);
            }

            api.Input.RegisterHotKey(
                "vsdofsettings",
                "Head Tracking Settings",
                GlKeys.KeypadMultiply,
                HotkeyType.GUIOrOtherControls,
                false,
                false,
                false);
            api.Input.SetHotKeyHandler("vsdofsettings", OnToggleSettingsDialog);

            harmony = new Harmony("vsdof.patch");
            harmony.PatchAll();
        }

        public override void Dispose()
        {
            harmony?.UnpatchAll("vsdof.patch");
            harmony = null;

            Reader?.Dispose();
            Reader = null;

            ZoomOverlay?.Dispose();
            ZoomOverlay = null;

            SettingsDialog?.TryClose();
            SettingsDialog?.Dispose();
            SettingsDialog = null;

            Capi = null;
        }

        internal static bool TryGetTracking(out FTData data)
        {
            data = default;
            if (Reader == null)
            {
                State.HasTracking = false;
                return false;
            }

            if (!Reader.TryRead(out data))
            {
                State.HasTracking = false;
                return false;
            }

            State.HasTracking = true;
            State.LastData = data;
            return true;
        }

        internal static bool TrySetZoomHotkeyState(bool pressed)
        {
            if (!IsZoomButtonAvailable || Capi?.Input == null)
            {
                return false;
            }

            var hotkey = Capi.Input.GetHotKeyByCode(ZoomButtonHotkeyCode);
            if (hotkey?.CurrentMapping == null)
            {
                return false;
            }

            var key = hotkey.CurrentMapping.KeyCode;
            if (key == (int)GlKeys.Unknown)
            {
                return false;
            }

            Capi.Input.KeyboardKeyState[key] = pressed;
            return true;
        }

        internal static bool TryGetZoomButtonConfig(out ZoomButtonConfig config)
        {
            config = null;
            if (Capi == null)
            {
                return false;
            }

            try
            {
                config = Capi.LoadModConfig<ZoomButtonConfig>(ZoomButtonConfig.ConfigFileName) ?? new ZoomButtonConfig();
            }
            catch
            {
                config = new ZoomButtonConfig();
            }

            return true;
        }

        private static bool OnToggleSettingsDialog(KeyCombination keyCombination)
        {
            if (Capi == null)
            {
                return false;
            }

            SettingsDialog ??= new GuiDialogHeadTrackingSettings(Capi);
            if (SettingsDialog.IsOpened())
            {
                SettingsDialog.TryClose();
            }
            else
            {
                SettingsDialog.SyncValues();
                SettingsDialog.TryOpen();
            }

            return true;
        }
    }

    internal sealed class TrackingState
    {
        public bool HasTracking;
        public FTData LastData;
        public bool CrouchToggled;
        public bool CrouchReady = true;
        public bool AppliedRotation;
        public bool LeanZoomPressed;
        public bool LeanZoomAxisActive;
        public int LeanZoomAxisFieldOfView;
        public int LeanZoomAxisMouseSensitivity;
        public int LeanZoomAxisMouseSmoothing;
    }

    internal sealed class ZoomButtonConfig
    {
        public const string ConfigFileName = "zoombutton119.json";
        public const string FieldOfViewSettingName = "fieldOfView";
        public const string MouseSensitivitySettingName = "mouseSensivity";
        public const string MouseSmoothingSettingName = "mouseSmoothing";

        public float zoomInTimeSec = 0.5f;
        public float zoomOutTimeSec = 0.1f;
        public int fieldOfView = 20;
        public float mouseSensitivityFactor = 0.5f;
        public bool changeMouseSmoothing = false;
        public float mouseSmoothing = 0.0f;
        public bool vignetteShaderEnabled = true;
    }
}
