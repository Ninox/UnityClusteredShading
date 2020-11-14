using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;


namespace NinoxSRP
{

    public class ForwardRenderer : Renderer
    {
        public ForwardRenderer(Settings settings) : base(settings)
        {
            var setting = GraphicProfile.CurrentProfile.lightingSetting;
            lighting=Lighting.GetLighting(setting);
        }

        private bool isInit = false;
        private Lighting lighting;
        private CommandBuffer buffer;

        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            BeginFrameRendering(context, cameras);

            foreach (var camera in cameras)
            {
                BeginCameraRendering(context, camera);


#if UNITY_EDITOR
                if (camera.cameraType == CameraType.SceneView)
                {
                    ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
                }
#endif

                RenderCamera(ref context, camera);

                EndCameraRendering(context, camera);
            }
            EndFrameRendering(context, cameras);
        }

        protected void RenderCamera(ref ScriptableRenderContext context, Camera camera)
        {

            if (!isInit)
            {
                lighting.Init(context,camera);
                isInit = true;
            }
            buffer = CommandBufferPool.Get(camera.name);
            //CameraClearFlags flags = camera.clearFlags;
            //buffer.ClearRenderTarget(
            //    flags <= CameraClearFlags.Depth,
            //    flags == CameraClearFlags.Color,
            //    flags == CameraClearFlags.Color ?
            //        camera.backgroundColor.linear : Color.clear
            //);
            buffer.ClearRenderTarget(
                true,true,Color.clear
            );
            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();

            if (!camera.TryGetCullingParameters(out var cullingParameters))
            {
                return;
            }
            cullingParameters.shadowDistance = Mathf.Min(GraphicProfile.CurrentProfile.lightingSetting.ShadowSetting.mainLightsShadowSetting.MaxDistance, camera.farClipPlane);
            var cullingResults = context.Cull(ref cullingParameters);

            lighting.Execute(context, camera, ref cullingResults);

            context.SetupCameraProperties(camera);

            DrawOpaque(ref context, camera, ref cullingResults);
            context.DrawSkybox(camera);
            //DrawTransparent(ref context, camera, ref cullingResults);
#if UNITY_EDITOR
            DrawGizmos(context, camera);
#endif

            context.Submit();
        }

        protected ScriptableRenderContext m_context;

        void DrawOpaque(ref ScriptableRenderContext context, Camera camera, ref CullingResults cullingResults)
        {
            buffer.BeginSample("Opaque");
            var sortingSettings = new SortingSettings(camera);
            sortingSettings.criteria = SortingCriteria.CommonOpaque;

            var drawSettings = new DrawingSettings(ShaderPassTags.Forward, sortingSettings)
            {
                enableDynamicBatching = true,
                enableInstancing = true,
                perObjectData = PerObjectData.Lightmaps | PerObjectData.LightProbe
            };

            var filterSettings = FilteringSettings.defaultValue;
            filterSettings.renderQueueRange = RenderQueueRange.opaque;
            filterSettings.layerMask = ~0;

            RenderStateBlock stateBlock = new RenderStateBlock(RenderStateMask.Nothing);

            context.DrawRenderers(cullingResults, ref drawSettings, ref filterSettings);
            buffer.EndSample("Opaque");
            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();
        }

        void DrawTransparent(ref ScriptableRenderContext context, Camera camera, ref CullingResults cullingResults)
        {
            var sortingSettings = new SortingSettings(camera);
            sortingSettings.criteria = SortingCriteria.CommonTransparent;

            var drawSettings = new DrawingSettings(ShaderPassTags.Forward, sortingSettings)
            {
                enableDynamicBatching = true,
                enableInstancing = true,
            };

            var filterSettings = new FilteringSettings(RenderQueueRange.opaque);
            filterSettings = FilteringSettings.defaultValue;
            filterSettings.renderQueueRange = RenderQueueRange.opaque;

            RenderStateBlock stateBlock = new RenderStateBlock(RenderStateMask.Nothing);

            drawSettings.sortingSettings = sortingSettings;
            filterSettings.renderQueueRange = RenderQueueRange.transparent;

            context.DrawRenderers(cullingResults, ref drawSettings, ref filterSettings);
        }

#if UNITY_EDITOR
        void DrawGizmos(ScriptableRenderContext context, Camera camera)
        {
            if (Handles.ShouldRenderGizmos())
            {
                context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
                context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
            }
        }
#endif

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            lighting.Dispose();
        }
    }
}