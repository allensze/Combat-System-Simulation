using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EnemyActions : MonoBehaviour
{
  [Header("Health")]
  [SerializeField] private Slider hpSlider;
  [SerializeField] private float maxHealth;
  private float currentHealth = 0.0f;
  private float damageTaken = 0.0f;
  [SerializeField] private float despawnTime;

  [Header("Damage Effect")]
  [SerializeField] private ParticleSystem damageEffect;
  [SerializeField] private ParticleSystem healEffect;
  [SerializeField] private float flashDuration;

  [Header("Movement & Detection")]
  [SerializeField] protected float moveSpeed;
  [SerializeField] private float detectRange;
  [SerializeField] private float attackRange;
  [SerializeField] private float attackZRange;
  [SerializeField] protected float safeDistance;
  [SerializeField] protected float retreatDuration;
  [SerializeField] protected float retreatCD;
  private Vector3 attackBoxSize;
  protected bool isRetreating = false;
  protected float retreatEndTime;
  protected string retreatKey = "";

  [Header("Attack Timers & CDs")]
  [SerializeField] private Slider cooldownSlider;
  [SerializeField] private float attackCD;
  [SerializeField] private float minAttackDelay;
  [SerializeField] private float maxAttackDelay;
  private string cooldownKey = "";
  private float attackDelayTime = 0.0f;

  private PlayerManager player;
  private LayerMask playerLayer;
  private SpriteRenderer enemySprite;
  protected Rigidbody rb;
  protected Animator animator;
  private UIManager uiManager;
  private TimeManager timeManager;
  private EnemyManager enemyManager;

  public event Action OnPlayerDetected;
  public event Action OnDeath;
  public event Action<EnemyManager, float, float> OnHealthChange;

  protected virtual void Awake()
  {
    player = FindAnyObjectByType<PlayerManager>();
    playerLayer = LayerMask.GetMask("Player");
    enemySprite = GetComponentInChildren<SpriteRenderer>();
    rb = GetComponent<Rigidbody>();
    animator = GetComponentInChildren<Animator>();
    enemyManager = GetComponent<EnemyManager>();
    attackBoxSize = new Vector3(attackRange * 2.0f, 0.0f, attackZRange);
    currentHealth = maxHealth;

    OnHealthChange?.Invoke(enemyManager, currentHealth, maxHealth);
  }

  protected virtual void Start()
  {
    uiManager = FindAnyObjectByType<UIManager>();
    timeManager = FindAnyObjectByType<TimeManager>();
    cooldownKey = $"enemy_{enemyManager.GetInstanceID()}";
    retreatKey = $"retreat_{enemyManager.GetInstanceID()}";

    if (cooldownSlider != null)
    {
      uiManager.RegisterEnemyCDSlider(enemyManager, cooldownSlider);
    }

    attackDelayTime = Time.time + UnityEngine.Random.Range(minAttackDelay, maxAttackDelay);
  }

  protected virtual void Update()
  {
    if (cooldownSlider != null)
    {
      float remaining = timeManager.GetTimeRemaining(cooldownKey);
      uiManager.UpdateCDSlider(cooldownKey, remaining, attackCD);
    }  
  }

  protected virtual void FixedUpdate()
  {
    if (IsRetreating())
    { 
      Vector3 direction = (transform.position - player.transform.position).normalized;
      //Vector3 targetPos = transform.position - direction * retreatDist;
      Vector3 move = moveSpeed * Time.deltaTime * direction;

      if (!Physics.Raycast(transform.position, move.normalized, out RaycastHit wall, move.magnitude + 0.1f, LayerMask.GetMask("Wall"))
          || !wall.collider.CompareTag("Wall"))
      {
        rb.MovePosition(transform.position + move);
      }
      animator.SetBool("IsWalk", move != Vector3.zero);
    }
    else if (isRetreating)
    {
      isRetreating = false;
      animator.SetBool("IsWalk", false);
    }

    SnapToGround();
  }

  public void MoveTowardsPlayer()
  {
    if (player == null)
    {
      return;
    }

    Vector3 direction = new Vector3(player.transform.position.x - transform.position.x, 0.0f, 0.0f);

    if (direction.magnitude > attackRange)
    {
      Vector3 move = moveSpeed * Time.deltaTime * direction.normalized;

      if (Physics.Raycast(transform.position, direction.normalized, out RaycastHit wall, move.magnitude + 0.1f)
          && wall.collider.CompareTag("Wall"))
      {
        animator.SetBool("IsWalk", false);
        return;
      }

      rb.MovePosition(transform.position + move);
      animator.SetBool("IsWalk", true);
    }
    else
    {
      animator.SetBool("IsWalk", false);
    }
  }

  public virtual void Retreat()
  {
    if (!CanRetreat())
    {
      return;
    }    

    retreatEndTime = Time.time + retreatDuration;
    timeManager.StartTimer(retreatKey, retreatCD);
    isRetreating = true;
  }

  public bool IsRetreating()
  {
    return isRetreating && Time.time <= retreatEndTime;
  }

  public virtual bool CanRetreat()
  {
    return !timeManager.IsTimerActive(retreatKey);
  }

  public virtual void PerformAttack(PlayerManager target)
  {
    if (target == null)
    {
      return;
    }

    if (PlayerInAttackRange() && !timeManager.IsTimerActive(cooldownKey) && Time.time >= attackDelayTime)
    {
      animator.SetTrigger("IsAttack");
      target.ChangeHealth(1.0f);
      timeManager.StartTimer(cooldownKey, attackCD);
    }
  }

  public float TakeDamage(float damage)
  {
    float damageApplied = Mathf.Min(currentHealth, damage);
    
    if (currentHealth <= 0 || IsDead())
    {
      return 0.0f;
    }

    currentHealth -= damageApplied;
    damageTaken += damageApplied;
    currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
    OnHealthChange?.Invoke(enemyManager, currentHealth, maxHealth);

    if (damageEffect != null)
    {
      ParticleSystem effect = Instantiate(damageEffect, transform.position, Quaternion.Euler(0.0f, 0.0f, -90.0f));
      Destroy(effect.gameObject, effect.main.duration + effect.main.startLifetime.constantMax);
    }

    StartCoroutine(EffectFlash(Color.red));

    if (currentHealth <= 0)
    {
      Die();
    }

    return damageApplied;
  }

  public void ChangeHealth(EnemyManager target, float healAmount)
  {
    var ally = target.EnemyActions;
    ally.currentHealth += healAmount;
    ally.currentHealth = Mathf.Clamp(ally.currentHealth, 0, ally.maxHealth);
    OnHealthChange?.Invoke(target, ally.currentHealth, ally.maxHealth);

    if (healEffect != null)
    {
      ParticleSystem effect = Instantiate(healEffect, transform.position, Quaternion.Euler(0.0f, 0.0f, -90.0f));
      Destroy(effect.gameObject, effect.main.duration + effect.main.startLifetime.constantMax);
    }

    StartCoroutine(EffectFlash(Color.cyan));
  }

  private IEnumerator EffectFlash(Color color)
  {
    enemySprite.color = color;
    yield return new WaitForSeconds(flashDuration);
    enemySprite.color = Color.white;
  }

  private void Die()
  {
    OnDeath?.Invoke();
    GetComponent<CapsuleCollider>().enabled = false;
    enemyManager.OnEnemyDeath();
    Destroy(gameObject, despawnTime);
  }

  public bool IsDead()
  {
    return currentHealth <= 0;
  }

  public void DetectPlayer()
  {
    OnPlayerDetected?.Invoke();
  }

  public virtual bool PlayerDetected()
  { 
    return Vector3.Distance(transform.position, player.transform.position) < detectRange;
  }

  public bool PlayerInAttackRange()
  {
    Collider[] player = Physics.OverlapBox(transform.position, attackBoxSize / 2, Quaternion.identity, playerLayer);
    
    foreach (var detected in player)
    {
      if (detected.CompareTag("Player"))
      {
        return true;
      }
    }

    return false;
  }

  public bool PlayerTooClose()
  {
    return Vector3.Distance(transform.position, player.transform.position) < safeDistance;
  }

  public void ResetDamageTaken()
  {
    damageTaken = 0.0f;
  }

  protected void SnapToGround()
  {
    Ray ray = new Ray(transform.position + Vector3.up, Vector3.down);

    if (Physics.Raycast(ray, out RaycastHit hit, 5.0f, LayerMask.GetMask("Default")))
    {
      float yOffset = 0.0f;
      
      if (TryGetComponent<CapsuleCollider>(out var capsule))
      {
        yOffset = capsule.height / 2.0f;
      }

      Vector3 newPosition = transform.position;
      newPosition.y = hit.point.y + yOffset;
      transform.position = newPosition;
    }
  }

  public Rigidbody Rb => rb;
  public Animator Animator => animator;
  public TimeManager TimeManager => timeManager;
  public UIManager UIManager => uiManager;
  public EnemyManager EnemyManager => enemyManager;
  public PlayerManager Player => player;
  public float AttackCD => attackCD;
  public float CurrentHealth => currentHealth;
  public float MaxHealth => maxHealth;
  public float DamageTaken => damageTaken;
  public string CDKey => cooldownKey;
}
