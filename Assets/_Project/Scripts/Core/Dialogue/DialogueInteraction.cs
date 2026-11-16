using System;
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

    [Header("Eventos Nativos")]
    [Tooltip("Conectar directamente a DialogueManager.StartDialogue (soporta UI Default y Custom automáticamente).")]
    public UnityEvent<DialogueSequence> OnInteractDialogue;

    private DialogueManager _dialogueManager;
    private bool _hasBeenUsed = false;
    private void Awake()
    {
        _dialogueManager = FindAnyObjectByType<DialogueManager>();
    }
    public void Interact()
    {
        if (!_hasBeenUsed || loopDialogue)
        {
            DialogueSequence finalSequence = EvaluateConditions(out string sceneUIID);
            InvokeDialogue(finalSequence, sceneUIID);

            if (!loopDialogue)
            {
                _hasBeenUsed = true;
                enabled = false;
            }
        }
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
        if (StateManager.Instance != null)
        {
            return StateManager.Instance.CheckFlag(flag);
        }

        return false;
    }
    private void InvokeDialogue(DialogueSequence sequenceToPlay, string sceneUIID)
    {
        if (!string.IsNullOrWhiteSpace(sceneUIID))
        {
            if (_dialogueManager == null) _dialogueManager = FindAnyObjectByType<DialogueManager>();
            if (_dialogueManager != null) _dialogueManager.SetPendingUI(sceneUIID);
        }

        OnInteractDialogue?.Invoke(sequenceToPlay);
    }
}