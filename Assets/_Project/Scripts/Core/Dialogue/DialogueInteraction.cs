using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
[Serializable] public struct ConditionalDialogue
{
    [Tooltip("El nombre de la bandera guardada en el StateManager (ej: 'Mision_Slimes_Completada')")]
    public string requiredStateFlag;

    [Tooltip("La secuencia que se disparará si esta bandera existe.")]
    public DialogueSequence overrideSequence;

    [Tooltip("Opcional: Sobrescribe el uiID si esta condición requiere otra interfaz.")]
    public string overrideUIID;
}
public class DialogueInteraction : MonoBehaviour, IInteractable
{
    private struct DynamicDialogueEntry
    {
        public DialogueSequence Sequence;
        public string OverrideUIID;
    }
    [Header("Secuencia Base")]
    [Tooltip("La secuencia por defecto si no se cumplen condiciones especiales.")]
    public DialogueSequence defaultSequence;

    [Header("Sobrescritura de UI en Escena (Opcional)")]
    [Tooltip("Opcional: Fuerza un uiID específico en este objeto sin modificar el ScriptableObject.")]
    [SerializeField] private string _overrideUIID;

    [Header("Secuencias Condicionales (Sobrescritura)")]
    [Tooltip("Si alguna de estas condiciones se cumple en el StateManager, se reproducirá esta secuencia. Se evalúan de arriba a abajo.")]
    public ConditionalDialogue[] conditionalOverrides;

    [Header("Opciones")]
    [SerializeField] private bool loopDialogue = false;

    [Header("Eventos Externos (Opcional)")]
    [Tooltip("Se ejecuta al interactuar, además de iniciar el diálogo automáticamente (útil para sonidos, puertas, dar ítems, etc.).")]
    public UnityEvent OnInteract;
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
            finalSequence = EvaluateConditions(out sceneUIID);
        }

        if (finalSequence == null) return;

        InvokeDialogue(finalSequence, sceneUIID);

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
    public void ResetInteraction()
    {
        _hasBeenUsed = false;
        enabled = true;
    }
    public void SetDynamicSequence(DialogueSequence newSequence)
    {
        SetDynamicSequence(newSequence, null);
    }
    public void SetDynamicSequence(DialogueSequence newSequence, string customUIID)
    {
        if (newSequence == null) return;

        _dynamicSequences.Clear();
        _dynamicSequences.Enqueue(new DynamicDialogueEntry
        {
            Sequence = newSequence,
            OverrideUIID = customUIID
        });

        ResetInteraction();
    }
    public void EnqueueDynamicSequence(DialogueSequence newSequence)
    {
        EnqueueDynamicSequence(newSequence, null);
    }
    public void EnqueueDynamicSequence(DialogueSequence newSequence, string customUIID)
    {
        if (newSequence == null) return;

        _dynamicSequences.Enqueue(new DynamicDialogueEntry
        {
            Sequence = newSequence,
            OverrideUIID = customUIID
        });

        ResetInteraction();
    }
    public void SetDefaultSequence(DialogueSequence newSequence)
    {
        defaultSequence = newSequence;
        ResetInteraction();
    }
    public void ClearDynamicSequences()
    {
        _dynamicSequences.Clear();
    }
    private DialogueSequence EvaluateConditions(out string sceneUIID)
    {
        sceneUIID = _overrideUIID;

        if (conditionalOverrides != null)
        {
            foreach (var condition in conditionalOverrides)
            {
                if (CheckGameState(condition.requiredStateFlag))
                {
                    if (!string.IsNullOrWhiteSpace(condition.overrideUIID))
                        sceneUIID = condition.overrideUIID;

                    return condition.overrideSequence;
                }
            }
        }

        return defaultSequence;
    }
    private bool CheckGameState(string flag)
    {
        return StateManager.Instance != null && StateManager.Instance.CheckFlag(flag);
    }
    private void InvokeDialogue(DialogueSequence sequenceToPlay, string sceneUIID)
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

        OnInteract?.Invoke();
    }
}