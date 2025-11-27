using UnityEngine;

public class EnemyStateManager : MonoBehaviour
{
  EnemyBaseState currentState;
  EnemyIdleState idleState;
  EnemyMoveState moveState;
  EnemyAttackState attackState;
  EnemyDeathState deathState;

  private PlayerManager player;
  [SerializeField] private EnemyManager enemyManager;
  [SerializeField] private EnemyActions enemyActions;

  void Awake()
  {
    //enemyManager.OnPlayerDetected += HandlePlayerDetected;
    //enemyManager.OnDeath += HandleDeath;
  }

  void Start()
  {
    player = FindAnyObjectByType<PlayerManager>();
    idleState = new EnemyIdleState();
    moveState = new EnemyMoveState();
    attackState = new EnemyAttackState();
    deathState = new EnemyDeathState();

    currentState = idleState;
    currentState.EnterState(this);
  }

  void Update()
  {
    currentState.UpdateState(this);
  }

  private void HandlePlayerDetected()
  {

  }

  private void HandleDeath()
  {

  }

  public void SwitchState(EnemyBaseState newState)
  {
    if (currentState == newState)
    {
      return;
    }

    if (!enemyActions.IsDead())
    {
      currentState.ExitState(this);
      currentState = newState;
      currentState.EnterState(this);
    }
  }

  public EnemyIdleState IdleState => idleState;
  public EnemyMoveState MoveState => moveState;
  public EnemyAttackState AttackState => attackState;
  public EnemyDeathState DeathState => deathState;
  public PlayerManager Player => player;
  public EnemyManager Enemy => enemyManager;
  public EnemyActions EnemyActions => enemyActions;
}
