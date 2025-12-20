// VSDOF - Vintage Story Head Tracking Integration Mod
// Implements 6DOF input from FreeTrack-compatible software via shared memory

using System;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.Client.NoObf;
using Vintagestory.API.MathTools;

namespace VSDOF
{
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct FTData
    {
        public uint DataID;
        public int CamWidth;
        public int CamHeight;
        public float Yaw;
        public float Pitch;
        public float Roll;
        public float X;
        public float Y;
        public float Z;
    }

    public class FreeTrackReader : IDisposable
    {
        private MemoryMappedFile mmf;
        private MemoryMappedViewAccessor accessor;
        private const string SHM_NAME = "FT_SharedMem";
        private readonly int structSize = Marshal.SizeOf(typeof(FTData));

        public FreeTrackReader()
        {
            try
            {
                mmf = MemoryMappedFile.OpenExisting(SHM_NAME, MemoryMappedFileRights.Read);
                accessor = mmf.CreateViewAccessor(0, structSize, MemoryMappedFileAccess.Read);
            }
            catch
            {
                mmf = null;
            }
        }

        public bool TryRead(out FTData data)
        {
            data = new FTData();
            if (accessor == null) return false;

            byte[] raw = new byte[structSize];
            accessor.ReadArray(0, raw, 0, structSize);

            GCHandle handle = GCHandle.Alloc(raw, GCHandleType.Pinned);
            data = Marshal.PtrToStructure<FTData>(handle.AddrOfPinnedObject());
            handle.Free();

            return true;
        }

        public void Dispose()
        {
            accessor?.Dispose();
            mmf?.Dispose();
        }
    }

    public class VSDOFModSystem : ModSystem
    {
        private Harmony harmony;
        internal static ICoreClientAPI capi;
        internal static FreeTrackReader reader;

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);

            capi = api;
            reader = new FreeTrackReader();

            harmony = new Harmony("vs6dof.patch");
            harmony.PatchAll();
        }

        public override void Dispose()
        {
            harmony?.UnpatchAll("vs6dof.patch");
            reader?.Dispose();
        }
    }

    // === ROTATION (ROLL) PATCH ===
    [HarmonyPatch(typeof(Camera), "Update")]
    public class VSDOFCameraPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Camera __instance, float deltaTime)
        {
            var capi = VSDOFModSystem.capi;
            var reader = VSDOFModSystem.reader;

            if (capi == null || reader == null || capi.Render.CameraType != EnumCameraMode.FirstPerson)
                return;

            if (!reader.TryRead(out FTData data)) return;

            var player = capi.World.Player;
            if (player?.Entity == null) return;

            // === CONFIGURABLE GAINS ===
            const double rollGain = 200.0;

            double roll = DegToRad(data.Roll * rollGain);

            // Get player view direction
            float yawDeg = player.Entity.Pos.Yaw;
            float pitchDeg = player.Entity.Pos.Pitch;
            double yawRad = DegToRad(yawDeg);
            double pitchRad = DegToRad(pitchDeg);

            Vec3d forward = new Vec3d(
                -Math.Cos(pitchRad) * Math.Sin(yawRad),
                 Math.Sin(pitchRad),
                -Math.Cos(pitchRad) * Math.Cos(yawRad)
            );

            Vec3d up = new Vec3d(0, 1, 0);
            Vec3d right = forward.Cross(up).Normalize();
            up = right.Cross(forward).Normalize();

            // Build rotation matrix (currently roll only)
            double[] rotMatrix = Mat4d.Create();
            Mat4d.Rotate(rotMatrix, rotMatrix, roll, forward.ToDoubleArray());

            // Apply rotation to camera matrix
            double[] baseMatrix = (double[])capi.Render.CameraMatrixOrigin.Clone();
            double[] rotatedMatrix = Mat4d.Mul(Mat4d.Create(), rotMatrix, baseMatrix);

            for (int i = 0; i < 16; i++)
            {
                capi.Render.CameraMatrixOrigin[i] = rotatedMatrix[i];
                capi.Render.CameraMatrixOriginf[i] = (float)rotatedMatrix[i];
            }

        }

        private static double DegToRad(double deg) => deg * Math.PI / 180.0;
    }

    // === YAW & PITCH PATCH ===
    [HarmonyPatch(typeof(PlayerCamera), nameof(PlayerCamera.OnBeforeRenderFrame3D))]
    public class VSDOFCameraFrustumFixPatch
    {
        private static bool wasPressed = false;

        [HarmonyPrefix]
        public static void Prefix(PlayerCamera __instance)
        {
            var capi = VSDOFModSystem.capi;
            var reader = VSDOFModSystem.reader;

            if (capi?.Render == null || reader == null || capi.Render.CameraType != EnumCameraMode.FirstPerson)
                return;

            if (!reader.TryRead(out FTData data)) return;

            var player = capi.World.Player;
            if (player?.Entity == null) return;


            // === CONFIGURABLE GAINS ===
            const double yawGain = 200.0;
            const double pitchGain = 200.0;

            float yaw = (float)DegToRad(-data.Yaw * yawGain);
            float pitch = (float)DegToRad(data.Pitch * pitchGain);

            __instance.CameraOffset.Rotation.Set(pitch, yaw, 0);


        }

        private static double DegToRad(double deg) => deg * Math.PI / 180.0;
    }

    // === TRANSLATION PATCH (6DOF Body Lean) ===
    [HarmonyPatch(typeof(PlayerCamera), nameof(PlayerCamera.OnBeforeRenderFrame3D))]
    public class Patch_BodyLean_TrackIR
    {
        private const double maxOffset = 1.0;  // Max movement in meters
        private const double gain = 0.7;       // Head translation gain

        [HarmonyPrefix]
        public static void Prefix(PlayerCamera __instance)
        {
            var capi = VSDOFModSystem.capi;
            var reader = VSDOFModSystem.reader;

            if (capi?.Render == null || reader == null || capi.Render.CameraType != EnumCameraMode.FirstPerson)
                return;

            if (!reader.TryRead(out FTData data)) return;

            if (capi.World?.Player?.Entity is not EntityPlayer pl) return;

            // Convert cm → meters and apply gain
            double tx = -(data.X / 100.0) * gain;
            double ty = (data.Y / 100.0) * gain;
            double tz = -(data.Z / 100.0) * gain;

            tx = GameMath.Clamp(tx, -maxOffset, maxOffset);
            ty = GameMath.Clamp(ty, -maxOffset, maxOffset);
            tz = GameMath.Clamp(tz, -maxOffset, maxOffset);

            float yaw = pl.BodyYaw;
            Vec3d forward = new Vec3d(Math.Sin(yaw), 0, Math.Cos(yaw));
            Vec3d right = new Vec3d(-forward.Z, 0, forward.X);
            Vec3d up = new Vec3d(0, 1, 0);

            right.Normalize();
            up.Normalize();

            Vec3d offset = right * tx + up * ty + forward * tz;

            pl.CameraPosOffset.X = offset.X;
            pl.CameraPosOffset.Y = offset.Y;
            pl.CameraPosOffset.Z = offset.Z;
        }
    }

    [HarmonyPatch(typeof(PlayerCamera), nameof(PlayerCamera.OnBeforeRenderFrame3D))]
    public class Patch_HoldCrouch_TrackIR
    {
        private const float crouchThresholdY = -0.30f; // Crouch below this (in meters)

        [HarmonyPrefix]
        public static void Prefix(PlayerCamera __instance)
        {
            var capi = VSDOFModSystem.capi;
            var reader = VSDOFModSystem.reader;

            if (capi?.Render == null || reader == null || capi.Render.CameraType != EnumCameraMode.FirstPerson)
                return;

            if (!reader.TryRead(out FTData data)) return;

            if (capi.World?.Player?.Entity is not EntityPlayer pl) return;

            float headY = (float)(data.Y / 100.0); // Convert cm to meters

            // Hold sneak if below crouch threshold
            pl.Controls.Sneak = headY < crouchThresholdY;
        }
    }

}



