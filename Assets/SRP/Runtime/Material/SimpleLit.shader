Shader "NinoxSRP/SimpleLit"
{
    Properties
    {
        [MainTexture] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [MainColor] _BaseMap("Base Map (RGB) Smoothness / Alpha (A)", 2D) = "white" {}

        _Cutoff("Alpha Clipping", Range(0.0, 1.0)) = 0.5

        _SpecColor("Specular Color", Color) = (0.5, 0.5, 0.5, 0.5)
        _SpecGlossMap("Specular Map", 2D) = "white" {}
        _Smoothness("Smoothness",Float)=16.0
        [Enum(Specular Alpha,0,Albedo Alpha,1)] _SmoothnessSource("Smoothness Source", Float) = 0.0
        [ToggleOff] _SpecularHighlights("Specular Highlights", Float) = 1.0

        [HideInInspector] _BumpScale("Scale", Float) = 1.0
        [NoScaleOffset] _BumpMap("Normal Map", 2D) = "bump" {}

        _EmissionColor("Emission Color", Color) = (0,0,0)
        [NoScaleOffset]_EmissionMap("Emission Map", 2D) = "white" {}

         //Blending state
        [HideInInspector] _Surface("__surface", Float) = 0.0
        [HideInInspector] _Blend("__blend", Float) = 0.0
        [HideInInspector] _AlphaClip("__clip", Float) = 0.0
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
        [HideInInspector] _ZWrite("__zw", Float) = 1.0
        [HideInInspector] _Cull("__cull", Float) = 2.0

        [MaterialToggle] _ReceiveShadows("Receive Shadows", Float) = 1.0

         //Editmode props
        [HideInInspector] _QueueOffset("Queue offset", Float) = 0.0
        [HideInInspector] _Smoothness("SMoothness", Float) = 0.50
    }
    SubShader
        {

            HLSLINCLUDE
            #include "Assets/SRP/Runtime/ShaderLibrary/Core.hlsl"


            ENDHLSL


            Pass
            {
                Tags {"LightMode" = "Forward"}
                LOD 100

                HLSLPROGRAM
                #pragma target 5.0
                #pragma shader_feature_local _RECEIVE_SHADOWS_OFF
                #pragma multi_compile_instancing
                #pragma multi_compile _ _DIRECTIONAL_PCF3 _DIRECTIONAL_PCF5 _DIRECTIONAL_PCF7
                #pragma multi_compile _ LIGHTMAP_ON
                #pragma multi_compile _ LOD_FADE_CROSSFADE
                #pragma multi_compile _ USE_CLUSTERED_LIGHTLIST
                //#pragma shader_feature USE_CLUSTERED_LIGHTLIST
                #include "Assets/SRP/Runtime/RenderPipeline/ShaderPass/ForwardPass.hlsl"
                #pragma vertex ForwardPassVert
                #pragma fragment ForwardPassFrag
                ENDHLSL
            }


            //Pass
            //{
            //    Tags {"LightMode" = "GBuffer"}
            //    LOD 100

            //    HLSLPROGRAM
            //    #pragma vertex vert
            //    #pragma fragment frag

            //    CBUFFER_START(UnityPerMaterial)
            //    float4 _BaseMap_ST;
            //    half4 _BaseColor;
            //    half4 _SpecColor;
            //    half4 _EmissionColor;
            //    half _Cutoff;
            //    half _Smoothness;
            //    half _Metallic;
            //    half _BumpScale;
            //    half _OcclusionStrength;
            //    CBUFFER_END

            //    ENDHLSL
            //}


            Pass{
                Tags {"LightMode" = "ShadowCaster"}

                ZWrite on

                HLSLPROGRAM

                #pragma vertex ShadowCasterPassVertex
                #pragma fragment ShadowCasterPassFragment


                bool _ShadowPancaking;

                struct Attributes {
                    float3 positionOS : POSITION;
                    //float2 uv : TEXCOORD0;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                struct Varyings {
                    float4 positionCS : SV_POSITION;
                    //float2 baseUV : VAR_BASE_UV;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };


                Varyings ShadowCasterPassVertex(Attributes input) {
                    Varyings output;
                    UNITY_SETUP_INSTANCE_ID(input);
                    UNITY_TRANSFER_INSTANCE_ID(input, output);
                    float3 positionWS = TransformObjectToWorld(input.positionOS);
                    output.positionCS = TransformWorldToHClip(positionWS);

                //    if (_ShadowPancaking) {
                //#if UNITY_REVERSED_Z
                //        output.positionCS.z = min(
                //            output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE
                //        );
                //#else
                //        output.positionCS.z = max(
                //            output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE
                //        );
                //#endif
                //    }

                    //output.baseUV = TRANSFORM_TEX(input.uv, _BaseMap);
                    return output;
                }

                void ClipLOD(float2 positionCS, float fade) {
                #if defined(LOD_FADE_CROSSFADE)
                                    float dither = InterleavedGradientNoise(positionCS.xy, 0);
                                    clip(fade + (fade < 0.0 ? dither : -dither));
                #endif
                }

                void ShadowCasterPassFragment(Varyings input) {
                    UNITY_SETUP_INSTANCE_ID(input);
                    //    ClipLOD(input.positionCS.xy, unity_LODFade.x);

                    //    InputConfig config = GetInputConfig(input.baseUV);
                    //    float4 base = GetBase(config);
                    //#if defined(_SHADOWS_CLIP)
                    //    clip(base.a - GetCutoff(config));
                    //#elif defined(_SHADOWS_DITHER)
                    //    float dither = InterleavedGradientNoise(input.positionCS.xy, 0);
                    //    clip(base.a - dither);
                    //#endif
                }
                ENDHLSL
            }


            //Pass
            //    {
            //    Tags {"LightMode" = "Meta"}
            //    LOD 100

            //    HLSLPROGRAM
            //    #pragma vertex vert
            //    #pragma fragment frag

            //    CBUFFER_START(UnityPerMaterial)
            //    float4 _BaseMap_ST;
            //    half4 _BaseColor;
            //    half4 _SpecColor;
            //    half4 _EmissionColor;
            //    half _Cutoff;
            //    half _Smoothness;
            //    half _Metallic;
            //    half _BumpScale;
            //    half _OcclusionStrength;
            //    CBUFFER_END

            //    ENDHLSL
            //    }

        }
        //Fallback "Hidden/NinoxSRP/ShadowCaster"
}
