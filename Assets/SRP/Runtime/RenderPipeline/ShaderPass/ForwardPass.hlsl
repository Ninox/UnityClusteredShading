#ifndef NINOX_SRP_FORWARDPASS_INCLUDED
#define NINOX_SRP_FORWARDPASS_INCLUDED





#include "Assets/SRP/Runtime/ShaderLibrary/Core.hlsl"
#include "Assets/SRP/Runtime/Lighting/Lighting.hlsl"


    //UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
    //    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)
    //    UNITY_DEFINE_INSTANCED_PROP(half4, _BaseColor)
    //    UNITY_DEFINE_INSTANCED_PROP(half4, _SpecColor)
    //    UNITY_DEFINE_INSTANCED_PROP(half4, _EmissionColor)
    //    UNITY_DEFINE_INSTANCED_PROP(half, _Cutoff)
    //    UNITY_DEFINE_INSTANCED_PROP(half, _Smoothness)
    //    UNITY_DEFINE_INSTANCED_PROP(half, _Metallic)
    //    UNITY_DEFINE_INSTANCED_PROP(half, _BumpScale)
    //    UNITY_DEFINE_INSTANCED_PROP(half, _OcclusionStrength)
    //    UNITY_DEFINE_INSTANCED_PROP(float, _ReceiveShadows)
    //UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

    CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4 _BaseColor;
    half4 _SpecColor;
    half4 _EmissionColor;
    half _Cutoff;
    half _Smoothness;
    half _Metallic;
    //half _BumpScale;
    half _OcclusionStrength;
    float _ReceiveShadows;
    CBUFFER_END
half _BumpScale;
    TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
    TEXTURE2D(_BumpMap);SAMPLER(sampler_BumpMap);
    TEXTURE2D(_EmissionMap);SAMPLER(sampler_EmissionMap);
    TEXTURE2D(_SpecGlossMap);SAMPLER(sampler_SpecGlossMap);
    TEXTURE2D(_MetallicGlossMap);SAMPLER(sampler_MetallicGlossMap);


    struct Attributes
    {
        float4 positionOS : POSITION;
        float3 normalOS : NORMAL;
        float4 tangentOS : TANGENT;
        float2 texcoord : TEXCOORD0;
        GI_ATTRIBUTE_DATA
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct Varyings
    {
        float4 posCS : SV_Position;
        float2 uv : TEXCOORD0;
                    //DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 1);

        float3 posWS : TEXCOORD2; // xyz: posWS
        float4 posNDC : TEXCOORD7;
        //#ifdef _NORMALMAP
        float4 normalWS : TEXCOORD3; // xyz: normal, w: viewDir.x
        float4 tangentWS : TEXCOORD4; // xyz: tangent, w: viewDir.y
        float4 bitangentWS : TEXCOORD5; // xyz: bitangent, w: viewDir.z
        //#else
        //float3  normal                  : TEXCOORD3;
        //float3 viewDir                  : TEXCOORD4;
        //#endif

        half4 fogFactorAndVertexLight : TEXCOORD6; // x: fogFactor, yzw: vertex light

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    float4 shadowCoord              : TEXCOORD7;
#endif
        
        GI_VARYINGS_DATA
        UNITY_VERTEX_INPUT_INSTANCE_ID
        UNITY_VERTEX_OUTPUT_STEREO
    };


    float2 TransformBaseUV(float2 baseUV)
    {
        //float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_BaseMap_ST);
        float4 baseST = _BaseMap_ST;
        return baseUV * baseST.xy + baseST.zw;
    }

    Varyings ForwardPassVert(Attributes input)
    {

        UNITY_SETUP_INSTANCE_ID(input);
        Varyings output = (Varyings) 0;
                    
        UNITY_TRANSFER_INSTANCE_ID(input, output);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

        VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
        VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
        output.posCS = vertexInput.positionCS;
        output.posWS = vertexInput.positionWS;
        output.posNDC = vertexInput.positionNDC;
        //output.posWS = mul(UNITY_MATRIX_V, float4(output.posWS, 1.0));
        //output.posWS = mul(UNITY_MATRIX_P, float4(output.posWS, 1.0));
        output.uv = TransformBaseUV(input.texcoord);

        half3 viewDirWS = GetCameraPositionWS() - vertexInput.positionWS;

        output.normalWS = half4(normalInput.normalWS, viewDirWS.x);
        output.tangentWS = half4(normalInput.tangentWS, viewDirWS.y);
        output.bitangentWS = half4(normalInput.bitangentWS, viewDirWS.z);

        output.fogFactorAndVertexLight.x = ComputeFogFactor(vertexInput.positionCS.z);
        TRANSFER_GI_DATA(input, output);
        
        return output;
    }

    
    half4 ForwardPassFrag(Varyings input) : SV_Target
    {  
    //return float4(ScreenToNDC(input.posCS).xy,0.0, 1.0);
        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    
        half3 normalSampleTS = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv));

        half3 normalWS = normalize(TransformTangentToWorld(normalSampleTS, half3x3(input.tangentWS.xyz, input.bitangentWS.xyz, input.normalWS.xyz)));
        normalWS = NormalizeNormalPerPixel(normalWS);
        half3 viewDirWS = normalize(half3(input.normalWS.w, input.tangentWS.w, input.bitangentWS.w));  
    
        half4 texSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
        //float4 specColor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _SpecColor);
        float4 specColor = _SpecColor;
        half4 specSample = SAMPLE_TEXTURE2D(_SpecGlossMap, sampler_SpecGlossMap, input.uv) * specColor;
        //float3 GI = SampleLightMap(GI_FRAGMENT_DATA(input));
        
        SurfaceData surface;
        surface.posWS = input.posWS;
        surface.posSS = input.posCS;
        surface.normalWS = normalWS;
        surface.viewDirWS = viewDirWS;
        surface.albedo = texSample;
        surface.specular = specSample;
        //surface.GI = GI;
        //surface.smooth = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Smoothness);
        surface.smooth = _Smoothness;

        float3 color = BlinnPhongLighting(surface);
    
        return float4(color, texSample.a);
}
    
#endif