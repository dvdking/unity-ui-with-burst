using System;
using Unity.Jobs;
using Unity.Mathematics;

namespace UnityEngine.UI
{
  public class NativeMaskableGraphic : MaskableGraphic, INativeRebuild
  {
    public virtual void Rebuild(CanvasUpdate update, Mesh.MeshData meshData)
    {
      if (canvasRenderer == null || canvasRenderer.cull)
        return;

      switch (update)
      {
        case CanvasUpdate.PreRender:
          if (m_VertsDirty)
          {
            // Profiler.BeginSample("Geom");
            UpdateGeometry(meshData);
            m_VertsDirty = false;
            // Profiler.EndSample();
          }

          if (m_MaterialDirty)
          {
            // Profiler.BeginSample("mat");
            UpdateMaterial();
            m_MaterialDirty = false;
            // Profiler.EndSample();
          }

          break;
      }
    }

    protected virtual void UpdateGeometry(Mesh.MeshData meshData)
    {
      LocalVertexHelper ??= new();

      LocalVertexHelper.SetMeshData(meshData);
      // Profiler.BeginSample("Populate");
      if (rectTransform != null && rectTransform.rect is { width: >= 0, height: >= 0 })
        OnPopulateMeshNative(LocalVertexHelper);
      else
        LocalVertexHelper.Clear();

      // Profiler.EndSample();
    }


    protected virtual void OnPopulateMeshNative(NativeVertexHelper vh)
    {
      var v = GetPixelAdjustedRect();
      var uv = new float4(v.x, v.y, v.x + v.width, v.y + v.height);
      
      const int quadCount = 1;
      const int vertCount = 4;
      const int indiciesCount = 6;
      vh.Reinit(quadCount * vertCount, quadCount * indiciesCount);

      var job = new FillSimpleSpriteJob
      {
        Verticies = vh.Vertices,
        Indicies = vh.Indices,
        Dimensions = new(v.xMin, v.yMin, v.xMax, v.yMax),
        Uv = uv,
        Color32 = colorFloat
      };

      job.Run();
    }

    public void SetMesh()
    {
      // Profiler.BeginSample("Set mesh");
      workerMesh.RecalculateBounds();
      canvasRenderer.SetMesh(workerMesh);
      // Profiler.EndSample();
    }

    protected override void OnDestroy()
    {
      base.OnDestroy();
      
      if (s_Mesh)
        Destroy(s_Mesh);

      s_Mesh = null;
      
      LocalVertexHelper?.Dispose();
    }

    public Mesh workerMesh
    {
      get
      {
        if (_localMesh == null)
        {
          _localMesh = new()
          {
            name = "Element UI Mesh",
            hideFlags = HideFlags.HideAndDontSave
          };
        }

        return _localMesh;
      }
    }
    
    [NonSerialized] private Mesh _localMesh;
    public NativeVertexHelper LocalVertexHelper { get; private set; }
  }
}