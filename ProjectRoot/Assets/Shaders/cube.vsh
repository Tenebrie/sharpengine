// Matches your PSO layout:
// - VS dynamic:   g_InstanceData (SRV), g_ObjectIndex (UBO)
// - VS static:    Constants (you bind from C#)
// - PS mutable:   g_Texture (set by MaterialInstance)

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
    // Adjust to your real layout if needed.
    // If your buffer has separate View/Proj, replace ViewProjection with mul(View, Projection)
    row_major float4x4 ViewProjection;
};

cbuffer g_ObjectIndex
{
    uint ObjectIndex;   // base index into the big instance buffer for this draw
    uint3 _pad;         // keep 16-byte alignment
};

struct InstanceRecord
{
    // CPU writes row-major: keep row_major here to avoid transpose
    row_major float4x4 World;
    float4              Tint;   // RGBA per-instance (you write this in C#)
};

StructuredBuffer<InstanceRecord> g_InstanceData;

VSOut main(VSIn IN, uint instId : SV_InstanceID)
{
    uint idx = ObjectIndex + instId;
    InstanceRecord inst = g_InstanceData[idx];
    
    float4 wp = mul(float4(IN.Pos, 1.0), inst.World);
    VSOut OUT;
    OUT.PosH  = mul(wp, ViewProjection);
    OUT.UV    = IN.UV;
    OUT.Color = IN.Col * inst.Tint;   // combine vertex color and instance tint
    return OUT;
}
