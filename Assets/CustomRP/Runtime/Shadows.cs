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

    public Vector2 ReserveDirtionalShadows(Light light, int visibleLightIndex)
    {
        if(ShadowedDirectionalLightCount < maxShadowedDirectionalLightCount && light.shadows != LightShadows.None && light.shadowStrength > 0f &&
        cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b))
        {
            ShadowedDirectionalLights[ShadowedDirectionalLightCount] = new ShadowedDirectionalLight{
                visibleLightIndex = visibleLightIndex
            };
            return new Vector2(light.shadowStrength, ShadowedDirectionalLightCount++);
        }
        return Vector2.zero;
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
        buffer.SetGlobalMatrixArray(dirShadowMatricesId, dirShadowMatrices);
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
        // SetTitleViewport(index, split, tileSize);
        dirShadowMatrices[index] = ConvertToAtlasMatrix(projectionMatrix * viewMatrix, SetTitleViewport(index, split, tileSize), split);
        buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
        ExecuteBuffer();
        context.DrawShadows(ref shadowSettings);
    }

    Vector2 SetTitleViewport(int index, int split, float titleSize)
    {
        Vector2 offset = new Vector2(index % split, index /  split);
        buffer.SetViewport(new Rect(offset.x * titleSize, offset.y  * titleSize,  titleSize, titleSize));
        return offset;
    }

    public void Cleanup()
    {
        if(ShadowedDirectionalLightCount>0)
        {
            buffer.ReleaseTemporaryRT(dirShadowAtlasId);
            ExecuteBuffer();
        }
    }

    Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 m, Vector2 offset, int split)
    {
        ////检查平台是否zBuffer反转,一般情况下，z轴方向是朝屏幕内，即近小远大。但是在zBuffer反转的情况下，z轴是朝屏幕外，即近大远小。
        if(SystemInfo.usesReversedZBuffer)
        {
            m.m20 = -m.m20;
            m.m21 = -m.m21;
            m.m22 = -m.m22;
            m.m23 = -m.m23;
        }
        //得到的投影矩阵，其对应的x,y,z范围分别为均为(-1,1).因此我们需要构造坐标变换矩阵，可以将世界坐标转换到ShadowMap齐次坐标空间。对应的xy范围为(0,1),z范围为(1,0)

        float scale = 1f / split;
        m.m00 = (0.5f * (m.m00 + m.m30) + offset.x * m.m30) * scale;
        m.m01 = (0.5f * (m.m01 + m.m31) + offset.x * m.m31) * scale;
        m.m02 = (0.5f * (m.m02 + m.m32) + offset.x * m.m32) * scale;
        m.m03 = (0.5f * (m.m03 + m.m33) + offset.x * m.m33) * scale;

        m.m10 = (0.5f * (m.m10 + m.m30) + offset.x * m.m30) * scale;
        m.m11 = (0.5f * (m.m11 + m.m31) + offset.x * m.m31) * scale;
        m.m12 = (0.5f * (m.m12 + m.m32) + offset.x * m.m32) * scale;
        m.m13 = (0.5f * (m.m13 + m.m33) + offset.x * m.m33) * scale;

        m.m20 = 0.5f * (m.m20 + m.m30);
        m.m21 = 0.5f * (m.m21 + m.m31);
        m.m22 = 0.5f * (m.m22 + m.m32);
        m.m23 = 0.5f * (m.m23 + m.m33);


        return m;
    }

}