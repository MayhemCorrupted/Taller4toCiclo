using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
[Serializable] public class StateDialogueBranch
{
    [Tooltip("Estado (int) requerido en el StateManager para activar este diálogo.")]
    public int requiredState = 0;

    [Tooltip("Opcional: Bandera adicional para ramificar dentro del mismo estado (Ej: 'MatoEnemigosSinPilares').")]
    public string optionalFlag;

    [Tooltip("Secuencia de diálogo que se reproducirá.")]
    public DialogueSequence sequence;

    [Tooltip("Opcional: Sobrescribe el uiID si esta rama requiere otra interfaz.")]
    public string overrideUIID;

    [Header("Evento Único de Interacción")]
    [Tooltip("ID único para evitar que el UnityEvent se repita. Si se deja vacío, se genera automáticamente con el estado y nombre del diálogo.")]
    public string uniqueEventID;

    [Tooltip("Si está activo, onDialogueTriggered solo se ejecutará la primera vez que se active este diálogo/estado.")]
    public bool triggerEventOnlyOnce = true;

    [Tooltip("Evento que se ejecuta al iniciar esta rama de diálogo (dar ítems, abrir puertas, cambiar estado, etc.).")]
    public UnityEvent onDialogueTriggered;
}
public class DialogueInteraction : MonoBehaviour, IInteractable
{
    private struct DynamicDialogueEntry
    {
        public DialogueSequence Sequence;
        public string OverrideUIID;
    }

    [Header("Secuencia por Defecto (Respaldo)")]
    [Tooltip("Se usa si ningún estado configurado coincide con el CurrentState actual.")]
    public DialogueSequence defaultSequence;

    [Header("Diálogos por Estado (Int) y Ramificaciones")]
    [Tooltip("Configura qué diálogo y qué evento único se dispara según el estado actual.")]
    public StateDialogueBranch[] stateDialogues;

    [Header("Sobrescritura de UI en Escena (Opcional)")]
    [SerializeField] private string _overrideUIID;

    [Header("Opciones")]
    [SerializeField] private bool loopDialogue = true;

    [Header("Evento General (Opcional - Se ejecuta en cada interacción)")]
    public UnityEvent OnInteractEveryTime;
    public bool HasBeenUsed
    {
        get => _hasBeenUsed;
        set
        {
            _hasBeenUsed = value;
            enabled = !_hasBeenUsed || loopDialogue || _dynamicSequences.Count > 0;
        }
    }
    private DialogueManager _dialogueManager;
    private bool _hasBeenUsed = false;
    private readonly Queue<DynamicDialogueEntry> _dynamicSequences = new();
    private void Awake()
    {
        _dialogueManager = FindAnyObjectByType<DialogueManager>();
    }
    public void Interact()
    {
        if (UIManager.Instance != null && UIManager.Instance.CurrentState == GameState.Dialogue)
            return;

        bool hasDynamic = _dynamicSequences.Count > 0;
        if (_hasBeenUsed && !loopDialogue && !hasDynamic)
            return;

        DialogueSequence finalSequence;
        string sceneUIID;
        StateDialogueBranch matchedBranch = null;

        if (hasDynamic)
        {
            DynamicDialogueEntry dynamicEntry = _dynamicSequences.Dequeue();
            finalSequence = dynamicEntry.Sequence;
            sceneUIID = !string.IsNullOrWhiteSpace(dynamicEntry.OverrideUIID)
                ? dynamicEntry.OverrideUIID
                : _overrideUIID;
        }
        else
        {
            matchedBranch = EvaluateStateBranch(out finalSequence, out sceneUIID);
        }

        if (finalSequence == null) return;

        StartDialogueSequence(finalSequence, sceneUIID);

        if (matchedBranch != null)
        {
            TryInvokeBranchEvent(matchedBranch);
        }

        OnInteractEveryTime?.Invoke();

        if (_dynamicSequences.Count > 0)
        {
            _hasBeenUsed = false;
            enabled = true;
        }
        else if (!loopDialogue)
        {
            _hasBeenUsed = true;
            enabled = false;
        }
    }
    private StateDialogueBranch EvaluateStateBranch(out DialogueSequence sequence, out string sceneUIID)
    {
        sequence = defaultSequence;
        sceneUIID = _overrideUIID;

        if (stateDialogues == null || stateDialogues.Length == 0 || StateManager.Instance == null)
            return null;

        int currentState = StateManager.Instance.CurrentState;
        StateDialogueBranch fallbackStateMatch = null;

        for (int i = 0; i < stateDialogues.Length; i++)
        {
            StateDialogueBranch branch = stateDialogues[i];
            if (branch == null || branch.sequence == null) continue;
            if (branch.requiredState != currentState) continue;

            bool requiresFlag = !string.IsNullOrWhiteSpace(branch.optionalFlag);

            if (requiresFlag)
            {
                if (StateManager.Instance.CheckFlag(branch.optionalFlag))
                {
                    sequence = branch.sequence;
                    if (!string.IsNullOrWhiteSpace(branch.overrideUIID))
                        sceneUIID = branch.overrideUIID;
                    return branch;
                }
            }
            else fallbackStateMatch ??= branch;
        }

        if (fallbackStateMatch != null)
        {
            sequence = fallbackStateMatch.sequence;
            if (!string.IsNullOrWhiteSpace(fallbackStateMatch.overrideUIID))
                sceneUIID = fallbackStateMatch.overrideUIID;
            return fallbackStateMatch;
        }

        return null;
    }
    private void TryInvokeBranchEvent(StateDialogueBranch branch)
    {
        if (branch.onDialogueTriggered == null) return;

        string eventKey = !string.IsNullOrWhiteSpace(branch.uniqueEventID)
            ? branch.uniqueEventID
            : $"{gameObject.name}_State_{branch.requiredState}_{branch.optionalFlag}_{branch.sequence.name}";

        if (branch.triggerEventOnlyOnce && StateManager.Instance != null)
        {
            if (StateManager.Instance.HasEventTriggered(eventKey))
                return;

            StateManager.Instance.MarkEventAsTriggered(eventKey);
        }

        branch.onDialogueTriggered.Invoke();
    }
    private void StartDialogueSequence(DialogueSequence sequenceToPlay, string sceneUIID)
    {
        if (_dialogueManager == null)
            _dialogueManager = FindAnyObjectByType<DialogueManager>();

        if (_dialogueManager != null)
        {
            if (!string.IsNullOrWhiteSpace(sceneUIID))
                _dialogueManager.StartDialogue(sequenceToPlay, sceneUIID);
            else
                _dialogueManager.StartDialogue(sequenceToPlay);
        }
    }
    public void ResetInteraction()
    {
        _hasBeenUsed = false;
        enabled = true;
    }
    public void SetDynamicSequence(DialogueSequence newSequence, string customUIID = null)
    {
        if (newSequence == null) return;
        _dynamicSequences.Clear();
        _dynamicSequences.Enqueue(new DynamicDialogueEntry { Sequence = newSequence, OverrideUIID = customUIID });
        ResetInteraction();
    }
    public void EnqueueDynamicSequence(DialogueSequence newSequence, string customUIID = null)
    {
        if (newSequence == null) return;
        _dynamicSequences.Enqueue(new DynamicDialogueEntry { Sequence = newSequence, OverrideUIID = customUIID });
        ResetInteraction();
    }
}