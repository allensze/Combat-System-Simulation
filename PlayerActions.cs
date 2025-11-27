using System.Collections.Generic;
using UnityEngine;

public class PlayerActions : MonoBehaviour
{
  private PlayerControls playerControls;
  private UIManager uiManager;
  private TimeManager timeManager;
  private AutoplayStateManager auto;
  private Telemetry telemetry;
  [SerializeField] private Animator animator;
  [SerializeField] private SpriteRenderer playerSprite;
  [SerializeField] private GameObject bulletPrefab;
  [SerializeField] private Transform firePoint;

  [Header("Movement")]
  [SerializeField] private Transform hitbox;
  [SerializeField] private float speed;
  [SerializeField] private float detectRange;
  [SerializeField] private float moveWeight;
  [Header("Ability 1")]
  [SerializeField] private UnityEngine.UI.Slider ability1Slider;
  [SerializeField] private float meleeRange;
  [SerializeField] private float zRange;
  [SerializeField] private float meleeDamage;
  [SerializeField] private float ability1CD;
  public float Ability1Weight { get; set; }
  [SerializeField] private float ability1WeightScale;
  [Header("Ability 2")]
  [SerializeField] private UnityEngine.UI.Slider ability2Slider;
  [SerializeField] private float rangedRange;
  [SerializeField] private float rangedDamage;
  [SerializeField] private float projectileSpeed;
  [SerializeField] private float maxTravelDistance;
  [SerializeField] private float ability2CD;
  public float Ability2Weight { get; set; }
  [SerializeField] private float ability2WeightScale;
  [Header("Ability 3")]
  [SerializeField] private UnityEngine.UI.Slider ability3Slider;
  [SerializeField] private float reloadDuration;
  [SerializeField] private float perfectReloadBonus;
  [SerializeField] private float reloadInputCD;
  [SerializeField] private float ability3CD;
  public float Ability3Weight { get; set; }
  [SerializeField] private float ability3WeightScale;
  [SerializeField] private float autoChance;
  [SerializeField] private float smartChance;
  private float reloadStartTime = 0.0f;
  private float lastReloadTime = -1.0f;
  private bool isReloading = false;
  private bool perfectReload = false;
  [Header("Ammo")]
  [SerializeField] private int maxAmmo;
  public int CurrentAmmo { get; set; }
  [Header("Ability 4")]
  [SerializeField] private GameObject aoeHitBox;
  [SerializeField] private UnityEngine.UI.Slider ability4Slider;
  [SerializeField] private float aoeRange;
  [SerializeField] private float aoeDamage;
  [SerializeField] private float ability4CD;
  public float Ability4Weight { get; set; }
  [SerializeField] private float ability4WeightScale;
  [Header("Ability 5")]
  [SerializeField] private UnityEngine.UI.Slider ability5Slider;
  [SerializeField] private float finisherRange;
  [SerializeField] private float finisherDamage;
  [SerializeField] private float ability5CD;
  public float Ability5Weight { get; set; }

  private Dictionary<string, float> cooldownDurations;

  private Rigidbody rb;
  private Vector3 inputMove;
  private Vector3 autoMove;
  private bool isAutoplay = false;
  [Header("Enemies in Range")]
  [SerializeField] private List<EnemyManager> detectedEnemies;

  private void Awake()
  {
    playerControls = new PlayerControls();
    uiManager = FindAnyObjectByType<UIManager>();
    timeManager = FindAnyObjectByType<TimeManager>();
    auto = FindAnyObjectByType<AutoplayStateManager>();
    telemetry = FindAnyObjectByType<Telemetry>();
    detectedEnemies = new List<EnemyManager>();
    cooldownDurations = new Dictionary<string, float>
    {
      {"melee", ability1CD},
      {"ranged", ability2CD},
      {"reload", ability3CD},
      {"aoe", ability4CD},
      {"finisher", ability5CD}
    };
    CurrentAmmo = maxAmmo;
    isReloading = false;
  }

  private void OnEnable()
  {
    playerControls.Enable();
  }
  
  private void OnDisable()
  {
    playerControls.Disable();
  }

  void Start()
  {
    rb = gameObject.GetComponent<Rigidbody>();
    uiManager.RegisterBullets(maxAmmo);
    uiManager.UpdateBullets(CurrentAmmo);
    uiManager.RegisterCooldownSlider("melee", ability1Slider);
    uiManager.RegisterCooldownSlider("ranged", ability2Slider);
    uiManager.RegisterCooldownSlider("reload", ability3Slider);
    uiManager.RegisterCooldownSlider("aoe", ability4Slider);
    uiManager.RegisterCooldownSlider("finisher", ability5Slider);
  }

  void Update()
  {
    if (!isAutoplay)
    {
      float x = playerControls.Player.Move.ReadValue<Vector2>().x;
      //float z = playerControls.Player.Move.ReadValue<Vector2>().y;      
      inputMove = new Vector3(x, 0, 0).normalized;
      animator.SetBool("IsWalk", inputMove != Vector3.zero);

      if (x != 0 && x < 0)
      {
        playerSprite.flipX = true;
      }

      if (x != 0 && x > 0)
      {
        playerSprite.flipX = false;
      }
    }

    detectedEnemies = DetectEnemies(meleeRange);

    if (Input.GetKeyDown(KeyCode.Alpha1) && !timeManager.IsTimerActive("melee"))
    {
      MeleeAttack();
    }

    if (Input.GetKeyDown(KeyCode.Alpha2) && !timeManager.IsTimerActive("ranged"))
    {
      RangeAttack();
    }

    if (Input.GetKeyDown(KeyCode.Alpha3) && !timeManager.IsTimerActive("reload"))
    {
      Reload();
    }

    if (isReloading && Input.GetKeyDown(KeyCode.Alpha3) && Time.time > lastReloadTime + reloadInputCD)
    {
      if (uiManager.CheckPerfectReload())
      {
        perfectReload = true;
      }

      CurrentAmmo = maxAmmo;
      uiManager.UpdateBullets(CurrentAmmo);
      uiManager.HideReloadBar();
      isReloading = false;
    }
    else if (isReloading && Time.time > reloadStartTime + reloadDuration)
    {
      CurrentAmmo = maxAmmo;
      uiManager.UpdateBullets(CurrentAmmo);
      uiManager.HideReloadBar();
      isReloading = false;
    }

    if (Input.GetKeyDown(KeyCode.Alpha4) && !timeManager.IsTimerActive("aoe"))
    {
      AOEAttack();
    }

    foreach (var entry in cooldownDurations)
    {
      string key = entry.Key;
      float duration = entry.Value;
      float remaining = timeManager.GetTimeRemaining(key);
      uiManager.UpdateCDSlider(key, remaining, duration);
    }
  }

  void FixedUpdate()
  {
    Vector3 moveType;
    if (isAutoplay)
    {
      moveType = autoMove;
      autoMove = Vector3.zero;
    }
    else
    {
      moveType = inputMove;
    }

    if (moveType != Vector3.zero)
    {
      rb.MovePosition(transform.position + speed * Time.fixedDeltaTime * moveType);
      animator.SetBool("IsWalk", true);
    }
    else
    {
      animator.SetBool("IsWalk", false);
    }
  }

  public float DealDamage(EnemyManager enemy, float damage)
  {
    if (enemy.TryGetComponent<EnemyActions>(out var enemyActions))
    {
      return enemyActions.TakeDamage(damage);
    }
    
    return 0.0f;
  }

  public void MeleeAttack()
  {    
    animator.SetTrigger("Attack_Melee");
    EnemyManager target = TargetSelect(detectedEnemies);
      
    if (target != null)
    {
      float damageDealt = DealDamage(target, meleeDamage);
      telemetry?.IncrementAbilityDamage("melee", damageDealt);
    }
    
    timeManager.StartTimer("melee", ability1CD);
    telemetry.IncrementAbility("melee");
  }

  public void AOEAttack()
  {
    aoeHitBox.SetActive(true);
    Collider[] hitEnemies = Physics.OverlapBox(
                            aoeHitBox.transform.position
                            , aoeHitBox.GetComponent<BoxCollider>().size / 2.0f
                            , aoeHitBox.transform.rotation
                            , LayerMask.GetMask("Enemy"));
    animator.SetTrigger("Attack_AOE");

    foreach (Collider enemy in hitEnemies)
    {
      if (enemy.TryGetComponent<EnemyManager>(out var target))
      {
        float damageDealt = DealDamage(target, aoeDamage);
        telemetry?.IncrementAbilityDamage("aoe", damageDealt);
      }
    }

    aoeHitBox.SetActive(false);
    timeManager.StartTimer("aoe", ability4CD);
    telemetry.IncrementAbility("aoe");
  }

  public void RangeAttack()
  {
    if (CurrentAmmo <= 0)
    {
      return;
    }

    //List<EnemyManager> targets = DetectEnemies(rangedRange);
    animator.SetTrigger("Attack_Range");
    //EnemyManager target = TargetSelect(targets);
    float damage = perfectReload == true ? rangedDamage * perfectReloadBonus : rangedDamage;
    
    if (perfectReload)
    {
      telemetry.PerfectReloadCount++;
      perfectReload = false;
    }

    CurrentAmmo--;
    uiManager.UpdateBullets(CurrentAmmo);
    FireProjectile(damage);
    timeManager.StartTimer("ranged", ability2CD);
    telemetry.IncrementAbility("ranged");
  }

  private void FireProjectile(float damage)
  {
    if (bulletPrefab == null || firePoint == null || CurrentAmmo == 0)
    {
      return;
    }

    GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
    Vector3 direction = playerSprite.flipX ? Vector3.left : Vector3.right;

    if (bullet.TryGetComponent<Rigidbody>(out var rb))
    {
#pragma warning disable CS0618
      rb.velocity = direction * projectileSpeed;
#pragma warning restore CS0618
    }

    // Set damage
    if (bullet.TryGetComponent<Projectile>(out var proj))
    {
      proj.SetTravelDistance(maxTravelDistance);
      proj.SetDamage(damage);
    }
  }

  public void Reload()
  {
    if (isReloading)
    {
      return;
    }

    uiManager.UpdateBullets(CurrentAmmo);
    animator.SetTrigger("Reload");
    timeManager.StartTimer("reload", ability3CD);
    telemetry.IncrementAbility("reload");
    isReloading = true;
    reloadStartTime = Time.time;
    lastReloadTime = Time.time;
    uiManager.ShowReloadBar();

    if (isAutoplay)
    {
      float perfectChance = auto.CurrentState == auto.SmartAutoState ? smartChance : autoChance;
      perfectReload = UnityEngine.Random.value < perfectChance;
    }
  }

  public void Finisher()
  {
    timeManager.StartTimer("finisher", ability5CD);
    telemetry.IncrementAbility("finisher");
  }

  public void MoveTowardsEnemy(EnemyManager enemy)
  {
    // Base Case
    if (enemy == null || enemy.EnemyActions.IsDead())
    {
      return;
    }

    if (EnemyInHitbox())
    {
      autoMove = Vector3.zero;
      animator.SetBool("IsWalk", false);
      return;
    }

    Vector3 distance = enemy.transform.position - transform.position;
    float directionX = Mathf.Sign(distance.x);
    autoMove = new Vector3(directionX, 0, 0);
    animator.SetBool("IsWalk", true);
    playerSprite.flipX = directionX < 0;
  }

  public void StopMovement()
  {
    autoMove = Vector3.zero;
    animator.SetBool("IsWalk", false);
  }

  public bool EnemyInHitbox()
  {
    Collider[] detected = Physics.OverlapBox(hitbox.position, hitbox.localScale / 2.0f, hitbox.rotation, LayerMask.GetMask("Enemy"));

    foreach (var enemy in detected)
    {
      if (enemy.TryGetComponent<EnemyManager>(out var target) && !target.EnemyActions.IsDead())
      {
        return true;
      }
    }

    return false;
  }

  private EnemyManager TargetSelect(List<EnemyManager> enemies)
  {
    // Base Case
    if (enemies.Count == 0)
    {
      return null;
    }

    // Lowest health filter
    float lowestHealth = float.MaxValue;
    List<EnemyManager> lowestHealthEnemies = new List<EnemyManager>();

    foreach (EnemyManager target in enemies)
    {
      if (target == null || target.gameObject == null || target.GetComponent<EnemyActions>() == null)
      {
        continue;
      }

      float targetHealth = target.GetComponent<EnemyActions>().CurrentHealth;

      if (targetHealth < lowestHealth)
      {
        lowestHealth = targetHealth;
        lowestHealthEnemies = new List<EnemyManager> { target };
      }
      else if (targetHealth == lowestHealth)
      {
        lowestHealthEnemies.Add(target);
      }
    }

    if (lowestHealthEnemies.Count == 0)
    {
      return null;
    }

    // Range-based filter
    float shortestDist = float.MaxValue;
    List<EnemyManager> closestEnemies = new List<EnemyManager>();

    foreach (EnemyManager target in lowestHealthEnemies)
    {
      if (target == null)
      {
        continue;
      }

      float distance = Vector3.Distance(transform.position, target.transform.position);

      if (distance < shortestDist)
      {
        shortestDist = distance;
        closestEnemies = new List<EnemyManager> { target };
      }
      if (distance == shortestDist)
      {
        closestEnemies.Add(target);
      }
    }

    return closestEnemies[Random.Range(0, closestEnemies.Count)];
  }

  public List<EnemyManager> DetectEnemies(float range)
  {
    List<EnemyManager> detected = new List<EnemyManager>();
    Vector3 meleeSize = new Vector3(range * 2, 0.0f, zRange);
    LayerMask enemyLayer = LayerMask.GetMask("Enemy");
    Collider[] hitColliders = Physics.OverlapBox(transform.position, meleeSize / 2, Quaternion.identity, enemyLayer);

    foreach (Collider collider in hitColliders)
    {      
      if (collider.TryGetComponent<EnemyManager>(out var enemy))
      {
        {
          detected.Add(enemy);
        }
      }
    }

    return detected;
  }

  public EnemyManager DetectClosestEnemy(Vector3 playerPos, List<EnemyManager> enemies)
  {
    if (enemies == null || enemies.Count == 0)
    {
      return null;
    }

    EnemyManager detected = null;
    float range = float.MaxValue;

    foreach (var enemy in enemies)
    {
      if (enemy == null || enemy.EnemyActions.IsDead())
      {
        continue;
      }

      float distance = Vector3.Distance(playerPos, enemy.transform.position);

      if (distance < range)
      {
  
        range = distance;
        detected = enemy;
      }
    }

    return detected;
  }

  public List<EnemyManager> GetAliveEnemies()
  {
    var aliveEnemies = FindObjectsByType<EnemyManager>(FindObjectsSortMode.None);
    List<EnemyManager> alive = new List<EnemyManager>();

    foreach (var enemy in aliveEnemies)
    {
      if (!enemy.EnemyActions.IsDead())
      {
        alive.Add(enemy);
      }
    }

    return alive;
  }

  public void SetAbilityWeight(string ability, float newWeight)
  {
    switch (ability)
    {
      case "melee":
        Ability1Weight = newWeight;
        break;
      case "ranged":
        Ability2Weight = newWeight;
        break;
      case "reload":
        Ability3Weight = newWeight;
        break;
      case "aoe":
        Ability4Weight = newWeight;
        break;
      case "finisher":
        Ability5Weight = newWeight;
        break;
    }
  }

  public void ClampAbilityWeights(float min, float max)
  {
    Ability1Weight = Mathf.Clamp(Ability1Weight, min, max);
    Ability2Weight = Mathf.Clamp(Ability2Weight, min, max);
    Ability3Weight = Mathf.Clamp(Ability3Weight, min, max);
    Ability4Weight = Mathf.Clamp(Ability4Weight, min, max);
    Ability5Weight = Mathf.Clamp(Ability5Weight, min, max);
  }

  public void ResetAbilityWeights()
  {
    Ability1Weight = 1.0f;
    Ability2Weight = 1.0f;
    Ability3Weight = 1.0f;
    Ability4Weight = 1.0f;
    Ability5Weight = 1.0f;
  }

  public void SetAutoplay(bool value) => isAutoplay = value;

  public TimeManager TimeManager => timeManager;
  public List<EnemyManager> DetectedEnemies => detectedEnemies;
  public Animator Animator => animator;
  public float MeleeRange => meleeRange;
  public float MeleeDamage => meleeDamage;
  public float Ability1CD => ability1CD;
  public float RangedRange => rangedRange;
  public float RangedDamage => rangedDamage;
  public float Ability2CD => ability2CD;
  public float ReloadDuration => reloadDuration;
  public float ReloadStartTime => reloadStartTime;
  public float Ability3CD => ability3CD;
  public float AOERange => aoeRange;
  public float AOEDamage => aoeDamage;
  public float Ability4CD => ability4CD;
  public float Ability5CD => ability5CD;
  public int MaxAmmo => maxAmmo;
  //public float Ability1Weight => Ability1Weight;
  //public float Ability2Weight => Ability2Weight;
  //public float Ability3Weight => Ability3Weight;
  //public float Ability4Weight => Ability4Weight;
  //public float Ability5Weight => Ability5Weight;
  public float Ability1WeightScale => ability1WeightScale;
  public float Ability2WeightScale => ability2WeightScale;
  public float Ability3WeightScale => ability3WeightScale;
  public float Ability4WeightScale => ability4WeightScale;
}
