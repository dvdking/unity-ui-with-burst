using System.Collections;
using System.Collections.Generic;
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
    _data = new NativeArray<MovingRect>(Count, Allocator.Persistent);
    yield return null;
    for (int i = 0; i < Count; i++)
    {
      var p = Instantiate(Prefab, transform);
      _transforms.Add(p.GetComponent<RectTransform>());
      _graphics.Add(p.GetComponent<Graphic>());
      _data[i] = new MovingRect();
      
      if (i % 500 == 0)
        yield return null;
    }

    ChangeTimer = ChangeTimerDurstion;
    
    Dt.transform.SetAsLastSibling();
  }
  
  private void OnDestroy()
  {
    _data.Dispose();
  }

  // Update is called once per frame
  unsafe void Update()
  {
    if (_data == default)
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
          t.Velocity = Random.insideUnitCircle * Random.Range(1f, 1000f);
          var tTargetColor = Random.ColorHSV();
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
    }
  }
}