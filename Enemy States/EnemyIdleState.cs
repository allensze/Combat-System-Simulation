using UnityEngine;

public class EnemyIdleState : EnemyBaseState
{
  public override void EnterState(EnemyStateManager manager)
  {
  }

  public override void UpdateState(EnemyStateManager manager)
  {
    if (manager.EnemyActions.PlayerDetected())
    {
      if (manager.EnemyActions.PlayerInAttackRange())
      { 
        manager.SwitchState(manager.AttackState);
      }
      else
      {
        manager.SwitchState(manager.MoveState);
      }
    }
    else
    {
      manager.SwitchState(manager.IdleState);
    }
  }

  public override void ExitState(EnemyStateManager manager)
  {
  }
}
