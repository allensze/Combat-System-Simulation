using UnityEngine;

public class PoisonEffect : MonoBehaviour
{
  private PlayerManager player;

  private float tickDamage;
  private float duration;
  private float damageInterval;

  private float elapsedTime = 0.0f;
  private float damageTimer = 0.0f;

  void Start()
  {
    player = transform.root.GetComponent<PlayerManager>();

    if (player == null )
    {
      player = FindAnyObjectByType<PlayerManager>();
    }
  }

  void Update()
  {
    if (player == null)
    {
      return;
    }

    elapsedTime += Time.deltaTime;
    damageTimer += Time.deltaTime;

    if (damageTimer >= damageInterval)
    {
      player.ChangeHealth(tickDamage, true);
      damageTimer = 0.0f;
    }

    if (elapsedTime >= duration)
    {
      Destroy(gameObject);
    }
  }

  public void SetTickDamage(float damage)
  {
    this.tickDamage = damage;
  }

  public void SetDuration(float duration)
  {
    this.duration = duration;
  }

  public void SetDamageInterval(float interval)
  {
    this.damageInterval = interval;
  }
}
