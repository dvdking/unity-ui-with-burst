using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Unity.Collections;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine.Profiling;
using UnityEngine.Serialization;
using UnityEngine.U2D;

namespace UnityEngine.UI
{
  /// <summary>
  /// Image is a textured element in the UI hierarchy.
  /// </summary>
  [RequireComponent(typeof(CanvasRenderer))]
  [AddComponentMenu("UI/Image", 11)]
  /// <summary>
  ///   Displays a Sprite inside the UI System.
  /// </summary>
  public class Image : NativeMaskableGraphic, ISerializationCallbackReceiver, ILayoutElement, ICanvasRaycastFilter
  {
    /// <summary>
    /// Image fill type controls how to display the image.
    /// </summary>
    public enum Type
    {
      /// <summary>
      /// Displays the full Image
      /// </summary>
      /// <remarks>
      /// This setting shows the entire image stretched across the Image's RectTransform
      /// </remarks>
      Simple,

      /// <summary>
      /// Displays the Image as a 9-sliced graphic.
      /// </summary>
      /// <remarks>
      /// A 9-sliced image displays a central area stretched across the image surrounded by a border comprising of 4 corners and 4 stretched edges.
      ///
      /// This has the effect of creating a resizable skinned rectangular element suitable for dialog boxes, windows, and general UI elements.
      ///
      /// Note: For this method to work properly the Sprite assigned to Image.sprite needs to have Sprite.border defined.
      /// </remarks>
      Sliced,

      /// <summary>
      /// Displays a sliced Sprite with its resizable sections tiled instead of stretched.
      /// </summary>
      /// <remarks>
      /// A Tiled image behaves similarly to a UI.Image.Type.Sliced|Sliced image, except that the resizable sections of the image are repeated instead of being stretched. This can be useful for detailed UI graphics that do not look good when stretched.
      ///
      /// It uses the Sprite.border value to determine how each part (border and center) should be tiled.
      ///
      /// The Image sections will repeat the corresponding section in the Sprite until the whole section is filled. The corner sections will be unaffected and will draw in the same way as a Sliced Image. The edges will repeat along their lengths. The center section will repeat across the whole central part of the Image.
      ///
      /// The Image section will repeat the corresponding section in the Sprite until the whole section is filled.
      ///
      /// Be aware that if you are tiling a Sprite with borders or a packed sprite, a mesh will be generated to create the tiles. The size of the mesh will be limited to 16250 quads; if your tiling would require more tiles, the size of the tiles will be enlarged to ensure that the number of generated quads stays below this limit.
      ///
      /// For optimum efficiency, use a Sprite with no borders and with no packing, and make sure the Sprite.texture wrap mode is set to TextureWrapMode.Repeat.These settings will prevent the generation of additional geometry.If this is not possible, limit the number of tiles in your Image.
      /// </remarks>
      Tiled,

      /// <summary>
      /// Displays only a portion of the Image.
      /// </summary>
      /// <remarks>
      /// A Filled Image will display a section of the Sprite, with the rest of the RectTransform left transparent. The Image.fillAmount determines how much of the Image to show, and Image.fillMethod controls the shape in which the Image will be cut.
      ///
      /// This can be used for example to display circular or linear status information such as timers, health bars, and loading bars.
      /// </remarks>
      Filled
    }

    /// <summary>
    /// The possible fill method types for a Filled Image.
    /// </summary>
    public enum FillMethod
    {
      /// <summary>
      /// The Image will be filled Horizontally.
      /// </summary>
      /// <remarks>
      /// The Image will be Cropped at either left or right size depending on Image.fillOriging at the Image.fillAmount
      /// </remarks>
      Horizontal,

      /// <summary>
      /// The Image will be filled Vertically.
      /// </summary>
      /// <remarks>
      /// The Image will be Cropped at either top or Bottom size depending on Image.fillOrigin at the Image.fillAmount
      /// </remarks>
      Vertical,

      /// <summary>
      /// The Image will be filled Radially with the radial center in one of the corners.
      /// </summary>
      /// <remarks>
      /// For this method the Image.fillAmount represents an angle between 0 and 90 degrees. The Image will be cut by a line passing at the Image.fillOrigin at the specified angle.
      /// </remarks>
      Radial90,

      /// <summary>
      /// The Image will be filled Radially with the radial center in one of the edges.
      /// </summary>
      /// <remarks>
      /// For this method the Image.fillAmount represents an angle between 0 and 180 degrees. The Image will be cut by a line passing at the Image.fillOrigin at the specified angle.
      /// </remarks>
      Radial180,

      /// <summary>
      /// The Image will be filled Radially with the radial center at the center.
      /// </summary>
      /// <remarks>
      /// or this method the Image.fillAmount represents an angle between 0 and 360 degrees. The Arc defined by the center of the Image, the Image.fillOrigin and the angle will be cut from the Image.
      /// </remarks>
      Radial360,
    }

    /// <summary>
    /// Origin for the Image.FillMethod.Horizontal.
    /// </summary>
    public enum OriginHorizontal
    {
      /// <summary>
      /// >Origin at the Left side.
      /// </summary>
      Left,

      /// <summary>
      /// >Origin at the Right side.
      /// </summary>
      Right,
    }


    /// <summary>
    /// Origin for the Image.FillMethod.Vertical.
    /// </summary>
    public enum OriginVertical
    {
      /// <summary>
      /// >Origin at the Bottom Edge.
      /// </summary>
      Bottom,

      /// <summary>
      /// >Origin at the Top Edge.
      /// </summary>
      Top,
    }

    /// <summary>
    /// Origin for the Image.FillMethod.Radial90.
    /// </summary>
    public enum Origin90
    {
      /// <summary>
      /// Radial starting at the Bottom Left corner.
      /// </summary>
      BottomLeft,

      /// <summary>
      /// Radial starting at the Top Left corner.
      /// </summary>
      TopLeft,

      /// <summary>
      /// Radial starting at the Top Right corner.
      /// </summary>
      TopRight,

      /// <summary>
      /// Radial starting at the Bottom Right corner.
      /// </summary>
      BottomRight,
    }

    /// <summary>
    /// Origin for the Image.FillMethod.Radial180.
    /// </summary>
    public enum Origin180
    {
      /// <summary>
      /// Center of the radial at the center of the Bottom edge.
      /// </summary>
      Bottom,

      /// <summary>
      /// Center of the radial at the center of the Left edge.
      /// </summary>
      Left,

      /// <summary>
      /// Center of the radial at the center of the Top edge.
      /// </summary>
      Top,

      /// <summary>
      /// Center of the radial at the center of the Right edge.
      /// </summary>
      Right,
    }

    /// <summary>
    /// One of the points of the Arc for the Image.FillMethod.Radial360.
    /// </summary>
    public enum Origin360
    {
      /// <summary>
      /// Arc starting at the center of the Bottom edge.
      /// </summary>
      Bottom,

      /// <summary>
      /// Arc starting at the center of the Right edge.
      /// </summary>
      Right,

      /// <summary>
      /// Arc starting at the center of the Top edge.
      /// </summary>
      Top,

      /// <summary>
      /// Arc starting at the center of the Left edge.
      /// </summary>
      Left,
    }

    static protected Material s_ETC1DefaultUI = null;

    [FormerlySerializedAs("m_Frame")] [SerializeField]
    private Sprite m_Sprite;

    /// <summary>
    /// The sprite that is used to render this image.
    /// </summary>
    /// <remarks>
    /// This returns the source Sprite of an Image. This Sprite can also be viewed and changed in the Inspector as part of an Image component. This can also be used to change the Sprite using a script.
    /// </remarks>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// //Attach this script to an Image GameObject and set its Source Image to the Sprite you would like.
    /// //Press the space key to change the Sprite. Remember to assign a second Sprite in this script's section of the Inspector.
    ///
    /// using UnityEngine;
    /// using UnityEngine.UI;
    ///
    /// public class Example : MonoBehaviour
    /// {
    ///     Image m_Image;
    ///     //Set this in the Inspector
    ///     public Sprite m_Sprite;
    ///
    ///     void Start()
    ///     {
    ///         //Fetch the Image from the GameObject
    ///         m_Image = GetComponent<Image>();
    ///     }
    ///
    ///     void Update()
    ///     {
    ///         //Press space to change the Sprite of the Image
    ///         if (Input.GetKey(KeyCode.Space))
    ///         {
    ///             m_Image.sprite = m_Sprite;
    ///         }
    ///     }
    /// }
    /// ]]>
    ///</code>
    /// </example>

    public Sprite sprite
    {
      get { return m_Sprite; }
      set
      {
        if (m_Sprite != null)
        {
          if (m_Sprite != value)
          {
            m_SkipLayoutUpdate = m_Sprite.rect.size.Equals(value ? value.rect.size : Vector2.zero);
            m_SkipMaterialUpdate = m_Sprite.texture == (value ? value.texture : null);
            m_Sprite = value;

            SetAllDirty();
            TrackSprite();
          }
        }
        else if (value != null)
        {
          m_SkipLayoutUpdate = value.rect.size == Vector2.zero;
          m_SkipMaterialUpdate = value.texture == null;
          m_Sprite = value;

          SetAllDirty();
          TrackSprite();
        }
      }
    }


    /// <summary>
    /// Disable all automatic sprite optimizations.
    /// </summary>
    /// <remarks>
    /// When a new Sprite is assigned update optimizations are automatically applied.
    /// </remarks>
    public void DisableSpriteOptimizations()
    {
      m_SkipLayoutUpdate = false;
      m_SkipMaterialUpdate = false;
    }

    [NonSerialized] private Sprite m_OverrideSprite;

    /// <summary>
    /// Set an override sprite to be used for rendering.
    /// </summary>
    /// <remarks>
    /// The UI.Image-overrideSprite|overrideSprite variable allows a sprite to have the
    /// sprite changed.This change happens immediately.When the changed
    /// sprite is no longer needed the sprite can be reverted back to the
    /// original version.This happens when the overrideSprite
    /// is set to /null/.
    /// </remarks>
    /// <example>
    /// Note: The script example below has two buttons.  The button textures are loaded from the
    /// /Resources/ folder.  (They are not used in the shown example).  Two sprites are added to
    /// the example code.  /Example1/ and /Example2/ are functions called by the button OnClick
    /// functions.  Example1 calls overrideSprite and Example2 sets overrideSprite to null.
    /// <code>
    /// <![CDATA[
    /// using System.Collections;
    /// using System.Collections.Generic;
    /// using UnityEngine;
    /// using UnityEngine.UI;
    ///
    /// public class ExampleClass : MonoBehaviour
    /// {
    ///     private Sprite sprite1;
    ///     private Sprite sprite2;
    ///     private Image i;
    ///
    ///     public void Start()
    ///     {
    ///         i = GetComponent<Image>();
    ///         sprite1 = Resources.Load<Sprite>("texture1");
    ///         sprite2 = Resources.Load<Sprite>("texture2");
    ///
    ///         i.sprite = sprite1;
    ///     }
    ///
    ///     // Called by a Button OnClick() with ExampleClass.Example1
    ///     // Uses overrideSprite to make this change temporary
    ///     public void Example1()
    ///     {
    ///         i.overrideSprite = sprite2;
    ///     }
    ///
    ///     // Called by a Button OnClick() with ExampleClass.Example2
    ///     // Removes the overrideSprite which causes the original sprite to be used again.
    ///     public void Example2()
    ///     {
    ///         i.overrideSprite = null;
    ///     }
    /// }
    /// ]]>
    ///</code>
    /// </example>
    public Sprite overrideSprite
    {
      get { return activeSprite; }
      set
      {
        if (SetPropertyUtility.SetClass(ref m_OverrideSprite, value))
        {
          SetAllDirty();
          TrackSprite();
        }
      }
    }

    private Sprite activeSprite
    {
      get { return m_OverrideSprite != null ? m_OverrideSprite : sprite; }
    }

    [NonSerialized] private Sprite _lastActiveSprite;

    /// How the Image is drawn.
    [SerializeField] private Type m_Type = Type.Simple;

    /// <summary>
    /// How to display the image.
    /// </summary>
    /// <remarks>
    /// Unity can interpret an Image in various different ways depending on the intended purpose. This can be used to display:
    /// - Whole images stretched to fit the RectTransform of the Image.
    /// - A 9-sliced image useful for various decorated UI boxes and other rectangular elements.
    /// - A tiled image with sections of the sprite repeated.
    /// - As a partial image, useful for wipes, fades, timers, status bars etc.
    /// </remarks>
    public Type type
    {
      get { return m_Type; }
      set
      {
        if (SetPropertyUtility.SetStruct(ref m_Type, value)) SetVerticesDirty();
      }
    }

    [SerializeField] private bool m_PreserveAspect = false;

    /// <summary>
    /// Whether this image should preserve its Sprite aspect ratio.
    /// </summary>
    public bool preserveAspect
    {
      get { return m_PreserveAspect; }
      set
      {
        if (SetPropertyUtility.SetStruct(ref m_PreserveAspect, value)) SetVerticesDirty();
      }
    }

    [SerializeField] private bool m_FillCenter = true;

    /// <summary>
    /// Whether or not to render the center of a Tiled or Sliced image.
    /// </summary>
    /// <remarks>
    /// This will only have any effect if the Image.sprite has borders.
    /// </remarks>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// using UnityEngine;
    /// using System.Collections;
    /// using UnityEngine.UI;
    ///
    /// public class FillCenterScript : MonoBehaviour
    /// {
    ///     public Image xmasCalenderDoor;
    ///
    ///     // removes the center of the image to reveal the image behind it
    ///     void OpenCalendarDoor()
    ///     {
    ///         xmasCalenderDoor.fillCenter = false;
    ///     }
    /// }
    /// ]]>
    ///</code>
    /// </example>
    public bool fillCenter
    {
      get { return m_FillCenter; }
      set
      {
        if (SetPropertyUtility.SetStruct(ref m_FillCenter, value)) SetVerticesDirty();
      }
    }

    /// Filling method for filled sprites.
    [SerializeField] private FillMethod m_FillMethod = FillMethod.Radial360;

    public FillMethod fillMethod
    {
      get { return m_FillMethod; }
      set
      {
        if (SetPropertyUtility.SetStruct(ref m_FillMethod, value))
        {
          SetVerticesDirty();
          m_FillOrigin = 0;
        }
      }
    }

    /// Amount of the Image shown. 0-1 range with 0 being nothing shown, and 1 being the full Image.
    [Range(0, 1)] [SerializeField] private float m_FillAmount = 1.0f;

    /// <summary>
    /// Amount of the Image shown when the Image.type is set to Image.Type.Filled.
    /// </summary>
    /// <remarks>
    /// 0-1 range with 0 being nothing shown, and 1 being the full Image.
    /// </remarks>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// using UnityEngine;
    /// using System.Collections;
    /// using UnityEngine.UI; // Required when Using UI elements.
    ///
    /// public class Cooldown : MonoBehaviour
    /// {
    ///     public Image cooldown;
    ///     public bool coolingDown;
    ///     public float waitTime = 30.0f;
    ///
    ///     // Update is called once per frame
    ///     void Update()
    ///     {
    ///         if (coolingDown == true)
    ///         {
    ///             //Reduce fill amount over 30 seconds
    ///             cooldown.fillAmount -= 1.0f / waitTime * Time.deltaTime;
    ///         }
    ///     }
    /// }
    /// ]]>
    ///</code>
    /// </example>
    public float fillAmount
    {
      get { return m_FillAmount; }
      set
      {
        if (SetPropertyUtility.SetStruct(ref m_FillAmount, Mathf.Clamp01(value))) SetVerticesDirty();
      }
    }

    /// Whether the Image should be filled clockwise (true) or counter-clockwise (false).
    [SerializeField] private bool m_FillClockwise = true;

    /// <summary>
    /// Whether the Image should be filled clockwise (true) or counter-clockwise (false).
    /// </summary>
    /// <remarks>
    /// This will only have any effect if the Image.type is set to Image.Type.Filled and Image.fillMethod is set to any of the Radial methods.
    /// </remarks>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// using UnityEngine;
    /// using System.Collections;
    /// using UnityEngine.UI; // Required when Using UI elements.
    ///
    /// public class FillClockwiseScript : MonoBehaviour
    /// {
    ///     public Image healthCircle;
    ///
    ///     // This method sets the direction of the health circle.
    ///     // Clockwise for the Player, Counter Clockwise for the opponent.
    ///     void SetHealthDirection(GameObject target)
    ///     {
    ///         if (target.tag == "Player")
    ///         {
    ///             healthCircle.fillClockwise = true;
    ///         }
    ///         else if (target.tag == "Opponent")
    ///         {
    ///             healthCircle.fillClockwise = false;
    ///         }
    ///     }
    /// }
    /// ]]>
    ///</code>
    /// </example>
    public bool fillClockwise
    {
      get { return m_FillClockwise; }
      set
      {
        if (SetPropertyUtility.SetStruct(ref m_FillClockwise, value)) SetVerticesDirty();
      }
    }

    /// Controls the origin point of the Fill process. Value means different things with each fill method.
    [SerializeField] private int m_FillOrigin;

    /// <summary>
    /// Controls the origin point of the Fill process. Value means different things with each fill method.
    /// </summary>
    /// <remarks>
    /// You should cast to the appropriate origin type: Image.OriginHorizontal, Image.OriginVertical, Image.Origin90, Image.Origin180 or Image.Origin360 depending on the Image.Fillmethod.
    /// Note: This will only have any effect if the Image.type is set to Image.Type.Filled.
    /// </remarks>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// using UnityEngine;
    /// using UnityEngine.UI;
    /// using System.Collections;
    ///
    /// [RequireComponent(typeof(Image))]
    /// public class ImageOriginCycle : MonoBehaviour
    /// {
    ///     void OnEnable()
    ///     {
    ///         Image image = GetComponent<Image>();
    ///         string fillOriginName = "";
    ///
    ///         switch ((Image.FillMethod)image.fillMethod)
    ///         {
    ///             case Image.FillMethod.Horizontal:
    ///                 fillOriginName = ((Image.OriginHorizontal)image.fillOrigin).ToString();
    ///                 break;
    ///             case Image.FillMethod.Vertical:
    ///                 fillOriginName = ((Image.OriginVertical)image.fillOrigin).ToString();
    ///                 break;
    ///             case Image.FillMethod.Radial90:
    ///
    ///                 fillOriginName = ((Image.Origin90)image.fillOrigin).ToString();
    ///                 break;
    ///             case Image.FillMethod.Radial180:
    ///
    ///                 fillOriginName = ((Image.Origin180)image.fillOrigin).ToString();
    ///                 break;
    ///             case Image.FillMethod.Radial360:
    ///                 fillOriginName = ((Image.Origin360)image.fillOrigin).ToString();
    ///                 break;
    ///         }
    ///         Debug.Log(string.Format("{0} is using {1} fill method with the origin on {2}", name, image.fillMethod, fillOriginName));
    ///     }
    /// }
    /// ]]>
    ///</code>
    /// </example>
    public int fillOrigin
    {
      get { return m_FillOrigin; }
      set
      {
        if (SetPropertyUtility.SetStruct(ref m_FillOrigin, value)) SetVerticesDirty();
      }
    }

    // Not serialized until we support read-enabled sprites better.
    private float m_AlphaHitTestMinimumThreshold = 0;

    // Whether this is being tracked for Atlas Binding.
    private bool m_Tracked = false;

    [Obsolete(
      "eventAlphaThreshold has been deprecated. Use eventMinimumAlphaThreshold instead (UnityUpgradable) -> alphaHitTestMinimumThreshold")]

    /// <summary>
    /// Obsolete. You should use UI.Image.alphaHitTestMinimumThreshold instead.
    /// The alpha threshold specifies the minimum alpha a pixel must have for the event to considered a "hit" on the Image.
    /// </summary>
    public float eventAlphaThreshold
    {
      get { return 1 - alphaHitTestMinimumThreshold; }
      set { alphaHitTestMinimumThreshold = 1 - value; }
    }

    /// <summary>
    /// The alpha threshold specifies the minimum alpha a pixel must have for the event to considered a "hit" on the Image.
    /// </summary>
    /// <remarks>
    /// Alpha values less than the threshold will cause raycast events to pass through the Image. An value of 1 would cause only fully opaque pixels to register raycast events on the Image. The alpha tested is retrieved from the image sprite only, while the alpha of the Image [[UI.Graphic.color]] is disregarded.
    ///
    /// alphaHitTestMinimumThreshold defaults to 0; all raycast events inside the Image rectangle are considered a hit. In order for greater than 0 to values to work, the sprite used by the Image must have readable pixels. This can be achieved by enabling Read/Write enabled in the advanced Texture Import Settings for the sprite and disabling atlassing for the sprite.
    /// </remarks>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// using UnityEngine;
    /// using System.Collections;
    /// using UnityEngine.UI; // Required when Using UI elements.
    ///
    /// public class ExampleClass : MonoBehaviour
    /// {
    ///     public Image theButton;
    ///
    ///     // Use this for initialization
    ///     void Start()
    ///     {
    ///         theButton.alphaHitTestMinimumThreshold = 0.5f;
    ///     }
    /// }
    /// ]]>
    ///</code>
    /// </example>
    public float alphaHitTestMinimumThreshold
    {
      get { return m_AlphaHitTestMinimumThreshold; }
      set { m_AlphaHitTestMinimumThreshold = value; }
    }

    /// Controls whether or not to use the generated mesh from the sprite importer.
    [SerializeField] private bool m_UseSpriteMesh;

    /// <summary>
    /// Allows you to specify whether the UI Image should be displayed using the mesh generated by the TextureImporter, or by a simple quad mesh.
    /// </summary>
    /// <remarks>
    /// When this property is set to false, the UI Image uses a simple quad. When set to true, the UI Image uses the sprite mesh generated by the [[TextureImporter]]. You should set this to true if you want to use a tightly fitted sprite mesh based on the alpha values in your image.
    /// Note: If the texture importer's SpriteMeshType property is set to SpriteMeshType.FullRect, it will only generate a quad, and not a tightly fitted sprite mesh, which means this UI image will be drawn using a quad regardless of the value of this property. Therefore, when enabling this property to use a tightly fitted sprite mesh, you must also ensure the texture importer's SpriteMeshType property is set to Tight.
    /// </remarks>
    public bool useSpriteMesh
    {
      get { return m_UseSpriteMesh; }
      set
      {
        if (SetPropertyUtility.SetStruct(ref m_UseSpriteMesh, value)) SetVerticesDirty();
      }
    }


    protected Image()
    {
      UseNativeBuffers = true;
    }

    /// <summary>
    /// Cache of the default Canvas Ericsson Texture Compression 1 (ETC1) and alpha Material.
    /// </summary>
    /// <remarks>
    /// Stores the ETC1 supported Canvas Material that is returned from GetETC1SupportedCanvasMaterial().
    /// Note: Always specify the UI/DefaultETC1 Shader in the Always Included Shader list, to use the ETC1 and alpha Material.
    /// </remarks>
    static public Material defaultETC1GraphicMaterial
    {
      get
      {
        if (s_ETC1DefaultUI == null)
          s_ETC1DefaultUI = Canvas.GetETC1SupportedCanvasMaterial();
        return s_ETC1DefaultUI;
      }
    }

    /// <summary>
    /// Image's texture comes from the UnityEngine.Image.
    /// </summary>
    public override Texture mainTexture
    {
      get
      {
        if (activeSprite == null)
        {
          if (material != null && material.mainTexture != null)
          {
            return material.mainTexture;
          }

          return s_WhiteTexture;
        }

        return activeSprite.texture;
      }
    }

    /// <summary>
    /// Whether the Sprite of the image has a border to work with.
    /// </summary>

    public bool hasBorder
    {
      get
      {
        if (activeSprite != null)
        {
          Vector4 v = activeSprite.border;
          return v.sqrMagnitude > 0f;
        }

        return false;
      }
    }


    [SerializeField] private float m_PixelsPerUnitMultiplier = 1.0f;

    /// <summary>
    /// Pixel per unit modifier to change how sliced sprites are generated.
    /// </summary>
    public float pixelsPerUnitMultiplier
    {
      get { return m_PixelsPerUnitMultiplier; }
      set
      {
        m_PixelsPerUnitMultiplier = Mathf.Max(0.01f, value);
        SetVerticesDirty();
      }
    }

    // case 1066689 cache referencePixelsPerUnit when canvas parent is disabled;
    private float m_CachedReferencePixelsPerUnit = 100;

    public float pixelsPerUnit
    {
      get
      {
        float spritePixelsPerUnit = 100;
        if (activeSprite)
          spritePixelsPerUnit = activeSprite.pixelsPerUnit;

        if (canvas)
          m_CachedReferencePixelsPerUnit = canvas.referencePixelsPerUnit;

        return spritePixelsPerUnit / m_CachedReferencePixelsPerUnit;
      }
    }

    protected float multipliedPixelsPerUnit
    {
      get { return pixelsPerUnit * m_PixelsPerUnitMultiplier; }
    }

    /// <summary>
    /// The specified Material used by this Image. The default Material is used instead if one wasn't specified.
    /// </summary>
    public override Material material
    {
      get
      {
        if (m_Material != null)
          return m_Material;
#if UNITY_EDITOR
        if (Application.isPlaying && activeSprite && activeSprite.associatedAlphaSplitTexture != null)
          return defaultETC1GraphicMaterial;
#else
        if (activeSprite && activeSprite.associatedAlphaSplitTexture != null)
          return defaultETC1GraphicMaterial;
#endif

        return defaultMaterial;
      }

      set { base.material = value; }
    }

    /// <summary>
    /// See ISerializationCallbackReceiver.
    /// </summary>
    public void OnBeforeSerialize()
    {
    }

    /// <summary>
    /// See ISerializationCallbackReceiver.
    /// </summary>
    public void OnAfterDeserialize()
    {
      if (m_FillOrigin < 0)
        m_FillOrigin = 0;
      else if (m_FillMethod == FillMethod.Horizontal && m_FillOrigin > 1)
        m_FillOrigin = 0;
      else if (m_FillMethod == FillMethod.Vertical && m_FillOrigin > 1)
        m_FillOrigin = 0;
      else if (m_FillOrigin > 3)
        m_FillOrigin = 0;

      m_FillAmount = Mathf.Clamp(m_FillAmount, 0f, 1f);
    }

    private void PreserveSpriteAspectRatio(ref Rect rect, Vector2 spriteSize)
    {
      var spriteRatio = spriteSize.x / spriteSize.y;
      var rectRatio = rect.width / rect.height;

      if (spriteRatio > rectRatio)
      {
        var oldHeight = rect.height;
        rect.height = rect.width * (1.0f / spriteRatio);
        rect.y += (oldHeight - rect.height) * rectTransform.pivot.y;
      }
      else
      {
        var oldWidth = rect.width;
        rect.width = rect.height * spriteRatio;
        rect.x += (oldWidth - rect.width) * rectTransform.pivot.x;
      }
    }

    /// Image's dimensions used for drawing. X = left, Y = bottom, Z = right, W = top.
    private Vector4 GetDrawingDimensions(bool shouldPreserveAspect)
    {
      var padding = activeSprite == null ? Vector4.zero : Sprites.DataUtility.GetPadding(activeSprite);
      var size = activeSprite == null ? Vector2.zero : new Vector2(activeSprite.rect.width, activeSprite.rect.height);

      Rect r = GetPixelAdjustedRect();
      // Debug.Log(string.Format("r:{2}, size:{0}, padding:{1}", size, padding, r));

      int spriteW = Mathf.RoundToInt(size.x);
      int spriteH = Mathf.RoundToInt(size.y);

      var v = new Vector4(
        padding.x / spriteW,
        padding.y / spriteH,
        (spriteW - padding.z) / spriteW,
        (spriteH - padding.w) / spriteH);

      if (shouldPreserveAspect && size.sqrMagnitude > 0.0f)
      {
        PreserveSpriteAspectRatio(ref r, size);
      }

      v = new Vector4(
        r.x + r.width * v.x,
        r.y + r.height * v.y,
        r.x + r.width * v.z,
        r.y + r.height * v.w
      );

      return v;
    }

    /// <summary>
    /// Adjusts the image size to make it pixel-perfect.
    /// </summary>
    /// <remarks>
    /// This means setting the Images RectTransform.sizeDelta to be equal to the Sprite dimensions.
    /// </remarks>
    public override void SetNativeSize()
    {
      if (activeSprite != null)
      {
        float w = activeSprite.rect.width / pixelsPerUnit;
        float h = activeSprite.rect.height / pixelsPerUnit;
        rectTransform.anchorMax = rectTransform.anchorMin;
        rectTransform.sizeDelta = new Vector2(w, h);
        SetAllDirty();
      }
    }

    /// Update the UI renderer mesh.
    /// </summary>
    protected override void OnPopulateMeshNative(NativeVertexHelper toFill)
    {
      if (activeSprite is null)
      {
        base.OnPopulateMeshNative(toFill);
        return;
      }

      switch (type)
      {
        case Type.Simple:
          GenerateSimpleSprite(toFill, m_PreserveAspect);
          break;
        case Type.Sliced:
          GenerateSlicedSprite(toFill);
          break;
        case Type.Tiled:
          GenerateTiledSprite(toFill);
          break;
        case Type.Filled:
          GenerateFilledSprite(toFill, m_PreserveAspect);
          break;
      }

      // return null;
    }

    private void TrackSprite()
    {
      if (activeSprite != null && activeSprite.texture == null)
      {
        TrackImage(this);
        m_Tracked = true;
      }
    }

    protected override void OnEnable()
    {
      base.OnEnable();
      TrackSprite();
    }

    protected override void OnDisable()
    {
      base.OnDisable();

      if (m_Tracked)
        UnTrackImage(this);
    }

    /// <summary>
    /// Update the renderer's material.
    /// </summary>
    protected override void UpdateMaterial()
    {
      base.UpdateMaterial();

      // check if this sprite has an associated alpha texture (generated when splitting RGBA = RGB + A as two textures without alpha)

      if (activeSprite == null)
      {
        canvasRenderer.SetAlphaTexture(null);
        return;
      }

      Texture2D alphaTex = activeSprite.associatedAlphaSplitTexture;

      if (alphaTex != null)
      {
        canvasRenderer.SetAlphaTexture(alphaTex);
      }
    }

    protected override void OnCanvasHierarchyChanged()
    {
      base.OnCanvasHierarchyChanged();
      if (canvas == null)
      {
        m_CachedReferencePixelsPerUnit = 100;
      }
      else if (canvas.referencePixelsPerUnit != m_CachedReferencePixelsPerUnit)
      {
        m_CachedReferencePixelsPerUnit = canvas.referencePixelsPerUnit;
        if (type == Type.Sliced || type == Type.Tiled)
        {
          SetVerticesDirty();
          SetLayoutDirty();
        }
      }
    }

    /// <summary>
    /// Generate vertices for a simple Image.
    /// </summary>
    void GenerateSimpleSprite(NativeVertexHelper vh, bool lPreserveAspect)
    {
      var v = GetDrawingDimensions(lPreserveAspect);
      var uv = activeSprite != null ? Sprites.DataUtility.GetOuterUV(activeSprite) : Vector4.zero;

      const int quadCount = 1;
      const int vertCount = 4;
      const int indiciesCount = 6;
      vh.Reinit(quadCount * vertCount, quadCount * indiciesCount);

      var job = new FillSimpleSpriteJob
      {
        Verticies = vh.Vertices,
        Indicies = vh.Indices,
        Dimensions = v,
        Uv = uv,
        Color32 = colorFloat
      };

      job.Run();
    }


    [NonSerialized] private NativeArray<float2> s_VertScratch;
    [NonSerialized] NativeArray<float2> s_UVScratch;
    [NonSerialized] Vector4 outer, inner, padding, border;
    [NonSerialized] private bool _init = false;

    /// <summary>
    /// Generate vertices for a 9-sliced Image.
    /// </summary>
    private void GenerateSlicedSprite(NativeVertexHelper toFill)
    {
      if (!hasBorder)
      {
        GenerateSimpleSprite(toFill, false);
        return;
      }

      //todo: do not update it every frame
      // if (!_init)
      {
        if (activeSprite is not null && !ReferenceEquals(_lastActiveSprite, activeSprite))
        {
          outer = Sprites.DataUtility.GetOuterUV(activeSprite);
          inner = Sprites.DataUtility.GetInnerUV(activeSprite);
          padding = Sprites.DataUtility.GetPadding(activeSprite);
          border = activeSprite.border;
          _lastActiveSprite = activeSprite;
        }
        else
        {
          if (activeSprite is null)
          {
            outer = Vector4.zero;
            inner = Vector4.zero;
            padding = Vector4.zero;
            border = Vector4.zero;
          }
        }

        Rect rect = GetPixelAdjustedRect();

        float4 adjustedBorders = GetAdjustedBorders(border / multipliedPixelsPerUnit, rect);
        padding /= multipliedPixelsPerUnit;

        s_VertScratch =
          new NativeArray<float2>(4, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        s_UVScratch =
          new NativeArray<float2>(4, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

        s_VertScratch[0] = new(padding.x, padding.y);
        s_VertScratch[3] = new(rect.width - padding.z, rect.height - padding.w);

        s_VertScratch[1] = adjustedBorders.xy;

        s_VertScratch[2] = new(rect.width - adjustedBorders.z, rect.height - adjustedBorders.w);

        for (int i = 0; i < 4; ++i)
          s_VertScratch[i] = new(s_VertScratch[i].x + rect.x, s_VertScratch[i].y + rect.y);

        s_UVScratch[0] = new(outer.x, outer.y);
        s_UVScratch[1] = new(inner.x, inner.y);
        s_UVScratch[2] = new(inner.z, inner.w);
        s_UVScratch[3] = new(outer.z, outer.w);
        _init = true;
      }

      const int quadCount = 9;
      const int vertCount = 4;
      const int indiciesCount = 6;
      if (fillCenter)
        toFill.Reinit(quadCount * vertCount, quadCount * indiciesCount);
      else
        toFill.Reinit((quadCount - 1) * vertCount, (quadCount - 1) * indiciesCount);

      var job = new FillSlicedSpriteJob
      {
        Verticies = toFill.Vertices,
        Indicies = toFill.Indices,
        Color = colorFloat,
        s_VertScratch = s_VertScratch,
        s_UVScratch = s_UVScratch,
        FillCenter = fillCenter
      };

      job.Run();

      s_VertScratch.Dispose();
      s_UVScratch.Dispose();
    }

    /// <summary>
    /// Generate vertices for a tiled Image.
    /// </summary>
    void GenerateTiledSprite(NativeVertexHelper toFill)
    {
      Vector4 outer, inner, border;
      Vector2 spriteSize;

      if (activeSprite != null)
      {
        outer = Sprites.DataUtility.GetOuterUV(activeSprite);
        inner = Sprites.DataUtility.GetInnerUV(activeSprite);
        border = activeSprite.border;
        spriteSize = activeSprite.rect.size;
      }
      else
      {
        outer = Vector4.zero;
        inner = Vector4.zero;
        border = Vector4.zero;
        spriteSize = Vector2.one * 100;
      }

      Rect rect = GetPixelAdjustedRect();
      float tileWidth = (spriteSize.x - border.x - border.z) / multipliedPixelsPerUnit;
      float tileHeight = (spriteSize.y - border.y - border.w) / multipliedPixelsPerUnit;

      border = GetAdjustedBorders(border / multipliedPixelsPerUnit, rect);

      var uvMin = new Vector2(inner.x, inner.y);
      var uvMax = new Vector2(inner.z, inner.w);

      // Min to max max range for tiled region in coordinates relative to lower left corner.
      float xMin = border.x;
      float xMax = rect.width - border.z;
      float yMin = border.y;
      float yMax = rect.height - border.w;

      var clipped = uvMax;

      // if either width is zero we cant tile so just assume it was the full width.
      if (tileWidth <= 0)
        tileWidth = xMax - xMin;

      if (tileHeight <= 0)
        tileHeight = yMax - yMin;

      const int quadCount = 1;
      const int vertCount = 4;
      const int indiciesCount = 6;
      toFill.Reinit(quadCount * vertCount, quadCount * indiciesCount);
      
      var job = new FillTiledSpriteJob
      {
        xMin = xMin,
        xMax = xMax,
        yMin = yMin,
        yMax = yMax,
        tileWidth = tileWidth,
        tileHeight = tileHeight,
        uvMin = uvMin,
        uvMax = uvMax,
        position = rect.position,
        Color32 = colorFloat,
        Verticies = toFill.Vertices,
        Indicies = toFill.Indices,
      };

      job.Run();
      
      if(activeSprite.texture.wrapMode != TextureWrapMode.Repeat)
        Debug.LogError($"Texture {activeSprite.texture.name} is not set to repeat mode. This will cause issues with tiling.");
      
      return;
    }

    private float4 GetAdjustedBorders(float4 border, Rect adjustedRect)
    {
      Rect originalRect = rectTransform.rect;

      for (int axis = 0; axis <= 1; axis++)
      {
        float borderScaleRatio;

        // The adjusted rect (adjusted for pixel correctness)
        // may be slightly larger than the original rect.
        // Adjust the border to match the adjustedRect to avoid
        // small gaps between borders (case 833201).
        if (originalRect.size[axis] != 0)
        {
          borderScaleRatio = adjustedRect.size[axis] / originalRect.size[axis];
          border[axis] *= borderScaleRatio;
          border[axis + 2] *= borderScaleRatio;
        }

        // If the rect is smaller than the combined borders, then there's not room for the borders at their normal size.
        // In order to avoid artefacts with overlapping borders, we scale the borders down to fit.
        float combinedBorders = border[axis] + border[axis + 2];
        if (adjustedRect.size[axis] < combinedBorders && combinedBorders != 0)
        {
          borderScaleRatio = adjustedRect.size[axis] / combinedBorders;
          border[axis] *= borderScaleRatio;
          border[axis + 2] *= borderScaleRatio;
        }
      }

      return border;
    }

    /// <summary>
    /// Generate vertices for a filled Image.
    /// </summary>
    void GenerateFilledSprite(NativeVertexHelper toFill, bool preserveAspect)
    {

      const int quadCount = 1;
      const int vertCount = 4;
      const int indiciesCount = 6;

      switch (m_FillAmount)
      {
        case < 0.001f:
          toFill.Clear();
          return;
        case < 1f when m_FillMethod is not FillMethod.Horizontal and not FillMethod.Vertical:
          switch (m_FillMethod)
          {
            case FillMethod.Radial90:
              toFill.Reinit(quadCount * vertCount, quadCount * indiciesCount);
              break;
            case FillMethod.Radial180:
            {
              const int quadCountRadial180 = 2;
              toFill.Reinit(quadCountRadial180 * vertCount, quadCountRadial180 * indiciesCount);
              break;
            }
            case FillMethod.Radial360:
            {
              const int quadCountRadial360 = 4;
              toFill.Reinit(quadCountRadial360 * vertCount, quadCountRadial360 * indiciesCount);
              break;
            }
          }

          break;
        default:
          toFill.Reinit(quadCount * vertCount, quadCount * indiciesCount);
          break;
      }

      var v = GetDrawingDimensions(preserveAspect);
      var outer = activeSprite != null ? Sprites.DataUtility.GetOuterUV(activeSprite) : Vector4.zero;
      var job = new FillFilledSpriteJob
      {
        Dimensions = v,
        Outer = outer,
        FillMethod = m_FillMethod,
        FillOrigin = m_FillOrigin,
        FillAmount = m_FillAmount,
        Color32 = colorFloat,
        Verticies = toFill.Vertices,
        Indicies = toFill.Indices,
        FillClockwise = m_FillClockwise
      };

      job.Run();
    }

    /// <summary>
    /// See ILayoutElement.CalculateLayoutInputHorizontal.
    /// </summary>
    public void CalculateLayoutInputHorizontal()
    {
    }

    /// <summary>
    /// See ILayoutElement.CalculateLayoutInputVertical.
    /// </summary>
    public void CalculateLayoutInputVertical()
    {
    }

    /// <summary>
    /// See ILayoutElement.minWidth.
    /// </summary>
    public float minWidth
    {
      get { return 0; }
    }

    /// <summary>
    /// If there is a sprite being rendered returns the size of that sprite.
    /// In the case of a slided or tiled sprite will return the calculated minimum size possible
    /// </summary>
    public float preferredWidth
    {
      get
      {
        if (activeSprite == null)
          return 0;
        if (type == Type.Sliced || type == Type.Tiled)
          return Sprites.DataUtility.GetMinSize(activeSprite).x / pixelsPerUnit;
        return activeSprite.rect.size.x / pixelsPerUnit;
      }
    }

    /// <summary>
    /// See ILayoutElement.flexibleWidth.
    /// </summary>
    public float flexibleWidth
    {
      get { return -1; }
    }

    /// <summary>
    /// See ILayoutElement.minHeight.
    /// </summary>
    public float minHeight
    {
      get { return 0; }
    }

    /// <summary>
    /// If there is a sprite being rendered returns the size of that sprite.
    /// In the case of a slided or tiled sprite will return the calculated minimum size possible
    /// </summary>
    public float preferredHeight
    {
      get
      {
        if (activeSprite == null)
          return 0;
        if (type == Type.Sliced || type == Type.Tiled)
          return Sprites.DataUtility.GetMinSize(activeSprite).y / pixelsPerUnit;
        return activeSprite.rect.size.y / pixelsPerUnit;
      }
    }

    /// <summary>
    /// See ILayoutElement.flexibleHeight.
    /// </summary>
    public float flexibleHeight
    {
      get { return -1; }
    }

    /// <summary>
    /// See ILayoutElement.layoutPriority.
    /// </summary>
    public int layoutPriority
    {
      get { return 0; }
    }

    /// <summary>
    /// Calculate if the ray location for this image is a valid hit location. Takes into account a Alpha test threshold.
    /// </summary>
    /// <param name="screenPoint">The screen point to check against</param>
    /// <param name="eventCamera">The camera in which to use to calculate the coordinating position</param>
    /// <returns>If the location is a valid hit or not.</returns>
    /// <remarks> Also see See:ICanvasRaycastFilter.</remarks>
    public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
    {
      if (alphaHitTestMinimumThreshold <= 0)
        return true;

      if (alphaHitTestMinimumThreshold > 1)
        return false;

      if (activeSprite == null)
        return true;

      Vector2 local;
      if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out local))
        return false;

      Rect rect = GetPixelAdjustedRect();

      // Convert to have lower left corner as reference point.
      local.x += rectTransform.pivot.x * rect.width;
      local.y += rectTransform.pivot.y * rect.height;

      local = MapCoordinate(local, rect);

      // Convert local coordinates to texture space.
      Rect spriteRect = activeSprite.textureRect;
      float x = (spriteRect.x + local.x) / activeSprite.texture.width;
      float y = (spriteRect.y + local.y) / activeSprite.texture.height;

      try
      {
        return activeSprite.texture.GetPixelBilinear(x, y).a >= alphaHitTestMinimumThreshold;
      }
      catch (UnityException e)
      {
        Debug.LogError(
          "Using alphaHitTestMinimumThreshold greater than 0 on Image whose sprite texture cannot be read. " + e.Message
          + " Also make sure to disable sprite packing for this sprite.",
          this);
        return true;
      }
    }

    private Vector2 MapCoordinate(Vector2 local, Rect rect)
    {
      Rect spriteRect = activeSprite.rect;
      if (type == Type.Simple || type == Type.Filled)
        return new Vector2(local.x * spriteRect.width / rect.width, local.y * spriteRect.height / rect.height);

      Vector4 border = activeSprite.border;
      Vector4 adjustedBorder = GetAdjustedBorders(border / pixelsPerUnit, rect);

      for (int i = 0; i < 2; i++)
      {
        if (local[i] <= adjustedBorder[i])
          continue;

        if (rect.size[i] - local[i] <= adjustedBorder[i + 2])
        {
          local[i] -= (rect.size[i] - spriteRect.size[i]);
          continue;
        }

        if (type == Type.Sliced)
        {
          float lerp = Mathf.InverseLerp(adjustedBorder[i], rect.size[i] - adjustedBorder[i + 2], local[i]);
          local[i] = Mathf.Lerp(border[i], spriteRect.size[i] - border[i + 2], lerp);
        }
        else
        {
          local[i] -= adjustedBorder[i];
          local[i] = Mathf.Repeat(local[i], spriteRect.size[i] - border[i] - border[i + 2]);
          local[i] += border[i];
        }
      }

      return local;
    }

    // To track textureless images, which will be rebuild if sprite atlas manager registered a Sprite Atlas that will give this image new texture
    static List<Image> m_TrackedTexturelessImages = new List<Image>();
    static bool s_Initialized;

    static void RebuildImage(SpriteAtlas spriteAtlas)
    {
      for (var i = m_TrackedTexturelessImages.Count - 1; i >= 0; i--)
      {
        var g = m_TrackedTexturelessImages[i];
        if (null != g.activeSprite && spriteAtlas.CanBindTo(g.activeSprite))
        {
          g.SetAllDirty();
          m_TrackedTexturelessImages.RemoveAt(i);
        }
      }
    }

    private static void TrackImage(Image g)
    {
      if (!s_Initialized)
      {
        SpriteAtlasManager.atlasRegistered += RebuildImage;
        s_Initialized = true;
      }

      m_TrackedTexturelessImages.Add(g);
    }

    private static void UnTrackImage(Image g)
    {
      m_TrackedTexturelessImages.Remove(g);
    }

    protected override void OnDidApplyAnimationProperties()
    {
      SetMaterialDirty();
      SetVerticesDirty();
      SetRaycastDirty();
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
      base.OnValidate();
      m_PixelsPerUnitMultiplier = Mathf.Max(0.01f, m_PixelsPerUnitMultiplier);
    }

#endif
  }
}