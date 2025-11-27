using UnityEngine;
using UnityEngine.UI;

public class SnakeActions : EnemyActions
{
  [SerializeField] private GameObject poisonDotPrefab;
  [SerializeField] private float biteDamage;
  [SerializeField] private Slider poisonSlider;
  [SerializeField] private float poisonCD;
  [SerializeField] private float poisonTickDamage;
  [SerializeField] private float poisonDuration;
  [SerializeField] private float poisonInterval;
  private string poisonKey;

  protected override void Start()
  {
    base.Start();
    poisonKey = $"poison_{EnemyManager.GetInstanceID()}";

    if (poisonSlider != null)
    {
      //Debug.Log("poison registered");
      UIManager.RegisterCooldownSlider(poisonKey, poisonSlider);
    }
  }

  protected override void Update()
  {
    base.Update();

    if (poisonSlider != null)
    {
      float poisonRemaining = TimeManager.GetTimeRemaining(poisonKey);
      UIManager.UpdateCDSlider(poisonKey, poisonRemaining, poisonCD);
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
      target.ChangeHealth(biteDamage);
    }
  }
  
  public void PerformPoisonAttack(PlayerManager target)
  {
    if (target == null)
    {
      return;
    }

    if (PlayerInAttackRange() && !TimeManager.IsTimerActive(poisonKey))
    {
      ApplyPoison(target);
      animator.SetTrigger("IsAttack");
      TimeManager.StartTimer(poisonKey, poisonCD);
    }
  }

  private void ApplyPoison(PlayerManager target)
  {
    if (poisonDotPrefab != null)
    {
      bool poisoned = target.GetComponentInChildren<PoisonEffect>();

      if (!poisoned)
      {
        //Debug.Log("applying poison");
        GameObject dot = Instantiate(poisonDotPrefab, target.transform);
        dot.transform.localPosition = Vector3.zero;

        if (dot.TryGetComponent<PoisonEffect>(out var poison))
        {
          poison.SetTickDamage(poisonTickDamage);
          poison.SetDuration(poisonDuration);
          poison.SetDamageInterval(poisonInterval);
        }
      }
    }
  }

  public string PoisonKey => poisonKey;
  public float PoisonCD => poisonCD;
}
