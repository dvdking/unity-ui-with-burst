using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace UnityEngine.UI
{
  //todo: no fill center job
  [BurstCompile(FloatPrecision.Low, FloatMode.Fast)]
  public struct FillSlicedSpriteJob : IJob
  {
    [NoAlias] 
    [NativeDisableContainerSafetyRestriction]
    public NativeArray<VertexHelper.VertexData> Verticies;

    [NoAlias] 
    [NativeDisableContainerSafetyRestriction]
    public NativeArray<ushort> Indicies;

    [ReadOnly] [NoAlias] public NativeArray<float2> s_VertScratch;

    [NoAlias] [ReadOnly] public NativeArray<float2> s_UVScratch;

    [ReadOnly] public float4 Color;
    [ReadOnly] public bool FillCenter;

    public void Execute()
    {
      AddQuad(0, 0, 0, 1);
      AddQuad(1, 1, 0, 1);
      AddQuad(2, 2, 0, 1);
      AddQuad(3, 0, 1, 2);
      if (FillCenter)
      {
        AddQuad(4, 1, 1, 2);
        AddQuad(5, 2, 1, 2);
        AddQuad(6, 0, 2, 3);
        AddQuad(7, 1, 2, 3);
        AddQuad(8, 2, 2, 3);
      }
      else
      {
        AddQuad(4, 2, 1, 2);
        AddQuad(5, 0, 2, 3);
        AddQuad(6, 1, 2, 3);
        AddQuad(7, 2, 2, 3);
      }
    }

    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AddQuad(ushort startIndex, int y, int x, int x2)
    {
      int y2 = y + 1;
      var vertIndex = (ushort)(startIndex * 4);
      var indIndex = (ushort)(startIndex * 6);

      var posMin = new float2(s_VertScratch[x].x, s_VertScratch[y].y);
      var posMax = new float2(s_VertScratch[x2].x, s_VertScratch[y2].y);
      var uvMin = new float2(s_UVScratch[x].x, s_UVScratch[y].y);
      var uvMax = new float2(s_UVScratch[x2].x, s_UVScratch[y2].y);

      Verticies[vertIndex + 0] = (FillVertexUtil.CreateVert(new(posMin.x, posMin.y, 0), Color, new(uvMin.x, uvMin.y, 0, 0)));
      Verticies[vertIndex + 1] = (FillVertexUtil.CreateVert(new(posMin.x, posMax.y, 0), Color, new(uvMin.x, uvMax.y, 0, 0)));
      Verticies[vertIndex + 2] = (FillVertexUtil.CreateVert(new(posMax.x, posMax.y, 0), Color, new(uvMax.x, uvMax.y, 0, 0)));
      Verticies[vertIndex + 3] = (FillVertexUtil.CreateVert(new(posMax.x, posMin.y, 0), Color, new(uvMax.x, uvMin.y, 0, 0)));

      Indicies[indIndex + 0] = (vertIndex);
      Indicies[indIndex + 1] = (ushort)(vertIndex + 1);
      Indicies[indIndex + 2] = (ushort)(vertIndex + 2);

      Indicies[indIndex + 3] = (ushort)(vertIndex + 2);
      Indicies[indIndex + 4] = (ushort)(vertIndex + 3);
      Indicies[indIndex + 5] = vertIndex;
    }
  }
}