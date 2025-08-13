struct VSIn {
    float3 Pos   : ATTRIB0;
    float2 UV    : ATTRIB1;
    float4 Col   : ATTRIB2;
};

struct VSOut {
    float4 PosH  : SV_Position;
    float2 UV    : TEXCOORD0;
    float4 Color : COLOR0;
};

VSOut main(VSIn IN)
{
    VSOut OUT;
    OUT.PosH  = float4(IN.Pos, 1.0);
    OUT.UV    = IN.UV;
    OUT.Color = IN.Col;
    return OUT;
}
