using System;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

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

    public sealed class FreeTrackReader : IDisposable
    {
        private const string SharedMemoryName = "FT_SharedMem";
        private readonly int structSize = Marshal.SizeOf(typeof(FTData));
        private MemoryMappedFile mmf;
        private MemoryMappedViewAccessor accessor;

        public FreeTrackReader()
        {
            TryOpen();
        }

        public bool TryRead(out FTData data)
        {
            data = default;
            if (accessor == null)
            {
                TryOpen();
                if (accessor == null)
                {
                    return false;
                }
            }

            byte[] raw = new byte[structSize];
            accessor.ReadArray(0, raw, 0, structSize);

            GCHandle handle = GCHandle.Alloc(raw, GCHandleType.Pinned);
            try
            {
                data = Marshal.PtrToStructure<FTData>(handle.AddrOfPinnedObject());
            }
            finally
            {
                handle.Free();
            }

            return true;
        }

        public void Dispose()
        {
            accessor?.Dispose();
            accessor = null;
            mmf?.Dispose();
            mmf = null;
        }

        private void TryOpen()
        {
            try
            {
                mmf = MemoryMappedFile.OpenExisting(SharedMemoryName, MemoryMappedFileRights.Read);
                accessor = mmf.CreateViewAccessor(0, structSize, MemoryMappedFileAccess.Read);
            }
            catch
            {
                accessor = null;
                mmf = null;
            }
        }
    }
}
