using MineSkinShape.Models;
using System.Drawing;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using Vortice.D3DCompiler;
using Vortice.Direct2D1;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;
using Vortice.WIC;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Player.Video;

namespace MineSkinShape
{
    internal class MineSkinShapeSource : IShapeSource
    {
        private readonly IGraphicsDevicesAndContext devices;
        private readonly MineSkinShapeParameter mineSkinShapeParameter;

        private readonly ID3D11Device d3dDevice;
        private readonly ID3D11DeviceContext d3dContext;

        ID3D11Texture2D? renderTargetTexture;
        ID3D11RenderTargetView? renderTargetView;
        ID2D1Bitmap1? d2dBitmap;

        private readonly BodyPart head;
        private readonly BodyPart body;
        private readonly BodyPart rightArm;
        private readonly BodyPart leftArm;
        private readonly BodyPart rightLeg;
        private readonly BodyPart leftLeg;
        private readonly BodyPart[] allParts;

        readonly ID3D11VertexShader? vertexShader;
        readonly ID3D11PixelShader? pixelShader;
        readonly ID3D11InputLayout? inputLayout;
        readonly ID3D11Buffer? constantBuffer;
        ID3D11ShaderResourceView? skinTextureView;
        readonly ID3D11SamplerState? samplerState;

        ID2D1CommandList? commandList;

        readonly ID3D11RasterizerState? rasterizerState;
        ID3D11Texture2D? depthStencilTexture;
        ID3D11DepthStencilView? depthStencilView;

        double size;

        public ID2D1Image Output => commandList ?? throw new Exception($"{nameof(commandList)}がnullです。事前にUpdateを呼び出す必要があります。");

        public MineSkinShapeSource(IGraphicsDevicesAndContext devices, MineSkinShapeParameter mineSkinShapeParameter)
        {
            this.devices = devices;
            this.mineSkinShapeParameter = mineSkinShapeParameter;

            d3dDevice = devices.D3D.Device;
            d3dContext = devices.D3D.DeviceContext;

            string shaderCode = @"
            cbuffer ConstantBuffer : register(b0)
            {
                matrix WorldViewProjection;
            }

            Texture2D SkinTexture : register(t0);
            SamplerState Sampler : register(s0);

            struct VS_INPUT
            {
                float4 Pos : SV_POSITION;
                float2 Tex : TEXCOORD;
            };

            struct PS_INPUT
            {
                float4 Pos : SV_POSITION;
                float2 Tex : TEXCOORD;
            };

            PS_INPUT VSMain(VS_INPUT input)
            {
                PS_INPUT output = (PS_INPUT)0;
                output.Pos = mul(input.Pos, WorldViewProjection);
                output.Tex = input.Tex;
                return output;
            }

            float4 PSMain(PS_INPUT input) : SV_TARGET
            {
                return SkinTexture.Sample(Sampler, input.Tex);
            }
            ";

            var vsByteCode = Compiler.Compile(shaderCode, "VSMain", "MineSkinShape.hlsl", "vs_5_0");
            var psByteCode = Compiler.Compile(shaderCode, "PSMain", "MineSkinShape.hlsl", "ps_5_0");

            vertexShader = d3dDevice.CreateVertexShader(vsByteCode);
            pixelShader = d3dDevice.CreatePixelShader(psByteCode);

            var inputElements = new[]
            {
                new Vortice.Direct3D11.InputElementDescription("SV_POSITION", 0, Format.R32G32B32_Float, 0, 0),
                new Vortice.Direct3D11.InputElementDescription("TEXCOORD", 0, Format.R32G32_Float, 12, 0)
            };
            inputLayout = d3dDevice.CreateInputLayout(inputElements, vsByteCode);

            constantBuffer = d3dDevice.CreateBuffer(Unsafe.SizeOf<ConstantBufferData>(), BindFlags.ConstantBuffer);

            var samplerDesc = new SamplerDescription(Vortice.Direct3D11.Filter.MinMagMipPoint, TextureAddressMode.Clamp, TextureAddressMode.Clamp, TextureAddressMode.Clamp);
            samplerState = d3dDevice.CreateSamplerState(samplerDesc);

            var rasterizerDesc = new RasterizerDescription(CullMode.None, Vortice.Direct3D11.FillMode.Solid);
            rasterizerState = d3dDevice.CreateRasterizerState(rasterizerDesc);

            // Body Parts
            const float blockScale = 1.0f / 16.0f;

            // 頭
            head = new BodyPart(d3dDevice, new Vector3(8, 8, 8) * blockScale, new Vector3(0, 28, 0) * blockScale, [
                new RectangleF(0, 8, 8, 8),  // Right
                new RectangleF(16, 8, 8, 8), // Left
                new RectangleF(8, 0, 8, 8),  // Top
                new RectangleF(16, 0, 8, 8), // Bottom
                new RectangleF(8, 8, 8, 8),  // Front
                new RectangleF(24, 8, 8, 8) // Back
                ]);

            // 胴体
            body = new BodyPart(d3dDevice, new Vector3(8, 12, 4) * blockScale, new Vector3(0, 18, 0) * blockScale, [
                new RectangleF(16, 20, 4, 12), 
                new RectangleF(28, 20, 4, 12), 
                new RectangleF(20, 16, 8, 4), 
                new RectangleF(28, 16, 8, 4), 
                new RectangleF(20, 20, 8, 12), 
                new RectangleF(32, 20, 8, 12) 
                ]);

            // 右腕
            rightArm = new BodyPart(d3dDevice, new Vector3(4, 12, 4) * blockScale, new Vector3(6, 18, 0) * blockScale, [
               new RectangleF(48, 20, 4, 12), 
                new RectangleF(40, 20, 4, 12), 
        new RectangleF(44, 16, 4, 4), 
        new RectangleF(48, 16, 4, 4), 
        new RectangleF(44, 20, 4, 12),
        new RectangleF(52, 20, 4, 12)
                ]);

            // 左腕
            leftArm = new BodyPart(d3dDevice, new Vector3(4, 12, 4) * blockScale, new Vector3(-6, 18, 0) * blockScale, [
                new RectangleF(40, 52, 4, 12),
                new RectangleF(32, 52, 4, 12),
        new RectangleF(36, 48, 4, 4), 
        new RectangleF(40, 48, 4, 4),  
        new RectangleF(36, 52, 4, 12), 
        new RectangleF(44, 52, 4, 12)  
                ]);

            // 右足
            rightLeg = new BodyPart(d3dDevice, new Vector3(4, 12, 4) * blockScale, new Vector3(2, 6, 0) * blockScale, [
           new RectangleF(0, 20, 4, 12),  
        new RectangleF(8, 20, 4, 12),  
        new RectangleF(4, 16, 4, 4),  
        new RectangleF(8, 16, 4, 4),  
        new RectangleF(4, 20, 4, 12),  
        new RectangleF(12, 20, 4, 12)
                ]);

            // 左足
            leftLeg = new BodyPart(d3dDevice, new Vector3(4, 12, 4) * blockScale, new Vector3(-2, 6, 0) * blockScale, [
           new RectangleF(16, 52, 4, 12), 
        new RectangleF(24, 52, 4, 12), 
        new RectangleF(20, 48, 4, 4), 
        new RectangleF(24, 48, 4, 4), 
        new RectangleF(20, 52, 4, 12), 
        new RectangleF(28, 52, 4, 12) 
                ]);

            allParts = [head, body, rightArm, leftArm, rightLeg, leftLeg];
        }

        public void Update(TimelineItemSourceDescription timelineItemSourceDescription)
        {
            var fps = timelineItemSourceDescription.FPS;
            var frame = timelineItemSourceDescription.ItemPosition.Frame;
            var length = timelineItemSourceDescription.ItemDuration.Frame;

            var size = mineSkinShapeParameter.Size.GetValue(frame, length, fps) * 5d;
            var skinFile = mineSkinShapeParameter.SkinFile;

            var rotateX = mineSkinShapeParameter.RotateX.GetValue(frame, length, fps);
            var rotateY = mineSkinShapeParameter.RotateY.GetValue(frame, length, fps);
            var rotateZ = mineSkinShapeParameter.RotateZ.GetValue(frame, length, fps);

            var headRotation = new Vector3(
                (float)mineSkinShapeParameter.Head_RotateX.GetValue(frame, length, fps),
                (float)mineSkinShapeParameter.Head_RotateY.GetValue(frame, length, fps),
                (float)mineSkinShapeParameter.Head_RotateZ.GetValue(frame, length, fps)
            );
            var rightArmRotation = new Vector3(
                (float)mineSkinShapeParameter.RightArm_RotateX.GetValue(frame, length, fps),
                (float)mineSkinShapeParameter.RightArm_RotateY.GetValue(frame, length, fps),
                (float)mineSkinShapeParameter.RightArm_RotateZ.GetValue(frame, length, fps)
            );
            var leftArmRotation = new Vector3(
                (float)mineSkinShapeParameter.LeftArm_RotateX.GetValue(frame, length, fps),
                (float)mineSkinShapeParameter.LeftArm_RotateY.GetValue(frame, length, fps),
                (float)mineSkinShapeParameter.LeftArm_RotateZ.GetValue(frame, length, fps)
            );
            var rightLegRotation = new Vector3(
                (float)mineSkinShapeParameter.RightLeg_RotateX.GetValue(frame, length, fps),
                (float)mineSkinShapeParameter.RightLeg_RotateY.GetValue(frame, length, fps),
                (float)mineSkinShapeParameter.RightLeg_RotateZ.GetValue(frame, length, fps)
            );
            var leftLegRotation = new Vector3(
                (float)mineSkinShapeParameter.LeftLeg_RotateX.GetValue(frame, length, fps),
                (float)mineSkinShapeParameter.LeftLeg_RotateY.GetValue(frame, length, fps),
                (float)mineSkinShapeParameter.LeftLeg_RotateZ.GetValue(frame, length, fps)
            );

            var dc = devices.DeviceContext;
            if (size < 1)
            {
                commandList?.Dispose();
                commandList = dc.CreateCommandList();
                dc.Target = commandList;
                dc.BeginDraw();
                dc.Clear(null);
                dc.EndDraw();
                dc.Target = null;
                commandList.Close();
                return;
            }

            if (this.size != size || renderTargetTexture == null || d2dBitmap == null || renderTargetView == null)
            {
                renderTargetTexture?.Dispose();
                renderTargetView?.Dispose();
                d2dBitmap?.Dispose();
                depthStencilTexture?.Dispose();
                depthStencilView?.Dispose();

                var textureDesc = new Texture2DDescription
                {
                    Width = (int)size,
                    Height = (int)size,
                    MipLevels = 1,
                    ArraySize = 1,
                    Format = Format.B8G8R8A8_UNorm,
                    SampleDescription = new SampleDescription(1, 0),
                    Usage = ResourceUsage.Default,
                    BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                    CPUAccessFlags = CpuAccessFlags.None,
                };
                renderTargetTexture = d3dDevice.CreateTexture2D(textureDesc);
                renderTargetView = d3dDevice.CreateRenderTargetView(renderTargetTexture);

                var depthTexDesc = new Texture2DDescription
                {
                    Width = (int)size,
                    Height = (int)size,
                    MipLevels = 1,
                    ArraySize = 1,
                    Format = Format.D24_UNorm_S8_UInt,
                    SampleDescription = new SampleDescription(1, 0),
                    Usage = ResourceUsage.Default,
                    BindFlags = BindFlags.DepthStencil,
                    CPUAccessFlags = CpuAccessFlags.None,
                };
                depthStencilTexture = d3dDevice.CreateTexture2D(depthTexDesc);
                depthStencilView = d3dDevice.CreateDepthStencilView(depthStencilTexture);

                using var dxgiSurface = renderTargetTexture.QueryInterface<IDXGISurface>();
                using var baseBitmap = devices.DeviceContext.CreateBitmapFromDxgiSurface(dxgiSurface);
                d2dBitmap = baseBitmap.QueryInterface<ID2D1Bitmap1>();

                this.size = size;
            }

            // ----------- スキン画像の読み込み -----------
            skinTextureView?.Dispose();
            if (File.Exists(skinFile))
            {
                skinTextureView = LoadTextureFromFile(d3dDevice, skinFile);
            }
            else
            {
                skinTextureView = null;
            }

            // ----------- D3Dでの描画 -----------
            d3dContext.OMSetRenderTargets(renderTargetView, depthStencilView);
            d3dContext.RSSetViewport(0, 0, (int)size, (int)size);

            d3dContext.ClearRenderTargetView(renderTargetView, new Color4(0, 0, 0, 0));
            d3dContext.ClearDepthStencilView(depthStencilView, DepthStencilClearFlags.Depth, 1.0f, 0);

            var rotXRadians = (float)(rotateX * (Math.PI / 180.0));
            var rotYRadians = (float)(rotateY * (Math.PI / 180.0));
            var rotZRadians = (float)(rotateZ * (Math.PI / 180.0));

            const float pivotY = 24.0f * (1.0f / 16.0f);
            var translationToPivot = Matrix4x4.CreateTranslation(0, -pivotY, 0);
            var rotation = Matrix4x4.CreateFromYawPitchRoll(rotYRadians, rotXRadians, rotZRadians);
            var translationBack = Matrix4x4.CreateTranslation(0, pivotY, 0);

            var characterWorld = translationToPivot * rotation * translationBack;

            var view = Matrix4x4.CreateLookAt(new Vector3(0, 16 * (1.0f / 16.0f), -60 * (1.0f / 16.0f)), new Vector3(0, pivotY, 0), Vector3.UnitY);
            var projection = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 2.0f, 1.0f, 0.1f, 100.0f);
            var baseWVP = characterWorld * view * projection;

            const float blockScale = 1.0f / 16.0f;
            var partTransforms = new Dictionary<BodyPart, Matrix4x4>
            {
                [head] = CreatePartTransform(headRotation, new Vector3(0, 24, 0) * blockScale),
                [body] = Matrix4x4.Identity,
                [rightArm] = CreatePartTransform(rightArmRotation, new Vector3(5, 22, 0) * blockScale),
                [leftArm] = CreatePartTransform(leftArmRotation, new Vector3(-5, 22, 0) * blockScale),
                [rightLeg] = CreatePartTransform(rightLegRotation, new Vector3(2, 12, 0) * blockScale),
                [leftLeg] = CreatePartTransform(leftLegRotation, new Vector3(-2, 12, 0) * blockScale)
            };

            d3dContext.RSSetState(rasterizerState);
            d3dContext.IASetInputLayout(inputLayout);
            d3dContext.IASetPrimitiveTopology(Vortice.Direct3D.PrimitiveTopology.TriangleList);
            d3dContext.VSSetShader(vertexShader);
            d3dContext.VSSetConstantBuffer(0, constantBuffer);
            d3dContext.PSSetShader(pixelShader);
            d3dContext.PSSetShaderResource(0, skinTextureView);
            d3dContext.PSSetSampler(0, samplerState);

            foreach (var part in allParts)
            {
                var wvpForPart = partTransforms[part] * baseWVP;
                part.Draw(d3dContext, constantBuffer, wvpForPart);
            }

            var bounds = CalculateScreenBoundingBox(new Vector2((float)size, (float)size), baseWVP, partTransforms);

            commandList?.Dispose();//新規作成前に、前回のCommandListを必ず破棄する
            commandList = dc.CreateCommandList();

            dc.Target = commandList;
            dc.BeginDraw();
            dc.Clear(null);

            if ((bounds.Right - bounds.Left) > 1 && (bounds.Bottom - bounds.Top) > 1)
            {
                var croppedWidth = bounds.Right - bounds.Left;
                var croppedHeight = bounds.Bottom - bounds.Top;

                var destRect = new Vortice.RawRectF(
                    -croppedWidth / 2f,
                    -croppedHeight / 2f,
                    croppedWidth / 2f,
                    croppedHeight / 2f
                );

                dc.DrawBitmap(d2dBitmap, destRect, 1.0f, Vortice.Direct2D1.BitmapInterpolationMode.Linear, bounds);
            }


            dc.EndDraw();
            dc.Target = null;//Targetは必ずnullに戻す。
            commandList.Close();//CommandListはEndDraw()の後に必ずClose()を呼んで閉じる必要がある
        }

        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    renderTargetTexture?.Dispose();
                    renderTargetView?.Dispose();
                    d2dBitmap?.Dispose();
                    commandList?.Dispose();
                    vertexShader?.Dispose();
                    pixelShader?.Dispose();
                    inputLayout?.Dispose();
                    constantBuffer?.Dispose();
                    skinTextureView?.Dispose();
                    samplerState?.Dispose();
                    rasterizerState?.Dispose();
                    depthStencilTexture?.Dispose();
                    depthStencilView?.Dispose();

                    head?.Dispose();
                    body?.Dispose();
                    rightArm?.Dispose();
                    leftArm?.Dispose();
                    rightLeg?.Dispose();
                    leftLeg?.Dispose();
                }

                disposedValue = true;
            }
        }

        // // 'Dispose(bool disposing)' にアンマネージド リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします
        // ~SampleShapeSource()
        // {
        //     // このコードを変更しないでください。クリーンアップ コードを 'Dispose(bool disposing)' メソッドに記述します
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private static ID3D11ShaderResourceView LoadTextureFromFile(ID3D11Device device, string filePath)
        {
            using var wicFactory = new IWICImagingFactory();
            using var decoder = wicFactory.CreateDecoderFromFileName(filePath);
            using var frame = decoder.GetFrame(0);

            using var converter = wicFactory.CreateFormatConverter();
            converter.Initialize(frame, PixelFormat.Format32bppPBGRA);

            var size = converter.Size;
            int stride = size.Width * 4;
            int bufferSize = stride * size.Height;

            byte[] pixels = new byte[bufferSize];
            converter.CopyPixels(stride, pixels);

            var textureDesc = new Texture2DDescription
            {
                Width = size.Width,
                Height = size.Height,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.B8G8R8A8_UNorm,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Immutable,
                BindFlags = BindFlags.ShaderResource,
                CPUAccessFlags = CpuAccessFlags.None,
            };

            unsafe
            {
                fixed (byte* pPixels = pixels)
                {
                    var initData = new SubresourceData(pPixels, stride);
                    using var texture = device.CreateTexture2D(textureDesc, [initData]);
                    return device.CreateShaderResourceView(texture);
                }
            }
        }

        private Vortice.RawRectF CalculateScreenBoundingBox(Vector2 viewportSize, Matrix4x4 baseWVP, Dictionary<BodyPart, Matrix4x4> partTransforms)
        {
            var corners = new Vector3[]
            {
                new(-0.5f, -0.5f, -0.5f), new(0.5f, -0.5f, -0.5f),
                new(0.5f,  0.5f, -0.5f), new(-0.5f,  0.5f, -0.5f),
                new(-0.5f, -0.5f,  0.5f), new(0.5f, -0.5f,  0.5f),
                new(0.5f,  0.5f,  0.5f), new(-0.5f,  0.5f,  0.5f),
            };

            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;
            bool anyPointVisible = false;

            foreach (var part in allParts)
            {
                var localTransform = Matrix4x4.CreateScale(part.Size) * Matrix4x4.CreateTranslation(part.Offset);
                var finalMatrix = localTransform * partTransforms[part] * baseWVP;

                foreach (var corner in corners)
                {
                    var clipSpace = Vector4.Transform(corner, finalMatrix);

                    if (clipSpace.W <= 0) continue; // カメラの後ろにある点は無視
                    var ndc = new Vector3(clipSpace.X / clipSpace.W, clipSpace.Y / clipSpace.W, clipSpace.Z / clipSpace.W);

                    var screenX = (ndc.X + 1.0f) * 0.5f * viewportSize.X;
                    var screenY = (1.0f - ndc.Y) * 0.5f * viewportSize.Y;

                    minX = Math.Min(minX, screenX);
                    minY = Math.Min(minY, screenY);
                    maxX = Math.Max(maxX, screenX);
                    maxY = Math.Max(maxY, screenY);
                    anyPointVisible = true;
                }
            }

            if (!anyPointVisible)
                return new Vortice.RawRectF(0, 0, 0, 0);

            minX = Math.Max(0, minX);
            minY = Math.Max(0, minY);
            maxX = Math.Min(viewportSize.X, maxX);
            maxY = Math.Min(viewportSize.Y, maxY);

            return new Vortice.RawRectF(minX, minY, maxX, maxY);
        }

        private static Matrix4x4 CreatePartTransform(Vector3 rotationAngles, Vector3 pivot)
        {
            var rotXRadians = (float)(rotationAngles.X * (Math.PI / 180.0));
            var rotYRadians = (float)(rotationAngles.Y * (Math.PI / 180.0));
            var rotZRadians = (float)(rotationAngles.Z * (Math.PI / 180.0));

            var partRotation = Matrix4x4.CreateFromYawPitchRoll(rotYRadians, rotXRadians, rotZRadians);

            return Matrix4x4.CreateTranslation(-pivot) * partRotation * Matrix4x4.CreateTranslation(pivot);
        }
    }
}
