using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace UnityEngine.UI
{
  [BurstCompile(FloatPrecision.Low, FloatMode.Fast)]
  public struct FillSimpleSpriteJob : IJob
  {
    [NoAlias] [NativeDisableContainerSafetyRestriction]
    public NativeArray<NativeVertexHelper.VertexData> Verticies;

    [NoAlias] [NativeDisableContainerSafetyRestriction]
    public NativeArray<ushort> Indicies;

    [ReadOnly]
    public float4 Uv;
      
    [ReadOnly]
    public float4 Color32;
      
    [ReadOnly]
    public float4 Dimensions;

    public void Execute()
    {
      Verticies[0] = FillVertexUtil.CreateVert(new Vector3(Dimensions.x, Dimensions.y), Color32, new(Uv.x, Uv.y, 0, 0));
      Verticies[1] = FillVertexUtil.CreateVert(new Vector3(Dimensions.x, Dimensions.w), Color32, new(Uv.x, Uv.w, 0, 0));
      Verticies[2] = FillVertexUtil.CreateVert(new Vector3(Dimensions.z, Dimensions.w), Color32, new(Uv.z, Uv.w, 0, 0));
      Verticies[3] = FillVertexUtil.CreateVert(new Vector3(Dimensions.z, Dimensions.y), Color32, new(Uv.z, Uv.y, 0, 0));

      Indicies[0] = 0;
      Indicies[1] = 1;
      Indicies[2] = 2;

      Indicies[3] = 2;
      Indicies[4] = 3;
      Indicies[5] = 0;
    }
  }
}