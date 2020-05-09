using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/Custom Render Pipeline")]
public class CustomRenderPipelineAsset : RenderPipelineAsset
{
    //创建自定义的管线实例
   protected override RenderPipeline CreatePipeline()
   {
       return new CustomRenderPipeline();
   }
}
