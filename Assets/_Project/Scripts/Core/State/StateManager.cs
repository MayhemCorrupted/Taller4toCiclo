using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class TagRequirement
{
    [Tooltip("El Tag del objeto/enemigo que estamos buscando (Ej: 'Slime', 'Cofre').")]
    public string requiredTag = "Enemy";

    [Tooltip("Cantidad necesaria a destruir/interactuar para cumplir el requisito.")]
    public int requiredCount = 3;

    [Tooltip("Falso = Buscar en enemigos destruidos. Verdadero = Buscar en interacciones.")]
    public bool isInteraction = false;

    [Tooltip("Bandera que se activará y guardará. El DialogueInteraction puede leer esto (Ej: 'Mision_Slimes_Completada').")]
    public string flagToUnlock;

    [Tooltip("Evento al cumplir (Aquí puedes arrastrar el propio StateManager y llamar a AddStatePoint).")]
    public UnityEvent onRequirementMet;

    [HideInInspector] public bool hasBeenMet = false;
}

public class StateManager : MonoBehaviour
{
    public static StateManager Instance { get; private set; }

    [Header("Estado Actual")]
    [SerializeField] private int _currentState = 0;

    [Header("Requisitos por Tags (Misiones/Objetivos)")]
    [SerializeField] private TagRequirement[] _tagRequirements;

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
    private readonly Dictionary<string, int> _killedTags = new();
    private readonly Dictionary<string, int> _interactedTags = new();
    private readonly HashSet<string> _unlockedFlags = new();

    private readonly Dictionary<int, UnityEvent> _stateEventsLookup = new();
    private bool _combatCompletedFired;
    private bool _interactionsCompletedFired;
    private bool _totalCompletedFired;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

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
    public void RegisterEntityKilled(string entityTag = "Untagged")
    {
        _currentProgression.EntitiesKilled++;

        if (!_killedTags.ContainsKey(entityTag)) _killedTags[entityTag] = 0;
        _killedTags[entityTag]++;

        EvaluateTagRequirements();
        EvaluateProgressionMilestones();
    }
    public void RegisterInteraction(string interactableTag = "Untagged")
    {
        _currentProgression.InteractionsCompleted++;

        if (!_interactedTags.ContainsKey(interactableTag)) _interactedTags[interactableTag] = 0;
        _interactedTags[interactableTag]++;

        EvaluateTagRequirements();
        EvaluateProgressionMilestones();
    }
    private void EvaluateTagRequirements()
    {
        foreach (var req in _tagRequirements)
        {
            if (req.hasBeenMet) continue;

            int currentCount = 0;
            if (req.isInteraction && _interactedTags.TryGetValue(req.requiredTag, out int iCount)) currentCount = iCount;
            else if (!req.isInteraction && _killedTags.TryGetValue(req.requiredTag, out int kCount)) currentCount = kCount;

            if (currentCount >= req.requiredCount)
            {
                req.hasBeenMet = true;

                if (!string.IsNullOrEmpty(req.flagToUnlock))
                    _unlockedFlags.Add(req.flagToUnlock);

                req.onRequirementMet?.Invoke();
            }
        }
    }
    public bool CheckFlag(string flag)
    {
        if (string.IsNullOrEmpty(flag)) return false;
        return _unlockedFlags.Contains(flag);
    }
    public void SetFlag(string flag, bool state)
    {
        if (state) _unlockedFlags.Add(flag);
        else _unlockedFlags.Remove(flag);
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