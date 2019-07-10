using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.LWRP;
/*
public class OutlinePassImpl : ScriptableRenderPass
{
    private Material outlineMaterial;
    
    private FilterRenderersSettings m_OutlineFilterSettings;
    private int OutlineColorId;

    public OutlinePassImpl(Color outlineColor)
    {
        // Must match the shader pass tag hanging on the object, as in the shader
        // SimpleColor
        RegisterShaderPassName("LightweightForward");
        // Corresponds to the name of the outline shader above
        outlineMaterial = CoreUtils.CreateEngineMaterial("Unlit / SimpleOutline");

        OutlineColorId = Shader.PropertyToID("_ OutlineColor");
        outlineMaterial.SetColor(OutlineColorId, outlineColor);

        m_OutlineFilterSettings = new FilterRenderersSettings(true)
        {
            renderQueueRange = RenderQueueRange.opaque,
        };
    }

    public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData)
    {
        Camera camera = renderingData.cameraData.camera;

        SortFlags sortFlags = renderingData.cameraData.defaultOpaqueSortFlags;

        // Create settings for rendering for the current camera
        DrawRendererSettings drawSettings = CreateDrawRendererSettings(camera, sortFlags, RendererConfiguration.None,
            renderingData.supportsDynamicBatching);

        drawSettings.SetOverrideMaterial(outlineMaterial, 0);

        context.DrawRenderers(renderingData.cullResults.visibleRenderers, ref drawSettings,
            m_OutlineFilterSettings);
    }
}
*/