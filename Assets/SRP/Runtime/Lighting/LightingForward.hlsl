#ifndef NINOX_SRP_FORWARDLIGHTING_INCLUDED
#define NINOX_SRP_FORWARDLIGHTING_INCLUDED


#include"LightingInterface.hlsl"
#include"LightingForward.cs.hlsl"

float Square(float x)
{
    return x * x;
}

Lighting GetMainLight(SurfaceData surface)
{
    float3 posWS = surface.posWS.xyz;
    float3 normalWS = surface.normalWS.xyz;
    Lighting lighting;
    float attenuation = 1.0;
    //attenuation=GetMainShadow(posWS, normalWS);
    lighting.color = _MainDirLightColor.xyz * attenuation;
    lighting.dirWS = _MainDirLightPosition.xyz;
    return lighting;
}


uint GetAdditionalLightsCount(SurfaceData surface)
{
    return _AdditionalLightsCount;
}


Lighting GetAdditionalLight(int lightIndex, float3 posWS)
{
    Lighting lighting;
    float3 lightPosWS = _AdditionalLightsPositions[lightIndex].xyz;
    lighting.color = _AdditionalLightsColors[lightIndex].xyz;
    float3 ray = lightPosWS - posWS;
    lighting.dirWS = normalize(ray);

    float distance2 = max(dot(ray, ray), 0.00001);
    float distanceAttenuation = pow(max(0,1 - pow(distance2 * _AdditionalLightsPositions[lightIndex].w,2)),2);
    float4 spotAngle = _AdditionalLightsSpotAngles[lightIndex];
    float3 spotDir = _AdditionalLightsSpotDirs[lightIndex].xyz;
    float spotAttenuation = Square(saturate(dot(spotDir, lighting.dirWS) * spotAngle.x + spotAngle.y));
    float attenuation = distanceAttenuation * rcp(distance2) * spotAttenuation;
    lighting.color *= attenuation;
    return lighting;
}
#endif