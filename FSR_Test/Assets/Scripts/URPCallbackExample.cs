using UnityEngine;
using UnityEngine.Rendering;

public class URPCallbackExample : MonoBehaviour
{
    // Unity calls this method automatically when it enables this component
    private void OnEnable()
    {
        // Add WriteLogMessage as a delegate of the RenderPipelineManager.beginCameraRendering event
        RenderPipelineManager.beginCameraRendering += WriteLogMessage;
    }

    // Unity calls this method automatically when it disables this component
    private void OnDisable()
    {
        // Remove WriteLogMessage as a delegate of the  RenderPipelineManager.beginCameraRendering event
        RenderPipelineManager.beginCameraRendering -= WriteLogMessage;
    }

    // When this method is a delegate of RenderPipeline.beginCameraRendering event, Unity calls this method every time it raises the beginCameraRendering event
    void WriteLogMessage(ScriptableRenderContext context, Camera camera)
    {
        // Write text to the console
        Debug.Log($"Beginning rendering the camera: {camera.name}");
    }
}