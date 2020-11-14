//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef LIGHTINGCLUSTER_CS_HLSL
#define LIGHTINGCLUSTER_CS_HLSL
//
// NinoxSRP.LightingCluster+Definitions:  static fields
//
#define MAX_ADDITIONAL_LIGHTS_COUNT (255)
#define CLUSTER_GRID_BUILD_NUMTHREADS_X (8)
#define CLUSTER_GRID_BUILD_NUMTHREADS_Y (4)
#define LIGHTING_CLUSTER_Z_SLICE (16)

// Generated from NinoxSRP.LightingCluster+LightingData
// PackingRules = Exact
    real4 _MainDirLightColor;
    real4 _MainDirLightPosition;
    uint _AdditionalLightsCount;

// Generated from NinoxSRP.LightingCluster+AdditionalLightData
// PackingRules = Exact
struct AdditionalLightData
{
    real3 PosWS;
    real AttenuationCoef;
    float3 Color;
    real3 SpotDir;
    real2 SpotAngle;
    real4 minPoint;
    real4 maxPoint;
};

// Generated from NinoxSRP.LightingCluster+AABB
// PackingRules = Exact
struct AABB
{
    float4 minPoint;
    float4 maxPoint;
};

// Generated from NinoxSRP.LightingCluster+ClusterData
// PackingRules = Exact
    float _LightingCluster_ZFar;
    float _LightingCluster_ZNear;
    float4 _LightingCluster_Data;


#endif
