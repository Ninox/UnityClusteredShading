#ifndef NINOX_SRP_LIGHTING_INTERFACE_INCLUDED
#define NINOX_SRP_LIGHTING_INTERFACE_INCLUDED


#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
struct SurfaceData
{
    float3 posWS;
    float4 posSS;
    float3 normalWS;
    float3 viewDirWS;
    float3 albedo;
    float3 specular;
    float3 bakedGI;
    float smooth;
};

struct Lighting
{
    float3 color;
    float3 dirWS;
};


uint GetAdditionalLightsCount(SurfaceData surface);
Lighting GetAdditionalLight(int lightIndex, float3 posWS);
Lighting GetMainLight(SurfaceData surface);



#if defined(USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA)

StructuredBuffer<LightData> _AdditionalLightsBuffer;
StructuredBuffer<int> _AdditionalLightsIndices;
#else

#endif

//float SampleCascadeMainShadowAtlas(float3 posWS, float3 normalWS);

///////////////////////////////////////////////////////////////////////////////
//                         Lighting Functions                                //
///////////////////////////////////////////////////////////////////////////////

struct BRDFData
{
    real3 diffuse;
    real3 reflectivity;
    real specular;
    real perceptualRoughness;
    real roughness;
    real roughness2;
    real grazingTerm;

    // We save some light invariant BRDF terms so we don't have to recompute
    // them in the light loop. Take a look at DirectBRDF function for detailed explaination.
    real normalizationTerm; // roughness * 4.0 + 2.0
    real roughness2MinusOne; // roughness^2 - 1.0
};

real3 BDRF(BRDFData brdfData, real3 normalWS, real3 lightDirWS, real3 viewDirWS);

real3 BlinnPhong(SurfaceData surface);

#endif