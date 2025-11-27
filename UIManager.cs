using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
  private PlayerManager playerManager;
  private PlayerActions playerActions;

  [Header("Round UI")]
  [SerializeField] private TextMeshProUGUI roundText;
  [SerializeField] private float displayDuration;

  [Header("Health Bar UI")]
  [SerializeField] private Slider playerHP;
  [SerializeField] private List<(EnemyManager, Slider)> enemyHealthBars;

  [Header("Bullets UI")]
  [SerializeField] private Transform bulletUI;
  [SerializeField] private GameObject bulletPrefab;

  [Header("Reload Bar UI")]
  [SerializeField] private GameObject reloadBar;
  [SerializeField] private Slider reloadSlider;
  [SerializeField] private float reloadSliderSpeed;
  [SerializeField] private RectTransform reloadHandle;
  [SerializeField] private RectTransform perfectReloadZone;
  private float reloadStartTime = 0.0f;
  private bool isReloading = false;

  [Header("Ability Button UI")]
  [SerializeField] private TextMeshProUGUI ability1Text;
  [SerializeField] private TextMeshProUGUI ability2Text;
  [SerializeField] private TextMeshProUGUI ability3Text;
  [SerializeField] private TextMeshProUGUI ability4Text;
  [SerializeField] private TextMeshProUGUI ability5Text;

  private List<GameObject> bulletContainer;
  private Dictionary<string, Slider> cooldownSliders;

  void Awake()
  {
    playerManager = FindAnyObjectByType<PlayerManager>();
    playerActions = FindAnyObjectByType<PlayerActions>();
    enemyHealthBars = new List<(EnemyManager, Slider)>();
    bulletContainer = new List<GameObject>();
    cooldownSliders = new Dictionary<string, Slider>();
  }

  private void Start()
  {
    roundText.gameObject.SetActive(false);
  }

  void Update()
  {
    UpdateUI();

    if (isReloading)
    {
      float timeElapsed = Time.time - reloadStartTime;
      float reloadTime = Mathf.Clamp01((timeElapsed / playerActions.ReloadDuration) * reloadSliderSpeed);
      reloadSlider.value = reloadTime;

      if (reloadTime > 1.0f)
      {
        isReloading = false;
      }
    }
  }

  void OnEnable()
  {
    playerManager.OnHealthChange += UpdatePlayerHealth;
  }

  void OnDisable()
  {
    playerManager.OnHealthChange -= UpdatePlayerHealth;
  }

  private void UpdateUI()
  {
    ability1Text.text = $"Melee\nCD: {playerActions.Ability1CD}s\nRange: {playerActions.MeleeRange}m\nDamage: {playerActions.MeleeDamage}";
    ability2Text.text = $"Ranged\nCD: {playerActions.Ability2CD}s\nRange: {playerActions.RangedRange}m\nDamage: {playerActions.RangedDamage}";
    ability3Text.text = $"Utility\nCD: {playerActions.Ability3CD}s\nReplenish\nBullets";
    ability4Text.text = $"AOE\nCD: {playerActions.Ability4CD}s\nRange: {playerActions.AOERange}m\nDamage: {playerActions.AOEDamage}";
    //ability5Text.text = $"Melee\nCD: {playerActions.Ability1CD}s\nRange: {playerActions.MeleeRange}m\nDamage: {playerActions.MeleeDamage}";
  }

  public void ShowVictory()
  {
    StartCoroutine(ShowMessage("VICTORY", Color.green));
  }

  public void ShowDefeat()
  {
    StartCoroutine(ShowMessage("DEFEAT", Color.red));
  }

  private IEnumerator ShowMessage(string message, Color color)
  {
    roundText.text = message;
    roundText.color = color;
    roundText.gameObject.SetActive(true);
    yield return new WaitForSeconds(displayDuration);
    roundText.gameObject.SetActive(false);
  }

  public void RegisterEnemy(EnemyManager enemy, Slider healthBar)
  {
    enemyHealthBars.Add((enemy, healthBar));
    enemy.EnemyActions.OnHealthChange += UpdateEnemyHealth;
    enemy.EnemyActions.OnDeath += () => UnregisterEnemy(enemy);
  }
  
  public void UnregisterEnemy(EnemyManager enemy)
  {
    var item = enemyHealthBars.Find(e => e.Item1 == enemy);

    if (item.Item2 != null)
    {
      Destroy(item.Item2.gameObject);
      enemyHealthBars.Remove(item);
    }
  }

  private void UpdatePlayerHealth(float currentHealth, float maxHealth)
  {
    if (playerHP != null)
    {
      playerHP.value = currentHealth / maxHealth;
    }
  }

  private void UpdateEnemyHealth(EnemyManager enemy, float currentHealth, float maxHealth)
  {
    var index = enemyHealthBars.FindIndex(e => e.Item1 == enemy);

    if (index >= 0 && enemyHealthBars[index].Item2 != null)
    {
      float normalizedHealth = (maxHealth > 0) ? (currentHealth / maxHealth) : 0;
      enemyHealthBars[index].Item2.value = normalizedHealth;
    }
  }

  public void RegisterCooldownSlider(string key, Slider slider)
  {
    if (!cooldownSliders.ContainsKey(key))
    {
      cooldownSliders.Add(key, slider);
      slider.gameObject.SetActive(false);
    }
  }

  public void RegisterEnemyCDSlider(EnemyManager enemy, Slider slider)
  {
    string key = $"enemy_{enemy.GetInstanceID()}";
    RegisterCooldownSlider(key, slider);
    //Debug.Log($"{enemy.EnemyActions.CDKey} has been added");
  }

  public void UpdateCDSlider(string key, float remaining, float max)
  {
    if (cooldownSliders.TryGetValue(key, out var slider))
    {
      float fillAmount = 1.0f - (remaining / max);
      slider.value = fillAmount;
      slider.gameObject.SetActive(remaining > 0.0f);
      slider.gameObject.SetActive(true);
    }
  }

  public bool CheckPerfectReload()
  {
    Vector3[] perfectBoundary = new Vector3[4];
    Vector3[] handleBoundary = new Vector3[4];
    perfectReloadZone.GetWorldCorners(perfectBoundary);
    reloadHandle.GetWorldCorners(handleBoundary);
    Rect perfectRect = new Rect(perfectBoundary[0], perfectBoundary[2] - perfectBoundary[0]);
    Rect handleRect = new Rect(handleBoundary[0], handleBoundary[2] - handleBoundary[0]);
    return perfectRect.Overlaps(handleRect);
  }

  public void ShowReloadBar()
  {
    reloadBar.SetActive(true);
    reloadStartTime = playerActions.ReloadStartTime;
    reloadSlider.value = 0.0f;
    isReloading = true;
  }

  public void HideReloadBar()
  {
    reloadBar.SetActive(false);
  }

  public void RegisterBullets(int ammo)
  {
    foreach (Transform child in bulletUI)
    {
      Destroy(child.gameObject);
    }

    bulletContainer.Clear();

    for (int i = 0; i < ammo; i++)
    {
      GameObject bullet = Instantiate(bulletPrefab, bulletUI);
      bulletContainer.Add(bullet);
    }
  }

  public void UpdateBullets(int ammo)
  {
    for (int i = 0; i < bulletContainer.Count; i++)
    {
      bulletContainer[i].SetActive(i < ammo);
    }
  }
}
