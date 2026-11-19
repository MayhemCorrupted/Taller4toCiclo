using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
[Serializable] public class DialogueBranch
{
    [Tooltip("Nombre descriptivo para identificar este diálogo en el Inspector.")]
    public string title = "Rama de Diálogo";

    [Tooltip("Estado requerido en StateManager para activar este diálogo.")]
    public int state = 0;

    [Tooltip("Banderas necesarias (Ej: 'EnemigosLimpios', 'PilaresRotos'). Gana la rama que cumpla más banderas.")]
    public string[] requiredFlags;

    [Tooltip("Secuencia de texto a reproducir.")]
    public DialogueSequence dialogue;

    [Header("Evento al Hablar")]
    [Tooltip("Si está activo, el evento 'onStart' solo se dispara la primera vez.")]
    public bool runOnce = true;

    public UnityEvent onStart;

    [Header("Opcional")]
    [Tooltip("Deja vacío para usar la interfaz por defecto.")]
    public string customUIID;
}
public class DialogueInteraction : MonoBehaviour, IInteractable
{
    private struct DynamicDialogueEntry
    {
        public DialogueSequence Sequence;
        public string OverrideUIID;
    }
    [Header("Diálogo por Defecto")]
    [Tooltip("Se reproduce si ningún estado configurado coincide.")]
    [SerializeField] private DialogueSequence _defaultDialogue;

    [Header("Diálogos por Estado y Condiciones")]
    [SerializeField] private DialogueBranch[] _branches;

    [Header("Configuración General")]
    [SerializeField] private string _defaultUIID;
    [SerializeField] private bool _canRepeat = true;

    [Header("Evento en Cada Interacción (Opcional)")]
    public UnityEvent OnInteract;
    public bool HasBeenUsed
    {
        get => _hasBeenUsed;
        set
        {
            _hasBeenUsed = value;
            enabled = !_hasBeenUsed || _canRepeat || _dynamicSequences.Count > 0;
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
        if (_hasBeenUsed && !_canRepeat && !hasDynamic)
            return;

        DialogueSequence sequenceToPlay;
        string targetUIID;
        DialogueBranch matchedBranch = null;
        int matchedIndex = -1;

        if (hasDynamic)
        {
            DynamicDialogueEntry entry = _dynamicSequences.Dequeue();
            sequenceToPlay = entry.Sequence;
            targetUIID = !string.IsNullOrWhiteSpace(entry.OverrideUIID) ? entry.OverrideUIID : _defaultUIID;
        }
        else
        {
            matchedBranch = FindBestBranch(out sequenceToPlay, out targetUIID, out matchedIndex);
        }

        if (sequenceToPlay == null) return;

        PlaySequence(sequenceToPlay, targetUIID);

        if (matchedBranch != null)
            TriggerBranchEvent(matchedBranch, matchedIndex);

        OnInteract?.Invoke();

        if (_dynamicSequences.Count > 0)
        {
            _hasBeenUsed = false;
            enabled = true;
        }
        else if (!_canRepeat)
        {
            _hasBeenUsed = true;
            enabled = false;
        }
    }
    /// <summary>
    /// Busca el diálogo que coincida con el estado actual y cumpla la mayor cantidad de requiredFlags.
    /// </summary>
    private DialogueBranch FindBestBranch(out DialogueSequence sequence, out string uiID, out int bestIndex)
    {
        sequence = _defaultDialogue;
        uiID = _defaultUIID;
        bestIndex = -1;

        if (_branches == null || _branches.Length == 0 || StateManager.Instance == null)
            return null;

        int currentState = StateManager.Instance.CurrentState;
        DialogueBranch bestMatch = null;
        int highestFlagCount = -1;

        for (int i = 0; i < _branches.Length; i++)
        {
            DialogueBranch branch = _branches[i];
            if (branch == null || branch.dialogue == null) continue;
            if (branch.state != currentState) continue;

            if (!AreAllFlagsActive(branch.requiredFlags, out int validFlagCount))
                continue;

            if (validFlagCount > highestFlagCount)
            {
                highestFlagCount = validFlagCount;
                bestMatch = branch;
                bestIndex = i;
            }
        }

        if (bestMatch != null)
        {
            sequence = bestMatch.dialogue;
            if (!string.IsNullOrWhiteSpace(bestMatch.customUIID))
                uiID = bestMatch.customUIID;
        }

        return bestMatch;
    }
    private static bool AreAllFlagsActive(string[] flags, out int validFlagCount)
    {
        validFlagCount = 0;
        if (flags == null || flags.Length == 0) return true;

        for (int i = 0; i < flags.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(flags[i])) continue;

            if (!StateManager.Instance.CheckFlag(flags[i]))
                return false;

            validFlagCount++;
        }

        return true;
    }
    private void TriggerBranchEvent(DialogueBranch branch, int branchIndex)
    {
        if (branch.onStart == null) return;

        string autoEventKey = $"{gameObject.name}_S{branch.state}_B{branchIndex}_{branch.dialogue.name}";

        if (branch.runOnce && StateManager.Instance != null)
        {
            if (StateManager.Instance.HasEventTriggered(autoEventKey))
                return;

            StateManager.Instance.MarkEventAsTriggered(autoEventKey);
        }

        branch.onStart.Invoke();
    }
    private void PlaySequence(DialogueSequence sequenceToPlay, string uiID)
    {
        if (_dialogueManager == null)
            _dialogueManager = FindAnyObjectByType<DialogueManager>();

        if (_dialogueManager == null) return;

        if (!string.IsNullOrWhiteSpace(uiID))
            _dialogueManager.StartDialogue(sequenceToPlay, uiID);
        else
            _dialogueManager.StartDialogue(sequenceToPlay);
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