using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
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
  public class VertexHelper : IDisposable
  {
    public int _currentVertIndex;
    public int _currentIndicesIndex;
    
    public NativeArray<VertexData> m_Vertices;
    public NativeArray<ushort> m_Indices;

    public static readonly float4 s_DefaultTangent = new Vector4(1.0f, 0.0f, 0.0f, -1.0f);
    public static readonly float3 s_DefaultNormal = new float3(0, 0, -1);

    private bool m_ListsInitalized = false;
    private static VertexAttributeDescriptor[] _layout;

    public VertexHelper()
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

    public void Reinit(int sizeVert, int sizeInd)
    {
      // if (!m_Lm_Vertices.LengthistsInitalized)
      _sizeVert = 0;
      _sizeInd = 0;
      
      // _dataArray = Mesh.AllocateWritableMeshData(1);
      // _data = _dataArray[0];

      // if (_sizeVert == sizeVert && _sizeInd == sizeInd)
        // return;
                                         
      _sizeVert = sizeVert;
      _sizeInd = sizeInd;

      _currentIndicesIndex = 0;
      _currentVertIndex = 0;

      _data.SetVertexBufferParams(sizeVert, _arr.AsArray());
      m_Vertices = _data.GetVertexData<VertexData>();
      _data.SetIndexBufferParams(sizeInd, IndexFormat.UInt16);
      m_Indices = _data.GetIndexData<ushort>();
              // One sub-mesh with all the indices.
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

    /// <summary>
    /// Cleanup allocated memory.
    /// </summary>
    public void Dispose()
    {
      _arr.Dispose();
      if (m_ListsInitalized)
      {
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
        // m_Vertices.Clear();
        // m_Indices.Clear();
      }
    }

    /// <summary>
    /// Current number of vertices in the buffer.
    /// </summary>
    public int currentVertCount => _currentVertIndex;

    /// <summary>
    /// Get the number of indices set on the VertexHelper.
    /// </summary>
    public int currentIndexCount => _currentIndicesIndex;

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
      if (m_Vertices.Length >= 65000)
        throw new ArgumentException("Mesh can not have more than 65000 vertices");

      Profiler.BeginSample("Set vertecies");

      _data.subMeshCount = 1;
      _data.SetSubMesh(0, new(0, m_Indices.Length));
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
      m_Vertices[_currentVertIndex++]= (new()
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
    public void AddTriangle(ushort idx0, ushort idx1, ushort idx2)
    {
      m_Indices[_currentIndicesIndex++] = idx0;
      m_Indices[_currentIndicesIndex++] = idx1;
      m_Indices[_currentIndicesIndex++] = idx2;
      // m_Indices.Add(idx0);
      // m_Indices.Add(idx1);
      // m_Indices.Add(idx2);
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