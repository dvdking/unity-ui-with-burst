using System;
using System.Collections.Generic;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Serialization;

namespace UnityEngine.UI
{
  /// <summary>
  /// Displays a Texture2D for the UI System.
  /// </summary>
  /// <remarks>
  /// If you don't have or don't wish to create an atlas, you can simply use this script to draw a texture.
  /// Keep in mind though that this will create an extra draw call with each RawImage present, so it's
  /// best to use it only for backgrounds or temporary visible graphics.
  /// </remarks>
  [RequireComponent(typeof(CanvasRenderer))]
  [AddComponentMenu("UI/Raw Image", 12)]
  public class RawImage : NativeMaskableGraphic
  {
    [FormerlySerializedAs("m_Tex")] [SerializeField]
    Texture m_Texture;

    [SerializeField] Rect m_UVRect = new Rect(0f, 0f, 1f, 1f);

    protected RawImage()
    {
    }

    /// <summary>
    /// Returns the texture used to draw this Graphic.
    /// </summary>
    public override Texture mainTexture
    {
      get
      {
        if (m_Texture == null)
        {
          if (material != null && material.mainTexture != null)
          {
            return material.mainTexture;
          }

          return s_WhiteTexture;
        }

        return m_Texture;
      }
    }

    /// <summary>
    /// The RawImage's texture to be used.
    /// </summary>
    /// <remarks>
    /// Use this to alter or return the Texture the RawImage displays. The Raw Image can display any Texture whereas an Image component can only show a Sprite Texture.
    /// Note : Keep in mind that using a RawImage creates an extra draw call with each RawImage present, so it's best to use it only for backgrounds or temporary visible graphics.Note: Keep in mind that using a RawImage creates an extra draw call with each RawImage present, so it's best to use it only for backgrounds or temporary visible graphics.
    /// </remarks>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// //Create a new RawImage by going to Create>UI>Raw Image in the hierarchy.
    /// //Attach this script to the RawImage GameObject.
    ///
    /// using UnityEngine;
    /// using UnityEngine.UI;
    ///
    /// public class RawImageTexture : MonoBehaviour
    /// {
    ///     RawImage m_RawImage;
    ///     //Select a Texture in the Inspector to change to
    ///     public Texture m_Texture;
    ///
    ///     void Start()
    ///     {
    ///         //Fetch the RawImage component from the GameObject
    ///         m_RawImage = GetComponent<RawImage>();
    ///         //Change the Texture to be the one you define in the Inspector
    ///         m_RawImage.texture = m_Texture;
    ///     }
    /// }
    /// ]]>
    ///</code>
    /// </example>
    public Texture texture
    {
      get { return m_Texture; }
      set
      {
        if (m_Texture == value)
          return;

        m_Texture = value;
        SetVerticesDirty();
        SetMaterialDirty();
      }
    }

    /// <summary>
    /// UV rectangle used by the texture.
    /// </summary>
    public Rect uvRect
    {
      get { return m_UVRect; }
      set
      {
        if (m_UVRect == value)
          return;
        m_UVRect = value;
        SetVerticesDirty();
      }
    }

    /// <summary>
    /// Adjust the scale of the Graphic to make it pixel-perfect.
    /// </summary>
    /// <remarks>
    /// This means setting the RawImage's RectTransform.sizeDelta  to be equal to the Texture dimensions.
    /// </remarks>
    public override void SetNativeSize()
    {
      Texture tex = mainTexture;
      if (tex != null)
      {
        int w = Mathf.RoundToInt(tex.width * uvRect.width);
        int h = Mathf.RoundToInt(tex.height * uvRect.height);
        rectTransform.anchorMax = rectTransform.anchorMin;
        rectTransform.sizeDelta = new Vector2(w, h);
      }
    }

    protected override void OnPopulateMeshNative(NativeVertexHelper vh)
    {
      var tex = mainTexture;
      vh.Clear();

      if (tex == null)
        return;

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

    protected override void OnDidApplyAnimationProperties()
    {
      SetMaterialDirty();
      SetVerticesDirty();
      SetRaycastDirty();
    }
  }
}