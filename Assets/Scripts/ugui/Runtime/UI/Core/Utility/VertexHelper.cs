using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Pool;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace UnityEngine.UI
{
  /// <summary>
  /// A utility class that can aid in the generation of meshes for the UI.
  /// </summary>
  /// <remarks>
  /// This class implements IDisposable to aid with memory management.
  /// </remarks>
  /// <example>
  /// <code>
  /// <![CDATA[
  /// using UnityEngine;
  /// using UnityEngine.UI;
  ///
  /// public class ExampleClass : MonoBehaviour
  /// {
  ///     Mesh m;
  ///
  ///     void Start()
  ///     {
  ///         Color32 color32 = Color.red;
  ///         using (var vh = new VertexHelper())
  ///         {
  ///             vh.AddVert(new Vector3(0, 0), color32, new Vector2(0f, 0f));
  ///             vh.AddVert(new Vector3(0, 100), color32, new Vector2(0f, 1f));
  ///             vh.AddVert(new Vector3(100, 100), color32, new Vector2(1f, 1f));
  ///             vh.AddVert(new Vector3(100, 0), color32, new Vector2(1f, 0f));
  ///
  ///             vh.AddTriangle(0, 1, 2);
  ///             vh.AddTriangle(2, 3, 0);
  ///             vh.FillMesh(m);
  ///         }
  ///     }
  /// }
  /// ]]>
  ///</code>
  /// </example>
  public unsafe class VertexHelper : IDisposable
  {
    // private NativeList<float3> m_Positions;
    // private NativeList<float4> m_Colors;
    // private NativeList<float4> m_Uv0S;
    // private NativeList<float4> m_Uv1S;
    // private NativeList<float4> m_Uv2S;
    // private NativeList<float4> m_Uv3S;
    // private NativeList<float3> m_Normals;
    // private NativeList<float4> m_Tangents;
    public NativeArray<VertexData> m_Vertices;

    public NativeArray<uint> m_Indices;

    public static readonly float4 s_DefaultTangent = new Vector4(1.0f, 0.0f, 0.0f, -1.0f);
    public static readonly float3 s_DefaultNormal = new float3(0, 0, -1);

    private bool m_ListsInitalized = false;
    private static VertexAttributeDescriptor[] _layout;

    public VertexHelper()
    {
      _dataArray = Mesh.AllocateWritableMeshData(1);
      _data = _dataArray[0];

      _arr = new NativeList<VertexAttributeDescriptor>(8, Allocator.Persistent)
      {
        new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
        new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3),
        new VertexAttributeDescriptor(VertexAttribute.Tangent, VertexAttributeFormat.Float32, 4),
        new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.Float32, 4),
        new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 4),
        new VertexAttributeDescriptor(VertexAttribute.TexCoord1, VertexAttributeFormat.Float32, 4),
        new VertexAttributeDescriptor(VertexAttribute.TexCoord2, VertexAttributeFormat.Float32, 4),
        new VertexAttributeDescriptor(VertexAttribute.TexCoord3, VertexAttributeFormat.Float32, 4),
      };
    }

    public void Reinit(int size)
    {
      if (!m_ListsInitalized)
      {
        _data.SetVertexBufferParams(64, _arr.AsArray());
        _data.SetIndexBufferParams(128, IndexFormat.UInt32);

        m_Vertices = _data.GetVertexData<VertexData>();
        m_Indices = _data.GetIndexData<uint>();
        // m_Positions = new(Allocator.Persistent);
        // m_Colors = new(Allocator.Persistent);
        // m_Uv0S = new(Allocator.Persistent);
        // m_Uv1S = new(Allocator.Persistent);
        // m_Uv2S = new(Allocator.Persistent);
        // m_Uv3S = new(Allocator.Persistent);
        // m_Normals = new(Allocator.Persistent);
        // m_Tangents = new(Allocator.Persistent);
        // m_Vertices = new(Allocator.Persistent);
        // m_Indices = new(Allocator.Persistent);
        m_ListsInitalized = true;
      }
    }

    /// <summary>
    /// Cleanup allocated memory.
    /// </summary>
    public void Dispose()
    {
      if (m_ListsInitalized)
      {
        // m_Positions.Dispose();
        // m_Colors.Dispose();
        // m_Uv0S.Dispose();
        // m_Uv1S.Dispose();
        // m_Uv2S.Dispose();
        // m_Uv3S.Dispose();
        // m_Normals.Dispose();
        // m_Tangents.Dispose();
        m_Vertices.Dispose();
        m_Indices.Dispose();

        m_ListsInitalized = false;
      }
    }

    /// <summary>
    /// Clear all vertices from the stream.
    /// </summary>
    public void Clear()
    {
      // Only clear if we have our lists created.
      if (m_ListsInitalized)
      {
        // m_Positions.Clear();
        // m_Colors.Clear();
        // m_Uv0S.Clear();
        // m_Uv1S.Clear();
        // m_Uv2S.Clear();
        // m_Uv3S.Clear();
        // m_Normals.Clear();
        // m_Tangents.Clear();
        m_Vertices.Clear();
        m_Indices.Clear();
      }
    }

    /// <summary>
    /// Current number of vertices in the buffer.
    /// </summary>
    public int currentVertCount => m_Vertices.Length;

    /// <summary>
    /// Get the number of indices set on the VertexHelper.
    /// </summary>
    public int currentIndexCount => m_Indices.Length;

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct VertexData
    {
      public float3 pos;
      public float3 normal;
      public float4 tangent;
      public float4 color;
      public float4 uv0;
      public float4 uv1;
      public float4 uv2;
      public float4 uv3;
    }

    private int _prevVertexCount = -1;
    private Mesh.MeshDataArray _dataArray;
    private Mesh.MeshData _data;
    private NativeList<VertexAttributeDescriptor> _arr;

    /// <summary>
    /// Fill the given mesh with the stream data.
    /// </summary>
    public unsafe void FillMesh(Mesh mesh)
    {
      _layout ??= new[]
      {
        new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
        new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3),
        new VertexAttributeDescriptor(VertexAttribute.Tangent, VertexAttributeFormat.Float32, 4),
        new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.Float32, 4),
        new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 4),
        new VertexAttributeDescriptor(VertexAttribute.TexCoord1, VertexAttributeFormat.Float32, 4),
        new VertexAttributeDescriptor(VertexAttribute.TexCoord2, VertexAttributeFormat.Float32, 4),
        new VertexAttributeDescriptor(VertexAttribute.TexCoord3, VertexAttributeFormat.Float32, 4),
      };


      // mesh.Clear();

      if (m_Vertices.Length >= 65000)
        throw new ArgumentException("Mesh can not have more than 65000 vertices");


      Profiler.BeginSample("Prepare data");


      Profiler.BeginSample("Set vertexBuffer params");
      var vertexCount = m_Vertices.Length;
      if (_prevVertexCount != vertexCount)
      {
        _prevVertexCount = vertexCount;

        mesh.SetVertexBufferParams(vertexCount, _layout);
      }

      Profiler.EndSample();


      Profiler.EndSample();

      Profiler.BeginSample("Set vertecies");

      mesh.SetVertexBufferData(m_Vertices.AsArray(), 0, 0, vertexCount);

      Profiler.EndSample();
      Profiler.BeginSample("Set indicies");
      mesh.SetIndices(m_Indices.AsArray(), MeshTopology.Triangles, 0);
      Profiler.EndSample();

      Profiler.BeginSample("Recalc bounds");
      mesh.RecalculateBounds();
      Profiler.EndSample();
    }

    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
    public struct FillBufferJob : IJob
    {
      [NoAlias] [ReadOnly] public NativeArray<float3> m_Positions;
      [NoAlias] [ReadOnly] public NativeArray<float4> m_Colors;

      [NoAlias] [ReadOnly] public NativeArray<float4> m_Uv0S;

      // public NativeList<float4> m_Uv1S;
      // public NativeList<float4> m_Uv2S;
      // public NativeList<float4> m_Uv3S;
      [NoAlias] [ReadOnly] public NativeArray<float3> m_Normals;
      [NoAlias] [ReadOnly] public NativeArray<float4> m_Tangents;

      [NativeDisableUnsafePtrRestriction] public unsafe VertexData* buffer;
      public int vertexCount;

      public unsafe void Execute()
      {
        for (int i = 0; i < vertexCount; i++)
        {
          buffer[i] = new()
          {
            pos = m_Positions[i],

            normal = m_Normals[i],
            tangent = m_Tangents[i],
            color = m_Colors[i],
            uv0 = m_Uv0S[i],
            // uv1 = m_Uv1S[i],
            // uv2 = m_Uv2S[i],
            // uv3 = m_Uv3S[i],

            uv1 = default,
            uv2 = default,
            uv3 = default,
          };
        }


        // for (int i = 0; i < vertexCount; i++)
        // {
        //   var p = i;
        //   buffer[p].pos = m_Positions[i];
        // }
        //
        // for (int i = 0; i < vertexCount; i++)
        // {
        //   var p = i;
        //   buffer[p].normal = m_Normals[i];
        // }
        //
        // for (int i = 0; i < vertexCount; i++)
        // {
        //   var p = i;
        //   buffer[p].tangent = m_Tangents[i];
        // }
        //
        // for (int i = 0; i < vertexCount; i++)
        // {
        //   var p = i;
        //
        //   buffer[p].color = m_Colors[i];
        // }
        //
        // for (int i = 0; i < vertexCount; i++)
        // {
        //   var p = i;
        //
        //   buffer[p].uv0 = m_Uv0S[i];
        //   buffer[p].uv1 = default;
        //   buffer[p].uv2 = default;
        //   buffer[p].uv3 = default;
        // }
      }
    }

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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddVert(float3 position,
      float4 color,
      float4 uv0,
      float4 uv1,
      float4 uv2,
      float4 uv3,
      float3 normal,
      float4 tangent)
    {
      m_Vertices.Add(new()
      {
        pos = position,
        normal = normal,
        tangent = tangent,
        color = color,
        uv0 = uv0,
        // uv1 = uv1,
        // uv2 = uv2,
        // uv3 = uv3,
      });
    }

    /// <summary>
    /// Add a single vertex to the stream.
    /// </summary>
    /// <param name="position">Position of the vert</param>
    /// <param name="color">Color of the vert</param>
    /// <param name="uv0">UV of the vert</param>
    /// <param name="uv1">UV1 of the vert</param>
    /// <param name="normal">Normal of the vert.</param>
    /// <param name="tangent">Tangent of the vert</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddVert(float3 position, float4 color, float4 uv0, float4 uv1, float3 normal, float4 tangent)
    {
      AddVert(position, color, uv0, uv1, Vector4.zero, Vector4.zero, normal, tangent);
    }

    /// <summary>
    /// Add a single vertex to the stream.
    /// </summary>
    /// <param name="position">Position of the vert</param>
    /// <param name="color">Color of the vert</param>
    /// <param name="uv0">UV of the vert</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddVert(float3 position, float4 color, float4 uv0)
    {
      AddVert(position, color, uv0, Vector4.zero, s_DefaultNormal, s_DefaultTangent);
    }

    /// <summary>
    /// Add a single vertex to the stream.
    /// </summary>
    /// <param name="v">The vertex to add</param>
    public void AddVert(UIVertex v)
    {
      // AddVert(v.position, v.color, v.uv0, v.uv1, v.uv2, v.uv3, v.normal, v.tangent);
    }

    /// <summary>
    /// Add a triangle to the buffer.
    /// </summary>
    /// <param name="idx0">index 0</param>
    /// <param name="idx1">index 1</param>
    /// <param name="idx2">index 2</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddTriangle(int idx0, int idx1, int idx2)
    {
      m_Indices.Add(idx0);
      m_Indices.Add(idx1);
      m_Indices.Add(idx2);
    }

    /// <summary>
    /// Add a quad to the stream.
    /// </summary>
    /// <param name="verts">4 Vertices representing the quad.</param>
    public void AddUIVertexQuad(UIVertex[] verts)
    {
      return;
      // int startIndex = currentVertCount;

      // for (int i = 0; i < 4; i++)
      // AddVert(verts[i].position, verts[i].color, verts[i].uv0, verts[i].uv1, verts[i].normal, verts[i].tangent);

      // AddTriangle(startIndex, startIndex + 1, startIndex + 2);
      // AddTriangle(startIndex + 2, startIndex + 3, startIndex);
    }
  }
}