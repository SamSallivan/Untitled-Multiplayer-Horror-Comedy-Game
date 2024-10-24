﻿using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine;

namespace SCPE
{
    public class ScanlinesRenderer : ScriptableRendererFeature
    {
        class ScanlinesRenderPass : PostEffectRenderer<Scanlines>
        {
            public ScanlinesRenderPass(EffectBaseSettings settings)
            {
                this.settings = settings;
                shaderName = ShaderNames.Scanlines;
                ProfilerTag = GetProfilerTag();
            }

            public void Setup(ScriptableRenderer renderer)
            {
                this.cameraColorTarget = GetCameraTarget(renderer);
                volumeSettings = VolumeManager.instance.stack.GetComponent<Scanlines>();
                
                if(volumeSettings && volumeSettings.IsActive()) renderer.EnqueuePass(this);
            }

            protected override void ConfigurePass(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
                if (!volumeSettings) return;

                base.ConfigurePass(cmd, cameraTextureDescriptor);
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (ShouldRender(renderingData) == false) return;

                var cmd = GetCommandBuffer(ref renderingData);

                CopyTargets(cmd, renderingData);

                cmd.SetGlobalVector(ShaderParameters.Params, new Vector4(volumeSettings.amount.value, volumeSettings.intensity.value / 1000, volumeSettings.speed.value * 8f, 0f));

                FinalBlit(this, context, cmd, renderingData, 0);
            }
        }

        ScanlinesRenderPass m_ScriptablePass;

        [SerializeField]
        public EffectBaseSettings settings = new EffectBaseSettings();

        public override void Create()
        {
            m_ScriptablePass = new ScanlinesRenderPass(settings);
            m_ScriptablePass.renderPassEvent = settings.injectionPoint;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            m_ScriptablePass.Setup(renderer);
        }
    }
}
