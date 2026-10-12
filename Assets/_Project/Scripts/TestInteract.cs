using UnityEngine;
using UnityEngine.Events;

public class TestInteract : MonoBehaviour, IInteractable
{
    public UnityEvent OnInteract;
    public void Interact()
    {
        OnInteract?.Invoke();
    }
}
