#ifndef NINOX_SRP_SPACE_TRANSFORMS_INCLUDED
#define NINOX_SRP_SPACE_TRANSFORMS_INCLUDED

#include "Assets/SRP/Runtime/ShaderLibrary/Input.hlsl"
#include "Assets/SRP/Runtime/ShaderLibrary/UnityInput.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

float4 ComputeScreenPos(float4 positionCS);

struct VertexPositionInputs
{
    float3 positionWS; // World space position
    float3 positionVS; // View space position
    float4 positionCS; // Homogeneous clip space position
    float4 positionNDC;// Homogeneous normalized device coordinates
};

struct VertexNormalInputs
{
    real3 tangentWS;
    real3 bitangentWS;
    float3 normalWS;
};

VertexPositionInputs GetVertexPositionInputs(float3 positionOS)
{
    VertexPositionInputs input;
    input.positionWS = TransformObjectToWorld(positionOS);
    input.positionVS = TransformWorldToView(input.positionWS);
    input.positionCS = TransformWorldToHClip(input.positionWS);
    input.positionNDC = ComputeScreenPos(input.positionCS);
    return input;
}

VertexNormalInputs GetVertexNormalInputs(float3 normalOS)
{
    VertexNormalInputs tbn;
    tbn.tangentWS = real3(1.0, 0.0, 0.0);
    tbn.bitangentWS = real3(0.0, 1.0, 0.0);
    tbn.normalWS = TransformObjectToWorldNormal(normalOS);
    return tbn;
}

VertexNormalInputs GetVertexNormalInputs(float3 normalOS, float4 tangentOS)
{
    VertexNormalInputs tbn;

    // mikkts space compliant. only normalize when extracting normal at frag.
    real sign = tangentOS.w * GetOddNegativeScale();
    tbn.normalWS = TransformObjectToWorldNormal(normalOS);
    tbn.tangentWS = TransformObjectToWorldDir(tangentOS.xyz);
    tbn.bitangentWS = cross(tbn.normalWS, tbn.tangentWS) * sign;
    return tbn;
}

#if UNITY_REVERSED_Z
#if SHADER_API_OPENGL || SHADER_API_GLES || SHADER_API_GLES3
//GL with reversed z => z clip range is [near, -far] -> should remap in theory but dont do it in practice to save some perf (range is close enough)
#define UNITY_Z_0_FAR_FROM_CLIPSPACE(coord) max(-(coord), 0)
#else
//D3d with reversed Z => z clip range is [near, 0] -> remapping to [0, far]
//max is required to protect ourselves from near plane not being correct/meaningfull in case of oblique matrices.
#define UNITY_Z_0_FAR_FROM_CLIPSPACE(coord) max(((1.0-(coord)/_ProjectionParams.y)*_ProjectionParams.z),0)
#endif
#elif UNITY_UV_STARTS_AT_TOP
//D3d without reversed z => z clip range is [0, far] -> nothing to do
#define UNITY_Z_0_FAR_FROM_CLIPSPACE(coord) (coord)
#else
//Opengl => z clip range is [-near, far] -> should remap in theory but dont do it in practice to save some perf (range is close enough)
#define UNITY_Z_0_FAR_FROM_CLIPSPACE(coord) (coord)
#endif

float3 GetCameraPositionWS()
{
    return _WorldSpaceCameraPos;
}

// float4 GetScaledScreenParams()
// {
//     return _ScaledScreenParams;
// }



// A word on normalization of normals:
// For better quality normals should be normalized before and after
// interpolation. 
// 1) In vertex, skinning or blend shapes might vary significantly the lenght of normal. 
// 2) In fragment, because even outputting unit-length normals interpolation can make it non-unit.
// 3) In fragment when using normal map, because mikktspace sets up non orthonormal basis. 
// However we will try to balance performance vs quality here as also let users configure that as 
// shader quality tiers. 
// Low Quality Tier: Normalize either per-vertex or per-pixel depending if normalmap is sampled.
// Medium Quality Tier: Always normalize per-vertex. Normalize per-pixel only if using normal map
// High Quality Tier: Normalize in both vertex and pixel shaders.
real3 NormalizeNormalPerVertex(real3 normalWS)
{
#if defined(SHADER_QUALITY_LOW) && defined(_NORMALMAP)
    return normalWS;
#else
    return normalize(normalWS);
#endif
}

real3 NormalizeNormalPerPixel(real3 normalWS)
{
#if defined(SHADER_QUALITY_HIGH) || defined(_NORMALMAP)
    return normalize(normalWS);
#else
    return normalWS;
#endif
}

void AlphaDiscard(real alpha, real cutoff, real offset = 0.0h)
{
#ifdef _ALPHATEST_ON
    clip(alpha - cutoff + offset);
#endif
}

float4 ComputeScreenPos(float4 positionCS)
{
    float4 o = positionCS * 0.5f;
    o.xy = float2(o.x, o.y * _ProjectionParams.x) + o.w;
    o.zw = positionCS.zw;
    return o;
}

float3 ScreenToNDC(float4 screenPos)
{
    float3 posNDC = float3(screenPos.xy / _ScreenParams.xy, screenPos.z);
#if UNITY_UV_STARTS_AT_TOP
    if (!(_ProjectionParams.x < 0))
    {
        posNDC.y = 1 - posNDC.y;
    }
#endif
    return posNDC;
}

inline float4 ComputeGrabScreenPos(float4 pos)
{
#if UNITY_UV_STARTS_AT_TOP
    float scale = -1.0;
#else
    float scale = 1.0;
#endif
    float4 o = pos * 0.5f;
    o.xy = float2(o.x, o.y * scale) + o.w;
#ifdef UNITY_SINGLE_PASS_STEREO
    o.xy = TransformStereoScreenSpaceTex(o.xy, pos.w);
#endif
    o.zw = pos.zw;
    return o;
}

real ComputeFogFactor(float z)
{
    float clipZ_01 = UNITY_Z_0_FAR_FROM_CLIPSPACE(z);

#if defined(FOG_LINEAR)
    // factor = (end-z)/(end-start) = z * (-1/(end-start)) + (end/(end-start))
    float fogFactor = saturate(clipZ_01 * unity_FogParams.z + unity_FogParams.w);
    return real(fogFactor);
#elif defined(FOG_EXP) || defined(FOG_EXP2)
    // factor = exp(-(density*z)^2)
    // -density * z computed at vertex
    return real(unity_FogParams.x * clipZ_01);
#else
    return 0.0h;
#endif
}

real ComputeFogIntensity(real fogFactor)
{
    real fogIntensity = 0.0h;
#if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
#if defined(FOG_EXP)
    // factor = exp(-density*z)
    // fogFactor = density*z compute at vertex
    fogIntensity = saturate(exp2(-fogFactor));
#elif defined(FOG_EXP2)
    // factor = exp(-(density*z)^2)
    // fogFactor = density*z compute at vertex
    fogIntensity = saturate(exp2(-fogFactor * fogFactor));
#elif defined(FOG_LINEAR)
    fogIntensity = fogFactor;
#endif
#endif
    return fogIntensity;
}

half3 MixFogColor(real3 fragColor, real3 fogColor, real fogFactor)
{
#if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
    real fogIntensity = ComputeFogIntensity(fogFactor);
    fragColor = lerp(fogColor, fragColor, fogIntensity);
#endif
    return fragColor;
}

half3 MixFog(real3 fragColor, real fogFactor)
{
    return MixFogColor(fragColor, unity_FogColor.rgb, fogFactor);
}

#endif