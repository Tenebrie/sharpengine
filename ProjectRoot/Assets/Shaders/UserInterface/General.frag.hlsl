struct PSIn
{
    float4 PosH  : SV_Position;
    float2 UV    : TEXCOORD0;
    float4 Color : COLOR0;
};

Texture2D    g_Texture;
SamplerState g_Texture_sampler;

float4 main(PSIn IN) : SV_Target
{
    float4 tex = g_Texture.Sample(g_Texture_sampler, IN.UV);
    return tex * IN.Color;
}
