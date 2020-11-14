#ifndef NINOX_SRP_LIGHTING_INCLUDED
#define NINOX_SRP_LIGHTING_INCLUDED

#include "LightingInterface.hlsl"

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/BSDF.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonLighting.hlsl"
#include "Assets/SRP/Runtime/Lighting/Shadow.hlsl"

#if defined(USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA)

StructuredBuffer<LightData> _AdditionalLightsBuffer;
StructuredBuffer<int> _AdditionalLightsIndices;
#else

#endif

//real SampleCascadeMainShadowAtlas(real3 posWS, real3 normalWS);

///////////////////////////////////////////////////////////////////////////////
//                         Phong Functions                                   //
///////////////////////////////////////////////////////////////////////////////

real3 DiffuseLambert(real3 lightDir, real3 normal)
{
    real NdotL = saturate(dot(normal, lightDir));
    return NdotL;
}

real3 SpecularBlinnPhong(real3 lightDir, real3 normal, real3 viewDir, real3 specular, real smoothness)
{
    real3 halfVec = SafeNormalize(real3(lightDir) + real3(viewDir));
    real NdotH = saturate(dot(normal, halfVec));
    real modifier = pow(NdotH, smoothness);
    real3 specularReflection = specular.rgb * modifier;
    return specularReflection;
}


///////////////////////////////////////////////////////////////////////////////
//                         BRDF Functions                                    //
///////////////////////////////////////////////////////////////////////////////

//const half4 dielBaseRefl = half4(0.04, 0.04, 0.04, 1.0 - 0.04);

//BRDFData InitBRDFData(real3 albedo, real metallic, real smoothness)
//{
//    BRDFData result;
//    result.reflectivity = lerp(dielBaseRefl.rgb, albedo, metallic);
//    result.diffuse = (1.0f - result.reflectivity) * albedo;
//    result.perceptualRoughness = 1;
//    result.roughness = max(1.0 - smoothness, HALF_MIN);
//    result.roughness2 = pow(result.roughness, 2);
//    result.roughness2MinusOne = result.roughness2 - 1.0h;
//    result.normalizationTerm = result.roughness * 4.0 + 2.0;
//    return result;
//}

//real DistributionGGX(real3 N, real3 H, real roughness)
//{
//    real a = roughness * roughness;
//    real a2 = a * a;
//    real NdotH = max(dot(N, H), 0.0);
//    real NdotH2 = NdotH * NdotH;

//    real nom = a2;
//    real denom = (NdotH2 * (a2 - 1.0) + 1.0);
//    denom = PI * denom * denom;

//    return nom / denom;
//}

//real GeometrySchlickGGX(real NdotV, real roughness)
//{
//    real r = (roughness + 1.0);
//    real k = (r * r) / 8.0;

//    real nom = NdotV;
//    real denom = NdotV * (1.0 - k) + k;

//    return nom / denom;
//}
//real GeometrySmith(real3 N, real3 V, real3 L, real roughness)
//{
//    real NdotV = max(dot(N, V), 0.0);
//    real NdotL = max(dot(N, L), 0.0);
//    real ggx2 = GeometrySchlickGGX(NdotV, roughness);
//    real ggx1 = GeometrySchlickGGX(NdotL, roughness);

//    return ggx1 * ggx2;
//}

//real3 fresnelSchlick(real cosTheta, real3 F0)
//{
//    return F0 + (1.0 - F0) * pow(1.0 - cosTheta, 5.0);
//}

// Based on Minimalist CookTorrance BRDF
// Implementation is slightly different from original derivation: http://www.thetenthplanet.de/archives/255
//
// * NDF [Modified] GGX
// * Modified Kelemen and Szirmay-Kalos for Visibility term
// * Fresnel approximated with 1/LdotH


//half3 PhysicalBaseLighting(SurfaceData surface, BRDFData brdfData)
//{
//    real3 col = 0.0.xxx;
//    real3 diff = 0.0.xxx;
//    real3 spec = 0.0.xxx;
//    Lighting mainLight = GetMainLight(surface);
//    diff = DiffuseLambert(mainLight.dirWS, surface.normalWS) * mainLight.color;
//    spec = SpecularBlinnPhong(mainLight.dirWS, surface.normalWS, surface.viewDirWS, mainLight.color, surface.smooth);
    
//    uint lightCount = GetAdditionalLightsCount(surface);
//    //return lightCount;
//    for (uint i = 0; i < lightCount; i++)
//    {
//        Lighting lighting = GetAdditionalLight(i, surface.posWS);
//        diff += DiffuseLambert(lighting.dirWS, surface.normalWS.xyz) * lighting.color;
//        spec += SpecularBlinnPhong(lighting.dirWS, surface.normalWS, surface.viewDirWS, lighting.color, surface.smooth);
//    }
//    return diff; //+spec;
//    real3 lightDirWS =
//    real3 halfDir = normalize(surface.viewDirWS + lightDirWS);
//    real NdotH = saturate(dot(surface.normalWS, halfDir));
//    real LdotH = saturate(dot(lightDirWS, halfDir));

//    //BRDFspec = roughness ^ 2 / (NoH ^ 2 * (roughness ^ 2 - 1) + 1) ^ 2 * (LoH ^ 2 * (roughness + 0.5) * 4.0)
//    real d = NdotH * NdotH * brdfData.roughness2MinusOne + 1.0f;
                        
//    real LdotH2 = LdotH * LdotH;
    
//    real specularTerm = brdfData.roughness2 / ((d * d) * max(0.1h, LdotH2) * brdfData.normalizationTerm);
    
//    real3 color = specularTerm * brdfData.reflectivity + brdfData.diffuse;
//    return color;
//}
//real3 SmithGGX()
//{
    
//}


//float SchlickFresnel(float u)
//{
//    float m = clamp(1 - u, 0, 1);
//    float m2 = m * m;
//    return m2 * m2 * m; // pow(m,5)
//}

//real BRDFDiffuse(real NdotV, real NdotL, real LdotV, real perceptualRoughness)
//{
//    return DisneyDiffuse(NdotV, NdotL, LdotV, perceptualRoughness);
//}

//real3 BRDF(SurfaceData surface, BRDFData brdfData, Lighting lighting)
//{
//    real NdotL = dot(surface.normalWS, lighting.dirWS);
//    real NdotV = dot(surface.normalWS, surface.viewDirWS);
//    if (NdotL < 0 || NdotV < 0 || LdotV < 0)
//    {
//        return 0.0.xxx;
//    }
    
//    real LdotV, NdotH, LdotH, invLenLV;
//    GetBSDFAngle(surface.viewDirWS, lighting.dirWS, NdotL, NdotV, LdotV, NdotH, LdotH, invLenLV);
    
//    real3 H = normalize(surface.viewDirWS + lighting.dirWS);
//    real HdotL = dot(H, lighting.dirWS);
    
//    real perceptualRoughness = RoughnessToPerceptualRoughness(brdfData.roughness);
    
//    //disney diffuse
//    real diffuseTerm = BRDFDiffuse(NdotV, NdotL, LdotV, perceptualRoughness);
//    real FL = SchlickFresnel(NdotL), FV = SchlickFresnel(NdotV);
//    float Fd90 = 0.5 + 2 * HdotL * HdotL * brdfData.roughness;
//    float Fd = lerp(1.0, Fd90, FL) * lerp(1.0, Fd90, FV);
    
    
    
//    return diffuseTerm + D() * F() * G() / (4 * NdotL * NdotV);
//    real3 h_n = SafeNormalize(surface.viewDirWS + lighting.dirWS);
//    real

//}

real3 BlinnPhongLighting(SurfaceData surface)
{
    real3 col = 0.0.xxx;
    real3 diff = 0.0.xxx;
    real3 spec = 0.0.xxx;
   
    Lighting mainLight = GetMainLight(surface);
    diff = DiffuseLambert(mainLight.dirWS, surface.normalWS) * mainLight.color;
    spec = SpecularBlinnPhong(mainLight.dirWS, surface.normalWS, surface.viewDirWS, mainLight.color, surface.smooth);
    
    uint lightCount = GetAdditionalLightsCount(surface);
    for (uint i = 0; i < lightCount; i++)
    {
        Lighting lighting = GetAdditionalLight(i, surface.posWS);
        diff += DiffuseLambert(lighting.dirWS, surface.normalWS.xyz) * lighting.color;
        spec += SpecularBlinnPhong(lighting.dirWS, surface.normalWS, surface.viewDirWS, lighting.color, surface.smooth);
    }
    return diff; //+spec;
}

#if defined(USE_CLUSTERED_LIGHTLIST)
#include "LightingCluster.hlsl"
#else
#include "LightingForward.hlsl"
#endif


#endif