using System;

namespace UnityEngine.Rendering.Universal
{

    [Serializable, VolumeComponentMenu("Addition-Post-Processing/AMD_FSR")]
    public class AMD_FSR : VolumeComponent, IPostProcessComponent
    {
        [Header("FSR Compute Shaders")]
        public ComputeShaderParameter computeShaderEASU = new ComputeShaderParameter(null,true);
        public ComputeShaderParameter computeShaderRCAS = new ComputeShaderParameter(null,true);

        [Header("Edge Adaptive Scale Upsampling")]
        [Tooltip("Ultra Quality 1.3, Quality 1.5f, Balanced 1.7f, Performance 2f")]
        public ClampedFloatParameter scaleFactor = new ClampedFloatParameter(1.3f,1.3f,2f);

        [Header("Robust Contrast Adaptive Sharpen")]
        public BoolParameter sharpening = new BoolParameter(true);

        [Tooltip("0 = sharpest, 2 = less sharp")]
        public ClampedFloatParameter sharpness = new ClampedFloatParameter(0.2f,0f,2f);
        public bool IsActive() => scaleFactor.value > 1.3f;

        public bool IsTileCompatible() => false;

        public override void Override(VolumeComponent state, float interpFactor)
        {
            base.Override(state, interpFactor);
        }

        internal void Cleanup()
        {
            CoreUtils.Destroy(computeShaderEASU.value);
            CoreUtils.Destroy(computeShaderRCAS.value);
        }
    }

    [Serializable]
    public sealed class ComputeShaderParameter : VolumeParameter<ComputeShader>
    {
        public ComputeShaderParameter(ComputeShader computeShader,bool overrideState = false):
            base(computeShader,overrideState)
        { value = computeShader; }
    }
}
