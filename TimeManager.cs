using System.Collections.Generic;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
  private float roundStartTime = 0.0f;
  private float startTime = 0.0f;
  private Dictionary<string, float> timers = new Dictionary<string, float>();

  public void StartTime()
  {
    startTime = Time.time;
  }  

  public float GetTime()
  {
    return Time.time - startTime;
  }

  public void StartRoundTimer()
  {
    roundStartTime = Time.time;
  }

  public float GetRoundTime()
  {
    return Time.time - roundStartTime;
  }

  public void StartTimer(string key, float duration)
  {
    timers[key] = Time.time + duration;
  }

  public bool IsTimerActive(string key)
  {
    return timers.ContainsKey(key) && Time.time < timers[key];
  }

  public float GetTimeRemaining(string key)
  {
    if (!timers.ContainsKey(key))
    {
      return 0.0f;
    }

    return Mathf.Max(0.0f, timers[key] - Time.time);
  }

  public void ClearTimer(string key)
  {
    if (timers.ContainsKey(key))
    {
      timers.Remove(key);
    }
  }

  public void ClearAllTimers()
  {
    timers.Clear();
  }
}
