Texture2D tex : register(t0);
SamplerState samLinear : register(s0);

cbuffer HueBuffer : register(b0)
{
    float hue;
}

struct VSIn
{
    float3 pos : POSITION;
    float2 uv  : TEXCOORD;
};

struct VSOut
{
    float4 pos : SV_Position;
    float2 uv  : TEXCOORD;
};

VSOut VSMain(VSIn input)
{
    VSOut o;
    o.pos = float4(input.pos, 1.0);
    o.uv = input.uv;
    return o;
}

float3x3 RGB2YIQ = float3x3(
    0.299,  0.587,  0.114,
    0.596, -0.274, -0.322,
    0.211, -0.523,  0.312);

float3x3 YIQ2RGB = float3x3(
    1.0,  0.956,  0.621,
    1.0, -0.272, -0.647,
    1.0, -1.107,  1.705);

float4 PSMain(VSOut input) : SV_Target
{
    float3 color = tex.Sample(samLinear, input.uv).rgb;
    float3 yiq = mul(RGB2YIQ, color);
    float cosH = cos(hue);
    float sinH = sin(hue);
    float3x3 rot = float3x3(
        1, 0, 0,
        0, cosH, -sinH,
        0, sinH, cosH);
    yiq = mul(rot, yiq);
    color = mul(YIQ2RGB, yiq);
    return float4(color, 1.0);
}

