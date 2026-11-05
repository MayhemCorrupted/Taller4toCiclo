using System;
using UnityEngine;
using UnityEngine.Events;

public class DialogueInteraction : MonoBehaviour, IInteractable
{
    public UnityEvent OnInteractDialogue;
    [SerializeField] private bool loopDialogue = false;
    public void Interact()
    {
    }
    private void InvokeDialogue()
    {
        OnInteractDialogue?.Invoke();
    }
}
