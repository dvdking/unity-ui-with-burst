using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class StressTest : MonoBehaviour
{
  public GameObject Prefab;
  public int Count = 1000;
  public float ChangeTimerDurstion = .5f;
  public Text Dt;
  
  public float ChangeTimer;

  private struct MovingRect
  {
    public float2 Velocity;
    public float4 CurrentColor;
    public float4 TargetColor;
    public float2 Postion;
  }
  
  private readonly List<RectTransform> _transforms = new();
  private readonly List<Graphic> _graphics = new();
  private NativeArray<MovingRect> _data;

  IEnumerator Start()
  {
    yield return null;
    
    _data = new(Count, Allocator.Persistent, NativeArrayOptions.ClearMemory);
    var go = new GameObject("CanvasGo");
    go.AddComponent<RectTransform>();
    go.transform.SetParent(transform);
    go.GetComponent<RectTransform>().anchoredPosition = default;
    go.transform.localScale = Vector3.one;
    
    go.AddComponent<Canvas>();
    
    var lastCanvas = Instantiate(go, transform);

    for (var i = 0; i < Count; i++)
    {
      var p = Instantiate(Prefab, lastCanvas.transform);
      _transforms.Add(p.GetComponent<RectTransform>());
      _graphics.Add(p.GetComponent<Graphic>());

      if (i == 499)
        break;
    }

    for (var i = 1; i < Count/500; i++)
    {
      lastCanvas = Instantiate(lastCanvas, transform);
      var rects = lastCanvas.GetComponentsInChildren<RectTransform>();
      _transforms.AddRange(rects.Where(x => x != lastCanvas.GetComponent<RectTransform>()));
      
      var graphs = lastCanvas.GetComponentsInChildren<Image>();
      foreach (var graph in graphs)
      {
        graph.fillMethod = Random.value > .5f ? Image.FillMethod.Horizontal : Image.FillMethod.Radial360;
        graph.fillMethod = Random.value > .5f ? Image.FillMethod.Radial90 : Image.FillMethod.Radial180;
        graph.fillAmount = Random.value;

        var v = Random.value;
        graph.type = v switch
        {
          > 0.7f => Image.Type.Sliced,
          > 0.5f => Image.Type.Filled,
          > 0.2f => Image.Type.Tiled,
          _ => Image.Type.Simple
        };
        graph.rectTransform.sizeDelta = new Vector2(Random.Range(10, 350), Random.Range(10, 350));
      }
      _graphics.AddRange(graphs.Where(x => x != lastCanvas.GetComponent<Graphic>()));
    }

    ChangeTimer = ChangeTimerDurstion;
  }
  
  private void OnDestroy()
  {
    _data.Dispose();
  }

  void Update()
  {
    if (_data == default || _transforms.Count != _data.Length)
      return;
    var updateJob = new UpdateJob()
    {
      Data = _data,
      Dt = Time.deltaTime
    };
    
    updateJob.Run();
    
    for (var i = 0; i < _transforms.Count; i++)
    {
      var t = _transforms[i];
      var g = _graphics[i];
      var movingRect = _data[i];
      g.color = new(movingRect.CurrentColor.x, movingRect.CurrentColor.y, movingRect.CurrentColor.z, movingRect.CurrentColor.w);
      t.anchoredPosition = new(movingRect.Postion.x, movingRect.Postion.y);
    }

    if (ChangeTimer > ChangeTimerDurstion)
    {
      for (int i = 0; i < _data.Length; i++)
      {
        var t = _data[i];
        {
          t.Velocity = Random.insideUnitCircle * Random.Range(1f, 100f);
          var tTargetColor = Random.ColorHSV();
          tTargetColor.a = Random.value;
          t.TargetColor = new(tTargetColor.r, tTargetColor.g, tTargetColor.b, tTargetColor.a);
        }
        _data[i] = t;
      }
      ChangeTimer = 0f;
    }

    ChangeTimer += Time.deltaTime;
    
    Debug.Log((Time.deltaTime * 1000f).ToString("F"));
  }

  [BurstCompile(FloatPrecision.Low, FloatMode.Fast)]
  private struct UpdateJob : IJob
  {
    [NoAlias]
    public NativeArray<MovingRect> Data;
    
    [ReadOnly]
    public float Dt;
    
    public void Execute()
    {
      for (int i = 0; i < Data.Length; i++)
      {
        var t = Data[i];

        t.Postion += t.Velocity * Dt;
        t.CurrentColor = math.lerp(t.CurrentColor, t.TargetColor, Dt);
        
        Data[i] = t;
      }
      
      //limit position
      for (int i = 0; i < Data.Length; i++)
      {
        var t = Data[i];
        t.Postion = math.clamp(t.Postion, new float2(-1000f, -1000f), new float2(1000f, 1000f));
        Data[i] = t;
      }
    }
  }
}