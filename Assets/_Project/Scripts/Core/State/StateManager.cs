using System.Collections.Generic;
using UnityEngine;

public class StateManager : MonoBehaviour
{
    [Header("Secuencia Narrativa")]
    [SerializeField] private int _currentState = 0;
    [SerializeField] private StateProgressionStep[] _stateSequence;

    [Header("Estadísticas de Progresión")]
    [SerializeField] private ProgressionState _currentProgression;
    private readonly Dictionary<int, StateProgressionStep> _quickLookup = new();
    private void Awake()
    {
        _currentState = 0;

        foreach (var step in _stateSequence)
        {
            if (!_quickLookup.TryAdd(step.requiredStateValue, step))
            {
                Debug.LogError($"[StateManager] Error: valor repetido ({step.requiredStateValue}). Revisa el Inspector.");
            }
        }
    }
    private void Start()
    {
        EvaluateState();
    }
    public void AddStatePoint()
    {
        _currentState++;
        EvaluateState();
    }
    public void SetExactState(int exactTargetState)
    {
        _currentState = exactTargetState;
        EvaluateState();
    }
    public void RegisterEntityKilled()
    {
        _currentProgression.EntitiesKilled++;
    }
    public void RegisterInteraction()
    {
        _currentProgression.InteractionsCompleted++;
    }
    private void EvaluateState()
    {
        if (_quickLookup.TryGetValue(_currentState, out StateProgressionStep currentStep))
        {
            currentStep.onStateTriggered?.Invoke();
        }
    }
}
