using UnityEngine;

public class DarkMageActions : EnemyActions
{
  [SerializeField] private float attack1Damage;
  private bool hasCharged = false;

  public override void PerformAttack(PlayerManager target)
  {
    if (target == null)
    {
      return;
    }

    if (PlayerInAttackRange())
    {
      if (!hasCharged)
      {
        hasCharged = true;
        return;
      }

      animator.SetTrigger("IsAttack");
      target.ChangeHealth(attack1Damage);
    }
  }
}
