using UnityEngine;

public class EnemyStateController : MonoBehaviour
{
    public EnemyDetection detector;
	private EnemyState currentState;

	private void Update()
    {
        RunStateMachine();
    }

    private void RunStateMachine()
    {
        if (!currentState) return;

        EnemyState nextState = currentState.Run(this);

        if (nextState && currentState != nextState)
            Next(nextState);
	}

    private void Next(EnemyState nextState)
    {
        currentState = nextState;
    }
}
