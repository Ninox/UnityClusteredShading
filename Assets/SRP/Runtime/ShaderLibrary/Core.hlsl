#ifndef NINOX_SRP_CORE_INCLUDED
#define NINOX_SRP_CORE_INCLUDED

#include "Assets/SRP/Runtime/ShaderLibrary/UnityInput.hlsl"
#include "Assets/SRP/Runtime/ShaderLibrary/Input.hlsl"
#include "Assets/SRP/Runtime/ShaderLibrary/GI.hlsl"
#include "Assets/SRP/Runtime/ShaderLibrary/SpaceTransforms.hlsl"


// Stereo-related bits
#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)

#define SLICE_ARRAY_INDEX   unity_StereoEyeIndex

#define TEXTURE2D_X                 TEXTURE2D_ARRAY
#define TEXTURE2D_X_PARAM           TEXTURE2D_ARRAY_PARAM
#define TEXTURE2D_X_ARGS            TEXTURE2D_ARRAY_ARGS
#define TEXTURE2D_X_HALF            TEXTURE2D_ARRAY_HALF
#define TEXTURE2D_X_FLOAT           TEXTURE2D_ARRAY_FLOAT

#define LOAD_TEXTURE2D_X(textureName, unCoord2)                         LOAD_TEXTURE2D_ARRAY(textureName, unCoord2, SLICE_ARRAY_INDEX)
#define LOAD_TEXTURE2D_X_LOD(textureName, unCoord2, lod)                LOAD_TEXTURE2D_ARRAY_LOD(textureName, unCoord2, SLICE_ARRAY_INDEX, lod)    
#define SAMPLE_TEXTURE2D_X(textureName, samplerName, coord2)            SAMPLE_TEXTURE2D_ARRAY(textureName, samplerName, coord2, SLICE_ARRAY_INDEX)
#define SAMPLE_TEXTURE2D_X_LOD(textureName, samplerName, coord2, lod)   SAMPLE_TEXTURE2D_ARRAY_LOD(textureName, samplerName, coord2, SLICE_ARRAY_INDEX, lod)
#define GATHER_TEXTURE2D_X(textureName, samplerName, coord2)            GATHER_TEXTURE2D_ARRAY(textureName, samplerName, coord2, SLICE_ARRAY_INDEX)
#define GATHER_RED_TEXTURE2D_X(textureName, samplerName, coord2)        GATHER_RED_TEXTURE2D(textureName, samplerName, float3(coord2, SLICE_ARRAY_INDEX))
#define GATHER_GREEN_TEXTURE2D_X(textureName, samplerName, coord2)      GATHER_GREEN_TEXTURE2D(textureName, samplerName, float3(coord2, SLICE_ARRAY_INDEX))
#define GATHER_BLUE_TEXTURE2D_X(textureName, samplerName, coord2)       GATHER_BLUE_TEXTURE2D(textureName, samplerName, float3(coord2, SLICE_ARRAY_INDEX))

#else

#define SLICE_ARRAY_INDEX       0

#define TEXTURE2D_X                 TEXTURE2D
#define TEXTURE2D_X_PARAM           TEXTURE2D_PARAM
#define TEXTURE2D_X_ARGS            TEXTURE2D_ARGS
#define TEXTURE2D_X_HALF            TEXTURE2D_HALF
#define TEXTURE2D_X_FLOAT           TEXTURE2D_FLOAT

#define LOAD_TEXTURE2D_X            LOAD_TEXTURE2D
#define LOAD_TEXTURE2D_X_LOD        LOAD_TEXTURE2D_LOD
#define SAMPLE_TEXTURE2D_X          SAMPLE_TEXTURE2D
#define SAMPLE_TEXTURE2D_X_LOD      SAMPLE_TEXTURE2D_LOD
#define GATHER_TEXTURE2D_X          GATHER_TEXTURE2D
#define GATHER_RED_TEXTURE2D_X      GATHER_RED_TEXTURE2D
#define GATHER_GREEN_TEXTURE2D_X    GATHER_GREEN_TEXTURE2D
#define GATHER_BLUE_TEXTURE2D_X     GATHER_BLUE_TEXTURE2D

#endif

#if defined(UNITY_SINGLE_PASS_STEREO)
float2 TransformStereoScreenSpaceTex(float2 uv, float w)
{
    // TODO: RVS support can be added here, if Universal decides to support it
    float4 scaleOffset = unity_StereoScaleOffset[unity_StereoEyeIndex];
    return uv.xy * scaleOffset.xy + scaleOffset.zw * w;
}

float2 UnityStereoTransformScreenSpaceTex(float2 uv)
{
    return TransformStereoScreenSpaceTex(saturate(uv), 1.0);
}

#else

#define UnityStereoTransformScreenSpaceTex(uv) uv

#endif // defined(UNITY_SINGLE_PASS_STEREO)

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#endif