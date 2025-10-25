struct PSIn
{
    float4 PosH         : SV_Position;
    float4 Color        : COLOR0;
    float2 UV           : TEXCOORD0;
    float2 SizePx       : TEXCOORD1; // on-screen size of the quad in pixels
    float4 BorderRadius : TEXCOORD2; // border radius in pixels
};

Texture2D    g_Texture;
SamplerState g_Texture_sampler;

// Hardcoded pixel corner radius
static const float kSoftnessPx = 0.2;  // AA falloff width in pixels

float RoundedBoxSDF_Px(float2 relativePixelPosition, float2 halfPixelSize, float4 radiusPerCorner)
{
    bool right  = relativePixelPosition.x >= 0.0;
    bool bottom = relativePixelPosition.y >= 0.0;

    float chosenRadius = !right && !bottom ? radiusPerCorner.x :   // TL
                          right && !bottom ? radiusPerCorner.y :   // TR
                          right && bottom  ? radiusPerCorner.z :   // BR
                                             radiusPerCorner.w;    // BL
    
    // clamp radius so it never exceeds the smallest half-size
    chosenRadius = max(0.0, min(chosenRadius, min(halfPixelSize.x, halfPixelSize.y)));

    // standard rounded-rect SDF
    float2 q = abs(relativePixelPosition) - (halfPixelSize - chosenRadius);
    float outside = length(max(q, 0.0));
    float inside  = min(max(q.x, q.y), 0.0);
    return outside + inside - chosenRadius;   // <0 inside
}

float4 main(PSIn IN) : SV_Target
{
    float4 tex = g_Texture.Sample(g_Texture_sampler, IN.UV);

    float2 sizePx  = IN.SizePx;
    float2 halfPixelSize  = sizePx * 0.5;
    float2 relativePixelPosition = IN.UV * sizePx - halfPixelSize;

    // TODO: Support different radius per corner
    float dist = RoundedBoxSDF_Px(relativePixelPosition, halfPixelSize, IN.BorderRadius);

    float aa = max(kSoftnessPx, fwidth(dist));
    float coverage = saturate(0.5 - dist / aa);

    float4 outColor = tex * IN.Color;
    outColor.a *= coverage;
    return outColor;
}