#include "Macros.fxh"

cbuffer Parameters : register(b0)
{
    float3 CameraPosition;
    float4x4 ViewProj;
};

struct VSVertexInput
{
    float4 Position : POSITION0;
};

struct VSInstanceInput
{
    float3 InstancePosition : TEXCOORD0;
    float3 Axis : TEXCOORD1;
    float Rotation : TEXCOORD2;
    float2 Size : TEXCOORD3;
    float4 Color : COLOR0;
};

struct VSOutput
{
    float4 PositionPS : SV_Position;
    float2 UV : TEXCOORD0;
    float4 Color : COLOR0;
};

float3 ComputeAxisBillboardUp(float3 position, float3 axis)
{
    float3 toCamera = normalize(CameraPosition - position);
    float3 up = normalize(toCamera - dot(toCamera, axis) * axis);
    return up;
}

VSOutput VSInstancedParticle(VSVertexInput vin, VSInstanceInput inst)
{
    VSOutput vout;

    // --- 1. Start with quad vertex ---
    float2 local = vin.Position.xy; // [-0.5, 0.5]

    // --- 2. Apply per-particle rotation (spin in plane) ---
    float s = sin(inst.Rotation);
    float c = cos(inst.Rotation);

    float2 rotated;
    rotated.x = local.x * c - local.y * s;
    rotated.y = local.x * s + local.y * c;

    // --- 3. Apply size ---
    float2 scaled = rotated * inst.Size;

    // --- 4. Build world position using orientation basis ---
    float3 up = ComputeAxisBillboardUp(inst.InstancePosition, inst.Axis);
    float3 right = cross(inst.Axis, up);
    float3 worldPos =
        inst.InstancePosition
        + inst.Axis * scaled.x
        + right * scaled.y;

    // --- 5. Transform to clip space ---
    vout.PositionPS = mul(float4(worldPos, 1.0), ViewProj);
    vout.UV = local * 2.0; // [-1,1] range for falloff
    vout.Color = inst.Color;

    return vout;
}

float4 PSInstancedParticle(VSOutput pin) : SV_Target0
{
    // --- Radial falloff ---
    float dist = length(pin.UV);
    float alpha = smoothstep(1.0, 0.6, dist);

    // --- Optional: anisotropic stretch ---
    // float streak = abs(pin.UV.y);
    // alpha *= smoothstep(1.0, 0.2, streak);

    // --- Final color ---
    float4 color = pin.Color;
    color.a *= alpha;
    color.rgb *= color.a;

    return color;
}

TECHNIQUE(ParticleEffect_InstancedParticle, VSInstancedParticle, PSInstancedParticle);