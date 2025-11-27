using UnityEngine;
using UnityEngine.UI;

public class GolemActions : EnemyActions
{
  [SerializeField] private float meleeDamage;
  [SerializeField] private Slider rangedSlider;
  [SerializeField] private GameObject projectilePrefab;
  [SerializeField] private Transform firePoint;
  [SerializeField] private float rangedRange;
  [SerializeField] private float maxTravelDistance;
  [SerializeField] private float rangedCD;
  [SerializeField] private float rangedDamage;
  [SerializeField] private float rangedSpeed;
  private string stoneKey;

  protected override void Start()
  {
    base.Start();
    stoneKey = $"stone_{EnemyManager.GetInstanceID()}";
    
    if (rangedSlider != null)
    {
      UIManager.RegisterCooldownSlider(stoneKey, rangedSlider);
    }
  }

  protected override void Update()
  {
    base.Update();

    if (rangedSlider != null)
    {
      float rangedRemaining = TimeManager.GetTimeRemaining(stoneKey);
      UIManager.UpdateCDSlider(stoneKey, rangedRemaining, rangedCD);
    }
  }

  public override void PerformAttack(PlayerManager target)
  {
    if (target == null)
    {
      return;
    }

    if (PlayerInAttackRange())
    {
      animator.SetTrigger("IsAttack");
      target.ChangeHealth(meleeDamage);
    }
  }

  public void PerformRangedAttack(PlayerManager target)
  {
    if (target == null)
    {
      return;
    }

    if (PlayerInRangedRange())
    {
      animator.SetTrigger("IsAttack");
      FireProjectile(target);
      TimeManager.StartTimer(stoneKey, rangedCD);
    }
  }

  private void FireProjectile(PlayerManager target)
  {
    if (projectilePrefab != null && firePoint != null)
    {
      GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
      Vector3 direction = (target.transform.position - firePoint.position).normalized;

      if (projectile.TryGetComponent<Rigidbody>(out var rb))
      {
#pragma warning disable CS0618
        rb.velocity = direction * rangedSpeed;
#pragma warning restore CS0618
      }

      if (projectile.TryGetComponent<Projectile>(out var proj))
      {
        proj.SetTravelDistance(maxTravelDistance);
        proj.SetDamage(rangedDamage);
      }
    }
  }

  public bool PlayerInRangedRange()
  {
    return Vector3.Distance(transform.position, Player.transform.position) <= rangedRange;
  }

  public override bool CanRetreat()
  {
    return false;
  }

  public string StoneKey => stoneKey;
  public float RangedCD => rangedCD;
}
