#ifndef NINOX_SRP_CLUSTER_LIGHTING_INCLUDED
#define NINOX_SRP_CLUSTER_LIGHTING_INCLUDED
#include "Assets/SRP/Runtime/ShaderLibrary/Core.hlsl"
#include "Assets/SRP/Runtime/ShaderLibrary/Math.hlsl"

#include "LightingInterface.hlsl"

CBUFFER_START(LightingClustered)
#include "LightingCluster.cs.hlsl"
CBUFFER_END
StructuredBuffer<AdditionalLightData> _LightingCluster_LightList;
StructuredBuffer<uint> _LightingCluster_LightIndex;
StructuredBuffer<uint> _LightingCluster_GridIndex;

RWStructuredBuffer<AABB> _LightingCluster_Grid_RW; // A linear list of AABB's with size = numclusters
RWStructuredBuffer<uint> _LightingCluster_LightIndex_RW;
RWStructuredBuffer<uint> _LightingCluster_GridIndex_RW;



float PosSS2GridIndex(float3 posSS);

void DecodeLightIndex(uint data, out uint start, out uint count)
{
    count = data >> 24;
    start = data & 0xffffff;
    //count = data;
}

uint EncodeLightIndex(uint start,uint count)
{
    //return count;
    return start | (count << 24);
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

uint lightingCluster_Offset;
uint GetAdditionalLightsCount(SurfaceData surface)
{
    float3 posNDC = ScreenToNDC(surface.posSS);
    float grid = PosSS2GridIndex(posNDC);
    uint data=_LightingCluster_GridIndex[grid];
    uint count;
    DecodeLightIndex(data, lightingCluster_Offset, count);
    return count;
}


Lighting GetAdditionalLight(int lightIndex, float3 posWS)
{
    uint index = lightingCluster_Offset + lightIndex;
    AdditionalLightData lightData = _LightingCluster_LightList[_LightingCluster_LightIndex[index]];
    Lighting lighting;
    lighting.color = lightData.Color;
    float3 ray = lightData.PosWS - posWS;
    lighting.dirWS = normalize(ray);

    float distance2 = max(dot(ray, ray), 0.00001);
    float distanceAttenuation = Square(
        max(0, 1 - Square(distance2 * lightData.AttenuationCoef))
        );
    half2 spotAngle = lightData.SpotAngle;
    float3 spotDir = lightData.SpotDir.xyz;
    float spotAttenuation = Square(saturate(dot(spotDir, lighting.dirWS) * spotAngle.x + spotAngle.y));
    float attenuation = distanceAttenuation * rcp(distance2) * spotAttenuation;
    lighting.color *= attenuation;
    return lighting;
}

uint GridPos2GridIndex(uint3 pos)
{
    uint gridIndex = pos.x +
    pos.y * _LightingCluster_Data.x +
    pos.z * _LightingCluster_Data.x * _LightingCluster_Data.y;
    return gridIndex;
}

float PosSS2GridIndex(float3 posNDC)
{   
    float zView = UNITY_MATRIX_P._m23 / (UNITY_MATRIX_P._m32 * posNDC.z - UNITY_MATRIX_P._m22);
    uint zTile = uint(max(log(-zView) * _LightingCluster_Data.z + _LightingCluster_Data.w, 0.0));
    uint2 xyTiles = uint2(posNDC.xy * _LightingCluster_Data.xy);
    
    return GridPos2GridIndex(uint3(xyTiles, zTile));
}



void ZIndex2ZPlane(uint slice, uint numSlices, float zNear, float zFar, out float clusterNear, out float clusterFar)
{
    clusterNear = -zNear * pow(zFar / zNear, slice / float(numSlices));
    clusterFar = -zNear * pow(zFar / zNear, (slice + 1) / float(numSlices));
}

uint Depth2Slice(float depth, float scale, float bias)
{
    return log2(depth) * scale - bias;

}

float AABBCollision(AABB light, AABB grid)
{
    bool x = light.minPoint.x <= grid.maxPoint.x && light.maxPoint.x >= grid.minPoint.x;
    bool y = light.minPoint.y <= grid.maxPoint.y && light.maxPoint.y >= grid.minPoint.y;
    bool z = light.minPoint.z >= grid.maxPoint.z && light.maxPoint.z <= grid.minPoint.z;

    return x&&y&&z;
}

bool LightGridIntersection(uint lightIndex, uint gridIndex)
{
    float4 min = _LightingCluster_LightList[lightIndex].minPoint;
    float4 max = _LightingCluster_LightList[lightIndex].maxPoint;
    AABB light;
    light.minPoint = min;
    light.maxPoint = max;
    AABB grid = _LightingCluster_Grid_RW[gridIndex];

    return AABBCollision(light, grid);

}

float3 LineIntersectionToZPlane(float3 A, float3 B, float ZDistance)
{
    float3 normal = float3(0.0, 0.0, 1.0);

    float3 ab = B - A;

    //Computing the intersection length for the line and the plane
    float t = (ZDistance - dot(normal, A)) / dot(normal, ab);

    //Computing the actual xyz position of the point along the line
    float3 result = A + t * ab;
        
    return result;
}




#endif
