using UnityEngine;
using UnityEngine.Rendering;

public class Shadows
{
    const string bufferName = "Shadows";
    CommandBuffer buffer = new CommandBuffer
    {
        name = bufferName
    };

    ScriptableRenderContext context;

    CullingResults cullingResults;

    ShadowSettings settings;

    const int maxShadowedDirectionalLightCount = 4;

    int ShadowedDirectionalLightCount;

    static int dirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas"),
    dirShadowMatricesId= Shader.PropertyToID("_DirectionalShadowMatrices");

    static Matrix4x4[] dirShadowMatrices = new Matrix4x4[maxShadowedDirectionalLightCount];

    struct ShadowedDirectionalLight
    {
        public int visibleLightIndex;
    }
    ShadowedDirectionalLight[] ShadowedDirectionalLights = new ShadowedDirectionalLight[maxShadowedDirectionalLightCount];
    public void Setup(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings settings)
    {
        this.context = context;
        this.cullingResults = cullingResults;
        this.settings = settings;
        ShadowedDirectionalLightCount = 0;
    }

    void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    public void ReserveDirtionalShadows(Light light, int visibleLightIndex)
    {
        if(ShadowedDirectionalLightCount < maxShadowedDirectionalLightCount && light.shadows != LightShadows.None && light.shadowStrength > 0f &&
        cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b))
        {
            ShadowedDirectionalLights[ShadowedDirectionalLightCount++] = new ShadowedDirectionalLight{
                visibleLightIndex = visibleLightIndex
            };
        }
    }

    public void Render()
    {
        if(ShadowedDirectionalLightCount > 0)
        {
            RenderDirectionalShadows();
        }
        else
        {
            buffer.GetTemporaryRT(dirShadowAtlasId, 1, 1, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        }
    }

    void RenderDirectionalShadows()
    {
        int atlasSize = (int)settings.directional.atlasSize;
        int split = ShadowedDirectionalLightCount <= 1 ? 1 : 2;
        int titleSize =  atlasSize / split;
        buffer.GetTemporaryRT(dirShadowAtlasId, atlasSize, atlasSize, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        buffer.SetRenderTarget(dirShadowAtlasId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        buffer.ClearRenderTarget(true, false, Color.clear);
        buffer.BeginSample(bufferName);
        ExecuteBuffer();

        for(int i=0; i < ShadowedDirectionalLightCount; i++)
        {
            RenderDirectionalShadows(i, split, atlasSize);
        }

        buffer.EndSample(bufferName);
        ExecuteBuffer();
    }

    void RenderDirectionalShadows (int index, int split, int tileSize) 
    {
        ShadowedDirectionalLight light = ShadowedDirectionalLights[index];
        var shadowSettings = new ShadowDrawingSettings(cullingResults, light.visibleLightIndex);

        cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
            light.visibleLightIndex, 0, 1, Vector3.zero, tileSize, 0f,
            out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, 
            out ShadowSplitData splitData
        );
        shadowSettings.splitData = splitData;
        SetTitleViewport(index, split, tileSize);
        buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
        ExecuteBuffer();
        context.DrawShadows(ref shadowSettings);
    }

    void SetTitleViewport(int index, int split, float titleSize)
    {
        Vector2 offset = new Vector2(index % split, index /  split);
        buffer.SetViewport(new Rect(offset.x * titleSize, offset.y  * titleSize,  titleSize, titleSize));
    }

    public void Cleanup()
    {
        if(ShadowedDirectionalLightCount>0)
        {
            buffer.ReleaseTemporaryRT(dirShadowAtlasId);
            ExecuteBuffer();
        }
    }

}