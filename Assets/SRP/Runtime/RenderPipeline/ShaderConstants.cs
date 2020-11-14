using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NinoxSRP
{
    public static class ShaderPassNames
    {
        public static readonly string Empty = "";
        public static readonly string Forward = "Forward";
        public static readonly string DepthOnly = "DepthOnly";
        public static readonly string DepthForwardOnly = "DepthForwardOnly";
        public static readonly string ForwardOnly = "ForwardOnly";
        public static readonly string GBuffer = "GBuffer";
        public static readonly string GBufferWithPrepass = "GBufferWithPrepass";
        public static readonly string SRPDefaultUnlit = "SRPDefaultUnlit";
        public static readonly string MotionVectors = "MotionVectors";
        public static readonly string DistortionVectors = "DistortionVectors";
        public static readonly string TransparentDepthPrepass = "TransparentDepthPrepass";
        public static readonly string TransparentBackface = "TransparentBackface";
        public static readonly string TransparentDepthPostpass = "TransparentDepthPostpass";
        public static readonly string Meta = "META";
        public static readonly string ShadowCaster = "ShadowCaster";
    }

    public static class ShaderPassTags
    {
        public static readonly ShaderTagId Empty = new ShaderTagId(ShaderPassNames.Empty);
        public static readonly ShaderTagId Forward = new ShaderTagId(ShaderPassNames.Forward);
        public static readonly ShaderTagId DepthOnly = new ShaderTagId(ShaderPassNames.DepthOnly);
        public static readonly ShaderTagId DepthForwardOnly = new ShaderTagId(ShaderPassNames.DepthForwardOnly);
        public static readonly ShaderTagId ForwardOnly = new ShaderTagId(ShaderPassNames.ForwardOnly);
        public static readonly ShaderTagId GBuffer = new ShaderTagId(ShaderPassNames.GBuffer);
        public static readonly ShaderTagId GBufferWithPrepass = new ShaderTagId(ShaderPassNames.GBufferWithPrepass);
        public static readonly ShaderTagId SRPDefaultUnlit = new ShaderTagId(ShaderPassNames.SRPDefaultUnlit);
        public static readonly ShaderTagId MotionVectorsName = new ShaderTagId(ShaderPassNames.MotionVectors);
        public static readonly ShaderTagId DistortionVectors = new ShaderTagId(ShaderPassNames.DistortionVectors);
        public static readonly ShaderTagId TransparentDepthPrepass = new ShaderTagId(ShaderPassNames.TransparentDepthPrepass);
        public static readonly ShaderTagId TransparentBackface = new ShaderTagId(ShaderPassNames.TransparentBackface);
        public static readonly ShaderTagId TransparentDepthPostpass = new ShaderTagId(ShaderPassNames.TransparentDepthPostpass);
    }

    public static class ShaderPropIDs
    {
        public static readonly int InvProj=Shader.PropertyToID("_InvProj");

        #region Shadow
        public static readonly int MainShadowAtlas = Shader.PropertyToID("_MainShadowAtlas");
        public static readonly int MainShadowVPMatrixArray = Shader.PropertyToID("_MainShadowVPMatrixArray");
        public static readonly int AdditionalShadowAtlas = Shader.PropertyToID("_AdditionalShadowAtlas");
        public static readonly int AdditionalShadowVPMatrixArray = Shader.PropertyToID("_AdditionalShadowVPMatrixArray");


        public static readonly int CascadeCount = Shader.PropertyToID("_CascadeCount");
        public static readonly int CascadeCullingSphereArray = Shader.PropertyToID("_CascadeCullingSphereArray");
        public static readonly int CascadeDistanceFade = Shader.PropertyToID("_CascadeDistanceFade");

        public static readonly int NormalBias = Shader.PropertyToID("_ShadowNormalBias");
        #endregion
    }


    public static class ShaderKeyword
    {
        public static string ZPrePassEnable = "Z_PREPASS_ENABLED";
        public static string UseClusterLightlist = "USE_CLUSTERED_LIGHTLIST";
    }
}