using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
public class StateManager : MonoBehaviour
{
    [Header("Estado Actual")]
    [SerializeField] private int _currentState = 0;

    [Header("Secuencia de Eventos por Estado")]
    [SerializeField] private StateProgressionStep[] _stateSequence;

    [Header("Estadísticas de Progresión")]
    [SerializeField] private ProgressionState _currentProgression;

    [Header("Hitos por Porcentaje Global")]
    [SerializeField] private ProgressionMilestone[] _progressionMilestones;

    [Header("Eventos Globales de Culminación")]
    public UnityEvent onCombatCompleted;
    public UnityEvent onInteractionsCompleted;
    public UnityEvent onTotalProgressionCompleted;

    public int CurrentState => _currentState;
    public ProgressionState CurrentProgression => _currentProgression;

    private readonly Dictionary<int, UnityEvent> _stateEventsLookup = new();
    private bool _combatCompletedFired;
    private bool _interactionsCompletedFired;
    private bool _totalCompletedFired;
    private void Awake()
    {
        InitializeLookup();
    }
    private void Start()
    {
        EvaluateState();
        EvaluateProgressionMilestones();
    }
    private void InitializeLookup()
    {
        _stateEventsLookup.Clear();
        foreach (var step in _stateSequence)
        {
            if (!_stateEventsLookup.TryAdd(step.requiredStateValue, step.onStateTriggered))
            {
                Debug.LogError($"[StateManager] Error: Valor de estado duplicado ({step.requiredStateValue}) en el Inspector.");
            }
        }
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
    public void SetTotals(int totalEntities, int totalInteractions)
    {
        _currentProgression.TotalEntities = totalEntities;
        _currentProgression.TotalInteractions = totalInteractions;
        EvaluateProgressionMilestones();
    }
    public void RegisterEntityKilled()
    {
        _currentProgression.EntitiesKilled++;
        EvaluateProgressionMilestones();
    }
    public void RegisterInteraction()
    {
        _currentProgression.InteractionsCompleted++;
        EvaluateProgressionMilestones();
    }
    private void EvaluateState()
    {
        if (_stateEventsLookup.TryGetValue(_currentState, out UnityEvent stateEvent))
        {
            stateEvent?.Invoke();
        }
    }
    private void EvaluateProgressionMilestones()
    {
        float currentPercent = _currentProgression.TotalProgressionPercentage;

        for (int i = 0; i < _progressionMilestones.Length; i++)
        {
            if (!_progressionMilestones[i].hasBeenTriggered && currentPercent >= _progressionMilestones[i].requiredPercentage)
            {
                _progressionMilestones[i].hasBeenTriggered = true;
                _progressionMilestones[i].onMilestoneReached?.Invoke();
            }
        }

        if (!_combatCompletedFired && _currentProgression.TotalEntities > 0 && _currentProgression.EntitiesKilled >= _currentProgression.TotalEntities)
        {
            _combatCompletedFired = true;
            onCombatCompleted?.Invoke();
        }

        if (!_interactionsCompletedFired && _currentProgression.TotalInteractions > 0 && _currentProgression.InteractionsCompleted >= _currentProgression.TotalInteractions)
        {
            _interactionsCompletedFired = true;
            onInteractionsCompleted?.Invoke();
        }

        if (!_totalCompletedFired && currentPercent >= 100f)
        {
            _totalCompletedFired = true;
            onTotalProgressionCompleted?.Invoke();
        }
    }
}