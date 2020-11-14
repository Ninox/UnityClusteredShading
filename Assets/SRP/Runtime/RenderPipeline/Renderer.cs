using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace NinoxSRP
{
    public abstract class Renderer : RenderPipeline
    {
        public enum Path
        {
            Forward,
            Deferred
        }

        [Serializable]
        public class Settings
        {
            public Path RenderingPath = Path.Forward;
            public bool ZPrePass;
        }

        protected Settings settings { get; set; }

        protected Renderer(Settings settings)
        {
            this.settings = settings;
        }

        protected virtual void Execute(ScriptableRenderContext context, Camera cameram, CullingResults cullingResults) { }

        protected virtual void Init() { }

        protected virtual void End() { }

        public static RenderPipeline GetPipeline(Settings settings)
        {
            switch (settings.RenderingPath)
            {
                case Path.Forward:
                    {
                        return new ForwardRenderer(settings);
                    }
                default:
                    throw new System.ArgumentException("Rendering path not supported");
            }
        }

    }
}