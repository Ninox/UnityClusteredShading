using UnityEditor;
#if UNITY_EDITOR
using UnityEditor.ProjectWindowCallback;
#endif
using UnityEngine;
using UnityEngine.Rendering;

namespace NinoxSRP
{

    public class GraphicProfile : RenderPipelineAsset
    {
        public ComputeShader cs;
        public static GraphicProfile CurrentProfile
        {
            get
            {
                return GraphicsSettings.currentRenderPipeline as GraphicProfile;
            }
            private set
            {

            }
        }

        Shader m_defaultShader => Shader.Find("NinoxSRP/SimpleLit");

        public override Material defaultMaterial => new Material(m_defaultShader);

        public Renderer.Settings renderingPath  = new Renderer.Settings();

        public Lighting.Settings lightingSetting  = new Lighting.Settings();

        protected override RenderPipeline CreatePipeline()
        {
            return Renderer.GetPipeline(this.renderingPath);
        }


        public const string packagePath = "Assets/SRP";

#if UNITY_EDITOR
        private class CreateGraphicProfileAsset : EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                var instance = CreateInstance<GraphicProfile>();
                ResourceReloader.TryReloadAllNullIn(instance, packagePath);
                AssetDatabase.CreateAsset(instance, pathName);
            }
        }

        [MenuItem("Assets/Create/NinoxSRP/GraphicProfile", priority = CoreUtils.assetCreateMenuPriority1)]
        static void CreateGraphicProfile()
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, CreateInstance<CreateGraphicProfileAsset>(),
    "GraphicProfileAsset.asset", null, null);
        }

        //public override void Action(int instanceId, string pathName, string resourceFile)
        //{
        //    var instance = CreateInstance<ForwardRendererData>();
        //    AssetDatabase.CreateAsset(instance, pathName);
        //    ResourceReloader.ReloadAllNullIn(instance, UniversalRenderPipelineAsset.packagePath);
        //    Selection.activeObject = instance;
        //}
#endif
    }
}