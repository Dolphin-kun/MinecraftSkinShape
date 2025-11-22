using System.Runtime.InteropServices;
using Vortice.Direct3D11;

namespace MineSkinShape.Commons
{
    public static class D3D11Extensions
    {
        public static ID3D11Buffer CreateBufferFromArray<T>(
            this ID3D11Device device, T[] data, BindFlags bindFlags)
            where T : struct
        {
            var size = Marshal.SizeOf<T>() * data.Length;
            var desc = new BufferDescription(size, bindFlags, ResourceUsage.Default);

            var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                var initData = new SubresourceData(handle.AddrOfPinnedObject());
                return device.CreateBuffer(desc, initData);
            }
            finally
            {
                handle.Free();
            }
        }
    }
}
