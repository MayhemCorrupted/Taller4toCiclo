using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
[Serializable] public class MissionGoal
{
    [Tooltip("Nombre para identificar este objetivo en el Inspector.")]
    public string title = "Nuevo Objetivo";

    [Tooltip("Estado requerido para contar. Usa -1 si puede cumplirse en cualquier estado.")]
    public int requiredState = 1;

    [Tooltip("Tag del enemigo u objeto a destruir (Ej: 'Enemy', 'Pillar').")]
    public string targetTag = "Enemy";

    [Tooltip("Cantidad necesaria para completar el objetivo.")]
    [Min(1)] public int amount = 1;

    [Header("Recompensa / Resultado")]
    [Tooltip("Bandera que se activa al completarlo (Ej: 'EnemigosLimpios').")]
    public string giveFlag;

    [Tooltip("Estado al que avanza automáticamente (-1 = mantener estado actual).")]
    public int nextState = -1;

    public UnityEvent onCompleted;

    [HideInInspector] public bool isCompleted;
}
[Serializable] public struct StateAction
{
    [Tooltip("Número de estado que dispara este evento.")]
    public int state;
    public UnityEvent onEnter;
}
[Serializable] public struct DialogueEventAction
{
    [Tooltip("Debe coincidir con el SequenceEndEventID o LineEndEventID del diálogo.")]
    public string eventID;
    public UnityEvent onTriggered;
}
public class StateManager : MonoBehaviour
{
    public static StateManager Instance { get; private set; }

    [Header("Progreso Actual")]
    [SerializeField] private int _currentState = 0;

    [Header("Objetivos (Matar / Destruir)")]
    [SerializeField] private MissionGoal[] _goals;

    [Header("Eventos al Entrar a un Estado")]
    [SerializeField] private StateAction[] _onStateEnter;

    [Header("Eventos Disparados desde Diálogos")]
    [SerializeField] private DialogueEventAction[] _dialogueEvents;

    public event Action<int> OnStateChanged;
    public int CurrentState => _currentState;
    private readonly Dictionary<(int state, string tag), int> _killsByStateAndTag = new();
    private readonly HashSet<string> _activeFlags = new();
    private readonly HashSet<string> _triggeredEvents = new();
    private readonly Dictionary<int, UnityEvent> _stateLookup = new();
    private DialogueManager _dialogueManager;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        BuildStateLookup();
    }
    private void Start()
    {
        _dialogueManager = FindAnyObjectByType<DialogueManager>();
        if (_dialogueManager != null)
            _dialogueManager.OnDialogueEventTriggered += HandleDialogueEvent;

        SyncFromSaveManager();
        NotifyStateEntered();
    }
    private void OnDestroy()
    {
        if (_dialogueManager != null)
            _dialogueManager.OnDialogueEventTriggered -= HandleDialogueEvent;
    }
    private void SyncFromSaveManager()
    {
        if (SaveManager.Instance == null || !SaveManager.Instance.HasActiveSession) return;
        if (SaveManager.Instance.CurrentRun == null) return;

        StageSaveData stageData = SaveManager.Instance.GetActiveStageSaveData();
        ImportStateData(stageData);
    }
    private void BuildStateLookup()
    {
        _stateLookup.Clear();
        if (_onStateEnter == null) return;

        foreach (var entry in _onStateEnter)
        {
            if (!_stateLookup.TryAdd(entry.state, entry.onEnter))
            {
                GameLogger.LogWarning($"[StateManager] Estado duplicado ({entry.state}) en _onStateEnter.");
            }
        }
    }
    private void HandleDialogueEvent(string eventID)
    {
        if (string.IsNullOrWhiteSpace(eventID) || _dialogueEvents == null) return;

        string trimmedID = eventID.Trim();
        for (int i = 0; i < _dialogueEvents.Length; i++)
        {
            if (string.Equals(_dialogueEvents[i].eventID?.Trim(), trimmedID, StringComparison.OrdinalIgnoreCase))
            {
                _dialogueEvents[i].onTriggered?.Invoke();
            }
        }
    }
    public void SetState(int newState)
    {
        if (_currentState == newState) return;

        _currentState = newState;
        NotifyStateEntered();
        EvaluateGoals();
        AutoSaveIfActive();
    }
    public void SetExactState(int newState) => SetState(newState);
    public void AddState(int amount)
    {
        if (amount == 0) return;

        _currentState += amount;
        NotifyStateEntered();
        EvaluateGoals();
        AutoSaveIfActive();
    }
    public void NextState() => AddState(1);
    public void AddStatePoint() => AddState(1);
    public void RegisterEntityKilled(string entityTag = "Untagged")
    {
        var key = (_currentState, entityTag);
        if (!_killsByStateAndTag.ContainsKey(key))
            _killsByStateAndTag[key] = 0;

        _killsByStateAndTag[key]++;
        EvaluateGoals();
    }
    public int GetKillsInState(int state, string entityTag)
    {
        return _killsByStateAndTag.TryGetValue((state, entityTag), out int count) ? count : 0;
    }
    public int GetTotalKills(string entityTag)
    {
        int total = 0;
        foreach (var kvp in _killsByStateAndTag)
        {
            if (kvp.Key.tag == entityTag)
                total += kvp.Value;
        }
        return total;
    }
    private void EvaluateGoals()
    {
        if (_goals == null) return;

        for (int i = 0; i < _goals.Length; i++)
        {
            MissionGoal goal = _goals[i];
            if (goal.isCompleted) continue;

            if (goal.requiredState >= 0 && goal.requiredState != _currentState) continue;

            int currentCount = goal.requiredState < 0
                ? GetTotalKills(goal.targetTag)
                : GetKillsInState(_currentState, goal.targetTag);

            if (currentCount >= goal.amount)
            {
                goal.isCompleted = true;

                if (!string.IsNullOrWhiteSpace(goal.giveFlag))
                    _activeFlags.Add(goal.giveFlag.Trim());

                goal.onCompleted?.Invoke();

                if (goal.nextState >= 0)
                    SetState(goal.nextState);
            }
        }
    }
    public bool CheckFlag(string flag)
    {
        return !string.IsNullOrWhiteSpace(flag) && _activeFlags.Contains(flag.Trim());
    }
    public void SetFlag(string flag, bool active)
    {
        if (string.IsNullOrWhiteSpace(flag)) return;

        if (active) _activeFlags.Add(flag.Trim());
        else _activeFlags.Remove(flag.Trim());
    }
    public void ActivateFlag(string flag) => SetFlag(flag, true);
    public void DeactivateFlag(string flag) => SetFlag(flag, false);
    public bool HasEventTriggered(string eventID)
    {
        return !string.IsNullOrWhiteSpace(eventID) && _triggeredEvents.Contains(eventID);
    }
    public void MarkEventAsTriggered(string eventID)
    {
        if (!string.IsNullOrWhiteSpace(eventID))
            _triggeredEvents.Add(eventID);
    }
    private void NotifyStateEntered()
    {
        OnStateChanged?.Invoke(_currentState);

        if (_stateLookup.TryGetValue(_currentState, out UnityEvent stateEvent))
        {
            stateEvent?.Invoke();
        }
    }
    private static void AutoSaveIfActive()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.HasActiveSession)
        {
            _ = SaveManager.Instance.AutoSaveRunAsync();
        }
    }
    public void ResetAllProgress()
    {
        _currentState = 0;
        _killsByStateAndTag.Clear();
        _activeFlags.Clear();
        _triggeredEvents.Clear();

        if (_goals != null)
        {
            for (int i = 0; i < _goals.Length; i++)
                _goals[i].isCompleted = false;
        }
    }
    public void ExportStateData(StageSaveData targetData)
    {
        if (targetData == null) return;

        targetData.currentState = _currentState;

        targetData.unlockedFlags.Clear();
        foreach (string flag in _activeFlags)
            targetData.unlockedFlags.Add(flag);

        targetData.triggeredEventIDs.Clear();
        foreach (string evtId in _triggeredEvents)
            targetData.triggeredEventIDs.Add(evtId);

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

    public void ImportStateData(StageSaveData sourceData)
    {
        if (sourceData == null) return;

        _currentState = sourceData.currentState;

        _activeFlags.Clear();
        if (sourceData.unlockedFlags != null)
        {
            for (int i = 0; i < sourceData.unlockedFlags.Count; i++)
                _activeFlags.Add(sourceData.unlockedFlags[i]);
        }

        _triggeredEvents.Clear();
        if (sourceData.triggeredEventIDs != null)
        {
            for (int i = 0; i < sourceData.triggeredEventIDs.Count; i++)
                _triggeredEvents.Add(sourceData.triggeredEventIDs[i]);
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

        if (_goals != null)
        {
            for (int i = 0; i < _goals.Length; i++)
            {
                MissionGoal goal = _goals[i];
                int kills = goal.requiredState < 0
                    ? GetTotalKills(goal.targetTag)
                    : GetKillsInState(goal.requiredState, goal.targetTag);

                bool flagActive = !string.IsNullOrWhiteSpace(goal.giveFlag) && _activeFlags.Contains(goal.giveFlag.Trim());
                bool pastState = goal.requiredState >= 0 && goal.requiredState < _currentState;

                goal.isCompleted = pastState || kills >= goal.amount || flagActive;
            }
        }
    }
}