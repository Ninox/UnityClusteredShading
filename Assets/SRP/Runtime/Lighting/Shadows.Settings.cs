using System;
using UnityEngine;

namespace NinoxSRP
{
    public partial class Lighting
    {
        public partial class Shadows
        {
            public enum CascadeMap
            {
                NoCascades = 1,
                TwoCascades = 2,
                FourCascades = 4
            }
            public enum TextureSize
            {
                _256 = 256, _512 = 512, _1024 = 1024, _2048 = 2048, _4096 = 4096, _8192 = 8192
            }
            public enum SoftShadowSample
            {
                PCF3 = 1,
                PCF5 = 2,
                PCF7 = 3
            }

            [Serializable]
            public class Settings
            {
                public bool SoftShadow = true;
                public bool ScreenSpaceShadowMap = false;
                [Range(0.01f, 2f)]
                public float NormalBias = 0.1f;
                [Range(0.01f, 2f)]
                public float SlopeBias = 0.1f;
                public DirectionalLight mainLightsShadowSetting = new DirectionalLight();
                public PunctualLight punctualsShadowSetting = new PunctualLight();

                [Serializable]
                public class DirectionalLight
                {
                    public float MaxDistance = 50f;
                    public TextureSize Resolution = TextureSize._2048;
                    public CascadeMap Cascades = CascadeMap.FourCascades;
                    [Range(0.0f, 1.0f)]
                    public float SplitRatio1 = 0.3f;
                    [Range(0.0f, 1.0f)]
                    public float SplitRatio2 = 0.4f;
                    [Range(0.0f, 1.0f)]
                    public float SplitRatio3 = 0.5f;
                    public float CascdeFading = 0.1f;
                }

                [Serializable]
                public class PunctualLight
                {
                    public TextureSize Resolution = TextureSize._2048;
                }


            }
        }
    }
}
