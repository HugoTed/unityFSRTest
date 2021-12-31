using System;


namespace UnityEngine.Rendering.Universal
{
    [Serializable, VolumeComponentMenu("Post-processing/Grayscale")]
    public sealed class Grayscale : VolumeComponent, IPostProcessComponent
    {
        [Range(0f, 1f), Tooltip("Grayscale effect intensity.")]
        public FloatParameter blend = new FloatParameter(0.5f);



        public bool IsActive() => blend.value > 0f;

        public bool IsTileCompatible() => false;

        public override void Override(VolumeComponent state, float interpFactor)
        {
            base.Override(state, interpFactor);
        }
    }
}

