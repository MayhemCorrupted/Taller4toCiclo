using System;
using UnityEngine;
using UnityEngine.Events;
[Serializable]
public struct DialogueEventBinding
{
    [Tooltip("El ID de texto escrito en el ScriptableObject del diálogo.")]
    public string eventID;
    [Tooltip("Lo que ocurrirá en la escena cuando se lea ese ID.")]
    public UnityEvent onEventTriggered;
}
public class DialogueSceneEventHandler : MonoBehaviour
{
    [SerializeField] private DialogueEventBinding[] _eventBindings;
    private DialogueManager _dialogueManager;
    private void Awake()
    {
        _dialogueManager = FindAnyObjectByType<DialogueManager>();
    }
    private void OnEnable()
    {
        if (_dialogueManager != null)
        {
            _dialogueManager.OnDialogueEventTriggered += HandleDialogueEvent;
        }
    }
    private void OnDisable()
    {
        if (_dialogueManager != null)
        {
            _dialogueManager.OnDialogueEventTriggered -= HandleDialogueEvent;
        }
    }
    private void HandleDialogueEvent(string incomingEventID)
    {
        foreach (var binding in _eventBindings)
        {
            if (binding.eventID == incomingEventID)
            {
                binding.onEventTriggered?.Invoke();
            }
        }
    }
}
