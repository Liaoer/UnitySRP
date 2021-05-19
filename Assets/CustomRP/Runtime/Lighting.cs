﻿using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class Lighting 
{
    const string bufferName = "Lighting";

    const int maxDirLightCount = 4;


    static int
		dirLightCountId = Shader.PropertyToID("_DirectionalLightCount"),
		dirLightColorsId = Shader.PropertyToID("_DirectionalLightColors"),
		dirLightDirectionsId = Shader.PropertyToID("_DirectionalLightDirections"),

    dirLightShadowDataId = Shader.PropertyToID("_DirectionalLightShadowData");


    static Vector4[]
		dirLightColors = new Vector4[maxDirLightCount],
		dirLightDirections = new Vector4[maxDirLightCount],
    dirLightShadowData = new Vector4[maxDirLightCount];

    CullingResults cullingResults;

    CommandBuffer buffer = new CommandBuffer 
    {
        name = bufferName
    };

    Shadows shadows = new Shadows();

    public void Setup(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings shadowSettings)
    {
        this.cullingResults = cullingResults;
        buffer.BeginSample(bufferName);
        shadows.Setup(context, cullingResults, shadowSettings);
        SetupLights();
        //SetupDirectionalLight();
        shadows.Render();
        buffer.EndSample(bufferName);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    void SetupLights () 
    {
        NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;
        int dirLightCount = 0;
        for (int i = 0; i < visibleLights.Length; i++) 
        {
          VisibleLight visibleLight = visibleLights[i];
          if (visibleLight.lightType == LightType.Directional) 
          {
            SetupDirectionalLight(dirLightCount++, ref visibleLight);
            if (dirLightCount >= maxDirLightCount) 
            {
                break;
            }
          }
        }
        buffer.SetGlobalInt(dirLightCountId, visibleLights.Length);
        buffer.SetGlobalVectorArray(dirLightColorsId, dirLightColors);
        buffer.SetGlobalVectorArray(dirLightDirectionsId, dirLightDirections);
        buffer.SetGlobalVectorArray(dirLightShadowDataId, dirLightShadowData);
    }

    void SetupDirectionalLight (int index, ref VisibleLight visibleLight) 
    {
        dirLightColors[index] = visibleLight.finalColor;
        //得到光照的forward方向 /GetColumn(0) right 方向 /GetColumn(1) up方向
        dirLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
        dirLightShadowData[index] = shadows.ReserveDirtionalShadows(visibleLight.light, index);
    }

    public void Cleanup()
    {
      shadows.Cleanup();
    }
}
