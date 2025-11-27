using System.Linq;
using UnityEngine;

public class EnemyAttackState : EnemyBaseState
{
  public override void EnterState(EnemyStateManager manager)
  {
  }

  public override void UpdateState(EnemyStateManager manager)
  {
    var actions = manager.EnemyActions;

    if (actions.IsDead())
    {
      manager.SwitchState(manager.DeathState);
      return;
    }

    bool isRangedEnemy = actions is MushroomActions
                      || actions is WizardActions
                      || actions is DarkMageActions;

    if (isRangedEnemy && actions.PlayerTooClose() && actions.CanRetreat() && !actions.IsRetreating())
    {
      actions.Retreat();
      //manager.SwitchState(manager.MoveState);
      //return;
    }

    if (actions is MushroomActions)
    {
      TryHeal(manager);
      return;
    }

    TryAttack(manager);
  }

  public override void ExitState(EnemyStateManager manager)
  {
  }

  private void TryAttack(EnemyStateManager manager)
  {
    var actions = manager.EnemyActions;
    var player = manager.Player;

    if (actions is SnakeActions snake)
    {
      bool poisoned = player.GetComponentInChildren<PoisonEffect>();
      
      if (!poisoned && !snake.TimeManager.IsTimerActive(snake.PoisonKey))
      {
        snake.PerformPoisonAttack(player);
        snake.TimeManager.StartTimer(snake.PoisonKey, snake.PoisonCD);
        return;
      }
      else if (!snake.TimeManager.IsTimerActive(snake.CDKey))
      {
        snake.PerformAttack(player);
        snake.TimeManager.StartTimer(snake.CDKey, snake.AttackCD);
        return;
      }
    }
      
    if (actions is GolemActions golem)
    {
      if (golem.PlayerInAttackRange() && !golem.TimeManager.IsTimerActive(golem.CDKey))
      {
        golem.Animator.SetBool("IsWalk", false);
        golem.PerformAttack(player);
        golem.TimeManager.StartTimer(golem.CDKey, golem.AttackCD);
        return;
      }

      if (golem.PlayerInRangedRange() && !golem.PlayerInAttackRange() && !golem.TimeManager.IsTimerActive(golem.StoneKey))
      {
        //Debug.Log($"player in range {golem.PlayerInRangedRange()}");
        golem.PerformRangedAttack(player);
        return;
      }
      
      if (!golem.PlayerInRangedRange() && !golem.PlayerInAttackRange())
      {
        manager.SwitchState(manager.MoveState);
        return;
      }
    }

    if (!actions.PlayerInAttackRange())
    {
      manager.SwitchState(manager.MoveState);
      return;
    }

    if (!actions.TimeManager.IsTimerActive(actions.CDKey))
    {
      manager.EnemyActions.PerformAttack(player);
      actions.TimeManager.StartTimer(actions.CDKey, manager.EnemyActions.AttackCD);
    }
  }

  private void TryHeal(EnemyStateManager manager)
  {
    if (manager.EnemyActions is not MushroomActions shroom)
    {
      return;
    }

    //Debug.Log($"{manager.name} attempting to heal");

    if (!shroom.CanHeal())
    {
      manager.SwitchState(manager.IdleState);
      return;
    }
    
    var allies = shroom.DetectAllies();
    var lowestAlly = allies
      .Where(ally => !ally.EnemyActions.IsDead() && ally.EnemyActions.CurrentHealth < (0.9f * ally.EnemyActions.MaxHealth))
      .OrderBy(ally => ally.EnemyActions.CurrentHealth)
      .FirstOrDefault();

    if (lowestAlly == null)
    {
      manager.SwitchState(manager.IdleState);
      return;
    }

    float allyDist = Vector3.Distance(shroom.transform.position, lowestAlly.transform.position);

    if (allyDist > shroom.HealRange)
    {
      manager.SwitchState(manager.MoveState);
      return;
    }

    shroom.Heal(lowestAlly);
  }
}
