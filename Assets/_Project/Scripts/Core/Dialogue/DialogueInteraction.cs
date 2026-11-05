using System;
using UnityEngine;
using UnityEngine.Events;

public class DialogueInteraction : MonoBehaviour, IInteractable
{
    public UnityEvent OnInteractDialogue;

    [SerializeField] private bool loopDialogue = false;

    private bool _hasBeenUsed = false;

    public void Interact()
    {
        if (!_hasBeenUsed || loopDialogue)
        {
            InvokeDialogue();

            if (!loopDialogue)
            {
                _hasBeenUsed = true;

                enabled = false;
            }
        }
    }
    private void InvokeDialogue()
    {
        OnInteractDialogue?.Invoke();
    }
}