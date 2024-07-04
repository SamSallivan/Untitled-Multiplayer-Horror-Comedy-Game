#if ENVIRO_URP && UNITY_6000_0_OR_NEWER
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering; 
using UnityEngine.Rendering.RenderGraphModule;

namespace Enviro {
    public class EnviroURPRenderGraph : ScriptableRenderPass
    {
        public class PassData
        {
            internal TextureHandle src;
            internal Material material;
        }

        private Vector4 m_ScaleBias = new Vector4(1f, 1f, 0f, 0f);
        private List<EnviroVolumetricCloudRenderer> volumetricCloudsRender = new List<EnviroVolumetricCloudRenderer>();

        private Material blitThroughMat, fogMat;

        private EnviroVolumetricCloudRenderer CreateCloudsRenderer(Camera cam)
        {
            EnviroVolumetricCloudRenderer r = new EnviroVolumetricCloudRenderer();
            r.camera = cam;
            volumetricCloudsRender.Add(r);
            return r;
        }

        private EnviroVolumetricCloudRenderer GetCloudsRenderer(Camera cam)
        {
            for (int i = 0; i < volumetricCloudsRender.Count; i++)
            {
                if(volumetricCloudsRender[i].camera == cam)
                    return volumetricCloudsRender[i];
            }
            return CreateCloudsRenderer(cam);
        }


        public void Blit (string passName, RenderGraph renderGraph, Material mat, TextureHandle src, TextureHandle target, int pass)
        {
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
            {                    
                builder.UseTexture(src,AccessFlags.Read);
                builder.SetRenderAttachment(target, 0);
                builder.AllowPassCulling(false); 

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    if(src.IsValid())
                       mat.SetTexture("_MainTex", src);

                   Blitter.BlitTexture(context.cmd, new Vector4(1f, 1f, 0f, 0f), mat, pass);
                });
            } 
        }
        public void Blit (string passName, RenderGraph renderGraph, Material mat, TextureHandle src, TextureHandle target, int pass,TextureHandle read1, string read1Name)
        {
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
            {                    
                builder.UseTexture(src,AccessFlags.Read);
                builder.UseTexture(read1,AccessFlags.Read);
                //builder.SetInputAttachment(read1,0);
                builder.SetRenderAttachment(target, 0);
                builder.AllowPassCulling(false); 

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    if(src.IsValid())
                       mat.SetTexture("_MainTex", src);

                    if(read1.IsValid())
                       mat.SetTexture(read1Name, read1); 

                   Blitter.BlitTexture(context.cmd, new Vector4(1f, 1f, 0f, 0f), mat, pass);
                });
            }
        }

        public void Blit (string passName, RenderGraph renderGraph, Material mat, TextureHandle src, TextureHandle target, int pass,TextureHandle read1, string read1Name,TextureHandle read2, string read2Name)
        {
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
            {                    
                builder.UseTexture(src,AccessFlags.Read);
                builder.UseTexture(read1,AccessFlags.Read);
                builder.UseTexture(read2,AccessFlags.Read);
                //builder.SetInputAttachment(read1,0);
                builder.SetRenderAttachment(target, 0);
                builder.AllowPassCulling(false); 
 
                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    if(src.IsValid())
                       mat.SetTexture("_MainTex", src);

                    if(read1.IsValid())
                       mat.SetTexture(read1Name, read1); 

                    if(read2.IsValid())
                       mat.SetTexture(read2Name, read2);  

                   Blitter.BlitTexture(context.cmd, new Vector4(1f, 1f, 0f, 0f), mat, pass);
                });
            }
        }


        private void SetMatrix(Camera myCam)
        {
        #if ENABLE_VR || ENABLE_XR_MODULE
            if (UnityEngine.XR.XRSettings.enabled && UnityEngine.XR.XRSettings.stereoRenderingMode == UnityEngine.XR.XRSettings.StereoRenderingMode.SinglePassInstanced && myCam.stereoEnabled) 
            {
                // Both stereo eye inverse view matrices
                Matrix4x4 left_world_from_view = myCam.GetStereoViewMatrix(Camera.StereoscopicEye.Left).inverse;
                Matrix4x4 right_world_from_view = myCam.GetStereoViewMatrix(Camera.StereoscopicEye.Right).inverse;

                // Both stereo eye inverse projection matrices, plumbed through GetGPUProjectionMatrix to compensate for render texture
                Matrix4x4 left_screen_from_view = myCam.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left);
                Matrix4x4 right_screen_from_view = myCam.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right);
                Matrix4x4 left_view_from_screen = GL.GetGPUProjectionMatrix(left_screen_from_view, true).inverse;
                Matrix4x4 right_view_from_screen = GL.GetGPUProjectionMatrix(right_screen_from_view, true).inverse;

                // Negate [1,1] to reflect Unity's CBuffer state
                if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.OpenGLCore && SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3)
                {
                    left_view_from_screen[1, 1] *= -1;
                    right_view_from_screen[1, 1] *= -1;
                }

                Shader.SetGlobalMatrix("_LeftWorldFromView", left_world_from_view);
                Shader.SetGlobalMatrix("_RightWorldFromView", right_world_from_view);
                Shader.SetGlobalMatrix("_LeftViewFromScreen", left_view_from_screen);
                Shader.SetGlobalMatrix("_RightViewFromScreen", right_view_from_screen);
            }
            else
            {
                // Main eye inverse view matrix
                Matrix4x4 left_world_from_view = myCam.cameraToWorldMatrix;

                // Inverse projection matrices, plumbed through GetGPUProjectionMatrix to compensate for render texture
                Matrix4x4 screen_from_view = myCam.projectionMatrix;
                Matrix4x4 left_view_from_screen = GL.GetGPUProjectionMatrix(screen_from_view, true).inverse;

                // Negate [1,1] to reflect Unity's CBuffer state
                if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.OpenGLCore && SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3)
                    left_view_from_screen[1, 1] *= -1;

                Shader.SetGlobalMatrix("_LeftWorldFromView", left_world_from_view);
                Shader.SetGlobalMatrix("_LeftViewFromScreen", left_view_from_screen);
            } 
        #else
                // Main eye inverse view matrix
                Matrix4x4 left_world_from_view = myCam.cameraToWorldMatrix;

                // Inverse projection matrices, plumbed through GetGPUProjectionMatrix to compensate for render texture
                Matrix4x4 screen_from_view = myCam.projectionMatrix;
                Matrix4x4 left_view_from_screen = GL.GetGPUProjectionMatrix(screen_from_view, true).inverse;

                // Negate [1,1] to reflect Unity's CBuffer state
                if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.OpenGLCore && SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3)
                    left_view_from_screen[1, 1] *= -1;

                Shader.SetGlobalMatrix("_LeftWorldFromView", left_world_from_view);
                Shader.SetGlobalMatrix("_LeftViewFromScreen", left_view_from_screen);
            #endif
        } 
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
              
            if(EnviroManager.instance == null)
               return; 

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();


            if(EnviroHelper.ResetMatrix(cameraData.camera))
                cameraData.camera.ResetProjectionMatrix();

             EnviroQuality myQuality = EnviroHelper.GetQualityForCamera(cameraData.camera);

            //Set what to render on this camera.
            bool renderVolumetricClouds = false;
            bool renderFog = false;

            if(EnviroManager.instance.Quality != null)
            {
                if(EnviroManager.instance.VolumetricClouds != null)
                    renderVolumetricClouds = myQuality.volumetricCloudsOverride.volumetricClouds;  

                if(EnviroManager.instance.Fog != null)
                    renderFog = myQuality.fogOverride.fog;  
            }
            else
            {
                if(EnviroManager.instance.VolumetricClouds != null)
                    renderVolumetricClouds = EnviroManager.instance.VolumetricClouds.settingsQuality.volumetricClouds;

                if(EnviroManager.instance.Fog != null)
                    renderFog = EnviroManager.instance.Fog.Settings.fog;
            }
 
            //Set some global matrixes used for all the enviro effects.
            SetMatrix(cameraData.camera);

            RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
            desc.colorFormat = RenderTextureFormat.ARGBHalf;
            desc.msaaSamples = 1;
            desc.depthBufferBits = 0;

            TextureHandle source = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "CopyTexture", false);
            TextureHandle target = resourceData.activeColorTexture;

            // This check is to avoid an error from the material preview in the scene
            if (!target.IsValid() || !source.IsValid())
                return;

            //Blit Main Texture
            using  ( var builder = renderGraph.AddRasterRenderPass<PassData>("Enviro 3 Copy Texture", out var passData))
            {                     
                passData.src = target;
                if(blitThroughMat == null)
                   blitThroughMat = new Material(Shader.Find("Hidden/EnviroBlitThroughURP17"));

                passData.material = blitThroughMat;
                builder.UseTexture(passData.src);
                builder.SetRenderAttachment(source, 0);
                                 
                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    blitThroughMat.SetTexture("_MainTex", passData.src);
                    Blitter.BlitTexture(context.cmd, m_ScaleBias, passData.material, 0);
                });
            }
             

            //Render volumetrics mask first
            if(EnviroManager.instance.Fog != null && renderFog)
               EnviroManager.instance.Fog.RenderVolumetricsURP(this,renderGraph,resourceData,cameraData,source);
          
            if(EnviroManager.instance.Fog != null && EnviroManager.instance.VolumetricClouds != null && renderVolumetricClouds && renderFog)
            { 
                TextureHandle temp1 = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "Temp1", false);

                if(cameraData.camera.transform.position.y < EnviroManager.instance.VolumetricClouds.settingsLayer1.bottomCloudsHeight)
                { 

                    EnviroVolumetricCloudRenderer renderer = GetCloudsRenderer(cameraData.camera);
                    EnviroManager.instance.VolumetricClouds.RenderVolumetricCloudsURP(this,renderGraph, resourceData, cameraData,source,temp1, renderer, myQuality);   

                    if(EnviroManager.instance.VolumetricClouds.settingsGlobal.cloudShadows && cameraData.camera.cameraType != CameraType.Reflection)
                    {
                        TextureHandle temp2 = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "Temp2", false);
                        EnviroManager.instance.VolumetricClouds.RenderCloudsShadowsURP(this,renderGraph, resourceData, cameraData,temp1,temp2, renderer);   
                        EnviroManager.instance.Fog.RenderHeightFogURP(this, renderGraph,resourceData,cameraData,temp2,resourceData.activeColorTexture);
                    }
                    else
                    {
                        EnviroManager.instance.Fog.RenderHeightFogURP(this, renderGraph,resourceData,cameraData,temp1,resourceData.activeColorTexture);
                    }
                }
                else
                { 
                    EnviroManager.instance.Fog.RenderHeightFogURP(this, renderGraph,resourceData,cameraData,source,temp1);
                    EnviroVolumetricCloudRenderer renderer = GetCloudsRenderer(cameraData.camera);

                    if(EnviroManager.instance.VolumetricClouds.settingsGlobal.cloudShadows && cameraData.camera.cameraType != CameraType.Reflection)
                    {
                        TextureHandle temp2 = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "Temp2", false);
                        EnviroManager.instance.VolumetricClouds.RenderVolumetricCloudsURP(this,renderGraph, resourceData, cameraData,temp1,temp2, renderer, myQuality);   
                        EnviroManager.instance.VolumetricClouds.RenderCloudsShadowsURP(this,renderGraph, resourceData, cameraData,temp2,resourceData.activeColorTexture, renderer);  
                    }
                    else
                    {
                        EnviroManager.instance.VolumetricClouds.RenderVolumetricCloudsURP(this,renderGraph, resourceData, cameraData,temp1,resourceData.activeColorTexture, renderer, myQuality);   
                    }
                }
            }
            else if(EnviroManager.instance.VolumetricClouds != null && renderVolumetricClouds && !renderFog)
            {
                EnviroVolumetricCloudRenderer renderer = GetCloudsRenderer(cameraData.camera);
                
                if(EnviroManager.instance.VolumetricClouds.settingsGlobal.cloudShadows && cameraData.camera.cameraType != CameraType.Reflection)
                {
                    TextureHandle temp1 = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "Temp1", false);
                    EnviroManager.instance.VolumetricClouds.RenderVolumetricCloudsURP(this,renderGraph, resourceData, cameraData,source,temp1, renderer, myQuality);   
                    EnviroManager.instance.VolumetricClouds.RenderCloudsShadowsURP(this,renderGraph, resourceData, cameraData,temp1,resourceData.activeColorTexture, renderer);  
                }
                else
                {
                    EnviroManager.instance.VolumetricClouds.RenderVolumetricCloudsURP(this,renderGraph, resourceData, cameraData,source,resourceData.activeColorTexture, renderer, myQuality);   
                }
            }
            else if (EnviroManager.instance.Fog != null && renderFog)
            {
                EnviroManager.instance.Fog.RenderHeightFogURP(this, renderGraph,resourceData,cameraData,source,resourceData.activeColorTexture);
            }
            else
            {


            }

        }
    }
}
#endif
