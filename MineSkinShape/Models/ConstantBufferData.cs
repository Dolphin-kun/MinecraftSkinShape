using System.Numerics;
using System.Runtime.InteropServices;

namespace MineSkinShape.Models
{
    [StructLayout(LayoutKind.Sequential)]
    struct ConstantBufferData
    {
        public Matrix4x4 WorldViewProjection;
    }
}
