// C#側から受け取る行列データ
cbuffer ConstantBuffer : register(b0)
{
    matrix WorldViewProjection;
}

// C#側から受け取る頂点データの構造
struct VertexInput
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
};

// 頂点シェーダーからピクセルシェーダーへ渡すデータの構造
struct PixelInput
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
};

// テクスチャ
Texture2D SkinTexture : register(t0);
// サンプラー（テクスチャの色をどのように取得するかの設定）
SamplerState SkinSampler : register(s0);


// 頂点シェーダー：各頂点の最終的な位置を計算する
PixelInput VSMain(VertexInput input)
{
    PixelInput output;
    // 頂点座標に行列を適用して、2Dスクリーン上の座標に変換
    output.Position = mul(input.Position, WorldViewProjection);
    // テクスチャ座標はそのままピクセルシェーダーに渡す
    output.TexCoord = input.TexCoord;
    return output;
}

// ピクセルシェーダー：各ピクセルの色を決定する
float4 PSMain(PixelInput input) : SV_TARGET
{
    // テクスチャ座標を使って、スキン画像から色を取得する
    return SkinTexture.Sample(SkinSampler, input.TexCoord);
}