using System;
using System.Collections.Generic;
using UnityEngine;

public class AutoplayAutoState : AutoplayBaseState
{
  public override void EnterState(AutoplayStateManager auto)
	{
    auto.PlayerActions.SetAutoplay(true);
	}

	public override void UpdateState(AutoplayStateManager auto)
  {
		var player = auto.PlayerActions;
		var time = player.TimeManager;
		var meleeTargets = player.DetectEnemies(player.MeleeRange);
		var rangedTargets = player.DetectEnemies(player.RangedRange);
		var closestTargets = player.DetectClosestEnemy(player.transform.position, player.GetAliveEnemies());

		if (!player.EnemyInHitbox() && Time.time < auto.RoundStartTime + auto.MovementDuration)
		{
			player.MoveTowardsEnemy(closestTargets);
			return;
		}

		List<Action> highPriority = new List<Action>();
		List<Action> midPriority = new List<Action>();
		List<Action> lowPriority = new List<Action>();

		if (!time.IsTimerActive("melee") && meleeTargets.Count > 0)
		{
			highPriority.Add(() => player.MeleeAttack());
		}

		if (!time.IsTimerActive("ranged") && rangedTargets.Count > 0)
		{
			lowPriority.Add(() => player.RangeAttack());
		}

		if (!time.IsTimerActive("aoe") && meleeTargets.Count > 0)
		{
			midPriority.Add(() => player.AOEAttack());
		}

		//if (!time.IsTimerActive("finisher"))
		//{
		//	var lowHPEnemies = meleeTargets.FindAll(e => e.EnemyActions.CurrentHealth < 0.2f);
		//	if (lowHPEnemies.Count > 0)
		//		highPriority.Add(() => player.Finisher());
		//}

		if (!time.IsTimerActive("reload"))
		{
			lowPriority.Add(() => player.Reload());
		}

		List<Action> actionQueue = highPriority.Count > 0 ? highPriority : midPriority.Count > 0 ? midPriority : lowPriority;
		
		if (actionQueue.Count > 0)
		{
			int index = UnityEngine.Random.Range(0, actionQueue.Count);
			actionQueue[index].Invoke();
		}
		else if (!player.EnemyInHitbox())
    {
      player.MoveTowardsEnemy(closestTargets);
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
