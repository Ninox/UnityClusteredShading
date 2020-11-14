#ifndef NINOX_SRP_SHADOW_INCLUDED
#define NINOX_SRP_SHADOW_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Shadow/ShadowSamplingTent.hlsl"

#define MAX_CASCADE_COUNT 4

#define SHADOW_SAMPLER sampler_linear_clamp_compare
SAMPLER_CMP(SHADOW_SAMPLER);

TEXTURE2D(_MainShadowAtlas);
TEXTURE2D(_AdditionalShadowAtlas);SAMPLER(sampler_AdditionalShadowAtlas);

float _ShadowNormalBias;
float4x4 _MainShadowVPMatrixArray[MAX_CASCADE_COUNT];

//#region Cascade
#define _MAIN_LIGHT_SHADOWS_CASCADE

#if defined(_MAIN_LIGHT_SHADOWS_CASCADE)
#define CASCADE_TILE_SIDE 2 
float2 _MainShadowAtlasSize;
int _CascadeCount;
float4 _CascadeDistanceFade;
float4 _CascadeCullingSphereArray[MAX_CASCADE_COUNT];

float Distance2(float3 pos1WS, float3 pos2WS)
{
    float3 dis = pos2WS - pos1WS;
    return dot(dis, dis);
}

float FadedShadowStrength(float distance2, float scale, float fade)
{
    return saturate((1.0 - distance2 * scale) * fade);
}

#endif


#define _DIRECTIONAL_PCF5

#if defined(_DIRECTIONAL_PCF3)
	#define DIRECTIONAL_FILTER_SAMPLES 4
	#define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_3x3
#elif defined(_DIRECTIONAL_PCF5)
	#define DIRECTIONAL_FILTER_SAMPLES 9
	#define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_5x5
#elif defined(_DIRECTIONAL_PCF7)
	#define DIRECTIONAL_FILTER_SAMPLES 16
	#define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_7x7
#endif

#define _OTHER_PCF3

#if defined(_OTHER_PCF3)
	#define OTHER_FILTER_SAMPLES 4
	#define OTHER_FILTER_SETUP SampleShadow_ComputeSamples_Tent_3x3
#elif defined(_OTHER_PCF5)
	#define OTHER_FILTER_SAMPLES 9
	#define OTHER_FILTER_SETUP SampleShadow_ComputeSamples_Tent_5x5
#elif defined(_OTHER_PCF7)
	#define OTHER_FILTER_SAMPLES 16
	#define OTHER_FILTER_SETUP SampleShadow_ComputeSamples_Tent_7x7
#endif


float SampleMainShadowAtlas(float3 posSTS)
{
#if defined(_DIRECTIONAL_PCF3)||defined(_DIRECTIONAL_PCF5)||defined(_DIRECTIONAL_PCF7)
    real attenuation = 0;
    real fetchesWeights[DIRECTIONAL_FILTER_SAMPLES];
    real2 fetchesUV[DIRECTIONAL_FILTER_SAMPLES];
    real width, height;
    _MainShadowAtlas.GetDimensions(width, height);
    real2 shadowMapTexture_TexelSize = real2(width, 1.0 / width);
    DIRECTIONAL_FILTER_SETUP(shadowMapTexture_TexelSize.yyxx, posSTS.xy, fetchesWeights, fetchesUV);
    for (int i = 0; i < DIRECTIONAL_FILTER_SAMPLES; i++)
    {
        attenuation += SAMPLE_TEXTURE2D_SHADOW(_MainShadowAtlas, SHADOW_SAMPLER, real3(fetchesUV[i], posSTS.z)) * fetchesWeights[i];
    }
    return attenuation;
#else
    return SAMPLE_TEXTURE2D_SHADOW(_MainShadowAtlas, SHADOW_SAMPLER, posSTS);
#endif
}

float GetMainShadow(float3 posWS, float3 normalWS)
{
    float3 biasedPos = posWS + normalWS * _ShadowNormalBias;
#if defined(_MAIN_LIGHT_SHADOWS_CASCADE)
    float fade;
    for (int i = 0; i < _CascadeCount; i++)
    {
        float4 sphere = _CascadeCullingSphereArray[i];
        float distance2 = Distance2(biasedPos, sphere.xyz);
        if (distance2 < sphere.w)
        {
            fade *= FadedShadowStrength(distance2, 1 / sphere.w, _CascadeDistanceFade.z);
            float4 posSTS = mul(_MainShadowVPMatrixArray[i], float4(biasedPos, 1));
            return SampleMainShadowAtlas(posSTS.xyz);
        }
    }
    return 1.0;
#else
    float4 posSTS=mul(_MainShadowVPMatrixArray[0],float4(biasedPos,1.0));
    return SampleMainShadowAtlas(posSTS.xyz);
#endif
}

float GetAdditionalShadow(float3 posWS, float3 normalWS)
{
    
}

#endif