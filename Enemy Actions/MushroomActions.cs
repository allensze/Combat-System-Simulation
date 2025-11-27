using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class MushroomActions : EnemyActions
{
  [Header("Support Action")]
  [SerializeField] private Slider healSlider;
  [SerializeField] private float healRange;
  [SerializeField] private float healZRange;
  [SerializeField] private float healThreshold;
  [SerializeField] private float healAmount;
  [SerializeField] private float healCD;
  private Vector3? healTarget = null;
  private Vector3? retreatTarget = null;
  private Vector3 healBoxSize;
  private string healKey;

  protected override void Awake()
  {
    base.Awake();
    healBoxSize = new Vector3(healRange * 2.0f, 0.0f, healZRange);
  }

  protected override void Start()
  {
    base.Start();
    healKey = $"heal_{EnemyManager.GetInstanceID()}";

    if (healSlider != null)
    {
      UIManager.RegisterCooldownSlider(healKey, healSlider);
    }
  }

  protected override void Update()
  {
    base.Update();

    if (healSlider != null)
    {
      float healRemaining = TimeManager.GetTimeRemaining(healKey);
      UIManager.UpdateCDSlider(healKey, healRemaining, healCD);
    }
  }

  protected override void FixedUpdate()
  {
    SnapToGround();
    //if (IsRetreating() && !retreatTarget.HasValue)
    //{
    //  return;
    //}

    if (retreatTarget.HasValue)
    {
      //Debug.Log($"retreat target: {retreatTarget.Value}");
      Vector3 direction = retreatTarget.Value - transform.position;

      if (direction.magnitude > 0.1f)
      {
        Vector3 move = moveSpeed * Time.deltaTime * direction.normalized;
        
        if (!Physics.Raycast(transform.position, direction.normalized, out RaycastHit hit, move.magnitude + 0.1f, LayerMask.GetMask("Wall")))
        {
          rb.MovePosition(transform.position + move);
        }
        else
        {
          retreatTarget = null;
          isRetreating = false;
          animator.SetBool("IsWalk", false);
        }

        animator.SetBool("IsWalk", true);
      }
      else
      {
        animator.SetBool("IsWalk", false);
      }
    }
    else if (healTarget.HasValue)
    {
      Vector3 direction = (healTarget.Value - transform.position);

      if (direction.magnitude > 0.1f)
      {
        Vector3 move = moveSpeed * Time.deltaTime * direction.normalized;
        rb.MovePosition(transform.position + move);
        animator.SetBool("IsWalk", true);
      }
      else
      {
        healTarget = null;
        animator.SetBool("IsWalk", false);
      }
    }
  }

  public override void Retreat()
  {
    //Debug.Log("Mushroom retreating");
    if (!CanRetreat())
    {
      return;
    }

    Vector3 direction = (transform.position - Player.transform.position).normalized;
    retreatTarget = transform.position + direction * safeDistance;
    retreatEndTime = Time.time + retreatDuration;
    TimeManager.StartTimer(retreatKey, retreatCD);
    isRetreating = true;
  }

  public void Heal(EnemyManager target)
  {
    if (target != null)
    {
      //Debug.Log($"Healing {target.name}");
      animator.SetTrigger("IsAttack");
      ChangeHealth(target, healAmount);
      TimeManager.StartTimer(healKey, healCD);
    }
  }

  public void MoveTowardsAlly(Vector3 targetPos)
  {
    healTarget = targetPos;
  }

  //public bool NeedsToHeal()
  //{
  //  if (!CanHeal())
  //  {
  //    return false;
  //  }

  //  var allies = DetectAllies();
  //  return allies.Any(ally => !ally.EnemyActions.IsDead() && ally.EnemyActions.CurrentHealth < 0.9f * ally.EnemyActions.MaxHealth);
  //}

  public bool CanHeal()
  {
    return !TimeManager.IsTimerActive(healKey);
  }

  public List<EnemyManager> DetectAllies()
  {
    List<EnemyManager> nearby = new List<EnemyManager>();
    Collider[] allies = Physics.OverlapBox(transform.position, healBoxSize / 2, Quaternion.identity, LayerMask.GetMask("Enemy"));

    foreach (Collider ally in allies)
    {
      if (ally.TryGetComponent<EnemyManager>(out var target))
      {
        nearby.Add(target);
      }
    }

    if (!nearby.Contains(this.EnemyManager))
    {
      nearby.Add(this.EnemyManager);
    }

    return nearby;
  }

  public EnemyManager GetLowestAlly()
  {
    return DetectAllies().Where(ally => !ally.EnemyActions.IsDead() && ally.EnemyActions.CurrentHealth < healThreshold * ally.EnemyActions.MaxHealth)
                         .OrderBy(ally => ally.EnemyActions.CurrentHealth)
                         .FirstOrDefault();
  }

  public float HealRange => healRange;
}
