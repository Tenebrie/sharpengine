struct PlaneData
{
    float3 Normal;
    float Distance;
};

struct Entry
{
    float3 Position;
    float BoundingSphereRadius;
};

StructuredBuffer<Entry> InData  : register(t0);
RWStructuredBuffer<float> OutData : register(u0);

cbuffer Constants : register(b0)
{
    uint Count;
    float3 _padding;

    PlaneData LeftPlane;
    PlaneData RightPlane;
    PlaneData TopPlane;
    PlaneData BottomPlane;
    PlaneData NearPlane;
    PlaneData FarPlane;
}

bool IsInsideFrustum(Entry entry, PlaneData left, PlaneData right, PlaneData top, PlaneData bottom, PlaneData nearP, PlaneData farP)
{
    float radius = entry.BoundingSphereRadius;
    float3 pos = entry.Position;

    if (dot(left.Normal, pos) + left.Distance < -radius) return false;
    if (dot(right.Normal, pos) + right.Distance < -radius) return false;
    if (dot(top.Normal, pos) + top.Distance < -radius) return false;
    if (dot(bottom.Normal, pos) + bottom.Distance < -radius) return false;
    if (dot(nearP.Normal, pos) + nearP.Distance < -radius) return false;
    if (dot(farP.Normal, pos) + farP.Distance < -radius) return false;

    return true;
}

[numthreads(256, 1, 1)]
void main(uint3 ThreadId : SV_DispatchThreadID)
{
    uint i = ThreadId.x;
    if (i >= Count)
        return;

    Entry entry = InData[i];
    // Perform frustum culling using the planes
    if (IsInsideFrustum(entry, LeftPlane, RightPlane, TopPlane, BottomPlane, NearPlane, FarPlane))
    {
        OutData[i] = 1.0f; // Visible
    }
    else
    {
        OutData[i] = 0.0f; // Culled
    }
}
