using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace NinoxSRP
{
    public partial class LightingForward : Lighting
    {
        [GenerateHLSL]
        public class Definitions
        {
            public const int MaxAdditionalLightsCount = 32;
        }

        [GenerateHLSL(needAccessors = false, omitStructDeclaration = true)]
        public unsafe struct LightData
        {
            [SurfaceDataAttributes(precision = FieldPrecision.Real)]
            public Vector4 _MainDirLightColor;
            [SurfaceDataAttributes(precision = FieldPrecision.Real)]
            public Vector4 _MainDirLightPosition;

            public uint _AdditionalLightsCount;
            [SurfaceDataAttributes(precision = FieldPrecision.Real)]
            [HLSLArray(Definitions.MaxAdditionalLightsCount, typeof(Vector4))]
            public fixed float _AdditionalLightsPositions[Definitions.MaxAdditionalLightsCount];
            [SurfaceDataAttributes(precision = FieldPrecision.Real)]
            [HLSLArray(Definitions.MaxAdditionalLightsCount, typeof(Vector4))]
            public fixed float _AdditionalLightsColors[Definitions.MaxAdditionalLightsCount];
            [SurfaceDataAttributes(precision = FieldPrecision.Real)]
            [HLSLArray(Definitions.MaxAdditionalLightsCount, typeof(Vector4))]
            public fixed float _AdditionalLightsSpotDirs[Definitions.MaxAdditionalLightsCount];
            [SurfaceDataAttributes(precision = FieldPrecision.Real)]
            [HLSLArray(Definitions.MaxAdditionalLightsCount, typeof(Vector4))]
            public fixed float _AdditionalLightsSpotAngles[Definitions.MaxAdditionalLightsCount];
        }

        public class ShaderPropIDs
        {
            public static readonly int _MainDirLightColorID = Shader.PropertyToID("_MainDirLightColor");
            public static readonly int _MainDirLightPositionID = Shader.PropertyToID("_MainDirLightPosition");
            public static readonly int _AdditionalLightsCountID = Shader.PropertyToID("_AdditionalLightsCount");
            public static readonly int _AdditionalLightsPositionsID = Shader.PropertyToID("_AdditionalLightsPositions");
            public static readonly int _AdditionalLightsColorsID = Shader.PropertyToID("_AdditionalLightsColors");
            public static readonly int _AdditionalLightsSpotDirsID = Shader.PropertyToID("_AdditionalLightsSpotDirs");
            public static readonly int _AdditionalLightsSpotAnglesID = Shader.PropertyToID("_AdditionalLightsSpotAngles");
        }


        protected CommandBuffer buffer;

        private int additionalLightsCount = 0;
        private Vector4[] additionalLightsPositions;
        private Vector4[] additionalLightsColors;
        private Vector4[] additionalLightsSpotAngles;
        private Vector4[] additionalLightsSpotDirs;
        private Vector4[] additionalLightsOcclusionProbes;

        public LightingForward()
        {
            buffer = new CommandBuffer()
            {
                name = "Lighting"
            };
            additionalLightsPositions = new Vector4[Definitions.MaxAdditionalLightsCount];
            additionalLightsColors = new Vector4[Definitions.MaxAdditionalLightsCount];
            additionalLightsSpotAngles = new Vector4[Definitions.MaxAdditionalLightsCount];
            additionalLightsSpotDirs = new Vector4[Definitions.MaxAdditionalLightsCount];
            additionalLightsOcclusionProbes = new Vector4[Definitions.MaxAdditionalLightsCount];

            shadow = new Shadows();
        }

        private Shadows shadow;

        public override void Execute(ScriptableRenderContext context, Camera camera, ref CullingResults cullingResults)
        {
            shadow.Init(context, camera, cullingResults);
            var visibleLights = cullingResults.visibleLights;
            int mainLightIndex = GetMainLightIndex(visibleLights);
            if (mainLightIndex >= 0)
            {
                var mainVisibleLight = visibleLights[mainLightIndex];
                SetupMainLight(mainVisibleLight);
                shadow.DrawMainDirShadow(mainLightIndex);
            }

            additionalLightsCount = 0;
            for (int i = 0; i < visibleLights.Length; i++)
            {
                if (additionalLightsCount == Definitions.MaxAdditionalLightsCount)
                {
                    break;
                }
                if (i == mainLightIndex)
                {
                    continue;
                }
                var visibleLight = visibleLights[i];

                if(visibleLight.light.lightmapBakeType == LightmapBakeType.Baked)
                {
                    continue;
                }
                switch (visibleLight.lightType)
                {
                    //case LightType.Directional:
                    //    {
                    //        //SetupDirectionalLight(additionalLightsCount, ref visibleLight);
                    //    }
                    //    break;
                    case LightType.Point:
                        {
                            SetupPointLight(additionalLightsCount, ref visibleLight);
                            if (visibleLight.light.shadows != LightShadows.None)
                            {
                                //shadow.DrawPointLightShadow(i);;
                            }
                        }
                        break;
                    case LightType.Spot:
                        {
                            SetupSpotLight(additionalLightsCount, ref visibleLight);
                            if (visibleLight.light.shadows != LightShadows.None)
                            {
                                //shadow.DrawSpotLightShadow(i);
                            }
                        }
                        break;
                    default: continue;
                }
                additionalLightsCount++;
            }
            Submit();
            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();
        }

        void Submit()
        {
            buffer.SetGlobalInt(ShaderPropIDs._AdditionalLightsCountID, additionalLightsCount);
            buffer.SetGlobalVectorArray(ShaderPropIDs._AdditionalLightsPositionsID, additionalLightsPositions);
            buffer.SetGlobalVectorArray(ShaderPropIDs._AdditionalLightsColorsID, additionalLightsColors);
            buffer.SetGlobalVectorArray(ShaderPropIDs._AdditionalLightsSpotAnglesID, additionalLightsSpotAngles);
            buffer.SetGlobalVectorArray(ShaderPropIDs._AdditionalLightsSpotDirsID, additionalLightsSpotDirs);
        }

        void SetupMainLight(VisibleLight light)
        {
            buffer.SetGlobalVector(ShaderPropIDs._MainDirLightColorID, light.light.color);
            Vector4 dir = -light.localToWorldMatrix.GetColumn(2);
            dir.w = 0.0f;
            buffer.SetGlobalVector(ShaderPropIDs._MainDirLightPositionID, dir);
        }

        void SetupSpotLight(int arrayIndex, ref VisibleLight visibleLight)
        {
            var light = visibleLight.light;

            Vector4 position = visibleLight.localToWorldMatrix.GetColumn(3);
            position.w = 1f / Mathf.Max(visibleLight.range * visibleLight.range, 0.00001f);
            additionalLightsPositions[arrayIndex] = position;
            additionalLightsColors[arrayIndex] = visibleLight.finalColor;
            float innerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * light.innerSpotAngle);
            float outerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * light.spotAngle);
            //prevent divide zero
            float angleRangeInv = 1f / Mathf.Max(innerCos - outerCos, 0.001f);
            additionalLightsSpotAngles[arrayIndex] = new Vector4(
                angleRangeInv, -outerCos * angleRangeInv
            );
            additionalLightsSpotDirs[arrayIndex] = -visibleLight.localToWorldMatrix.GetColumn(2);
        }

        void SetupPointLight(int arrayIndex, ref VisibleLight visibleLight)
        {
            Vector4 position = visibleLight.localToWorldMatrix.GetColumn(3);
            //distance attenuation:max(0,1-(distance2/range2)2)2
            //store 1/range2
            position.w = 1f / Mathf.Max(visibleLight.range * visibleLight.range, 0.0001f);
            additionalLightsPositions[arrayIndex] = position;
            additionalLightsColors[arrayIndex] = visibleLight.finalColor;
            additionalLightsSpotAngles[arrayIndex] = new Vector4(0f, 1f);
        }

        public override void Dispose()
        {
            buffer.Release();
            shadow.Dispose();
        }

        public override void Init(ScriptableRenderContext context, Camera camera)
        {
            
        }
    }
}
