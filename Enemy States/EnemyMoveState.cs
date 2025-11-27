using UnityEngine;

public class EnemyMoveState : EnemyBaseState
{
  public override void EnterState(EnemyStateManager manager)
  {
  }

  public override void UpdateState(EnemyStateManager manager)
  {
    var actions = manager.EnemyActions;

    if (actions.IsDead())
    {
      return;
    }

    bool isRangedEnemy = actions is MushroomActions
                      || actions is WizardActions
                      || actions is DarkMageActions
                      || actions is GolemActions;

    if (isRangedEnemy)
    {
      if (actions is MushroomActions shroom )
      {
        var lowestAlly = shroom.GetLowestAlly();

        if (actions.PlayerTooClose() && !shroom.IsRetreating() && shroom.CanRetreat())
        {
          shroom.Retreat();
          return;
        }

        if (lowestAlly != null)
        {
          float distance = Vector3.Distance(shroom.transform.position, lowestAlly.transform.position);

          if (distance <= shroom.HealRange && shroom.CanHeal())
          {
            manager.SwitchState(manager.AttackState);
            return;
          }
          else
          {
            shroom.MoveTowardsAlly(lowestAlly.transform.position);
            return;
          }
        }

        manager.SwitchState(manager.IdleState);
        return;
      }

      if (actions is GolemActions golem)
      {
        if (golem.PlayerInAttackRange())
        {
          manager.SwitchState(manager.AttackState);
          return;
        }

        if (golem.PlayerInRangedRange() && !golem.TimeManager.IsTimerActive(golem.StoneKey))
        {
          golem.PerformRangedAttack(manager.Player);
        }

        golem.MoveTowardsPlayer();
        return;
      }

      if (actions.PlayerTooClose())
      {
        if (!actions.IsRetreating() && actions.CanRetreat())
        {
          actions.Retreat();
          return;
        }
      }
      else if (!actions.PlayerInAttackRange() && actions is not MushroomActions)
      {
        actions.MoveTowardsPlayer();
        return;
      }
      else
      {
        actions.Animator.SetBool("IsWalk", false);
        manager.SwitchState(manager.AttackState);
        return;
      }
    }

    if (!isRangedEnemy)
    {
      if (actions.PlayerInAttackRange())
      {
        actions.Animator.SetBool("IsWalk", false);
        manager.SwitchState(manager.AttackState);
        return;
      }
      else
      {
        actions.MoveTowardsPlayer();
      }
    }
    
    if (!actions.PlayerDetected())
    {
      actions.Animator.SetBool("IsWalk", false);
      manager.SwitchState(manager.IdleState);
    }
  }

  public override void ExitState(EnemyStateManager manager)
  {
  }
}
