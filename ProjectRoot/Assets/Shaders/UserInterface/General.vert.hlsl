struct VSIn {
    float3 Pos   : ATTRIB0;
    float2 UV    : ATTRIB1;
    float4 Col   : ATTRIB2;
    float3 Nrm   : ATTRIB3; // not used here
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
    uint ObjectIndex;   // base index into the big instance buffer for this draw
    uint3 _pad;         // keep 16-byte alignment
};

struct InstanceRecord
{
    row_major float4x4 World;
    float4 Tint;
    float2 UvOffset;
    float2 UvScale;
};

StructuredBuffer<InstanceRecord> g_InstanceData;

VSOut main(VSIn IN, uint instId : SV_InstanceID)
{
    uint idx = ObjectIndex + instId;
    InstanceRecord inst = g_InstanceData[idx];
    
    // float4 wp = float4(IN.Pos, 1.0);
    float4 wp = mul(float4(IN.Pos * 1.0, 1.0), inst.World * 1.0);
    float2 uv = float2(IN.UV.x, 1.0 - IN.UV.y);
    // float4 wp = mul(float4(IN.Pos * 1.0, 1.0), inst.World);
    // float2 uv = float2(IN.UV.x, 1.0 - IN.UV.y);
    VSOut OUT;
    OUT.PosH  = wp;
    OUT.UV    = uv * inst.UvScale + inst.UvOffset;
    OUT.Color = IN.Col * inst.Tint;
    return OUT;
}
