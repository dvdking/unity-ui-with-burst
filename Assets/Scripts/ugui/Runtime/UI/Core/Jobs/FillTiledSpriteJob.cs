using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace UnityEngine.UI
{
  [BurstCompile(FloatPrecision.Low, FloatMode.Fast)]
  public struct FillTiledSpriteJob : IJob
  {
    [NoAlias] [NativeDisableContainerSafetyRestriction]
    public NativeArray<NativeVertexHelper.VertexData> Verticies;

    [NoAlias] [NativeDisableContainerSafetyRestriction]
    public NativeArray<ushort> Indicies;

    public float4 Color32;
    
    public float xMin;
    public float xMax;
    public float yMin;
    public float yMax;
    
    public float2 uvMin;
    public float2 uvMax;

    public float tileWidth;
    public float tileHeight;

    public float2 position;

    public void Execute()
    {
      var uvScale = new float2((xMax - xMin) / tileWidth, (yMax - yMin) / tileHeight);

      var posMin = new float2(xMin, yMin) + position;
      var posMax = new float2(xMax, yMax) + position;
      
      uvMin = math.mul(uvMin, uvScale);
      uvMax = math.mul(uvMax, uvScale);

      Verticies[0] = FillVertexUtil.CreateVert(new Vector3(posMin.x, posMin.y), Color32, new(uvMin.x, uvMin.y, 0, 0));
      Verticies[1] = FillVertexUtil.CreateVert(new Vector3(posMin.x, posMax.y), Color32, new(uvMin.x, uvMax.y, 0, 0));
      Verticies[2] = FillVertexUtil.CreateVert(new Vector3(posMax.x, posMax.y), Color32, new(uvMax.x, uvMax.y, 0, 0));
      Verticies[3] = FillVertexUtil.CreateVert(new Vector3(posMax.x, posMin.y), Color32, new(uvMax.x, uvMin.y, 0, 0));

      Indicies[0] = 0;
      Indicies[1] = 1;
      Indicies[2] = 2;

      Indicies[3] = 2;
      Indicies[4] = 3;
      Indicies[5] = 0;
    }
  }
}