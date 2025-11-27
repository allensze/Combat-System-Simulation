using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
  private Animator animator;
  private Telemetry telemetry;
  private float damage;
  private Vector3 startPosition;
  private float maxTravelDistance;
  private bool hasHit = false;

  void Awake()
  {
    animator = GetComponentInChildren<Animator>();
    telemetry = FindAnyObjectByType<Telemetry>();
  }

  void Start()
  {
    startPosition = transform.position;
    Destroy(gameObject, 10.0f);
  }

  void Update()
  {
    if (Vector3.Distance(transform.position, startPosition) >= maxTravelDistance)
    {
      Destroy(gameObject);
    }
  }

  private void OnTriggerEnter(Collider other)
  {
    if (hasHit)
    {
      return;
    }

    if (other.CompareTag("Player"))
    {
      var player = other.GetComponentInParent<PlayerManager>();

      if (player != null)
      {
        player.ChangeHealth(damage);
        animator?.SetTrigger("OnHit");
        hasHit = true;
        StartCoroutine(DestroyAfterAnimation());
      }
    }

    if (other.CompareTag("Enemy"))
    {
      CapsuleCollider collider = GetComponentInParent<CapsuleCollider>();
      Vector3 center = transform.position + collider.center;
      float halfHeight = Mathf.Max(0, (collider.height * 0.5f) - collider.radius);
      Vector3 point1, point2;

      switch (collider.direction)
      {
        case 0: // X-axis
          point1 = center + transform.right * halfHeight;
          point2 = center - transform.right * halfHeight;
          break;
        case 1: // Y-axis (default)
          point1 = center + transform.up * halfHeight;
          point2 = center - transform.up * halfHeight;
          break;
        case 2: // Z-axis
          point1 = center + transform.forward * halfHeight;
          point2 = center - transform.forward * halfHeight;
          break;
        default:
          point1 = point2 = center;
          break;
      }

      Collider[] hits = Physics.OverlapCapsule(point1, point2, collider.radius, LayerMask.GetMask("Enemy"));
      List<EnemyManager> targets = new List<EnemyManager>();

      foreach (var hit in hits)
      {
        if (hit.TryGetComponent<EnemyManager>(out var target) && !target.EnemyActions.IsDead())
        {
          targets.Add(target);
        }
      }

      var selected = TargetSelect(targets);

      if (selected != null)
      {
        float damageDealt = selected.EnemyActions.TakeDamage(damage);
        telemetry?.IncrementAbilityDamage("ranged", damageDealt);
        hasHit = true;
        StartCoroutine(DestroyAfterAnimation());
      }
    }
  }

  private EnemyManager TargetSelect(List<EnemyManager> targets)
  {
    if (targets.Count == 0)
    {
      return null;
    }

    float closestPos = float.MaxValue;
    List<EnemyManager> closestEnemies = new List<EnemyManager>();

    foreach (var enemy in targets)
    {
      float distance = Mathf.Abs(enemy.transform.position.x - transform.position.x);

      if (distance < closestPos)
      {
        closestPos = distance;
        closestEnemies = new List<EnemyManager> {enemy};
      }
      else if (Mathf.Approximately(distance, closestPos))
      {
        closestEnemies.Add(enemy);
      }
    }

    float lowestHealth = float.MaxValue;
    List<EnemyManager> lowestEnemies = new List<EnemyManager>();

    foreach (var enemy in closestEnemies)
    {
      float health = enemy.EnemyActions.CurrentHealth;

      if (health < lowestHealth)
      {
        lowestHealth = health;
        lowestEnemies = new List<EnemyManager> { enemy };
      }
      else if (Mathf.Approximately((float)health, lowestHealth))
      {
        lowestEnemies.Add(enemy);
      }
    }

    return lowestEnemies[Random.Range(0, lowestEnemies.Count)];
  }

  private IEnumerator DestroyAfterAnimation()
  {
    yield return new WaitForSeconds(0.1f);
    Destroy(gameObject);
  }

  public void SetDamage(float damage)
  {
    this.damage = damage;
  }

  public void SetTravelDistance(float distance)
  {
    maxTravelDistance = distance;
  }
}
