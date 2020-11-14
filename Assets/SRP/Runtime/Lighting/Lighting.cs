using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace NinoxSRP
{
    public abstract partial class Lighting
    {
        public struct LightingRequest
        {

        }

        protected Lighting()
        {

        }

        public static Lighting GetLighting(Settings setting)
        {
            if (setting.ClusterLighting)
            {
                return new LightingCluster();
            }
            else
            {
                return new LightingForward();
            }
        }

        [Serializable]
        public class Settings
        {
            public bool ClusterLighting = true;
            public Shadows.Settings ShadowSetting = new Shadows.Settings();
        }

        public abstract void Init(ScriptableRenderContext context, Camera camera);
        public abstract void Execute(ScriptableRenderContext context, Camera camera, ref CullingResults cullingResults);
        public abstract void Dispose();
        protected static int GetMainLightIndex(NativeArray<VisibleLight> visibleLights)
        {
            int totalVisibleLights = visibleLights.Length;

            if (totalVisibleLights == 0)
                return -1;

            Light sunLight = RenderSettings.sun;
            int brightestDirectionalLightIndex = -1;
            float brightestLightIntensity = 0.0f;
            for (int i = 0; i < totalVisibleLights; ++i)
            {
                VisibleLight currVisibleLight = visibleLights[i];
                Light currLight = currVisibleLight.light;

                // Particle system lights have the light property as null. We sort lights so all particles lights
                // come last. Therefore, if first light is particle light then all lights are particle lights.
                // In this case we either have no main light or already found it.
                if (currLight == null)
                    break;

                if (currLight == sunLight)
                    return i;

                // In case no shadow light is present we will return the brightest directional light
                if (currVisibleLight.lightType == LightType.Directional && currLight.intensity > brightestLightIntensity)
                {
                    brightestLightIntensity = currLight.intensity;
                    brightestDirectionalLightIndex = i;
                }
            }

            return brightestDirectionalLightIndex;
        }
    }

}