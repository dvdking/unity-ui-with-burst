using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace UnityEngine.UI
{
  public static class FillVertexUtil
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NativeVertexHelper.VertexData CreateVert(float3 pos,
      float4 color,
      float4 uv)
    {
      return new()
      {
        pos = pos,
        normal = NativeVertexHelper.DefaultNormal,
        tangent = NativeVertexHelper.DefaultTangent,
        color = color,
        uv0 = uv
        // uv1 = new float4(uvMax.x, uvMax.y, 0, 0),
        // uv2 = new float4(uvMax.x, uvMax.y, 0, 0),
        // uv3 = new float4(uvMax.x, uvMax.y, 0, 0),
      };
    }
  }
}