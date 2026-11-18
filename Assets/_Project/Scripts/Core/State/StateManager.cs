using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
[Serializable]
public class StateObjectiveRule
{
    [Tooltip("Nombre descriptivo para identificar la regla en el Inspector (Ej: 'Destruir Pilares en Estado 1').")]
    public string ruleName = "Objetivo de Estado";

    [Tooltip("Estado (int) en el que debe estar el juego para que cuenten estas bajas/destrucciones.")]
    public int activeInState = 1;

    [Tooltip("Tag del objeto o enemigo a destruir/matar (Ej: 'Pillar', 'Errante', 'Custodio').")]
    public string targetTag = "Enemy";

    [Tooltip("Cantidad necesaria dentro de ese estado para cumplir la regla.")]
    [Min(1)] public int requiredCount = 3;

    [Header("Resultado al Cumplirse")]
    [Tooltip("Si es >= 0, cambia automáticamente al estado indicado. Dejar en -1 para mantener el estado actual.")]
    public int targetStateOnComplete = -1;

    [Tooltip("Opcional: Bandera de ramificación que se activa (Ej: 'MatoEnemigosSinPilares').")]
    public string flagToUnlock;

    [Tooltip("Evento único que se dispara al cumplir esta condición.")]
    public UnityEvent onObjectiveMet;

    [HideInInspector] public bool hasBeenMet = false;
}
[Serializable]
public struct StateEventEntry
{
    [Tooltip("Valor exacto de CurrentState (int) que dispara este evento.")]
    public int stateValue;
    public UnityEvent onStateEntered;
}
public class StateManager : MonoBehaviour
{
    public static StateManager Instance { get; private set; }

    [Header("Estado Actual (Int)")]
    [SerializeField] private int _currentState = 0;

    [Header("Reglas de Entidades/Objetos por Estado")]
    [Tooltip("Define qué ocurre al matar entidades o destruir objetos en un estado específico.")]
    [SerializeField] private StateObjectiveRule[] _objectiveRules;

    [Header("Eventos al Entrar en un Estado")]
    [SerializeField] private StateEventEntry[] _stateEvents;

    public event Action<int> OnStateChanged;
    public int CurrentState => _currentState;

    private readonly Dictionary<(int state, string tag), int> _killsByStateAndTag = new();
    private readonly HashSet<string> _unlockedFlags = new();
    private readonly HashSet<string> _triggeredEventIDs = new();
    private readonly Dictionary<int, UnityEvent> _stateEventsLookup = new();
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        InitializeStateLookup();
    }

    private void Start()
    {
        SyncFromSaveManager();
        EvaluateCurrentState();
    }

    private void SyncFromSaveManager()
    {
        if (SaveManager.Instance == null || !SaveManager.Instance.HasActiveSession) return;
        if (SaveManager.Instance.CurrentRun == null) return;

        ImportStateData(SaveManager.Instance.CurrentRun.world);
    }
    private void InitializeStateLookup()
    {
        _stateEventsLookup.Clear();
        if (_stateEvents == null) return;

        foreach (var entry in _stateEvents)
        {
            if (!_stateEventsLookup.TryAdd(entry.stateValue, entry.onStateEntered))
            {
                GameLogger.LogWarning($"[StateManager] Estado duplicado ({entry.stateValue}) en _stateEvents.");
            }
        }
    }
    public void ResetAllProgress()
    {
        _currentState = 0;
        _killsByStateAndTag.Clear();
        _unlockedFlags.Clear();
        _triggeredEventIDs.Clear();

        if (_objectiveRules != null)
        {
            for (int i = 0; i < _objectiveRules.Length; i++)
            {
                _objectiveRules[i].hasBeenMet = false;
            }
        }
    }
    public void ExportStateData(WorldSaveData targetData)
    {
        if (targetData == null) return;

        targetData.currentState = _currentState;

        targetData.unlockedFlags.Clear();
        foreach (string flag in _unlockedFlags)
        {
            targetData.unlockedFlags.Add(flag);
        }

        targetData.triggeredEventIDs.Clear();
        foreach (string evtId in _triggeredEventIDs)
        {
            targetData.triggeredEventIDs.Add(evtId);
        }

        targetData.stateKillRecords.Clear();
        foreach (var kvp in _killsByStateAndTag)
        {
            targetData.stateKillRecords.Add(new StateKillRecord
            {
                state = kvp.Key.state,
                tag = kvp.Key.tag,
                count = kvp.Value
            });
        }
    }
    public void ImportStateData(WorldSaveData sourceData)
    {
        if (sourceData == null) return;

        _currentState = sourceData.currentState;

        _unlockedFlags.Clear();
        if (sourceData.unlockedFlags != null)
        {
            for (int i = 0; i < sourceData.unlockedFlags.Count; i++)
                _unlockedFlags.Add(sourceData.unlockedFlags[i]);
        }

        _triggeredEventIDs.Clear();
        if (sourceData.triggeredEventIDs != null)
        {
            for (int i = 0; i < sourceData.triggeredEventIDs.Count; i++)
                _triggeredEventIDs.Add(sourceData.triggeredEventIDs[i]);
        }

        _killsByStateAndTag.Clear();
        if (sourceData.stateKillRecords != null)
        {
            for (int i = 0; i < sourceData.stateKillRecords.Count; i++)
            {
                StateKillRecord rec = sourceData.stateKillRecords[i];
                _killsByStateAndTag[(rec.state, rec.tag)] = rec.count;
            }
        }

        if (_objectiveRules != null)
        {
            for (int i = 0; i < _objectiveRules.Length; i++)
            {
                StateObjectiveRule rule = _objectiveRules[i];
                int kills = GetKillsInState(rule.activeInState, rule.targetTag);
                bool flagAlreadyUnlocked = !string.IsNullOrWhiteSpace(rule.flagToUnlock) && _unlockedFlags.Contains(rule.flagToUnlock);

                rule.hasBeenMet = (rule.activeInState < _currentState) || (kills >= rule.requiredCount) || flagAlreadyUnlocked;
            }
        }
    }
    /// <summary>
    /// Establece directamente el estado exacto deseado.
    /// </summary>
    public void SetState(int exactTargetState)
    {
        if (_currentState == exactTargetState) return;

        _currentState = exactTargetState;
        EvaluateCurrentState();

        if (SaveManager.Instance != null && SaveManager.Instance.HasActiveSession)
        {
            _ = SaveManager.Instance.AutoSaveRunAsync();
        }
    }
    /// <summary>
    /// Alias de compatibilidad para llamadas previas desde UnityEvents.
    /// </summary>
    public void SetExactState(int exactTargetState) => SetState(exactTargetState);
    /// <summary>
    /// Suma una cantidad específica al estado actual (Ej: +1, +2).
    /// </summary>
    public void AddState(int amount)
    {
        if (amount == 0) return;

        _currentState += amount;
        EvaluateCurrentState();

        if (SaveManager.Instance != null && SaveManager.Instance.HasActiveSession)
        {
            _ = SaveManager.Instance.AutoSaveRunAsync();
        }
    }
    /// <summary>
    /// Avanza el estado actual en +1.
    /// </summary>
    public void AddStatePoint() => AddState(1);

    /// <summary>
    /// Registra la muerte de una entidad o destrucción de un objeto en el estado actual.
    /// </summary>
    public void RegisterEntityKilled(string entityTag = "Untagged")
    {
        var key = (_currentState, entityTag);
        if (!_killsByStateAndTag.ContainsKey(key))
            _killsByStateAndTag[key] = 0;

        _killsByStateAndTag[key]++;
        EvaluateObjectiveRules();
    }

    /// <summary>
    /// Consulta cuántas entidades con cierto Tag fueron eliminadas en un estado específico.
    /// </summary>
    public int GetKillsInState(int state, string entityTag)
    {
        return _killsByStateAndTag.TryGetValue((state, entityTag), out int count) ? count : 0;
    }
    private void EvaluateObjectiveRules()
    {
        if (_objectiveRules == null) return;

        int stateAtEvaluation = _currentState;

        for (int i = 0; i < _objectiveRules.Length; i++)
        {
            StateObjectiveRule rule = _objectiveRules[i];
            if (rule.hasBeenMet) continue;
            if (rule.activeInState != stateAtEvaluation) continue;

            int currentKills = GetKillsInState(stateAtEvaluation, rule.targetTag);
            if (currentKills >= rule.requiredCount)
            {
                rule.hasBeenMet = true;

                if (!string.IsNullOrWhiteSpace(rule.flagToUnlock))
                    _unlockedFlags.Add(rule.flagToUnlock);

                rule.onObjectiveMet?.Invoke();

                if (rule.targetStateOnComplete >= 0)
                {
                    SetState(rule.targetStateOnComplete);
                }
            }
        }
    }
    public bool CheckFlag(string flag)
    {
        return !string.IsNullOrWhiteSpace(flag) && _unlockedFlags.Contains(flag);
    }
    public void SetFlag(string flag, bool active)
    {
        if (string.IsNullOrWhiteSpace(flag)) return;

        if (active) _unlockedFlags.Add(flag);
        else _unlockedFlags.Remove(flag);
    }
    public bool HasEventTriggered(string eventID)
    {
        return !string.IsNullOrWhiteSpace(eventID) && _triggeredEventIDs.Contains(eventID);
    }
    public void MarkEventAsTriggered(string eventID)
    {
        if (!string.IsNullOrWhiteSpace(eventID))
            _triggeredEventIDs.Add(eventID);
    }
    private void EvaluateCurrentState()
    {
        OnStateChanged?.Invoke(_currentState);

        if (_stateEventsLookup.TryGetValue(_currentState, out UnityEvent stateEvent))
        {
            stateEvent?.Invoke();
        }
    }
}