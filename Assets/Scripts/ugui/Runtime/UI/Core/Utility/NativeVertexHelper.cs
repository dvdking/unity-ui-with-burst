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
  public class NativeVertexHelper : IDisposable
  {
    public NativeArray<VertexData> m_Vertices;
    public NativeArray<ushort> m_Indices;

    public static readonly float4 s_DefaultTangent = new Vector4(1.0f, 0.0f, 0.0f, -1.0f);
    public static readonly float3 s_DefaultNormal = new float3(0, 0, -1);

    private bool m_ListsInitalized = false;
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
    }
    public void Reinit(int sizeVert, int sizeInd)
    {
      _data.SetVertexBufferParams(sizeVert, _arr.AsArray());
      m_Vertices = _data.GetVertexData<VertexData>();
      _data.SetIndexBufferParams(sizeInd, IndexFormat.UInt16);
      m_Indices = _data.GetIndexData<ushort>();
      m_ListsInitalized = true;
    }

    /// <summary>
    /// Cleanup allocated memory.
    /// </summary>
    public void Dispose()
    {
      _arr.Dispose();
      m_ListsInitalized = false;
    }

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

    /// <summary>
    /// Add a single vertex to the stream.
    /// </summary>
    /// <param name="position">Position of the vert</param>
    /// <param name="color">Color of the vert</param>
    /// <param name="uv0">UV of the vert</param>
    /// <param name="uv1">UV1 of the vert</param>
    /// <param name="uv2">UV2 of the vert</param>
    /// <param name="uv3">UV3 of the vert</param>
    /// <param name="normal">Normal of the vert.</param>
    /// <param name="tangent">Tangent of the vert</param>
    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public void AddVert(float3 position,
      // float4 color,
      // float4 uv0,
      // float4 uv1,
      // float4 uv2,
      // float4 uv3,
      // float3 normal,
      // float4 tangent)
    // {
      // m_Vertices[_currentVertIndex++]= new()
      // {
        // pos = position,
        // normal = normal,
        // tangent = tangent,
        // color = color,
        // uv0 = uv0,
        // uv1 = uv1,
        // uv2 = uv2,
        // uv3 = uv3,
      // };
    // }

    /// <summary>
    /// Add a single vertex to the stream.
    /// </summary>
    /// <param name="position">Position of the vert</param>
    /// <param name="color">Color of the vert</param>
    /// <param name="uv0">UV of the vert</param>
    /// <param name="uv1">UV1 of the vert</param>
    /// <param name="normal">Normal of the vert.</param>
    /// <param name="tangent">Tangent of the vert</param>
    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public void AddVert(float3 position, float4 color, float4 uv0, float4 uv1, float3 normal, float4 tangent) => AddVert(position, color, uv0, uv1, Vector4.zero, Vector4.zero, normal, tangent);

    /// <summary>
    /// Add a single vertex to the stream.
    /// </summary>
    /// <param name="position">Position of the vert</param>
    /// <param name="color">Color of the vert</param>
    /// <param name="uv0">UV of the vert</param>
    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public void AddVert(float3 position, float4 color, float4 uv0) => AddVert(position, color, uv0, Vector4.zero, s_DefaultNormal, s_DefaultTangent);

    /// <summary>
    /// Add a triangle to the buffer.
    /// </summary>
    /// <param name="idx0">index 0</param>
    /// <param name="idx1">index 1</param>
    /// <param name="idx2">index 2</param>
    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public void AddTriangle(ushort idx0, ushort idx1, ushort idx2)
    // {
      // m_Indices[_currentIndicesIndex++] = idx0;
      // m_Indices[_currentIndicesIndex++] = idx1;
      // m_Indices[_currentIndicesIndex++] = idx2;
    // }
  }
}