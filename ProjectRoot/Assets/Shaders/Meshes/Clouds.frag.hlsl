cbuffer CloudParams
{
    float Time;
    float DensityMaskMin;
    float DensityMaskMax;
    float3 SunDir;
    float2 _padding;
};

// ==== Noise functions ====
float hash(float2 p)
{
    return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
}

float noise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);

    float a = hash(i);
    float b = hash(i + float2(1, 0));
    float c = hash(i + float2(0, 1));
    float d = hash(i + float2(1, 1));

    float2 u = f * f * (3.0 - 2.0 * f);
    return lerp(a, b, u.x) + (c - a) * u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
}

float fbm(float2 p)
{
    float sum = 0.0;
    float amp = 0.5;
    for (int i = 0; i < 5; i++)
    {
        sum += amp * noise(p);
        p *= 2.0;
        amp *= 0.5;
    }
    return sum;
}

struct PSIn
{
    float4 PosH  : SV_Position;
    float2 UV    : TEXCOORD0;
    float4 Color : COLOR0;
};

float4 main(PSIn IN) : SV_Target
{
    // Base cloud mask
    float base = fbm(IN.UV);
    float mask = smoothstep(DensityMaskMin, DensityMaskMax, base);

    // Fake lighting from sun dir
    float gradX = fbm(IN.UV + float2(0.01, 0)) - fbm(IN.UV - float2(0.01, 0));
    float gradY = fbm(IN.UV + float2(0, 0.01)) - fbm(IN.UV - float2(0, 0.01));
    float3 n = normalize(float3(-gradX, -gradY, 1.0));
    float light = saturate(dot(n, normalize(SunDir)));

    // Final color
    float alpha = mask * IN.Color.a;
    float3 cloudColor = lerp(float3(0.8, 0.8, 0.8), float3(1.0, 1.0, 1.0), light);
    cloudColor *= IN.Color.rgb;

    return float4(min(1.0, cloudColor.r), min(1.0, cloudColor.g), min(1.0, cloudColor.b), min(1.0, alpha));
}
