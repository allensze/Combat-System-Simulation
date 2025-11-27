using UnityEngine;

public abstract class AutoplayBaseState
{
  public abstract void EnterState(AutoplayStateManager auto);
  public abstract void UpdateState(AutoplayStateManager auto);
  public abstract void ExitState(AutoplayStateManager auto);
}
