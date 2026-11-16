using System.Collections;
using UnityEngine;
using UnityEngine.Events;
public class DoorController : MonoBehaviour, IInteractable
{
    [Header("Door Settings")]
    [SerializeField] private Transform _hinge;
    [SerializeField] private float _openAngle = 90f;
    [SerializeField] private float _doorSpeed = 0.5f;

    [Header("Lock Settings")]
    [Tooltip("Si está activo, la puerta no podrá abrirse hasta desbloquearla.")]
    [SerializeField] private bool _isLocked = false;
    [Tooltip("Opcional: Se ejecuta cuando el jugador intenta interactuar con la puerta mientras está bloqueada.")]
    public UnityEvent OnInteractWhileLocked;

    [Header("Direction Restrictions")]
    [SerializeField] private bool _revertDirection = false;
    [SerializeField][Range(-1, 1)] private int _forcedDirection = 0;
    public bool IsLocked
    {
        get => _isLocked;
        set => _isLocked = value;
    }
    public bool IsOpen => _isOpen;
    public bool IsMoving => _isMoving;
    private Quaternion _closedRotation;
    private bool _isOpen = false;
    private bool _isMoving = false;
    private Transform _playerTransform;
    private Coroutine _moveCoroutine;
    private void Awake()
    {
        if (_hinge == null)
        {
            if (transform.childCount > 0)
                _hinge = transform.GetChild(0);
            else
            {
                Debug.LogError($"[DoorController] No se asignó un Hinge en {gameObject.name}.");
                enabled = false;
                return;
            }
        }

        _closedRotation = _hinge.localRotation;
    }
    private void Start()
    {
        PlayerMovement player = FindAnyObjectByType<PlayerMovement>();
        if (player != null)
        {
            _playerTransform = player.transform;
        }
    }
    public void Interact()
    {
        if (_isMoving) return;

        if (!_isOpen)
        {
            if (_isLocked)
            {
                OnInteractWhileLocked?.Invoke();
                return;
            }

            OpenDoor();
        }
        else
        {
            CloseDoor();
        }
    }
    public void OpenDoor()
    {
        if (_isLocked || _isOpen) return;

        int side = CalculateSide();
        float targetAngle = side * _openAngle;
        StartDoorMovement(targetAngle, targetOpenState: true);
    }
    public void CloseDoor()
    {
        if (!_isOpen && !_isMoving) return;

        StartDoorMovement(0f, targetOpenState: false);
    }
    public void ToggleDoor()
    {
        if (_isOpen) CloseDoor();
        else OpenDoor();
    }
    public void LockDoor()
    {
        _isLocked = true;
    }
    public void UnlockDoor()
    {
        _isLocked = false;
    }
    public void SetLocked(bool locked)
    {
        _isLocked = locked;
    }
    public void UnlockAndOpenDoor()
    {
        _isLocked = false;
        OpenDoor();
    }
    private int CalculateSide()
    {
        if (_forcedDirection != 0) return _forcedDirection;

        if (_playerTransform == null)
        {
            PlayerMovement player = FindAnyObjectByType<PlayerMovement>();
            if (player != null) _playerTransform = player.transform;
            else return 1;
        }

        Vector3 localPlayerPos = transform.InverseTransformPoint(_playerTransform.position);
        int side = localPlayerPos.z >= 0 ? 1 : -1;

        if (_revertDirection) side *= -1;

        return side;
    }
    private void StartDoorMovement(float targetAngle, bool targetOpenState)
    {
        if (_moveCoroutine != null)
        {
            StopCoroutine(_moveCoroutine);
        }

        _moveCoroutine = StartCoroutine(MoveDoorRoutine(targetAngle, targetOpenState));
    }
    private IEnumerator MoveDoorRoutine(float targetAngle, bool targetOpenState)
    {
        _isMoving = true;

        Quaternion startRot = _hinge.localRotation;
        Quaternion endRot = _closedRotation * Quaternion.Euler(0f, targetAngle, 0f);

        float elapsed = 0f;

        while (elapsed < _doorSpeed)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / _doorSpeed);
            _hinge.localRotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }

        _hinge.localRotation = endRot;
        _isOpen = targetOpenState;
        _isMoving = false;
        _moveCoroutine = null;
    }
}