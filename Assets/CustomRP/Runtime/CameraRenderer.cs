
using UnityEngine;
using UnityEngine.Rendering;

//为方便每一个相机单独订制渲染方案，我们可以为每一个相机自定义一个相机渲染器类
public partial class CameraRenderer
{
    ScriptableRenderContext context;

    Camera camera;

    const string bufferName = "Render Camera";

    static ShaderTagId
		unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit"),
		litShaderTagId = new ShaderTagId("CustomLit");

    CullingResults cullingResults;

    CommandBuffer buffer = new CommandBuffer{
        name = bufferName
    };

    Lighting lighting = new Lighting();

    public void Render (ScriptableRenderContext context, Camera camera,bool useDynamicBatching, bool useGPUInstancing,
    ShadowSettings shadowSettings)
    {
        this.context = context;
        this.camera = camera;
        
        PrepareBuffer();
        PrepareForSceneWindow();
        if(!Cull(shadowSettings.maxDistance))
        {
            return;
        }

        //buffer.BeginSample(SampleName);
        lighting.Setup(context, cullingResults, shadowSettings);
        //buffer.BeginSample(SampleName);
        Setup();
        DrawVisibleGeometry(useDynamicBatching, useGPUInstancing);
        DrawUnsupportedShaders();
        lighting.Cleanup();
        Submit();
    }

    void Setup()
    {
        context.SetupCameraProperties(camera);
        //清除渲染目标
        //buffer.ClearRenderTarget(true, true, Color.clear);
        CameraClearFlags flags = camera.clearFlags;
        buffer.ClearRenderTarget(
            flags <= CameraClearFlags.Depth,
            flags == CameraClearFlags.Color,
            flags == CameraClearFlags.Color ?
                camera.backgroundColor.linear : Color.clear
        );

        //buffer.BeginSample/buffer.EndSample 来注入我们的分析代码，这样在FrameDebuger中我们就能看到我们想分析渲染代码
        buffer.BeginSample(bufferName);
        ExecuteBuffer();
    }

    void DrawVisibleGeometry(bool useDynamicBatching, bool useGPUInstancing)
    {
        var sortingSettings = new SortingSettings(camera){
            criteria = SortingCriteria.CommonOpaque
        };
		var drawingSettings = new DrawingSettings(
			unlitShaderTagId, sortingSettings
		){
            enableDynamicBatching = useDynamicBatching,
			enableInstancing = useGPUInstancing
        };
        drawingSettings.SetShaderPassName(1, litShaderTagId);

        //不透明物体
		var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);

		context.DrawRenderers(
			cullingResults, ref drawingSettings, ref filteringSettings
		);

        //天空盒
        context.DrawSkybox(camera);

        //透明物体(透明材质的物体是不会写入深度缓冲的)
        filteringSettings = new FilteringSettings(RenderQueueRange.transparent);
        context.DrawRenderers(
            cullingResults, ref drawingSettings, ref filteringSettings
        );
    }

    void Submit()
    {
        buffer.EndSample(bufferName);
        ExecuteBuffer();
        context.Submit();
    }

    void ExecuteBuffer () {
		context.ExecuteCommandBuffer(buffer);
		buffer.Clear();
	}

    bool Cull(float maxShadowDistance) 
    {
        if (camera.TryGetCullingParameters(out ScriptableCullingParameters p)) {
            p.shadowDistance = Mathf.Min(maxShadowDistance, camera.farClipPlane);
            cullingResults = context.Cull(ref p);
			return true;
		}
		return false;
    }
}
