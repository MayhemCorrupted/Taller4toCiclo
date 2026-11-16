using System;
using UnityEngine;
public interface IInteractable
{
    void Interact();
}
[RequireComponent(typeof(PlayerInputs))]
[RequireComponent(typeof(PlayerFSM))]
public class PlayerInteraction : MonoBehaviour
{
    public event Action<bool> OnCanInteractChanged;
    public bool CanInteract { get; private set; }

    [SerializeField] private LayerMask interactLayer;
    [SerializeField] private float interactRange = 3f;

    private readonly Collider[] _interactBuffer = new Collider[8];
    private PlayerFSM _playerFSM;
    private PlayerInputs _playerInputs;
    private IInteractable _currentClosestInteractable;

    private void Awake()
    {
        _playerFSM = GetComponent<PlayerFSM>();
        _playerInputs = GetComponent<PlayerInputs>();
    }
    private void OnEnable()
    {
        if (_playerInputs != null)
            _playerInputs.OnInteract += TryInteract;
    }
    private void OnDisable()
    {
        if (_playerInputs != null)
            _playerInputs.OnInteract -= TryInteract;

        _currentClosestInteractable = null;
        SetCanInteractState(false);
    }
    private void Update()
    {
        _currentClosestInteractable = FindClosestValidInteractable();
        SetCanInteractState(_currentClosestInteractable != null);
    }
    private void TryInteract()
    {
        IInteractable target = _currentClosestInteractable ?? FindClosestValidInteractable();
        target?.Interact();
    }
    private IInteractable FindClosestValidInteractable()
    {
        if (_playerFSM == null) return null;

        if (UIManager.Instance != null && UIManager.Instance.CurrentState != GameState.Gameplay)
            return null;

        if (_playerFSM.CurrentState != PlayerState.Idle &&
            _playerFSM.CurrentState != PlayerState.Walking &&
            _playerFSM.CurrentState != PlayerState.Running)
        {
            return null;
        }

        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, interactRange, _interactBuffer, interactLayer);
        if (hitCount == 0) return null;

        float closestDistance = Mathf.Infinity;
        IInteractable closestInteractable = null;

        for (int i = 0; i < hitCount; i++)
        {
            Collider col = _interactBuffer[i];
            if (col == null) continue;

            IInteractable interactable = col.GetComponent<IInteractable>();
            interactable ??= col.GetComponentInParent<IInteractable>();

            if (interactable == null) continue;

            if (interactable is Behaviour behaviour && !behaviour.enabled)
                continue;

            float distance = Vector3.Distance(transform.position, col.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestInteractable = interactable;
            }
        }

        return closestInteractable;
    }
    private void SetCanInteractState(bool newState)
    {
        if (CanInteract == newState) return;

        CanInteract = newState;
        OnCanInteractChanged?.Invoke(CanInteract);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetInteractableCursor(CanInteract);
        }
    }
}