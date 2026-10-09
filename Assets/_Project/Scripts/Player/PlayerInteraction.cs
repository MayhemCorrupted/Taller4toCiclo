using UnityEngine;
public interface IInteractable
{
    void Interact();
}

[RequireComponent(typeof(PlayerInputs))]
[RequireComponent(typeof(PlayerFSM))]
public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private LayerMask interactLayer;
    [SerializeField] private float interactRange = 3;

    readonly private Collider[] _interactBuffer = new Collider[5];
    private PlayerFSM _playerFSM;
    private PlayerInputs _playerInputs;

    private void Awake()
    {
        _playerFSM = GetComponent<PlayerFSM>();
        _playerInputs = GetComponent<PlayerInputs>();
    }
    private void OnEnable()
    {
        _playerInputs.OnInteract += TryInteract;
    }
    private void OnDisable()
    {
        _playerInputs.OnInteract -= TryInteract;
    }
    private void TryInteract()
    {
        if (_playerFSM.CurrentState != PlayerState.Idle &&
            _playerFSM.CurrentState != PlayerState.Walking &&
            _playerFSM.CurrentState != PlayerState.Running)
        {
            return;
        }
        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, interactRange, _interactBuffer, interactLayer);

        if (hitCount == 0) return;

        float closestDistance = Mathf.Infinity;
        IInteractable closestInteractable = null;

        for (int i = 0; i < hitCount; i++)
        {
            if (_interactBuffer[i].TryGetComponent(out IInteractable interactable))
            {
                float distance = Vector3.Distance(transform.position, _interactBuffer[i].transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestInteractable = interactable;
                }
            }
        }
        closestInteractable?.Interact();
    }
}
