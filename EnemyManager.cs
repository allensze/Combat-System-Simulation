using System;
using UnityEngine;
using UnityEngine.UI;

public class EnemyManager : MonoBehaviour
{
  [SerializeField] private EnemyActions enemyActions;
  private UIManager uiManager;
  private Slider healthBar;

  public event Action OnDeath;

  void Start()
  {
    uiManager = FindAnyObjectByType<UIManager>();
    healthBar = GetComponentInChildren<Slider>();

    if (uiManager != null)
    {
      uiManager.RegisterEnemy(this, healthBar);
    }
  }

  public void OnEnemyDeath()
  {
    OnDeath?.Invoke();
  }

  public EnemyActions EnemyActions => enemyActions;
}
