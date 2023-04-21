using System.Runtime.CompilerServices;
using System.Xml;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace UnityEngine.UI
{
  [BurstCompile(FloatPrecision.Low, FloatMode.Fast)]
  public struct FillFilledSpriteJob : IJob
  {
    [NoAlias] [NativeDisableContainerSafetyRestriction]
    public NativeArray<NativeVertexHelper.VertexData> Verticies;

    [NoAlias] [NativeDisableContainerSafetyRestriction]
    public NativeArray<ushort> Indicies;

    public Image.FillMethod FillMethod;
    public float FillAmount;
    public int FillOrigin;
    public float4 Outer;
    public float4 Dimensions;
    public float4 Color32;
    public bool FillClockwise;


    public void Execute()
    {
      var tx0 = Outer.x;
      var ty0 = Outer.y;
      var tx1 = Outer.z;
      var ty1 = Outer.w;

      // Horizontal and vertical filled sprites are simple -- just end the Image prematurely
      if (FillMethod is Image.FillMethod.Horizontal or Image.FillMethod.Vertical)
      {
        if (FillMethod == Image.FillMethod.Horizontal)
        {
          float fill = (tx1 - tx0) * FillAmount;

          if (FillOrigin == 1)
          {
            Dimensions.x = Dimensions.z - (Dimensions.z - Dimensions.x) * FillAmount;
            tx0 = tx1 - fill;
          }
          else
          {
            Dimensions.z = Dimensions.x + (Dimensions.z - Dimensions.x) * FillAmount;
            tx1 = tx0 + fill;
          }
        }
        else if (FillMethod == Image.FillMethod.Vertical)
        {
          float fill = (ty1 - ty0) * FillAmount;

          if (FillOrigin == 1)
          {
            Dimensions.y = Dimensions.w - (Dimensions.w - Dimensions.y) * FillAmount;
            ty0 = ty1 - fill;
          }
          else
          {
            Dimensions.w = Dimensions.y + (Dimensions.w - Dimensions.y) * FillAmount;
            ty1 = ty0 + fill;
          }
        }
      }

      var s_Xy = new NativeArray<float3>(4, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
      var s_Uv = new NativeArray<float4>(4, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

      s_Xy[0] = new(Dimensions.x, Dimensions.y, 0);
      s_Xy[1] = new(Dimensions.x, Dimensions.w, 0);
      s_Xy[2] = new(Dimensions.z, Dimensions.w, 0);
      s_Xy[3] = new(Dimensions.z, Dimensions.y, 0);

      s_Uv[0] = new(tx0, ty0, 0, 0);
      s_Uv[1] = new(tx0, ty1, 0, 0);
      s_Uv[2] = new(tx1, ty1, 0, 0);
      s_Uv[3] = new(tx1, ty0, 0, 0);

      {
        if (FillAmount < 1f && FillMethod != Image.FillMethod.Horizontal
                            && FillMethod != Image.FillMethod.Vertical)
        {
          if (FillMethod == Image.FillMethod.Radial90)
          {
            RadialCut(s_Xy, s_Uv, FillAmount, FillClockwise, FillOrigin);
            AddQuad(0, s_Xy, s_Uv);
          }
          else if (FillMethod == Image.FillMethod.Radial180)
          {
            for (int side = 0; side < 2; ++side)
            {
              float fx0, fx1, fy0, fy1;
              int even = FillOrigin > 1 ? 1 : 0;

              if (FillOrigin is 0 or 2)
              {
                fy0 = 0f;
                fy1 = 1f;
                if (side == even)
                {
                  fx0 = 0f;
                  fx1 = 0.5f;
                }
                else
                {
                  fx0 = 0.5f;
                  fx1 = 1f;
                }
              }
              else
              {
                fx0 = 0f;
                fx1 = 1f;
                if (side == even)
                {
                  fy0 = 0.5f;
                  fy1 = 1f;
                }
                else
                {
                  fy0 = 0f;
                  fy1 = 0.5f;
                }
              }

              var xy0 = s_Xy[0];
              xy0.x = math.lerp(Dimensions.x, Dimensions.z, fx0);
              var xy1 = s_Xy[1];
              xy1.x = xy0.x;
              var xy2 = s_Xy[2];
              xy2.x = math.lerp(Dimensions.x, Dimensions.z, fx1);
              var xy3 = s_Xy[3];
              xy3.x = xy2.x;

              xy0.y = math.lerp(Dimensions.y, Dimensions.w, fy0);
              xy1.y = math.lerp(Dimensions.y, Dimensions.w, fy1);
              xy2.y = xy1.y;
              xy3.y = xy0.y;

              var uv0 = s_Uv[0];
              uv0.x = math.lerp(tx0, tx1, fx0);
              var uv1 = s_Uv[1];
              uv1.x = uv0.x;
              var uv2 = s_Uv[2];
              uv2.x = math.lerp(tx0, tx1, fx1);
              var uv3 = s_Uv[3];
              uv3.x = uv2.x;

              uv0.y = math.lerp(ty0, ty1, fy0);
              uv1.y = math.lerp(ty0, ty1, fy1);
              uv2.y = uv1.y;
              uv3.y = uv0.y;

              s_Xy[0] = xy0;
              s_Xy[1] = xy1;
              s_Xy[2] = xy2;
              s_Xy[3] = xy3;

              s_Uv[0] = uv0;
              s_Uv[1] = uv1;
              s_Uv[2] = uv2;
              s_Uv[3] = uv3;


              var val = FillClockwise ? FillAmount * 2f - side : FillAmount * 2f - (1 - side);
              RadialCut(s_Xy, s_Uv, Mathf.Clamp01(val), FillClockwise, ((side + FillOrigin + 3) % 4));
              AddQuad((ushort)side, s_Xy, s_Uv);
            }
          }
          else if (FillMethod == Image.FillMethod.Radial360)
          {
            for (int corner = 0; corner < 4; ++corner)
            {
              float fx0, fx1, fy0, fy1;

              if (corner < 2)
              {
                fx0 = 0f;
                fx1 = 0.5f;
              }
              else
              {
                fx0 = 0.5f;
                fx1 = 1f;
              }

              if (corner is 0 or 3)
              {
                fy0 = 0f;
                fy1 = 0.5f;
              }
              else
              {
                fy0 = 0.5f;
                fy1 = 1f;
              }

              var sxy0 = s_Xy[0];
              sxy0.x = math.lerp(Dimensions.x, Dimensions.z, fx0);
              var sxy1 = s_Xy[1];
              sxy1.x = sxy0.x;
              var sxy2 = s_Xy[2];
              sxy2.x = math.lerp(Dimensions.x, Dimensions.z, fx1);
              var sxy3 = s_Xy[3];
              sxy3.x = sxy2.x;

              sxy0.y = math.lerp(Dimensions.y, Dimensions.w, fy0);
              sxy1.y = math.lerp(Dimensions.y, Dimensions.w, fy1);
              sxy2.y = sxy1.y;
              sxy3.y = sxy0.y;

              var suv0 = s_Uv[0];
              suv0.x = math.lerp(tx0, tx1, fx0);
              var suv1 = s_Uv[1];
              suv1.x = suv0.x;
              var suv2 = s_Uv[2];
              suv2.x = math.lerp(tx0, tx1, fx1);
              var suv3 = s_Uv[3];
              suv3.x = suv2.x;

              suv0.y = math.lerp(ty0, ty1, fy0);
              suv1.y = math.lerp(ty0, ty1, fy1);
              suv2.y = suv1.y;
              suv3.y = suv0.y;
              
              s_Xy[0] = sxy0;
              s_Xy[1] = sxy1;
              s_Xy[2] = sxy2;
              s_Xy[3] = sxy3;
              
              s_Uv[0] = suv0;
              s_Uv[1] = suv1;
              s_Uv[2] = suv2;
              s_Uv[3] = suv3;

              float val = FillClockwise
                ? FillAmount * 4f - ((corner + FillOrigin) % 4)
                : FillAmount * 4f - (3 - ((corner + FillOrigin) % 4));

              RadialCut(s_Xy, s_Uv, Mathf.Clamp01(val), FillClockwise, ((corner + 2) % 4));
              AddQuad((ushort)corner, s_Xy, s_Uv);
            }
          }
        }
        else
        {
          AddQuad(0, s_Xy, s_Uv);
        }

        s_Xy.Dispose();
        s_Uv.Dispose();
      }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AddQuad(ushort startIndex, NativeArray<float3> xy, NativeArray<float4> uv)
    {
      var vertIndex = (ushort)(startIndex * 4);
      var indIndex = (ushort)(startIndex * 6);

      Verticies[vertIndex + 0] = FillVertexUtil.CreateVert(xy[0], Color32, uv[0]);
      Verticies[vertIndex + 1] = FillVertexUtil.CreateVert(xy[1], Color32, uv[1]);
      Verticies[vertIndex + 2] = FillVertexUtil.CreateVert(xy[2], Color32, uv[2]);
      Verticies[vertIndex + 3] = FillVertexUtil.CreateVert(xy[3], Color32, uv[3]);

      Indicies[indIndex + 0] = (vertIndex);
      Indicies[indIndex + 1] = (ushort)(vertIndex + 1);
      Indicies[indIndex + 2] = (ushort)(vertIndex + 2);

      Indicies[indIndex + 3] = (ushort)(vertIndex + 2);
      Indicies[indIndex + 4] = (ushort)(vertIndex + 3);
      Indicies[indIndex + 5] = vertIndex;
    }

    static void RadialCut(NativeArray<float3> xy, NativeArray<float4> uv, float fill, bool invert, int corner)
    {
      if (fill < 0.0001f)
      {
        xy[0] = 0;
        xy[1] = 0;
        xy[2] = 0;
        xy[3] = 0;

        return;
      }

      // Even corners invert the fill direction
      if ((corner & 1) == 1)
        invert = !invert;

      // Nothing to adjust
      if (!invert && fill > 0.999f)
        return;

      // Convert 0-1 value into 0 to 90 degrees angle in radians
      var angle = fill;
      if (invert)
        angle = 1f - angle;
      angle *= 90f * math.PI / 180f;

      // Calculate the effective X and Y factors
      var cos = math.cos(angle);
      var sin = math.sin(angle);

      RadialCut(xy, uv, cos, sin, invert, corner);
    }

    static void RadialCut(NativeArray<float3> xy, NativeArray<float4> uv, float cos, float sin, bool invert, int corner)
    {
      int i0 = corner;
      int i1 = ((corner + 1) % 4);
      int i2 = ((corner + 2) % 4);
      int i3 = ((corner + 3) % 4);
      
      var xy0 = xy[i0];
      var xy1 = xy[i1];
      var xy2 = xy[i2];
      var xy3 = xy[i3];
      
      var uv0 = uv[i0];
      var uv1 = uv[i1];
      var uv2 = uv[i2];
      var uv3 = uv[i3];

      if ((corner & 1) == 1)
      {
        if (sin > cos)
        {
          cos /= sin;
          sin = 1f;

          if (invert)
          {
            xy1.x = math.lerp(xy0.x, xy2.x, cos);
            xy2.x = xy1.x;
            
            uv1.x = math.lerp(uv0.x, uv2.x, cos);
            uv2.x = uv1.x;
          }
        }
        else if (cos > sin)
        {
          sin /= cos;
          cos = 1f;

          if (!invert)
          {
            xy2.y = math.lerp(xy0.y, xy2.y, sin);
            xy3.y = xy2.y;
            
            uv2.y = math.lerp(uv0.y, uv2.y, sin);
            uv3.y = uv2.y;
          }
        }
        else
        {
          cos = 1f;
          sin = 1f;
        }

        if (!invert)
        {
          xy3.x = math.lerp(xy0.x, xy2.x, cos);
          uv3.x = math.lerp(uv0.x, uv2.x, cos);
        }
        else
        {
          xy1.y = math.lerp(xy0.y, xy2.y, sin);
          uv1.y = math.lerp(uv0.y, uv2.y, sin);
        }
      }
      else
      {
        if (cos > sin)
        {
          sin /= cos;
          cos = 1f;

          if (!invert)
          {
            xy1.y = math.lerp(xy0.y, xy2.y, sin);
            xy2.y = xy1.y;
            
            uv1.y = math.lerp(uv0.y, uv2.y, sin);
            uv2.y = uv1.y;
          }
        }
        else if (sin > cos)
        {
          cos /= sin;
          sin = 1f;

          if (invert)
          {
            xy2.x = math.lerp(xy0.x, xy2.x, cos);
            xy3.x = xy2.x;
            
            uv2.x = math.lerp(uv0.x, uv2.x, cos);
            uv3.x = uv2.x;
          }
        }
        else
        {
          cos = 1f;
          sin = 1f;
        }

        if (invert)
        {
          xy3.y = math.lerp(xy0.y, xy2.y, sin);
          uv3.y = math.lerp(uv0.y, uv2.y, sin);
        }
        else
        {
          xy1.x = math.lerp(xy0.x, xy2.x, cos);
          uv1.x = math.lerp(uv0.x, uv2.x, cos);
        }
      }
      
      xy[i0] = xy0;
      xy[i1] = xy1;
      xy[i2] = xy2;
      xy[i3] = xy3;
      
      uv[i0] = uv0;
      uv[i1] = uv1;
      uv[i2] = uv2;
      uv[i3] = uv3;
    }
  }
}