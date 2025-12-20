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

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);

            Capi = api;
            Config = api.LoadModConfig<HeadTrackingConfig>("vsdof.json") ?? new HeadTrackingConfig();
            api.StoreModConfig(Config, "vsdof.json");

            Reader = new FreeTrackReader();

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
    }
}
