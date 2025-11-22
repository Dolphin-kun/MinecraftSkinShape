using MineSkinShape.Commons;
using MineSkinShape.Models;
using System.Numerics;
using System.Runtime.CompilerServices;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace MineSkinShape
{
    internal class BodyPart : IDisposable
    {
        private readonly ID3D11Buffer vertexBuffer;
        private readonly ID3D11Buffer indexBuffer;
        private readonly int indexCount;

        public Vector3 Size { get; }
        public Vector3 Offset { get; }

        public BodyPart(ID3D11Device device, Vector3 partSize, Vector3 partOffset, System.Drawing.RectangleF[] uv)
        {
            Size = partSize;
            Offset = partOffset;

            const float textureWidth = 64.0f;
            const float textureHeight = 64.0f;

            var uvs = uv.Select(r => new System.Drawing.RectangleF(r.X / textureWidth, r.Y / textureHeight, r.Width / textureWidth, r.Height / textureHeight)).ToArray();
            var uvRight = uvs[0];
            var uvLeft = uvs[1];
            var uvTop = uvs[2];
            var uvBottom = uvs[3];
            var uvFront = uvs[4];
            var uvBack = uvs[5];

            var vertices = new[]
            { 
                // Front 
                new Vertex(new Vector3(-0.5f, 0.5f, -0.5f), new Vector2(uvFront.Right, uvFront.Top)),
    new Vertex(new Vector3( 0.5f, 0.5f, -0.5f), new Vector2(uvFront.Left, uvFront.Top)),
    new Vertex(new Vector3( 0.5f, -0.5f, -0.5f), new Vector2(uvFront.Left, uvFront.Bottom)),
    new Vertex(new Vector3(-0.5f, -0.5f, -0.5f), new Vector2(uvFront.Right, uvFront.Bottom)),

                // Back 
                new Vertex(new Vector3( 0.5f, 0.5f, 0.5f), new Vector2(uvBack.Right, uvBack.Top)),
    new Vertex(new Vector3(-0.5f, 0.5f, 0.5f), new Vector2(uvBack.Left, uvBack.Top)),
    new Vertex(new Vector3(-0.5f, -0.5f, 0.5f), new Vector2(uvBack.Left, uvBack.Bottom)),
    new Vertex(new Vector3( 0.5f, -0.5f, 0.5f), new Vector2(uvBack.Right, uvBack.Bottom)),

                // Left 
                new Vertex(new Vector3(-0.5f,  0.5f,  0.5f), new Vector2(uvLeft.Right, uvLeft.Top)),
                new Vertex(new Vector3(-0.5f,  0.5f, -0.5f), new Vector2(uvLeft.Left, uvLeft.Top)),
                new Vertex(new Vector3(-0.5f, -0.5f, -0.5f), new Vector2(uvLeft.Left, uvLeft.Bottom)),
                new Vertex(new Vector3(-0.5f, -0.5f,  0.5f), new Vector2(uvLeft.Right, uvLeft.Bottom)),

                // Right 
                new Vertex(new Vector3( 0.5f,  0.5f, -0.5f), new Vector2(uvRight.Right, uvRight.Top)),
                new Vertex(new Vector3( 0.5f,  0.5f,  0.5f), new Vector2(uvRight.Left, uvRight.Top)),
                new Vertex(new Vector3( 0.5f, -0.5f,  0.5f), new Vector2(uvRight.Left, uvRight.Bottom)),
                new Vertex(new Vector3( 0.5f, -0.5f, -0.5f), new Vector2(uvRight.Right, uvRight.Bottom)),

                // Top 
                new Vertex(new Vector3(-0.5f, 0.5f, 0.5f), new Vector2(uvTop.Right, uvTop.Top)),
    new Vertex(new Vector3( 0.5f, 0.5f, 0.5f), new Vector2(uvTop.Left, uvTop.Top)),
    new Vertex(new Vector3( 0.5f, 0.5f, -0.5f), new Vector2(uvTop.Left, uvTop.Bottom)),
    new Vertex(new Vector3(-0.5f, 0.5f, -0.5f), new Vector2(uvTop.Right, uvTop.Bottom)),

                // Bottom 
                new Vertex(new Vector3(-0.5f, -0.5f, -0.5f), new Vector2(uvBottom.Right, uvBottom.Bottom)),
    new Vertex(new Vector3( 0.5f, -0.5f, -0.5f), new Vector2(uvBottom.Left, uvBottom.Bottom)),
    new Vertex(new Vector3( 0.5f, -0.5f, 0.5f), new Vector2(uvBottom.Left, uvBottom.Top)),
    new Vertex(new Vector3(-0.5f, -0.5f, 0.5f), new Vector2(uvBottom.Right, uvBottom.Top))
            };

            var indices = new ushort[]
            {
                0,  1,  2,  0,  2,  3, // Front
                4,  5,  6,  4,  6,  7, // Back
                8,  9, 10,  8, 10, 11, // Left
                12, 13, 14, 12, 14, 15, // Right
                16, 17, 18, 16, 18, 19, // Top
                20, 21, 22, 20, 22, 23, // Bottom
            };
            indexCount = indices.Length;

            vertexBuffer = device.CreateBufferFromArray(vertices, BindFlags.VertexBuffer);
            indexBuffer = device.CreateBufferFromArray(indices, BindFlags.IndexBuffer);
        }

        public void Draw(ID3D11DeviceContext context, ID3D11Buffer constantBuffer, Matrix4x4 characterWorldViewProjection)
        {
            var localTransform = Matrix4x4.CreateScale(Size) * Matrix4x4.CreateTranslation(Offset);

            var finalMatrix = localTransform * characterWorldViewProjection;

            var data = new ConstantBufferData
            {
                WorldViewProjection = Matrix4x4.Transpose(finalMatrix)
            };
            context.UpdateSubresource(data, constantBuffer);

            context.IASetVertexBuffer(0, vertexBuffer, Unsafe.SizeOf<Vertex>());
            context.IASetIndexBuffer(indexBuffer, Format.R16_UInt, 0);
            context.DrawIndexed(indexCount, 0, 0);
        }

        public void Dispose()
        {
            vertexBuffer?.Dispose();
            indexBuffer?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
