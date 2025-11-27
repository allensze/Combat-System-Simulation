using System;
using System.Collections;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
  private SpriteRenderer playerSprite;
  private PlayerActions playerActions;
  private UIManager uiManager;
  private CombatManager combatManager;

  [Header("Health")]
  [SerializeField] private float maxHealth;
  private float currentHealth = 0.0f;
  [Header("Damage Effect")]
  [SerializeField] private ParticleSystem damageEffect;
  [SerializeField] private ParticleSystem poisonEffect;
  [SerializeField] private float flashDuration;

  public event Action<float, float> OnHealthChange;

  void Awake()
  {
    playerSprite = GetComponentInChildren<SpriteRenderer>();
    playerActions = FindAnyObjectByType<PlayerActions>();
    uiManager = FindAnyObjectByType<UIManager>();
    combatManager = FindAnyObjectByType<CombatManager>();
    currentHealth = maxHealth;
    OnHealthChange?.Invoke(currentHealth, maxHealth);
  }

  public void ChangeHealth(float damage, bool isPoisoned = false)
  {
    currentHealth -= damage;
    currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
    OnHealthChange?.Invoke(currentHealth, maxHealth);

    if (damageEffect != null)
    {
      ParticleSystem effect = Instantiate(isPoisoned ? poisonEffect : damageEffect, transform.position, Quaternion.Euler(0.0f, 0.0f, 90.0f));
      Destroy(effect.gameObject, effect.main.duration + effect.main.startLifetime.constantMax);
    }

    StartCoroutine(isPoisoned ? DamageFlash(Color.green) : DamageFlash(Color.red));

    if (currentHealth <= 0)
    {
      Die();
    }
  }

  private void Die()
  {
    combatManager.OnPlayerDeath();
  }

  public bool IsDead()
  {
    return currentHealth <= 0;
  }

  public void PlayerReset()
  {
    playerActions.CurrentAmmo = playerActions.MaxAmmo;
    uiManager.UpdateBullets(playerActions.CurrentAmmo);
    currentHealth = maxHealth;
    OnHealthChange?.Invoke(currentHealth, maxHealth);
    combatManager.ResetPosition();
    playerActions.TimeManager.ClearAllTimers();
    var auto = FindAnyObjectByType<AutoplayStateManager>();
    auto?.ResetRoundStartTime();
  }

  private IEnumerator DamageFlash(Color color)
  {
    playerSprite.color = color;
    yield return new WaitForSeconds(flashDuration);
    playerSprite.color = Color.white;
  }

  public float CurrentHealth => currentHealth;
  public float MaxHealth => maxHealth;
}
