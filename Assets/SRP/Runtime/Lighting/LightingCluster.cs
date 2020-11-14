using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
using System;
using UnityEditor;
using UnityEngine.Rendering.UI;

namespace NinoxSRP
{
    public partial class LightingCluster : Lighting
    {
        #region HLSL
        [GenerateHLSL(needAccessors = false, omitStructDeclaration = true)]
        public struct Definitions
        {
            public const int MaxAdditionalLightsCount = 255;
            public const int CLUSTER_GRID_BUILD_NUMTHREADS_X = 8;
            public const int CLUSTER_GRID_BUILD_NUMTHREADS_Y = 4;
            public const int LIGHTING_CLUSTER_Z_SLICE = 16;
        }

        [GenerateHLSL(needAccessors = false, omitStructDeclaration = true)]
        public unsafe struct LightingData
        {
            [SurfaceDataAttributes(precision = FieldPrecision.Real)]
            public Vector4 _MainDirLightColor;
            [SurfaceDataAttributes(precision = FieldPrecision.Real)]
            public Vector4 _MainDirLightPosition;

            public uint _AdditionalLightsCount;
        }

        [GenerateHLSL(needAccessors = false)]
        public struct AdditionalLightData
        {
            [SurfaceDataAttributes(precision = FieldPrecision.Real)]
            public Vector3 PosWS;
            [SurfaceDataAttributes(precision = FieldPrecision.Real)]
            public float AttenuationCoef;
            [SurfaceDataAttributes(sRGBDisplay = true)]
            public Vector3 Color;
            //public float Range;
            [SurfaceDataAttributes(precision = FieldPrecision.Real)]
            public Vector3 SpotDir;
            [SurfaceDataAttributes(precision = FieldPrecision.Real)]
            public Vector2 SpotAngle;
            [SurfaceDataAttributes(precision = FieldPrecision.Real)]
            public Vector4 minPoint;
            [SurfaceDataAttributes(precision = FieldPrecision.Real)]
            public Vector4 maxPoint;
        }

        [GenerateHLSL(needAccessors = false)]
        public struct AABB
        {
            //XY in screen space,Z in view space and forward is negative.
            [SurfaceDataAttributes(precision = FieldPrecision.Default)]
            public Vector4 minPoint;//bottom left near
            [SurfaceDataAttributes(precision = FieldPrecision.Default)]
            public Vector4 maxPoint;//top right far
        };

        [GenerateHLSL(needAccessors = false, omitStructDeclaration = true)]
        public unsafe struct ClusterData
        {
            [SurfaceDataAttributes(precision = FieldPrecision.Default)]
            public float _LightingCluster_ZFar;
            [SurfaceDataAttributes(precision = FieldPrecision.Default)]
            public float _LightingCluster_ZNear;
            [SurfaceDataAttributes(precision = FieldPrecision.Default)]
            public Vector4 _LightingCluster_Data;//x:Cluster size x y:Cluster size y z:_Cluster_Scale w:_Cluster_Bias 
        }

        private class ShaderPropIDs
        {
            public static readonly int _MainDirLightColorID = Shader.PropertyToID("_MainDirLightColor");
            public static readonly int _MainDirLightPositionID = Shader.PropertyToID("_MainDirLightPosition");
            public static readonly int _AdditionalLightsCountID = Shader.PropertyToID("_AdditionalLightsCount");
            public static readonly int _ZFarID = Shader.PropertyToID("_LightingCluster_ZFar");
            public static readonly int _ZNearID = Shader.PropertyToID("_LightingCluster_ZNear");
            public static readonly int _DataID = Shader.PropertyToID("_LightingCluster_Data");

            public static readonly int _LightListID = Shader.PropertyToID("_LightingCluster_LightList");
            public static readonly int _LightCountID = Shader.PropertyToID("_LightingCluster_LightCount");

            public static readonly int _GridRWID = Shader.PropertyToID("_LightingCluster_Grid_RW");
            public static readonly int _LightIndexRWID = Shader.PropertyToID("_LightingCluster_LightIndex_RW");
            public static readonly int _LightIndexID = Shader.PropertyToID("_LightingCluster_LightIndex");
            public static readonly int _GridLightRWID = Shader.PropertyToID("_LightingCluster_GridIndex_RW");
            public static readonly int _GridLightID = Shader.PropertyToID("_LightingCluster_GridIndex");
        }
        #endregion

        private CommandBuffer buffer;
        public LightingCluster()
        {
            if (!SystemInfo.supportsComputeShaders)
            {
                throw new PlatformNotSupportedException("Compute Shader Not Supported.");
            }
            //this.gridXY = gridXY;

        }

        //[Reload("Runtime/Lighting/LightingCluster.compute")]
        public ComputeShader LightingClusterBuildCS => GraphicProfile.CurrentProfile.cs;

        Vector2Int gridXY;
        private int clusterGridBuildKernel;
        private int gridLightBuildKernel;
        private AdditionalLightData[] lightListArray;
        private ComputeBuffer lightListBuffer;
        private ComputeBuffer gridBuffer;
        private ComputeBuffer lightIndexBuffer;
        private ComputeBuffer gridLightIndexBuffer;

        private ScriptableRenderContext context;
        private Camera camera;

        void test(ScriptableRenderContext context)
        {
            var kernel = LightingClusterBuildCS.FindKernel("CSMain");
            buffer.SetComputeBufferParam(LightingClusterBuildCS, kernel, "result", lightIndexBuffer);
            buffer.DispatchCompute(LightingClusterBuildCS, kernel, 1, 1, 1);
            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();
        }

        int count = 0;
        public override void Execute(ScriptableRenderContext context, Camera camera, ref CullingResults cullingResults)
        {
            this.context = context;
            this.camera = camera;
            count++;
            if (count == 30)
            {
                //test(context);
            }
            if (count == 60)
            {
                count = 0;
                //BuildGrid(context,camera);
            }
            //BuildGrid(context,camera);
            BuildLightList(cullingResults.visibleLights, camera);
            BuildGridLight();
            BindShaderConstant(context, camera);
            //var a = GetData<AABB>(gridBuffer);
            //foreach (var i in a)
            //{
            //    if (i.minPoint.z < -2)
            //    {
            //        Debug.Log(i.minPoint);
            //    }
            //}
        }



        Vector3Int clusterSize = new Vector3Int(Definitions.CLUSTER_GRID_BUILD_NUMTHREADS_X, Definitions.CLUSTER_GRID_BUILD_NUMTHREADS_Y, Definitions.LIGHTING_CLUSTER_Z_SLICE);
        private float zFar;
        private float zNear;
        private Vector4 clusterData;
        public override unsafe void Init(ScriptableRenderContext context, Camera camera)
        {
            buffer = new CommandBuffer
            {
                name = "ClusterLighting"
            };
            clusterGridBuildKernel = LightingClusterBuildCS.FindKernel("ClusterGridBuild");
            gridLightBuildKernel = LightingClusterBuildCS.FindKernel("GridLightBuild");

            int total = clusterSize.x * clusterSize.y * clusterSize.z;

            lightListArray = new AdditionalLightData[Definitions.MaxAdditionalLightsCount];

            gridBuffer = new ComputeBuffer(total, sizeof(AABB));
            lightListBuffer = new ComputeBuffer(Definitions.MaxAdditionalLightsCount, sizeof(AdditionalLightData));
            lightIndexBuffer = new ComputeBuffer(total * Definitions.MaxAdditionalLightsCount, sizeof(uint));
            gridLightIndexBuffer = new ComputeBuffer(total, sizeof(uint));

            zFar = camera.farClipPlane;
            zNear = camera.nearClipPlane;
            zFar = 1000f;
            zNear = 0.3f;
            var scale = clusterSize.z / Mathf.Log(zFar / zNear);
            var bias = -clusterSize.z * Mathf.Log(zNear) / Mathf.Log(zFar / zNear);
            clusterData = new Vector4(clusterSize.x, clusterSize.y, scale, bias);

            BuildGrid(context, camera);
        }


        void BindShaderConstant(ScriptableRenderContext context, Camera camera)
        {
            buffer.EnableShaderKeyword(ShaderKeyword.UseClusterLightlist);
            buffer.SetGlobalVector(ShaderPropIDs._DataID, clusterData);
            buffer.SetGlobalBuffer(ShaderPropIDs._LightListID, lightListBuffer);
            buffer.SetGlobalBuffer(ShaderPropIDs._LightIndexID, lightIndexBuffer);
            buffer.SetGlobalBuffer(ShaderPropIDs._GridLightID, gridLightIndexBuffer);
            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();
        }

        #region LightList
        private void BuildLightList(NativeArray<VisibleLight> visibleLights, Camera camera)
        {
            var w2v = camera.worldToCameraMatrix;
            int mainIndex = GetMainLightIndex(visibleLights);
            if (mainIndex >= 0)
            {
                SetupMainLight(visibleLights[mainIndex]);
            }
            int count = 0;
            for (int i = 0; i < visibleLights.Length; i++)
            {
                if (i == mainIndex)
                {
                    continue;
                }
                if (count == Definitions.MaxAdditionalLightsCount)
                {
                    break;
                }
                VisibleLight visibleLight = visibleLights[i];

                switch (visibleLight.lightType)
                {
                    case LightType.Point:
                        {
                            SetupPointLight(count, ref visibleLight, w2v);
                            break;
                        }
                    case LightType.Spot:
                        {
                            SetupSpotLight(count, ref visibleLight, w2v);
                            break;
                        }
                    default: continue;
                }
                count++;
            }
            lightListBuffer.SetData(lightListArray, 0, 0, count);
            buffer.SetComputeIntParam(LightingClusterBuildCS, ShaderPropIDs._AdditionalLightsCountID, count);
            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();
        }

        void SetupSpotLight(int arrayIndex, ref VisibleLight visibleLight, Matrix4x4 worldToView)
        {
            var rect = visibleLight.screenRect;
            var light = visibleLight.light;
            Vector4 minPoint = new Vector4();
            Vector4 maxPoint = new Vector4();

            minPoint.x = rect.x;
            minPoint.y = rect.y;
            maxPoint.x = rect.x + rect.width;
            maxPoint.y = rect.y + rect.height;
            var posZ = Vector4.Dot(worldToView.GetRow(2), new Vector4(light.transform.position.x, light.transform.position.y, light.transform.position.z, 1.0f));

            var dir = light.transform.forward;
            var dirV = worldToView * new Vector4(dir.x, dir.y, dir.z, 0.0f);
            var zDir = Mathf.Sign(dirV.z);

            var costheta = Mathf.Max(0.0001f, zDir * dirV.z);
            var vertical = new Vector3(-dirV.x, -dirV.y, zDir / costheta - dirV.z);
            vertical.Normalize();
            var radius = Mathf.Tan(visibleLight.spotAngle / 2 * Mathf.Deg2Rad) * visibleLight.range;
            var deltaZ = vertical.z * radius * zDir;
            var dirVZ = dirV.z * visibleLight.range;

            var z1 = posZ + dirVZ - deltaZ;
            z1 = Mathf.Min(z1, posZ);
            maxPoint.z = z1;
            var z2 = posZ + dirVZ + deltaZ;
            z2 = Mathf.Max(z2, posZ);
            minPoint.z = z2;

            Vector4 position = visibleLight.localToWorldMatrix.GetColumn(3);

            lightListArray[arrayIndex].minPoint = minPoint;
            lightListArray[arrayIndex].maxPoint = maxPoint;
            lightListArray[arrayIndex].PosWS = position;
            lightListArray[arrayIndex].Color = (Vector4)visibleLight.finalColor;
            //distance attenuation:max(0,1-(distance2/range2)2)2
            //store 1/range2
            lightListArray[arrayIndex].AttenuationCoef = 1f / Mathf.Max(visibleLight.range * visibleLight.range, 0.0001f);

            float innerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * light.innerSpotAngle);
            float outerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * light.spotAngle);
            //prevent divide zero
            float angleRangeInv = 1f / Mathf.Max(innerCos - outerCos, 0.001f);
            lightListArray[arrayIndex].SpotAngle = new Vector4(
                angleRangeInv, -outerCos * angleRangeInv
            );
            lightListArray[arrayIndex].SpotDir = -visibleLight.localToWorldMatrix.GetColumn(2);

        }

        void SetupPointLight(int arrayIndex, ref VisibleLight visibleLight, Matrix4x4 worldToView)
        {
            var rect = visibleLight.screenRect;
            var light = visibleLight.light;
            Vector4 minPoint = new Vector4();
            Vector4 maxPoint = new Vector4();
            Vector4 position = visibleLight.localToWorldMatrix.GetColumn(3);
            minPoint.x = rect.x;
            minPoint.y = rect.y;
            maxPoint.x = rect.x + rect.width;
            maxPoint.y = rect.y + rect.height;
            var z = Vector4.Dot(worldToView.GetRow(2), new Vector4(light.transform.position.x, light.transform.position.y, light.transform.position.z, 1.0f));
            minPoint.z = z + visibleLight.range;
            maxPoint.z = z - visibleLight.range;

            lightListArray[arrayIndex].minPoint = minPoint;
            lightListArray[arrayIndex].maxPoint = maxPoint;
            lightListArray[arrayIndex].PosWS = position;
            //lightListArray[arrayIndex].Range = visibleLight.range;
            lightListArray[arrayIndex].Color = (Vector4)visibleLight.finalColor;
            //distance attenuation:max(0,1-(distance2/range2)2)2
            //store 1/range2
            lightListArray[arrayIndex].AttenuationCoef = 1f / Mathf.Max(visibleLight.range * visibleLight.range, 0.0001f);
            lightListArray[arrayIndex].SpotAngle = new Vector2(0f, 1f);
            lightListArray[arrayIndex].SpotDir = Vector4.zero;
            //Debug.Log(minPoint);

        }

        void SetupMainLight(VisibleLight light)
        {
            buffer.SetGlobalVector(ShaderPropIDs._MainDirLightColorID, light.light.color);
            Vector4 dir = -light.localToWorldMatrix.GetColumn(2);
            dir.w = 0.0f;
            buffer.SetGlobalVector(ShaderPropIDs._MainDirLightPositionID, dir);
            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();
        }
        #endregion

        private void BuildGridLight()
        {
            buffer.SetComputeFloatParam(LightingClusterBuildCS, ShaderPropIDs._ZFarID, zFar);
            buffer.SetComputeFloatParam(LightingClusterBuildCS, ShaderPropIDs._ZNearID, zNear);
            buffer.SetComputeBufferParam(LightingClusterBuildCS, gridLightBuildKernel, ShaderPropIDs._GridRWID, gridBuffer);
            buffer.SetComputeBufferParam(LightingClusterBuildCS, gridLightBuildKernel, ShaderPropIDs._LightListID, lightListBuffer);
            buffer.SetComputeBufferParam(LightingClusterBuildCS, gridLightBuildKernel, ShaderPropIDs._LightIndexRWID, lightIndexBuffer);
            buffer.SetComputeBufferParam(LightingClusterBuildCS, gridLightBuildKernel, ShaderPropIDs._GridLightRWID, gridLightIndexBuffer);
            buffer.BeginSample("LightGridBuild");
            //buffer.DispatchCompute(LightingClusterBuildCS, gridLightBuildKernel,
            //    clusterSize.x / Definitions.CLUSTER_GRID_BUILD_NUMTHREADS_X,
            //    clusterSize.y / Definitions.CLUSTER_GRID_BUILD_NUMTHREADS_Y,
            //    clusterSize.z / Definitions.CLUSTER_GRID_BUILD_NUMTHREADS_Z);
            buffer.DispatchCompute(LightingClusterBuildCS, gridLightBuildKernel,
                1,
                1,
                1);
            buffer.EndSample("LightGridBuild");
            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();
        }

        #region Grid
        private void BuildGrid(ScriptableRenderContext context, Camera camera)
        {
            buffer.SetComputeVectorParam(LightingClusterBuildCS, ShaderPropIDs._DataID, clusterData);
            buffer.SetComputeFloatParam(LightingClusterBuildCS, ShaderPropIDs._ZFarID, zFar);
            buffer.SetComputeFloatParam(LightingClusterBuildCS, ShaderPropIDs._ZNearID, zNear);
            buffer.SetComputeBufferParam(LightingClusterBuildCS, clusterGridBuildKernel, ShaderPropIDs._GridRWID, gridBuffer);
            //buffer.DispatchCompute(LightingClusterBuildCS,clusterGridBuildKernel, 
            //    clusterSize.x / Definitions.CLUSTER_GRID_BUILD_NUMTHREADS_X, 
            //    clusterSize.y / Definitions.CLUSTER_GRID_BUILD_NUMTHREADS_Y, 
            //    clusterSize.z / Definitions.CLUSTER_GRID_BUILD_NUMTHREADS_Z);
            buffer.DispatchCompute(LightingClusterBuildCS, clusterGridBuildKernel,
            1, 1, 1);
            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();
        }
        #endregion

        public override void Dispose()
        {
            //lightListBuffer.Release();
            //gridBuffer.Release();
            //lightIndexBuffer.Release();
            //gridLightIndexBuffer.Release();
            buffer.Release();
            Shader.DisableKeyword(ShaderKeyword.UseClusterLightlist);
        }

        unsafe T[] GetData<T>(ComputeBuffer buffer)
        {
            var count = buffer.count;
            var data = new T[count];
            buffer.GetData(data);
            return data;
        }
    }

}