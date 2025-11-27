using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AutoplaySmartState : AutoplayBaseState
{
  private class SmartAction
  {
    public float score;
    public Action execute;

    public SmartAction(float score, Action execute)
    {
      this.score = score;
      this.execute = execute;
    }
  }

  private int shotsFired;
  private int openingShots;

  public override void EnterState(AutoplayStateManager auto)
  {
    auto.PlayerActions.SetAutoplay(true);
    shotsFired = 0;
    openingShots = UnityEngine.Random.Range(2, 6);
  }

  public override void UpdateState(AutoplayStateManager auto)
  {
    var player = auto.PlayerActions;
    var time = player.TimeManager;
    var allEnemies = player.GetAliveEnemies();

    if (allEnemies.Count == 0)
    {
      player.StopMovement();
      player.Animator.SetBool("IsWalk", false);
      player.Animator.Play("Idle");
      return;
    }

    var closestEnemy = player.DetectClosestEnemy(player.transform.position, allEnemies);
    float distance = Vector3.Distance(player.transform.position, closestEnemy.transform.position);

    // Dynamically Adjust Weights
    float meleeWeight = player.Ability1Weight;
    float rangedWeight = player.Ability2Weight;
    float reloadWeight = player.Ability3Weight;
    float aoeWeight = player.Ability4Weight;

    if (distance > player.MeleeRange)
    {
      rangedWeight *= player.Ability2WeightScale * 0.75f;
    }
    else
    {
      meleeWeight *= player.Ability1WeightScale * 1.25f;
    }

    if (allEnemies.Count > 0)
    {
      aoeWeight *= player.Ability4WeightScale;
    }

    if (player.CurrentAmmo <= 1)
    {
      reloadWeight *= player.Ability3WeightScale;
    }

    if (player.EnemyInHitbox())
    {
      player.StopMovement();
    }

    var actions = new List<SmartAction>();
    var meleeTargets = player.DetectEnemies(player.MeleeRange);

    // Finisher
    //if (!time.IsTimerActive("finisher"))
    //{
    //  actions.Add(new SmartAction(player.Ability5Weight, () => player.Finisher()));
    //}

    // Melee
    if (!time.IsTimerActive("melee") && meleeTargets.Count > 0)
    {
      actions.Add(new SmartAction(meleeWeight, () => player.MeleeAttack()));
    }

    // AOE
    if (!time.IsTimerActive("aoe") && meleeTargets.Count > 0)
    {
      actions.Add(new SmartAction(aoeWeight, () => player.AOEAttack()));
    }

    // Ranged
    var rangedTargets = player.DetectEnemies(player.RangedRange);

    if (!time.IsTimerActive("ranged") && rangedTargets.Count > 0 && player.CurrentAmmo > 0)
    {
      actions.Add(new SmartAction(rangedWeight, () =>
      { 
        player.RangeAttack();
        shotsFired++; 
      }));
    }

    // Reload
    if (player.CurrentAmmo <= 2 && !time.IsTimerActive("reload") && (time.IsTimerActive("ranged") || player.CurrentAmmo <= 0))
    {
      actions.Add(new SmartAction(reloadWeight, () => player.Reload()));
    }

    // Execute highest scoring action
    if (actions.Count > 0)
    {
      //float totalWeight = actions.Sum(a => a.score);
      //float roll = UnityEngine.Random.Range(0, totalWeight);
      //float cumulative = 0.0f;

      //foreach (var action in actions)
      //{
      //  cumulative += action.score;

      //  if (roll <= cumulative)
      //  {
      //    action.execute.Invoke();
      //    break;
      //  }
      //}
      actions.Sort((a, b) => b.score.CompareTo(a.score));
      actions[0].execute.Invoke();
      bool shouldMove = !player.EnemyInHitbox() && (shotsFired >= openingShots);


      if (shouldMove)
      {
        player.MoveTowardsEnemy(closestEnemy);
      }
    }
    else if (!player.EnemyInHitbox())
    {
      player.MoveTowardsEnemy(closestEnemy);
    }
    else
    {
      player.StopMovement();
    }
  }

  public override void ExitState(AutoplayStateManager auto)
  {
    auto.PlayerActions.SetAutoplay(false);
  }
}
