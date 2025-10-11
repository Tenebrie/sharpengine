struct VSIn {
    float3 Pos   : ATTRIB0;
    float2 UV    : ATTRIB1;
    float4 Col   : ATTRIB2;
    float3 Nrm   : ATTRIB3;
};

struct VSOut {
    float4 PosH  : SV_Position;
    float2 UV    : TEXCOORD0;
    float4 Color : COLOR0;
};

cbuffer Constants
{
    row_major float4x4 ViewProjection;
    float4 ScreenSize; // xy = size, zw = 1/size
};

cbuffer g_ObjectIndex
{
    uint ObjectIndex;
    uint3 _pad;
};

struct InstanceRecord
{
    row_major float4x4 WorldTransform;
    float4 Tint;
    float2 UvOffset;
    float2 UvScale;
};

StructuredBuffer<InstanceRecord> g_InstanceData;
VSOut main(VSIn Vertex, uint InstanceID : SV_InstanceID)
{
    uint idx = ObjectIndex + InstanceID;
    InstanceRecord instance = g_InstanceData[idx];

    float4 pixelPos = mul(float4(Vertex.Pos, 1.0f), instance.WorldTransform);

    float2 instanceSize = float2(instance.WorldTransform._11, instance.WorldTransform._22);
    pixelPos.xy += instanceSize * 0.5f;

    float2 uv01 = pixelPos.xy * ScreenSize.zw;
    float2 clipXY = float2(uv01.x * 2.0f - 1.0f, 1.0f - uv01.y * 2.0f);

    VSOut OUT;
    OUT.PosH  = float4(clipXY, 0.0f, 1.0f);
    OUT.UV    = float2(Vertex.UV.x, Vertex.UV.y) * instance.UvScale + instance.UvOffset;
    OUT.Color = Vertex.Col * instance.Tint;
    return OUT;
}
