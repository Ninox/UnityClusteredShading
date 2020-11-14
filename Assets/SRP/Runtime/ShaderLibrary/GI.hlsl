#ifndef NINOX_SRP_GI_INCLUDED
#define NINOX_SRP_GI_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"

#if defined(LIGHTMAP_ON)
#define GI_ATTRIBUTE_DATA float2 lightMapUV : TEXCOORD1;
#define GI_VARYINGS_DATA float2 lightMapUV : VAR_LIGHT_MAP_UV;

#define TRANSFER_GI_DATA(input, output) \
		output.lightMapUV = input.lightMapUV * \
		unity_LightmapST.xy + unity_LightmapST.zw;

#define GI_FRAGMENT_DATA(input) input.lightMapUV
#else
#define GI_ATTRIBUTE_DATA
#define GI_VARYINGS_DATA
#define TRANSFER_GI_DATA(input, output)
#define GI_FRAGMENT_DATA(input) 0.0
#endif


struct GI
{
    float3 diffuse;
};

float3 SampleLightMap(float2 lightMapUV)
{
    bool encodedLightmap;
    			#if defined(UNITY_LIGHTMAP_FULL_HDR)
				encodedLightmap=false;
			#else
				encodedLightmap=true;
			#endif
#if defined(LIGHTMAP_ON)
	return SampleSingleLightmap(TEXTURE2D_ARGS(unity_Lightmap,samplerunity_Lightmap),lightMapUV,float4(1.0,1.0,0.0,0.0),encodedLightmap,float4(LIGHTMAP_HDR_MULTIPLIER, LIGHTMAP_HDR_EXPONENT, 0.0, 0.0));
#else
    return 0.0.xxx;
#endif
}

GI GetGI(float2 lightMapUV)
{
    GI gi;
    gi.diffuse = float3(lightMapUV, 0.0);
    return gi;
}

// Samples SH L0, L1 and L2 terms
half3 SampleSH(half3 normalWS)
{
    // LPPV is not supported in Ligthweight Pipeline
    real4 SHCoefficients[7];
    SHCoefficients[0] = unity_SHAr;
    SHCoefficients[1] = unity_SHAg;
    SHCoefficients[2] = unity_SHAb;
    SHCoefficients[3] = unity_SHBr;
    SHCoefficients[4] = unity_SHBg;
    SHCoefficients[5] = unity_SHBb;
    SHCoefficients[6] = unity_SHC;

    return max(half3(0, 0, 0), SampleSH9(SHCoefficients, normalWS));
}

#endif