
namespace UnityEngine.Rendering.Universal
{
    public class AdditionPPRenderPassFeature : ScriptableRendererFeature
    {
        public AdditionPostProcessData postProcessData;

        private static RenderTexture RT_FSR_Source;
        private static RenderTexture RT_FSR_Dest;

        //static AMD_FSR m_Fsr;

        static float scaleFactor = 1.5f;
        class BeforeEverythingPass : ScriptableRenderPass
        {
            public BeforeEverythingPass(RenderPassEvent renderPass)
            {
                this.renderPassEvent = renderPass;
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                renderingData.cameraData.targetTexture = RT_FSR_Source;
            }
        }
        class AdditionRenderPass : ScriptableRenderPass
        {


            RenderTargetIdentifier m_ColorAttachment;
            RenderTargetHandle m_Destination;

            const string k_RenderPostProcessingTag = "Render AdditionalPostProcessing Effects";
            const string k_RenderFinalPostProcessingTag = "Render Final AdditionalPostProcessing Effects";

            InvertColor m_InvertColor;
            AMD_FSR m_Fsr;


            ComputeShader EASU;
            ComputeShader RCAS;
            private ComputeBuffer EASUConstsCB, RCASConstsCB;
            MYMaterialLibrary m_Materials;

            RenderTargetHandle m_TempColorTexture01;
            RenderTargetHandle m_TempColorTexture02;
            RenderTargetHandle m_TempColorTexture03;

            RenderTexture outputImage, outputImage2;
            private int scaledPixelWidth = 0;
            private int scaledPixelHeight = 0;
            private int scaledPixelWidthPrev = 0;
            private int scaledPixelHeightPrev = 0;
            bool isRCASSetup = false;

            public AdditionRenderPass(AdditionPostProcessData data)
            {
                m_Materials = new MYMaterialLibrary(data);
                m_TempColorTexture01.Init("_TempColorTexture01");
                m_TempColorTexture02.Init("_TempColorTexture02");
                m_TempColorTexture03.Init("_TempColorTexture03");



            }

            public void Setup(RenderPassEvent @event, RenderTargetIdentifier source, RenderTargetHandle destination)
            {
                renderPassEvent = @event;
                m_ColorAttachment = source;
                m_Destination = destination;

                EASUConstsCB = new ComputeBuffer(4, sizeof(uint) * 4);
                EASUConstsCB.name = "EASU Consts";

                RCASConstsCB = new ComputeBuffer(1, sizeof(uint) * 4);
                RCASConstsCB.name = "RCAS Consts";

                //var stack = VolumeManager.instance.stack;
                //m_InvertColor = stack.GetComponent<InvertColor>();
                //m_Fsr = stack.GetComponent<AMD_FSR>();
                //EASU = m_Fsr.computeShaderEASU.value;
                //RCAS = m_Fsr.computeShaderRCAS.value;

                //scaledPixelWidth = (int)(renderingData.cameraData.camera.pixelWidth / m_Fsr.scaleFactor.value);
                //scaledPixelHeight = (int)(renderingData.cameraData.camera.pixelHeight / m_Fsr.scaleFactor.value);
                //isRCASSetup = true;


            }
            // TRCASConstsCB = new ComputeBuffer(1, sizeof(uint) * 4);his method is called before executing the render pass.
            // IRCASConstsCB.name = "RCAS Consts";t can be used to configure render targets and their clear state. Also to create temporary render target textures.
            // When empty this render pass will render to the active camera render target.
            // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
            // The render pipeline will ensure target setup and clearing happens in a performant manner.
            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
            }

            // Here you can implement the rendering logic.
            // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
            // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
            // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                var stack = VolumeManager.instance.stack;
                m_InvertColor = stack.GetComponent<InvertColor>();
                m_Fsr = stack.GetComponent<AMD_FSR>();
                EASU = m_Fsr.computeShaderEASU.value;
                RCAS = m_Fsr.computeShaderRCAS.value;

                scaledPixelWidth = (int)(renderingData.cameraData.camera.pixelWidth / m_Fsr.scaleFactor.value);
                scaledPixelHeight = (int)(renderingData.cameraData.camera.pixelHeight / m_Fsr.scaleFactor.value);
                isRCASSetup = true;

                var cmd = CommandBufferPool.Get(k_RenderPostProcessingTag);

                Render(cmd, ref renderingData);

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);

            }

            private void Render(CommandBuffer cmd, ref RenderingData renderingData)
            {
                ref var cameraData = ref renderingData.cameraData;


                if (m_Fsr.IsActive() && !cameraData.isSceneViewCamera)
                {
                    //CreateRenderTexture(cameraData.camera);
                    SetupAMDFsr(cmd, ref renderingData, cameraData);
                }
                if (m_InvertColor.IsActive() && !cameraData.isSceneViewCamera)
                {
                    SetupInvertColor(cmd, ref renderingData, m_Materials.invertColor);
                }

            }

            private void SetupInvertColor(CommandBuffer cmd, ref RenderingData renderingData, Material invertMaterial)
            {
                RenderTextureDescriptor opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;
                cmd.GetTemporaryRT(m_TempColorTexture01.id, opaqueDesc);
                cmd.GetTemporaryRT(m_TempColorTexture02.id, opaqueDesc);
                cmd.GetTemporaryRT(m_TempColorTexture03.id, opaqueDesc);
                cmd.BeginSample("invertColor");
                cmd.Blit(this.m_ColorAttachment, m_TempColorTexture01.Identifier());
                cmd.Blit(m_TempColorTexture01.Identifier(), m_TempColorTexture02.Identifier(), invertMaterial);
                cmd.Blit(m_TempColorTexture02.Identifier(), m_ColorAttachment);
                cmd.Blit(m_TempColorTexture02.Identifier(), this.m_Destination.Identifier());
                cmd.EndSample("invertColor");

            }

            private void SetupAMDFsr(CommandBuffer cmd, ref RenderingData renderingData, CameraData cameraData)
            {


                Camera cam = cameraData.camera;
                cam.allowDynamicResolution = true;
                RenderTextureDescriptor opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;
                opaqueDesc.width = scaledPixelWidth;
                opaqueDesc.height = scaledPixelHeight;
                cmd.GetTemporaryRT(m_TempColorTexture01.id, opaqueDesc);
                //cmd.GetTemporaryRT(m_Destination.id, opaqueDesc);
                //cmd.GetTemporaryRT(m_TempColorTexture02.id, opaqueDesc);
                //cmd.GetTemporaryRT(m_TempColorTexture01.id, scaledPixelWidth, scaledPixelHeight);
                cmd.BeginSample("AMDFsr");
                cmd.Blit(m_ColorAttachment, m_TempColorTexture01.Identifier());

                if (outputImage == null || scaledPixelWidth != scaledPixelWidthPrev || scaledPixelHeight != scaledPixelHeightPrev || isRCASSetup == false && m_Fsr.sharpening.value)
                {
                    scaledPixelWidthPrev = scaledPixelWidth;
                    scaledPixelHeightPrev = scaledPixelHeight;

                    if (outputImage) outputImage.Release();
                    outputImage = new RenderTexture(cam.pixelWidth, cam.pixelHeight, 0, RenderTextureFormat.RGB111110Float, RenderTextureReadWrite.sRGB);
                    outputImage.enableRandomWrite = true;
                    outputImage.name = "outputImage";
                    outputImage.Create();

                    if (m_Fsr.sharpening.value)
                    {
                        if (outputImage2) outputImage2.Release();
                        outputImage2 = new RenderTexture(cam.pixelWidth, cam.pixelHeight, 0, RenderTextureFormat.RGB111110Float, RenderTextureReadWrite.sRGB);
                        outputImage2.enableRandomWrite = true;
                        outputImage2.name = "outputImage2";
                        outputImage2.Create();
                    }
                }





                //cmd.SetComputeTextureParam(EASU, 0, "InputTexture", m_ColorAttachment);
                cmd.SetComputeVectorParam(EASU, "_RenderViewportSize", new Vector4(cam.pixelWidth, cam.pixelHeight));
                cmd.SetComputeVectorParam(EASU, "_ContainerTextureSize", new Vector4(cam.pixelWidth, cam.pixelHeight));
                cmd.SetComputeVectorParam(EASU, "_UpscaledViewportSzie", new Vector4(outputImage.width, outputImage.height, 1f / outputImage.width, 1f / outputImage.height));
                cmd.SetComputeBufferParam(EASU, 1, "_EASUConsts", EASUConstsCB);
                cmd.DispatchCompute(EASU, 1, 1, 1, 1);//init



                cmd.SetComputeTextureParam(EASU, 0, "InputTexture", m_TempColorTexture01.Identifier());

                cmd.SetComputeTextureParam(EASU, 0, "OutputTexture", outputImage);


                const int ThreadGroupWorkRegionRim = 8;
                int dispatchX = (cam.pixelWidth + ThreadGroupWorkRegionRim - 1) / ThreadGroupWorkRegionRim;
                int dispatchY = (cam.pixelHeight + ThreadGroupWorkRegionRim - 1) / ThreadGroupWorkRegionRim;
                cmd.SetComputeBufferParam(EASU, 0, "_EASUConsts", EASUConstsCB);
                cmd.DispatchCompute(EASU, 0, dispatchX, dispatchY, 1);//main

                //RenderTextureDescriptor renderDesc = renderingData.cameraData.cameraTargetDescriptor;
                //renderDesc.colorFormat = renderingData.cameraData.camera.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
                //renderDesc.width = RT_FSR_Dest.width;
                //renderDesc.height = RT_FSR_Dest.height;
                //renderDesc.enableRandomWrite = true;
                //renderDesc.useMipMap = false;
                //renderDesc.depthBufferBits = 24;

                //cmd.GetTemporaryRT(m_TempColorTexture02.id, renderDesc);
                //cmd.GetTemporaryRT(m_TempColorTexture02.id,RT_FSR_Dest.descriptor);

                //cmd.Blit(RT_FSR_Dest, m_TempColorTexture02.Identifier());
                if (m_Fsr.sharpening.value)
                {
                    //cmd.GetTemporaryRT(m_TempColorTexture02.id, renderDesc);

                    cmd.SetComputeBufferParam(RCAS, 1, "_RCASConsts", RCASConstsCB);

                    cmd.SetComputeFloatParam(RCAS, "_Sharpness", m_Fsr.sharpness.value);
                    cmd.DispatchCompute(RCAS, 1, 1, 1, 1);//init

                    cmd.SetComputeBufferParam(RCAS, 0, "_RCASConsts", RCASConstsCB);
                    cmd.SetComputeTextureParam(RCAS, 0, "InputTexture", outputImage);


                    cmd.SetComputeTextureParam(RCAS, 0, "OutputTexture", outputImage2);

                    cmd.DispatchCompute(RCAS, 0, dispatchX, dispatchY, 1);//main


                }
                //Blit(cmd, m_Fsr.sharpening.value ? outputImage2 : outputImage, m_Destination.id);
                cmd.Blit(m_Fsr.sharpening.value ? outputImage2 : outputImage, m_Destination.Identifier());
                //cmd.Blit(m_TempColorTexture02.Identifier(), m_Destination.Identifier());

                cmd.EndSample("AMDFsr");
            }

            // Cleanup any allocated resources that were created during the execution of this render pass.
            public override void OnCameraCleanup(CommandBuffer cmd)
            {
                //EASUConstsCB.Release();
                //RCASConstsCB.Release();
                if (m_Destination == RenderTargetHandle.CameraTarget)
                    return;
                if (m_Destination.HasInternalRenderTargetId())
                    return;
                if (RT_FSR_Source != null) RT_FSR_Source.Release();
                if (RT_FSR_Dest != null) RT_FSR_Dest.Release();
                if (outputImage)
                {
                    outputImage.Release();
                    outputImage = null;
                }
                if (EASUConstsCB != null)
                {
                    EASUConstsCB.Dispose();
                    EASUConstsCB = null;
                }
                if (outputImage2)
                {
                    outputImage2.Release();
                    outputImage2 = null;
                }
                if (RCASConstsCB != null)
                {
                    RCASConstsCB.Dispose();
                    RCASConstsCB = null;
                }
                isRCASSetup = false;
            }
        }

        AdditionRenderPass m_ScriptablePass;
        BeforeEverythingPass beforeEverythingPass;
        /// <inheritdoc/>
        public override void Create()
        {
            //m_Fsr = VolumeManager.instance.stack.GetComponent<AMD_FSR>();

            m_ScriptablePass = new AdditionRenderPass(postProcessData);

            // Configures where the render pass should be injected.
            m_ScriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;

            //beforeEverythingPass = new BeforeEverythingPass(RenderPassEvent.BeforeRendering);
        }

        private void CreateRenderTexture(Camera cam)
        {
            //if (RT_FSR_Source != null) RT_FSR_Source.Release();
            //int scaledPixelWidth = (int)(cam.pixelWidth / m_Fsr.scaleFactor.value);
            //int scaledPixelHeight = (int)(cam.pixelHeight / m_Fsr.scaleFactor.value);

            //RT_FSR_Source = new RenderTexture(scaledPixelWidth, scaledPixelHeight, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);

            //if (RT_FSR_Dest != null) RT_FSR_Dest.Release();

            //RT_FSR_Dest = new RenderTexture(cam.pixelWidth, cam.pixelHeight, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            //RT_FSR_Dest.name = "RT_FSR_Dest";
            //RT_FSR_Dest.enableRandomWrite = true;
            //RT_FSR_Dest.useMipMap = false;
            //RT_FSR_Dest.Create();
        }

        // Here you can inject one or multiple render passes in the renderer.
        // This method is called when setting up the renderer once per-camera.
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            //CreateRenderTexture(renderingData.cameraData.camera);
            var cameraColorTarget = renderer.cameraColorTarget;
            var dest = RenderTargetHandle.CameraTarget;
            if (postProcessData == null) return;
            m_ScriptablePass.Setup(RenderPassEvent.AfterRenderingTransparents, cameraColorTarget, dest);
            renderer.EnqueuePass(m_ScriptablePass);
            //renderer.EnqueuePass(beforeEverythingPass);
        }
    }
}


