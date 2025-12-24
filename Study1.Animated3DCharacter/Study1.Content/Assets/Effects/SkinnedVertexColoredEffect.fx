#include "Macros.fxh"

#define SKINNED_EFFECT_MAX_BONES 72

cbuffer Parameters : register(b0) {

    float4 DiffuseColor;
    float3 AmbientColor;
    float3 SpecularColor;
    float  SpecularPower;

    float3 DirLight0Direction;
    float3 DirLight0DiffuseColor;
    float3 DirLight0SpecularColor;

    float3 DirLight1Direction;
    float3 DirLight1DiffuseColor;
    float3 DirLight1SpecularColor;

    float3 DirLight2Direction;
    float3 DirLight2DiffuseColor;
    float3 DirLight2SpecularColor;

    float3 EyePosition;

    float3 FogColor;
    float4 FogVector;

    float4x4 World;
    float3x3 WorldInverseTranspose;
    float4x4 WorldViewProj;
    
    float4x3 Bones[SKINNED_EFFECT_MAX_BONES];

};

struct CommonVSOutput
{
    float4 PositionPS;
    float4 Diffuse;
    float3 Specular;
    float FogFactor;
};

struct VSInput
{
    float4 Position : POSITION0;
    uint4 BoneIndices : BLENDINDICES0;
    float4 BoneWeights : BLENDWEIGHT0;
    float3 Normal : NORMAL0;
    float4 Color : COLOR;
};

struct VSOutput
{
    float4 PositionPS : SV_Position;
    float4 Diffuse : COLOR0;
    float4 Specular : COLOR1;
};

// START FILE: Lighting.fxh
struct ColorPair
{
    float3 Diffuse;
    float3 Specular;
};


ColorPair ComputeLights(float3 eyeVector, float3 worldNormal, uniform int numLights)
{
    float3x3 lightDirections = 0;
    float3x3 lightDiffuse = 0;
    float3x3 lightSpecular = 0;
    float3x3 halfVectors = 0;
    
    [unroll]
    for (int i = 0; i < numLights; i++)
    {
        lightDirections[i] = float3x3(DirLight0Direction,     DirLight1Direction,     DirLight2Direction    )[i];
        lightDiffuse[i]    = float3x3(DirLight0DiffuseColor,  DirLight1DiffuseColor,  DirLight2DiffuseColor )[i];
        lightSpecular[i]   = float3x3(DirLight0SpecularColor, DirLight1SpecularColor, DirLight2SpecularColor)[i];

        halfVectors[i] = normalize(eyeVector - lightDirections[i]);
    }

    float3 dotL = mul(-lightDirections, worldNormal);
    float3 dotH = mul(halfVectors, worldNormal);
    
    float3 zeroL = step(float3(0,0,0), dotL);

    float3 diffuse  = zeroL * dotL;
    float3 specular = pow(max(dotH, 0) * zeroL, SpecularPower);

    ColorPair result;
    result.Diffuse  = mul(diffuse,  lightDiffuse)  * DiffuseColor.rgb + AmbientColor;
    result.Specular = mul(specular, lightSpecular) * SpecularColor;
    return result;
}

float ComputeFogFactor(float4 position)
{
    return saturate(dot(position, FogVector));
}

void ApplyFog(inout float4 color, float fogFactor)
{
    color.rgb = lerp(color.rgb, FogColor * color.a, fogFactor);
}

CommonVSOutput ComputeCommonVSOutputWithLighting(float4 position, float3 normal, uniform int numLights)
{
    CommonVSOutput vout;
    
    float4 posWS = mul(position, World);
    float3 eyeVector = normalize(EyePosition - posWS.xyz);
    float3 worldNormal = normalize(mul(normal, WorldInverseTranspose));

    ColorPair lightResult = ComputeLights(eyeVector, worldNormal, numLights);
    
    vout.PositionPS = mul(position, WorldViewProj);
    vout.Diffuse = float4(lightResult.Diffuse, DiffuseColor.a);
    vout.Specular = lightResult.Specular;
    vout.FogFactor = ComputeFogFactor(position);
    
    return vout;
}

void AddSpecular(inout float4 color, float3 specular)
{
    color.rgb += specular * color.a;
}
// END FILE: Lighting.fxh

void Skin(inout VSInput vin, uniform int boneCount)
{
    float4x3 skinning = 0;

    [unroll]
    for (int i = 0; i < boneCount; i++)
    {
        skinning += Bones[vin.BoneIndices[i]] * vin.BoneWeights[i];
    }

    vin.Position.xyz = mul(vin.Position, skinning);
    vin.Normal = mul(vin.Normal, (float3x3)skinning);
}

VSOutput VSSkinnedVertexLighting(VSInput vin)
{
    VSOutput vout;
    
    Skin(vin, 4);
    
    CommonVSOutput cout = ComputeCommonVSOutputWithLighting(vin.Position, vin.Normal, 3);
    vout.PositionPS = cout.PositionPS;
    vout.Diffuse = cout.Diffuse * vin.Color;
    vout.Specular = float4(cout.Specular, cout.FogFactor);

    return vout;
}

// TODO: move this to content processor
// sRGB Gamma correction: https://stackoverflow.com/a/61138576
// Same as what blender uses: https://blender.stackexchange.com/a/311654
float RgbToSrgb (float theLinearValue) {
  return theLinearValue <= 0.0031308f
       ? theLinearValue * 12.92f
       : pow(theLinearValue, 1.0f/2.4f) * 1.055f - 0.055f;
}

float4 PSSkinnedVertexLighting(VSOutput pin) : SV_Target0
{
    float4 color = pin.Diffuse;
    AddSpecular(color, pin.Specular.rgb);
    ApplyFog(color, pin.Specular.w);
    
    return float4(RgbToSrgb(color.x), RgbToSrgb(color.y), RgbToSrgb(color.z), color.w);
}

TECHNIQUE(SkinnedEffect_VertexLighting,	VSSkinnedVertexLighting, PSSkinnedVertexLighting);
