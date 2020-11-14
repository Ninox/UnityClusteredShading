using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace NinoxSRP
{
    public partial class Lighting
    {
        public partial class Shadows : IDisposable
        {
            [GenerateHLSL(PackingRules.Exact, false)]
            public struct Definitions
            {
                public const int MaxCascade = 4;
            }

            [GenerateHLSL(PackingRules.Exact, false)]
            public struct MainShadowData
            {

            }

            [GenerateHLSL(PackingRules.Exact, false)]
            public struct AdditionalShadowData
            {


            }

            Shadows Instance = null;

            void Init(Settings settings)
            {

            }

            //void Render(ShadowMapRequest)
            //{

            //}

            //public struct ShadowParameter
            //{
            //    public Matrix4x4 view;
            //    // Use the y flipped device projection matrix as light projection matrix
            //    public Matrix4x4 shadowToWorld;
            //    public Vector3 position;
            //    public Vector4 zBufferParam;
            //    // Warning: this field is updated by ProcessShadowRequests and is invalid before
            //    public Rect atlasViewport;
            //    public bool zClip;
            //    public Vector4[] frustumPlanes;

            //    // Store the final shadow indice in the shadow data array
            //    // Warning: the index is computed during ProcessShadowRequest and so is invalid before calling this function
            //    public int shadowIndex;

            //    // Determine in which atlas the shadow will be rendered
            //    public ShadowMapType shadowMapType = ShadowMapType.PunctualAtlas;

            //    // TODO: Remove these field once scriptable culling is here (currently required by ScriptableRenderContext.DrawShadows)
            //    public int lightIndex;
            //    public ShadowSplitData splitData;
            //    // end

            //    public float normalBias;
            //    public float worldTexelSize;

            //    // PCSS parameters
            //    public float shadowSoftness;
            //    public int blockerSampleCount;
            //    public int filterSampleCount;
            //    public float minFilterSize;

            //    // IMS parameters
            //    public float kernelSize;
            //    public float lightAngle;
            //    public float maxDepthBias;

            //    public Vector4 evsmParams;

            //    public bool shouldUseCachedShadow = false;
            //    public HDShadowData cachedShadowData;
            //}


            //public struct ShadowMapRequest
            //{
            //    public Matrix4x4 view;
            //    // Use the y flipped device projection matrix as light projection matrix
            //    public Matrix4x4 shadowToWorld;
            //    public Vector3 position;
            //    public Vector4 zBufferParam;
            //    // Warning: this field is updated by ProcessShadowRequests and is invalid before
            //    public Rect atlasViewport;
            //    public bool zClip;
            //    public Vector4[] frustumPlanes;

            //    public ShadowMapType shadowMapType = ShadowMapType.PunctualAtlas;

            //    // TODO: Remove these field once scriptable culling is here (currently required by ScriptableRenderContext.DrawShadows)
            //    public int lightIndex;
            //    public ShadowSplitData splitData;
            //    // end

            //    public float normalBias;
            //    public float worldTexelSize;

            //    // PCSS parameters
            //    public float shadowSoftness;
            //    public int blockerSampleCount;
            //    public int filterSampleCount;
            //    public float minFilterSize;

            //    // IMS parameters
            //    public float kernelSize;
            //    public float lightAngle;
            //    public float maxDepthBias;

            //    public Vector4 evsmParams;

            //    public bool shouldUseCachedShadow = false;
            //    public HDShadowData cachedShadowData;
            //}

            public Settings settings => GraphicProfile.CurrentProfile.lightingSetting.ShadowSetting;

            private string passName = "ShadowMap";

            Vector4[] cascadeCullingSphere = new Vector4[Definitions.MaxCascade];
            Matrix4x4[] MainShadowVPMatrixArray = new Matrix4x4[Definitions.MaxCascade];

            public Shadows()
            {
                buffer = new CommandBuffer()
                {
                    name = passName,
                };
                int mainRes = (int)settings.mainLightsShadowSetting.Resolution;
                RenderTextureDescriptor mainDesc = new RenderTextureDescriptor(mainRes, mainRes, RenderTextureFormat.Shadowmap, 32);
                mainDesc.dimension = TextureDimension.Tex2D;
                mainDesc.shadowSamplingMode = ShadowSamplingMode.RawDepth;
                mainShadowAtlas = RTHandles.Alloc(mainRes, mainRes, filterMode: FilterMode.Bilinear, depthBufferBits: DepthBits.Depth16, isShadowMap: true, name: "MainShadowAtlas");
                int addRes = (int)settings.punctualsShadowSetting.Resolution;
                RenderTextureDescriptor addDesc = new RenderTextureDescriptor(addRes, addRes, RenderTextureFormat.Shadowmap, 32);
                addDesc.dimension = TextureDimension.Tex2D;
                addDesc.shadowSamplingMode = ShadowSamplingMode.RawDepth;
                //additionalShadowAtlas = new RenderTexture(addDesc)
                //{
                //    name = "additionalShadowAtlas",
                //};
                //additionalShadowAtlas.Create();
                //additionalShadowVPMatrixArray = new Matrix4x4[ForwardLighting.Definitions.MaxAdditionalLightsCount];
            }

            RTHandle mainShadowAtlas;
            RenderTexture additionalShadowAtlas;

            CommandBuffer buffer;

            public void Init(ScriptableRenderContext context, Camera camera, CullingResults cullingResults)
            {
                this.context = context;
                this.camera = camera;
                this.cullingResults = cullingResults;

            }

            private ScriptableRenderContext context;
            private Camera camera;
            private CullingResults cullingResults;
            private bool disposedValue;
            Matrix4x4[] additionalShadowVPMatrixArray;

            public void DrawMainDirShadow(int lightIndex)
            {
                var light = cullingResults.visibleLights[lightIndex].light;
                if (light.shadows == LightShadows.None)
                {
                    return;
                }

                if (!cullingResults.GetShadowCasterBounds(lightIndex, out _))
                {
                    Debug.Log("return");
                    return;
                }
                buffer.SetGlobalTexture(ShaderPropIDs.MainShadowAtlas, mainShadowAtlas);
                buffer.SetRenderTarget(mainShadowAtlas, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                buffer.ClearRenderTarget(true, false, Color.clear);
                context.ExecuteCommandBuffer(buffer);
                buffer.Clear();


                Vector3 splitRatio = new Vector3(settings.mainLightsShadowSetting.SplitRatio1, settings.mainLightsShadowSetting.SplitRatio2, settings.mainLightsShadowSetting.SplitRatio3);

                int split;
                switch (settings.mainLightsShadowSetting.Cascades)
                {
                    case CascadeMap.TwoCascades:
                        {
                            split = 2;
                            //splitRatio.y = 1.0f;
                            break;
                        }
                    case CascadeMap.FourCascades:
                        {
                            split = 2;
                            break;
                        }
                    default:
                        {
                            split = 1;
                            splitRatio.x = 1.0f;
                            break;
                        }
                }
                int cascadedCount = (int)settings.mainLightsShadowSetting.Cascades;
                int tileSize = Mathf.FloorToInt((int)settings.mainLightsShadowSetting.Resolution / split);
                for (int i = 0; i < cascadedCount; i++)
                {
                    if (!cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(lightIndex, i, 4, splitRatio, (int)settings.mainLightsShadowSetting.Resolution, light.shadowNearPlane, out var viewMatrix, out var projMatrix, out var shadowSplitData))
                    {
                        Debug.Log("CascadeShadowCullingError");
                    }

                    buffer.SetViewProjectionMatrices(viewMatrix, projMatrix);
                    ShadowDrawingSettings shadowDrawingSettings = new ShadowDrawingSettings(cullingResults, lightIndex)
                    {
                        splitData = shadowSplitData
                    };
                    var rect = GetTileViewport(i, split, tileSize, out var offset);
                    buffer.SetViewport(rect);
                    buffer.SetGlobalDepthBias(1f, settings.SlopeBias);
                    context.ExecuteCommandBuffer(buffer);
                    context.DrawShadows(ref shadowDrawingSettings);
                    buffer.Clear();

                    var cullingSphere = shadowSplitData.cullingSphere;
                    cullingSphere.w = Mathf.Pow(cullingSphere.w, 2);
                    cascadeCullingSphere[i] = cullingSphere;
                    MainShadowVPMatrixArray[i] = ConvertToAtlasMatrix(projMatrix * viewMatrix, offset, split);
                }
                buffer.SetGlobalInt(ShaderPropIDs.CascadeCount, cascadedCount);
                float f = 1f - settings.mainLightsShadowSetting.CascdeFading;
                buffer.SetGlobalVector(ShaderPropIDs.CascadeDistanceFade, new Vector4(1f / settings.mainLightsShadowSetting.MaxDistance,
                    1f / settings.mainLightsShadowSetting.CascdeFading,
                1f / (1f - f * f)));
                buffer.SetGlobalVectorArray(ShaderPropIDs.CascadeCullingSphereArray, cascadeCullingSphere);
                buffer.SetGlobalMatrixArray(ShaderPropIDs.MainShadowVPMatrixArray, MainShadowVPMatrixArray);
                buffer.SetGlobalFloat(ShaderPropIDs.NormalBias, settings.NormalBias);
                context.ExecuteCommandBuffer(buffer);
                buffer.Clear();
            }

            public void DrawSpotLightShadow(int lightIndex)
            {
                //buffer.SetRenderTarget(additionalShadowAtlas);
                //buffer.ClearRenderTarget(true, false, Color.clear);

                //VisibleLight visibleLight = cullingResults.visibleLights[lightIndex];
                //if (!cullingResults.ComputeSpotShadowMatricesAndCullingPrimitives(lightIndex, out var viewMatrix, out var projMatrix, out var shadowSplitData))
                //{
                //    Debug.Log("SpotShadowCullingError");
                //}

                //buffer.SetViewProjectionMatrices(viewMatrix, projMatrix);
                //ShadowDrawingSettings settings = new ShadowDrawingSettings(cullingResults, lightIndex)
                //{
                //    splitData = shadowSplitData
                //};
                //var rect = GetTileViewport(i, split, tileSize, out var offset);
                //buffer.SetViewport(rect);
                //buffer.SetGlobalDepthBias(0f, 3f);
                //context.ExecuteCommandBuffer(buffer);
                //context.DrawShadows(ref settings);
                //buffer.Clear();

                //var cullingSphere = shadowSplitData.cullingSphere;
                //cullingSphere.w = Mathf.Pow(cullingSphere.w, 2);
                //additionalShadowVPMatrixArray[i] = ConvertToAtlasMatrix(projMatrix * viewMatrix, offset, split);

                //buffer.SetGlobalVectorArray(cascadeCullingSphereArrayId, cascadeCullingSphere);
                //buffer.SetGlobalMatrixArray(cascadeVPMatrixArrayId, cascadeVPMatrixArray);
                //buffer.SetGlobalDepthBias(0f, 0f);
                //context.ExecuteCommandBuffer(buffer);
                //buffer.Clear();
            }

            public void DrawPointLightShadow(int lightIndex)
            {
                //VisibleLight visibleLight= cullingResults.visibleLights[lightIndex];
                //for (int i = 0; i < 6; i++)
                //{
                //    if (!cullingResults.ComputePointShadowMatricesAndCullingPrimitives(lightIndex, (CubemapFace)i,0, out var viewMatrix, out var projMatrix, out var shadowSplitData))
                //    {
                //        Debug.Log("PointShadowCullingError");
                //    }

                //    cascadeBuffer.SetViewProjectionMatrices(viewMatrix, projMatrix);
                //    ShadowDrawingSettings settings = new ShadowDrawingSettings(cullingResults, lightIndex)
                //    {
                //        splitData = shadowSplitData
                //    };
                //    var rect = GetTileViewport(i, split, tileSize, out var offset);
                //    cascadeBuffer.SetViewport(rect); 
                //    cascadeBuffer.SetGlobalDepthBias(0f, 3f);
                //    context.ExecuteCommandBuffer(cascadeBuffer);
                //    context.DrawShadows(ref settings);
                //    cascadeBuffer.Clear();

                //    var cullingSphere = shadowSplitData.cullingSphere;
                //    cullingSphere.w = Mathf.Pow(cullingSphere.w, 2);
                //    cascadeCullingSphere[i] = cullingSphere;
                //    cascadeVPMatrixArray[i] = ConvertToAtlasMatrix(projMatrix * viewMatrix, offset, split);
                //}
                //float f = 1f - settings.CascdeFading;
                //buffer.SetGlobalVector(cascadeDistanceFadeId, new Vector4(1f / settings.MaxDistance,
                //    1f / settings.CascdeFading,
                //1f / (1f - f * f)));
                //buffer.SetGlobalVectorArray(cascadeCullingSphereArrayId, cascadeCullingSphere);
                //buffer.SetGlobalMatrixArray(cascadeVPMatrixArrayId, cascadeVPMatrixArray);
                //buffer.SetGlobalFloat(normalBiasId, settings.NormalBias);
                //buffer.SetGlobalDepthBias(0f, 0f);
                //context.ExecuteCommandBuffer(buffer);
                //buffer.Clear();
            }

            public void Clear()
            {
                buffer.ReleaseTemporaryRT(ShaderPropIDs.MainShadowAtlas);
                context.ExecuteCommandBuffer(buffer);
                buffer.Clear();
            }

            private Rect GetTileViewport(int spiltIndex, int split, int tileSize, out Vector2Int offset)
            {
                offset = new Vector2Int(spiltIndex % split, spiltIndex / split);
                return new Rect(
                    offset.x * tileSize, offset.y * tileSize, tileSize, tileSize
                );
            }

            Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 m, Vector2 offset, float split)
            {
                if (SystemInfo.usesReversedZBuffer)
                {
                    m.m20 = -m.m20;
                    m.m21 = -m.m21;
                    m.m22 = -m.m22;
                    m.m23 = -m.m23;
                }
                float scale = 1f / split;
                m.m00 = (0.5f * (m.m00 + m.m30) + offset.x * m.m30) * scale;
                m.m01 = (0.5f * (m.m01 + m.m31) + offset.x * m.m31) * scale;
                m.m02 = (0.5f * (m.m02 + m.m32) + offset.x * m.m32) * scale;
                m.m03 = (0.5f * (m.m03 + m.m33) + offset.x * m.m33) * scale;
                m.m10 = (0.5f * (m.m10 + m.m30) + offset.y * m.m30) * scale;
                m.m11 = (0.5f * (m.m11 + m.m31) + offset.y * m.m31) * scale;
                m.m12 = (0.5f * (m.m12 + m.m32) + offset.y * m.m32) * scale;
                m.m13 = (0.5f * (m.m13 + m.m33) + offset.y * m.m33) * scale;
                m.m20 = 0.5f * (m.m20 + m.m30);
                m.m21 = 0.5f * (m.m21 + m.m31);
                m.m22 = 0.5f * (m.m22 + m.m32);
                m.m23 = 0.5f * (m.m23 + m.m33);
                return m;
            }


            protected virtual void Dispose(bool disposing)
            {

                if (!disposedValue)
                {
                    if (disposing)
                    {
                        // TODO: 释放托管状态(托管对象)


                    }
                    mainShadowAtlas.Release();
                    buffer.Release();

                    disposedValue = true;
                }
            }

            public void Dispose()
            {

                // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }
        }
    }
}