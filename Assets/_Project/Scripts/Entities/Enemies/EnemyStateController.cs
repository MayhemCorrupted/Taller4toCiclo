using UnityEngine;

public class EnemyStateController : MonoBehaviour
{
    EnemyState currentState;

    private void Update()
    {
        RunStateMachine();
    }

    private void RunStateMachine()
    {
        EnemyState nextState = currentState?.Run();

        if (nextState != null)
        {
            Next(nextState);
        }
    }

    private void Next(EnemyState nextState)
    {
        currentState = nextState;
    }
}
