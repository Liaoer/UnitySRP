using UnityEngine;
using UnityEngine.Rendering;

//创建pipeline实例
public class CustomRenderPipeline : RenderPipeline
{
    CameraRenderer renderer = new CameraRenderer();
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        foreach (Camera camera in cameras)
        {
            renderer.Render(context, camera);
        }
    }

    public CustomRenderPipeline () {
		GraphicsSettings.useScriptableRenderPipelineBatching = true;
	}
}
