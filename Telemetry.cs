using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Telemetry : MonoBehaviour
{
  private PlayerManager playerManager;
  private CombatManager combatManager;
  private TimeManager timeManager;
  private AutoplayStateManager auto;

  // Telemetry Stats
  public string AIType { get; private set; }
  public string Category { get; private set; }
  public string Enemies { get; private set; }
  public float DisplayRating { get; private set; }
  public float TotalPlayerHealthPercent { get; private set; }
  public float TotalEnemyHealthPercent { get; private set; }
  public float Wins { get; private set; }
  public float Losses { get; private set; }
  public float WinPercent { get; private set; }
  public float RoundTime { get; private set; }


  // Ability Usage
  public float Ability1Count { get; private set; }
  public float Ability1Percent { get; private set; }
  public float Ability2Count { get; private set; }
  public float Ability2Percent { get; private set; }
  public float Ability3Count { get; private set; }
  public float Ability3Percent { get; private set; }
  public float Ability4Count { get; private set; }
  public float Ability5Percent { get; private set; }
  public float Ability5Count { get; private set; }
  public float Ability4Percent { get; private set; }
  public float TotalAbilityCount { get; private set; }


  // End of Round Data
  public float TotalTime { get; private set; }
  public float WinTotal {  get; private set; }
  public float LossTotal {  get; private set; }
  public float Ability1DamageTotal {  get; private set; }
  public float Ability2DamageTotal {  get; private set; }
  public float Ability4DamageTotal {  get; private set; }
  public float TotalDamage {  get; private set; }
  public float PerfectReloadCount {  get; set; }

  private static StreamWriter dataStream;

  private void Awake()
  {
    playerManager = FindAnyObjectByType<PlayerManager>();
    timeManager = FindAnyObjectByType<TimeManager>();
    combatManager = FindAnyObjectByType<CombatManager>();
    auto = FindAnyObjectByType<AutoplayStateManager>();

    if (dataStream == null)
    {
      string date = DateTime.Now.ToString("yyyy-MM-dd");
      string time = DateTime.Now.ToString("hh") + "_" + DateTime.Now.ToString("mm");
      string fileName = $"TelemetryData_{date}_{time}.csv";
      dataStream = new StreamWriter(fileName, true);
      //dataStream?.WriteLine("AI TYPE, CATEGORY, ENEMIES, RATING, DIFF, WINS, LOSSES, WIN %, ROUND TIME, PROD %, PEWPEW %, ACTIVE RELOAD %, SIDESWIPE %, FINISHER %");
      //dataStream?.WriteLine("AI TYPE, CATEGORY, ENEMIES, RATING, DIFF, WINS, LOSSES, WIN %, ROUND TIME, PROD %, PEWPEW %, ACTIVE RELOAD %, SIDESWIPE %");
    }
  }

  private void OnApplicationQuit()
  {
    dataStream?.Flush();
    dataStream?.Close();
  }

  public void SetWaveData(string type, List<CombatManager.WaveEnemy> wave)
  {
    AIType = type;
    Category = wave.Count == 1 ? wave[0].Category : "Mixed";
    List<string> waveDescription = new List<string>();

    foreach (var enemy in wave)
    {
      string label = $"{enemy.count}x{enemy.enemyPrefab.name.Replace("(Clone)", "")}";
      waveDescription.Add(label);
    }

    Enemies = string.Join("/", waveDescription);
  }

  public void SetHealthPercent(List<GameObject> enemies)
  {
    float playerHealthPercent = (playerManager.CurrentHealth / playerManager.MaxHealth) * 100.0f;
    TotalPlayerHealthPercent += playerHealthPercent;

    float enemyHealthRemaining = 0.0f;
    float enemyHealthTotal = 0.0f;

    foreach (var enemy in enemies)
    {
      if (enemy != null && enemy.TryGetComponent<EnemyActions>(out var target))
      {
        enemyHealthRemaining += target.CurrentHealth;
        enemyHealthTotal += target.MaxHealth;
        TotalDamage = Mathf.Min(target.DamageTaken, target.MaxHealth);
      }
    }

    float enemyHealthPercent = (enemyHealthTotal > 0) ? enemyHealthRemaining / enemyHealthTotal * 100.0f : 0.0f;
    TotalEnemyHealthPercent += enemyHealthPercent;
  }

  public void AddWin()
  {
    Wins++;
    WinTotal++;
  }

  public void AddLoss()
  {
    Losses++;
    LossTotal++;
  }

  public void SetRoundTime(float time)
  {
    RoundTime = time;
  }

  public void IncrementAbility(string ability)
  {
    switch (ability)
    {
      case "melee":
        Ability1Count++;
        break;
      case "ranged":
        Ability2Count++;
        break;
      case "reload":
        Ability3Count++;
        break;
      case "aoe":
        Ability4Count++;
        break;
      case "finisher":
        Ability5Count++;
        break;
    }
    
    TotalAbilityCount++;
  }

  public float GetAbilityCount(string ability)
  {
    return ability switch
    {
      "melee" => Ability1Count,
      "ranged" => Ability2Count,
      "reload" => Ability3Count,
      "aoe" => Ability4Count,
      "finisher" => Ability5Count,
      _ => 0.0f
    };
  }


  public float GetAbilityUsagePercent(string ability)
  {
    if (TotalAbilityCount == 0)
    {
      return 0.0f;
    }
    
    return ability switch
    {
      "melee" => Ability1Count / TotalAbilityCount * 100.0f,
      "ranged" => Ability2Count / TotalAbilityCount * 100.0f,
      "reload" => Ability3Count / TotalAbilityCount * 100.0f,
      "aoe" => Ability4Count / TotalAbilityCount * 100.0f,
      "finisher" => Ability5Count / TotalAbilityCount * 100.0f,
      _ => 0.0f
    };
  }

  public void IncrementAbilityDamage(string ability, float damage)
  {
    TotalDamage += damage;

    switch (ability)
    {
      case "melee":
        Ability1DamageTotal += damage;
        break;
      case "ranged":
        Ability2DamageTotal += damage;
        break;
      case "aoe":
        Ability4DamageTotal += damage;
        break;
      //case "finisher":
      //  finisherDamageTotal += damage;
      //  break;
    }
  }

  public float GetAbilityDamage(string ability)
  {
    return ability switch
    {
      "melee" => Ability1DamageTotal,
      "ranged" => Ability2DamageTotal,
      "aoe" => Ability4DamageTotal,
      //"finisher" => Ability5DamageTotal,
      _ => 0.0f
    };
  }  

  public void ResetRoundCounts()
  {
    TotalPlayerHealthPercent = 0.0f;
    TotalEnemyHealthPercent = 0.0f;
    Wins = 0.0f;
    Losses = 0.0f;
    WinPercent = 0.0f;
    Ability1Count = 0.0f;
    Ability2Count = 0.0f;
    Ability3Count = 0.0f;
    Ability4Count = 0.0f;
    Ability5Count = 0.0f;
    TotalAbilityCount = 0.0f;

    foreach (var enemy in combatManager.ActiveWave)
    {
      if (enemy != null && enemy.TryGetComponent<EnemyActions>(out var target))
      {
        target.ResetDamageTaken();
      }
    }
  }

  private void ResetSummaryCounts()
  {
    WinTotal = 0.0f;
    LossTotal = 0.0f;
    Ability1DamageTotal = 0.0f;
    Ability2DamageTotal = 0.0f;
    Ability4DamageTotal = 0.0f;
    TotalDamage = 0.0f;
    PerfectReloadCount = 0.0f;
  }


  public void RecordWaveData()
  {
    float rating = 0.0f;
    float smartRating = 0.0f;
    float healthPercentRating = TotalPlayerHealthPercent - TotalEnemyHealthPercent;
    bool isTelemetry = combatManager.CurrentMode == CombatManager.GameMode.Telemetry;
    float rounds = combatManager.TelemetryRoundCount > 0.0f ? combatManager.TelemetryRoundCount : 1.0f;

    if (rounds <= 0.0f)
    {
      rounds = 1.0f;
    }

    if (auto.CurrentState == auto.SmartAutoState)
    {
      smartRating = isTelemetry ? healthPercentRating / rounds : healthPercentRating;
    }
    else if (auto.CurrentState == auto.AutoState)
    {
      rating = isTelemetry ? healthPercentRating / rounds : healthPercentRating;
    }
    else
    {
      rating = healthPercentRating;
    }

    DisplayRating = auto.CurrentState == auto.SmartAutoState ? smartRating : rating;
    float diffRating = auto.CurrentState == auto.SmartAutoState ? rating - smartRating : 0.0f;
    float totalRounds = combatManager.CurrentMode == CombatManager.GameMode.Telemetry ? combatManager.TelemetryRoundCount : Wins + Losses;
    WinPercent = totalRounds > 0.0f ? (Wins / totalRounds) * 100 : 0.0f;
    Ability1Percent = GetAbilityUsagePercent("melee");
    Ability2Percent = GetAbilityUsagePercent("ranged");
    Ability3Percent = GetAbilityUsagePercent("reload");
    Ability4Percent = GetAbilityUsagePercent("aoe");
    Ability5Percent = GetAbilityUsagePercent("finisher");
    //dataStream?.WriteLine($"{aiType}, {category}, {enemies}, {displayRating}%, {diffRating}%, {wins}, {losses}, {winPercent:#0}%, {roundTime:#0.0}, {ability1Percent:#0.0}%, {ability2Percent:#0.0}%, {ability3Percent:#0.0}%, {ability4Percent:#0.0}%, {ability5Percent:#0.0}%");
    dataStream?.WriteLine($"{AIType}, {Category}, {Enemies}, {DisplayRating}%, {diffRating}%, {Wins}, {Losses}, {WinPercent:#0}%, {RoundTime:#0.0}, {Ability1Percent:#0.0}%, {Ability2Percent:#0.0}%, {Ability3Percent:#0.0}%, {Ability4Percent:#0.0}%");
    dataStream?.Flush();
    ResetRoundCounts();
  }

  public void RecordSummary()
  {
    TotalDamage = Ability1DamageTotal + Ability2DamageTotal + Ability4DamageTotal;
    TotalTime = timeManager.GetTime();
    float dps = TotalDamage / TotalTime;
    dataStream?.WriteLine("TOTAL ROUNDS, TOTAL WINS, TOTAL LOSSES, TOTAL TIME, DPS, TOTAL DAMAGE, ENERGY THRUST, KINETIC SHOT, ARC SLASH, PERFECT RELOAD COUNT");
    dataStream?.WriteLine($"{WinTotal + LossTotal}, {WinTotal}, {LossTotal}, {TotalTime:#0.00}, {dps:#0.00}, {TotalDamage:#0.0}, {Ability1DamageTotal:#0.0}, {Ability2DamageTotal:#0.0}, {Ability4DamageTotal:#0.0}, {PerfectReloadCount}");
    dataStream?.Flush();
    ResetSummaryCounts();
  }

  public StreamWriter DataStream => dataStream;
}