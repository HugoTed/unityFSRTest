
namespace UnityEngine.Rendering.Universal
{
    public class MYMaterialLibrary
    {
        public readonly Material invertColor;

        public MYMaterialLibrary(AdditionPostProcessData data)
        {
            invertColor = Load(data.shaders.invertColor);
        }

        private Material Load(Shader shader)
        {
            if (shader == null)
            {
                Debug.LogErrorFormat($"Missing shader. {GetType().DeclaringType.Name} render pass will not execute. Check for missing reference in the renderer resources.");
                return null;
            }
            return shader.isSupported ? CoreUtils.CreateEngineMaterial(shader) : null;
        }
        internal void Cleanup()
        {
            CoreUtils.Destroy(invertColor);
        }
    }
}