using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class AutoplayStateManager : MonoBehaviour
{
  AutoplayBaseState currentState;
  AutoplayNormalState normalState;
  AutoplayAutoState autoState;
  AutoplaySmartState smartAutoState;

  [SerializeField] private CombatManager combatManager;
  [SerializeField] private PlayerManager player;
  [SerializeField] private PlayerActions playerActions;
  private Telemetry telemetry;
  //[SerializeField] private EnemyManager enemyManager;

  [Header("Autoplay Speed")]
  [SerializeField] private float regularSpeed;
  [SerializeField] private float normalSpeed;
  [SerializeField] private float fastSpeed;

  [SerializeField] private float movementDuration;
  private float roundStartTime;

  private enum AutoplaySpeed
  {
    Off
    , Normal
    , Fast
  }

  private AutoplaySpeed autoplaySpeed = AutoplaySpeed.Off;

  void Start()
  {
    telemetry = FindAnyObjectByType<Telemetry>();
    normalState = new AutoplayNormalState();
    autoState = new AutoplayAutoState();
    smartAutoState = new AutoplaySmartState();
    currentState = normalState;
    currentState.EnterState(this);
  }

  void Update()
  {
    if (autoplaySpeed != AutoplaySpeed.Off)
    {
      currentState.UpdateState(this);
      Time.timeScale = (autoplaySpeed == AutoplaySpeed.Fast) ? fastSpeed : normalSpeed;
    }
    else
    {
      Time.timeScale = regularSpeed;
    }
  }

  public void SwitchState(AutoplayBaseState newState)
  {
    if (currentState == newState)
    {
      return;
    }

    if (!player.IsDead())
    {
      currentState.ExitState(this);
      currentState = newState;
      currentState.EnterState(this);
    }
  }

  public void ToggleAutoplay()
  {
    autoplaySpeed = (AutoplaySpeed)(((int)autoplaySpeed + 1) % 3);

    if (autoplaySpeed == AutoplaySpeed.Normal)
    {
      SwitchState(autoState);
      Debug.Log("Autoplay - Normal");
    }
    else if (autoplaySpeed == AutoplaySpeed.Fast)
    {
      Debug.Log("Autoplay - Fast");
    }
    else
    {
      SwitchState(NormalState);
      Debug.Log("Autoplay - Off");
    }
  }

  public void EnableAutoplay()
  {
    autoplaySpeed = AutoplaySpeed.Fast;
    SwitchState(AutoState);
  }

  public void ToggleSmartPlay()
  {
    autoplaySpeed = (AutoplaySpeed)(((int)autoplaySpeed + 1) % 3);

    if (autoplaySpeed == AutoplaySpeed.Normal)
    {
      SwitchState(smartAutoState);
      Debug.Log("Smartplay - Normal");
    }
    else if (autoplaySpeed == AutoplaySpeed.Fast)
    {
      Debug.Log("Smartplay - Fast");
    }
    else
    {
      SwitchState(NormalState);
      Debug.Log("Smartplay - Off");
    }
  }

  public void EnableSmartPlay()
  {
    autoplaySpeed = AutoplaySpeed.Fast;
    SwitchState(smartAutoState);
  }

  public void ResetRoundStartTime()
  {
    roundStartTime = Time.time;
  }

  public void WeightsInit()
  {
    Debug.Log("Attempt to initialize weights.");
    if (playerActions.Ability1Weight <= 0)
    {
      playerActions.Ability1Weight = 80.0f;
      playerActions.Ability2Weight = 60.0f;
      playerActions.Ability3Weight = 50.0f;
      playerActions.Ability4Weight = 40.0f;
      playerActions.Ability5Weight = 20.0f;
      Debug.Log("Weights initialized.");
    }
  }

  public void AdjustSmartAIWeights(PlayerActions player)
  {
    if (telemetry.AIType != "Smart" || player == null)
    {
      return;
    }

    float winPercent = telemetry.WinPercent;
    float totalAbilities = telemetry.TotalAbilityCount;

    Adjust("melee", player.Ability1Weight, value => player.SetAbilityWeight("melee", value));
    Adjust("ranged", player.Ability2Weight, value => player.SetAbilityWeight("ranged", value));
    Adjust("reload", player.Ability3Weight, value => player.SetAbilityWeight("reload", value));
    Adjust("aoe", player.Ability4Weight, value => player.SetAbilityWeight("aoe", value));
    //Adjust("finisher", player.Ability5Weight, value => player.SetAbilityWeight("finisher", value));

    void Adjust(string ability, float currentWeight, Action<float> setter)
    {
      float count = telemetry.GetAbilityCount(ability);
      float damage = telemetry.GetAbilityDamage(ability);
      float percent = telemetry.GetAbilityUsagePercent(ability);
      float dps = (count > 0) ? damage / count : 0;
      float adjustment = 0.0f;

      if (dps > 20.0f && percent < 20.0f)
      {
        adjustment = +15.0f;
      }
      else if (dps < 10.0f && percent > 50.0f)
      {
        adjustment = -15.0f;
      }

      float newWeight = Mathf.Round(currentWeight + adjustment);
      newWeight = Mathf.Clamp(newWeight, 10.0f, 300.0f);
      setter(newWeight);
    }

    float sum = player.Ability1Weight + player.Ability2Weight + player.Ability3Weight + player.Ability4Weight;
    
    if (sum == 0)
    {
      return;
    }

    float scale = 300.0f / sum;
    player.SetAbilityWeight("melee", player.Ability1Weight * scale);
    player.SetAbilityWeight("ranged", player.Ability2Weight * scale);
    player.SetAbilityWeight("reload", player.Ability3Weight * scale);
    player.SetAbilityWeight("aoe", player.Ability4Weight * scale);
    //player.SetAbilityWeight("finisher", player.Ability5Weight * scale);

    //if (telemetry.Ability1Percent > 50.0f && telemetry.WinPercent < 50.0f)
    //{
    //  player.SetAbilityWeight("melee", player.Ability1Weight * 0.9f);
    //  Debug.Log($"{player.Ability1Weight}");
    //}

    //if (telemetry.Ability2Percent < 20.0f && telemetry.WinPercent > 60.0f)
    //{
    //  player.SetAbilityWeight("ranged", player.Ability2Weight * 1.1f);
    //  Debug.Log($"{player.Ability2Weight}");
    //}

    //if (telemetry.Ability3Percent > 30)
    //{
    //  player.SetAbilityWeight("reload", player.Ability3Weight * 0.9f);
    //  Debug.Log($"{player.Ability3Weight}");
    //}

    //if (telemetry.Ability4Percent < 10.0f && telemetry.WinPercent < 50.0f)
    //{
    //  player.SetAbilityWeight("aoe", player.Ability4Weight * 0.85f);
    //  Debug.Log($"{player.Ability4Weight}");
    //}

    //player.ClampAbilityWeights(0.0f, 200.0f);
  }

  public AutoplayBaseState CurrentState => currentState;
  public AutoplayNormalState NormalState => normalState;
  public AutoplayAutoState AutoState => autoState;
  public AutoplaySmartState SmartAutoState => smartAutoState;
  public PlayerActions PlayerActions => playerActions;
  public float RoundStartTime => roundStartTime;
  public float MovementDuration => movementDuration;
}
