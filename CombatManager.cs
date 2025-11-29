using NUnit.Framework.Constraints;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Orchestrates combat waves, enemy spawning, and game mode flow.
/// In telemetry mode, it also runs structured experiments comparing Random vs Smart AI
/// and stream  performance data into a CSV format.
/// </summary>
public class CombatManager : MonoBehaviour
{
  public enum GameMode
  {
    Normal
    , Group
    , Mixed
    , Telemetry
  }

  [System.Serializable]
  public class Wave
  {
    public List<WaveEnemy> enemies;
  }

  [System.Serializable]
  public class WaveEnemy
  {
    public GameObject enemyPrefab;
    public int count;
    public string Category;
  }

  private UIManager uiManager;
  private TimeManager timeManager;
  private PlayerManager playerManager;
  private AutoplayStateManager auto;
  private Telemetry telemetry;

  // How many times each wave is repeated in telemetry mode.
  [SerializeField] private int telemetryRuns;

  [Header("Player Data")]
  [SerializeField] private Vector3 playerPos;

  [Header("Enemy Data")]
  [SerializeField] private Vector3 startPos;
  [SerializeField] private float zOffset;
  [SerializeField] private float backRowOffset;
  private int activeEnemies = 0;

  // Live enemy instances that belong to the current wave.
  private List<GameObject> activeWave;
  
  // Cached copy of a wave used when repeating it multiple times for telemetry.
  private Wave cachedWave;

  [Header("Enemy Waves")]
  [SerializeField] private List<Wave> singleEnemyWaves;
  [SerializeField] private List<Wave> groupEnemyWaves;
  [SerializeField] private List<Wave> mixedEnemyWaves;

  // Queue of prefabs waiting to be spawned for the current wave/round.
  [SerializeField] private Queue<GameObject> enemyQueue = new Queue<GameObject>();
  private GameMode currentMode = GameMode.Normal;
  private int currentWave = 0;
  private int telemetryRoundCount = 0;
  private bool isSmartRunning = false;

  void Awake()
  {
    // Find shared managers in the scene.
    uiManager = FindAnyObjectByType<UIManager>();
    timeManager = FindAnyObjectByType<TimeManager>();
    playerManager = FindAnyObjectByType<PlayerManager>();
    auto = FindAnyObjectByType<AutoplayStateManager>();
    telemetry = FindAnyObjectByType<Telemetry>();
  }

  void Start()
  {
    activeWave = new List<GameObject>();

    // Default mode = Normal mode
    SwitchMode(GameMode.Normal);
  }

  void Update()
  {
    // Keyboard shortcuts to quickly switch modes and toggle autoplay for testing.
    if (Input.GetKeyDown(KeyCode.A))
    {
      auto.ToggleAutoplay();
    }

    if (Input.GetKeyDown(KeyCode.G))
    {
      SwitchMode(GameMode.Group);
    }

    if (Input.GetKeyDown(KeyCode.M))
    {
      SwitchMode(GameMode.Mixed);
    }

    if (Input.GetKeyDown(KeyCode.T))
    {
      SwitchMode(GameMode.Telemetry);
    }
    
    if (Input.GetKeyDown(KeyCode.S))
    {
      auto.ToggleSmartPlay();
    }
    
    if (Input.GetKeyDown(KeyCode.R))
    {
      // Scene restart for rapid iteration.
      SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    if (Input.GetKeyDown(KeyCode.Escape))
    {
      Application.Quit();
    }
  }

  private void SwitchMode(GameMode mode)
  {
    ClearCurrentWave();
    timeManager.StartTime();
    currentMode = mode;
    currentWave = 0;
    telemetryRoundCount = 0;

    if (mode == GameMode.Telemetry)
    {
      isSmartRunning = false;
      auto.EnableAutoplay();
    }

    telemetry.DataStream?.WriteLine("AI TYPE, CATEGORY, ENEMIES, RATING, DIFF, WINS, LOSSES, WIN %, ROUND TIME, ENERGY THRUST %, KINETIC SHOT %, ACTIVE RELOAD %, ARC SLASH %");
    StartNextWave();
  }

  private void StartNextWave()
  {
    List<Wave> waveList = GetWaveList();
    
    if (currentWave >= waveList.Count)
    {
      telemetry.RecordSummary();
      return;
    }

    if (currentMode != GameMode.Telemetry || telemetryRoundCount == 0)
    {
      ClearCurrentWave();
    }
    else
    {
      ClearCurrentRound();
    }

    timeManager.StartRoundTimer();

    if (telemetryRoundCount == 0)
    {
      cachedWave = waveList[currentWave];
    }

    Wave activeWaveData = (currentMode == GameMode.Telemetry) ? cachedWave : waveList[currentWave];
    enemyQueue.Clear();

    foreach (var waveEnemy in activeWaveData.enemies)
    {
      for (int i = 0; i < waveEnemy.count; i++)
      {
        enemyQueue.Enqueue(waveEnemy.enemyPrefab);
        //Debug.Log($"{enemyQueue.Count}");
      }
    }

    var waveEnemies = cachedWave.enemies;
    string aiType;

    if (currentMode == GameMode.Telemetry)
    {
      aiType = currentWave >= GetWaveList().Count / 2 ? "Smart" : "Random";
      
      if (currentWave == GetWaveList().Count / 2)
      {
        auto.WeightsInit();
      }
    }
    else
    {
      aiType = "Manual";
    }

    telemetry.SetWaveData(aiType, waveEnemies);
    SpawnNextEnemy();
  }

  private List<Wave> GetWaveList()
  {
    //Debug.Log($"{currentMode}");

    var baseWaves = singleEnemyWaves.Concat(groupEnemyWaves).Concat(mixedEnemyWaves).ToList();

    return currentMode switch
    { GameMode.Group => groupEnemyWaves,
      GameMode.Mixed => mixedEnemyWaves,
      GameMode.Telemetry => baseWaves.Concat(baseWaves).ToList(),
      _ => baseWaves
    };
  }
  
  private void ClearCurrentWave()
  {
    foreach (var enemy in activeWave)
    {
      if (enemy != null)
      {
        Destroy(enemy);
      }
    }

    activeWave.Clear();
    enemyQueue.Clear();
    activeEnemies = 0;
  }

  private void ClearCurrentRound()
  {
    foreach (var enemy in activeWave)
    {
      if (enemy != null)
      {
        Destroy(enemy);
      }
    }

    activeWave.Clear();
    activeEnemies = 0;
  }

  private void SpawnEnemy(Vector3 pos, GameObject enemyPrefab)
  {
    if (enemyPrefab == null)
    {
      return;
    }

    GameObject enemy = Instantiate(enemyPrefab, pos, Quaternion.identity);

    if (enemy.TryGetComponent<EnemyManager>(out var enemyManager))
    {
      uiManager.RegisterEnemy(enemyManager, enemy.GetComponentInChildren<Slider>());
      activeEnemies++;
      activeWave.Add(enemy);
    }

    enemyManager.OnDeath += () => EnemyDefeated(enemy);
  }

  private void SpawnNextEnemy()
  {
    if (enemyQueue.Count == 0)
    {
      return;
    }

    int totalEnemies = enemyQueue.Count;
    
    for (int i = 0; i < totalEnemies; i++)
    {
      GameObject enemyPrefab = enemyQueue.Dequeue();
      Vector3 spawnPos = SpawnPosition(i);
      SpawnEnemy(spawnPos, enemyPrefab);
    }
  }

  private Vector3 SpawnPosition(int index)
  {
    int row = index / 3;
    int col = index % 3;

    float zPos = col switch
    {
      0 => startPos.z,
      1 => startPos.z - zOffset,
      2 => startPos.z + zOffset,
      _ => startPos.z
    };

    float xPos = startPos.x - (row * backRowOffset);
    return new Vector3(xPos, startPos.y, zPos);
  }

  public void EnemyDefeated(GameObject enemy)
  {
    if (currentMode == GameMode.Telemetry && activeEnemies == 1 && enemyQueue.Count == 0)
    {
      telemetry.SetHealthPercent(activeWave);
    }
    
    if (activeWave.Contains(enemy))
    {
      activeWave.Remove(enemy);
    }

    activeEnemies--;
    Destroy(enemy);

    if (activeEnemies <= 0 && enemyQueue.Count == 0)
    {
      telemetry.AddWin();
      uiManager.ShowVictory();

      if (currentMode == GameMode.Telemetry)
      {
        NextRound();

        if (telemetryRoundCount >= telemetryRuns)
        {
          telemetry.SetRoundTime(timeManager.GetRoundTime());
          WaveReset();
        }
        else
        {
          StartNextWave();
        }
      }
      else
      {
        telemetry.SetRoundTime(timeManager.GetRoundTime());
        telemetry.RecordWaveData(); 
        playerManager.PlayerReset();
        currentWave++;
        StartNextWave();
      }
    }
    else if (activeEnemies <= 0)
    {
      SpawnNextEnemy();
    }
  }

  public void OnPlayerDeath()
  {
    telemetry.SetHealthPercent(activeWave);
    telemetry.AddLoss();
    uiManager.ShowDefeat();

    if (currentMode == GameMode.Telemetry)
    {
      NextRound();
      ClearCurrentRound();

      if (telemetryRoundCount >= telemetryRuns)
      {
        telemetry.SetRoundTime(timeManager.GetRoundTime());
        WaveReset();
      }
      else
      {
        StartNextWave();
      }
    }
    else
    {
      telemetry.SetRoundTime(timeManager.GetRoundTime());
      telemetry.RecordWaveData();
      playerManager.PlayerReset();
      ClearCurrentWave();
      currentWave++;
      StartNextWave();
    }
  }

  private void WaveReset()
  {
    telemetry.RecordWaveData();

    telemetry.ResetRoundCounts();
    telemetryRoundCount = 0;
    currentWave++;

    List<Wave> waveList = GetWaveList();
    int totalWaves = waveList.Count;

    if (currentWave == totalWaves / 2)
    {
      telemetry.RecordSummary();
    }
    else if (currentWave >= totalWaves)
    {
      telemetry.RecordSummary();
    }

    if (currentMode == GameMode.Telemetry && !isSmartRunning && currentWave == totalWaves / 2)
    {
      isSmartRunning = true;
      auto.EnableSmartPlay();
      timeManager.StartTime();
    }

    if (currentWave < waveList.Count)
    {
      cachedWave = waveList[currentWave];
      StartNextWave();
    }
    else
    {
      telemetry.DataStream?.Flush();
      Time.timeScale = 0.0f;
    }

    playerManager.PlayerReset();
  }

  private void NextRound()
  {
    float roundTime = timeManager.GetRoundTime();
    telemetry.SetRoundTime(roundTime);
    telemetryRoundCount++;

    if (currentMode == GameMode.Telemetry && auto.CurrentState == auto.SmartAutoState)
    {
      auto.AdjustSmartAIWeights(auto.PlayerActions);
    }

    playerManager.PlayerReset();
  }

  public void ResetPosition()
  {
    if (auto != null && auto.PlayerActions != null)
    {
      auto.PlayerActions.transform.position = playerPos;
      auto.PlayerActions.Animator.SetBool("IsWalk", false);
    }
  }

  public List<GameObject> ActiveWave => activeWave;
  public GameMode CurrentMode => currentMode;
  public int TelemetryRoundCount => telemetryRoundCount;
}
