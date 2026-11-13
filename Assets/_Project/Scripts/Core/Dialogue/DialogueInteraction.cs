using System;
using UnityEngine;
using UnityEngine.Events;
[Serializable] public struct ConditionalDialogue
{
    [Tooltip("El nombre de la bandera guardada en el StateManager (ej: 'Mision_Slimes_Completada')")]
    public string requiredStateFlag;

    [Tooltip("La secuencia que se disparará si esta bandera existe.")]
    public DialogueSequence overrideSequence;
}
public class DialogueInteraction : MonoBehaviour, IInteractable
{
    [Header("Secuencia Base")]
    [Tooltip("La secuencia por defecto si no se cumplen condiciones especiales.")]
    public DialogueSequence defaultSequence;

    [Header("Secuencias Condicionales (Sobrescritura)")]
    [Tooltip("Si alguna de estas condiciones se cumple en el StateManager, se reproducirá esta secuencia. Se evalúan de arriba a abajo.")]
    public ConditionalDialogue[] conditionalOverrides;

    [Header("Opciones")]
    [SerializeField] private bool loopDialogue = false;
    [Header("Eventos Nativos")]
    public UnityEvent<DialogueSequence> OnInteractDialogue;
    private bool _hasBeenUsed = false;
    public void Interact()
    {
        if (!_hasBeenUsed || loopDialogue)
        {
            DialogueSequence finalSequence = EvaluateConditions();
            InvokeDialogue(finalSequence);

            if (!loopDialogue)
            {
                _hasBeenUsed = true;
                enabled = false;
            }
        }
    }
    private DialogueSequence EvaluateConditions()
    {
        foreach (var condition in conditionalOverrides)
        {
            bool stateIsMet = CheckGameState(condition.requiredStateFlag);

            if (stateIsMet)
            {
                return condition.overrideSequence;
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
    private void InvokeDialogue(DialogueSequence sequenceToPlay)
    {
        OnInteractDialogue?.Invoke(sequenceToPlay);
    }
}