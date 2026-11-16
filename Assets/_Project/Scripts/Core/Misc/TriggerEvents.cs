using UnityEngine;
using UnityEngine.Events;
[RequireComponent(typeof(Collider))]
public class TriggerEvents : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("Tag asignado al GameObject principal del jugador.")]
    [SerializeField] private string _playerTag = "Player";
    [Tooltip("Si está activo, el evento solo se ejecutará una única vez.")]
    [SerializeField] private bool _triggerOnce = true;
    [Header("Eventos")]
    [SerializeField] private UnityEvent OnTriggered;
    private bool triggered;
    private void Awake()
    {
        if (TryGetComponent(out Collider col) && !col.isTrigger)
        {
            col.isTrigger = true;
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (_triggerOnce && triggered) return;
        if (!IsPlayer(other)) return;

        triggered = true;
        OnTriggered?.Invoke();
    }
    private bool IsPlayer(Collider other)
    {
        if (other.isTrigger) return false;

        if (!string.IsNullOrEmpty(_playerTag) && other.CompareTag(_playerTag))
        {
            return true;
        }

        return other.GetComponentInParent<PlayerFSM>() != null;
    }
    public void ResetTrigger()
    {
        triggered = false;
    }
}