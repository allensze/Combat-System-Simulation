using UnityEngine;

public class HarpyActions : EnemyActions
{
  [SerializeField] private float attack1Damage;

  public override void PerformAttack(PlayerManager target)
  {
    if (target == null)
    {
      return;
    }

    if (PlayerInAttackRange())
    {
      animator.SetTrigger("IsAttack");
      target.ChangeHealth(attack1Damage);
    }
  }
}
