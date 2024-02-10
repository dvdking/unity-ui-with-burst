using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine.Pool;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace UnityEngine.UI
{
  public class NativeVertexHelper 
  {
    public NativeArray<VertexData> Vertices;
    public NativeArray<ushort> Indices;

    public static readonly float4 DefaultTangent = new Vector4(1.0f, 0.0f, 0.0f, -1.0f);
    public static readonly float3 DefaultNormal = new float3(0, 0, -1);
    public int IndiciesLength;

    private static VertexAttributeDescriptor[] _layout;

    public NativeVertexHelper()
    {
      _arr = new(8, Allocator.Persistent)
      {
        new(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
        new(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3),
        new(VertexAttribute.Tangent, VertexAttributeFormat.Float32, 4),
        new(VertexAttribute.Color, VertexAttributeFormat.Float32, 4),
        new(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 4),
        new(VertexAttribute.TexCoord1, VertexAttributeFormat.Float32, 4),
        new(VertexAttribute.TexCoord2, VertexAttributeFormat.Float32, 4),
        new(VertexAttribute.TexCoord3, VertexAttributeFormat.Float32, 4),
      };
    }

    public void SetMeshData(Mesh.MeshData meshData)
    {
      _data = meshData;
    }

    public void Clear()
    {
      Reinit(0, 0);
      IndiciesLength = 0;
    }
    public void Reinit(int sizeVert, int sizeInd)
    {
      _data.SetVertexBufferParams(sizeVert, _arr);
      Vertices = _data.GetVertexData<VertexData>();
      _data.SetIndexBufferParams(sizeInd, IndexFormat.UInt16);
      Indices = _data.GetIndexData<ushort>();
      IndiciesLength = sizeInd;
    }

    public void Dispose() => _arr.Dispose();

    [StructLayout(LayoutKind.Sequential)]
    public struct VertexData
    {
      public float3 pos;
      public float3 normal;
      public float4 tangent;
      public float4 color;
      public float4 uv0;
      
      [UsedImplicitly]
      public float4 uv1;
      [UsedImplicitly]
      public float4 uv2;
      [UsedImplicitly]
      public float4 uv3;
    }

    private Mesh.MeshDataArray _dataArray;
    private Mesh.MeshData _data;

    private NativeList<VertexAttributeDescriptor> _arr;
  }
}