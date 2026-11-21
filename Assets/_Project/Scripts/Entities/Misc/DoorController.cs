using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;
public class DoorController : MonoBehaviour, IInteractable
{
    [Header("Prompt de Interacción")]
    [Tooltip("Texto localizado con '{key}' (Ej: '[{key}] Abrir').")]
    [SerializeField] private LocalizedString _interactionPrompt;

    [Header("Door Settings")]
    [SerializeField] private Transform _hinge;
    [SerializeField] private float _openAngle = 90f;
    [SerializeField] private float _doorSpeed = 0.5f;
    [Header("Lock Settings")]
    [Tooltip("Si está activo, el jugador no podrá abrirla interactuando directamente hasta desbloquearla.")]
    [SerializeField] private bool _isLocked = false;
    [Tooltip("Se ejecuta cuando el jugador intenta interactuar con la puerta mientras está bloqueada.")]
    public UnityEvent OnInteractWhileLocked;
    [Tooltip("Se ejecuta cuando la puerta pasa de bloqueada a desbloqueada.")]
    public UnityEvent OnDoorUnlocked;

    [Header("Direction Restrictions")]
    [SerializeField] private bool _revertDirection = false;
    [SerializeField][Range(-1, 1)] private int _forcedDirection = 0;
    public LocalizedString InteractionPrompt => _interactionPrompt;
    public bool IsLocked
    {
        get => _isLocked;
        set => SetLocked(value);
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
    public void UnlockAndOpenDoor()
    {
        SetLocked(false);

        if (_isOpen && !_isMoving) return;

        int side = CalculateSide();
        float targetAngle = side * _openAngle;
        StartDoorMovement(targetAngle, targetOpenState: true);
    }
    public void ForceOpenDoor()
    {
        UnlockAndOpenDoor();
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
        SetLocked(true);
    }
    public void UnlockDoor()
    {
        SetLocked(false);
    }
    public void SetLocked(bool locked)
    {
        if (_isLocked == locked) return;

        _isLocked = locked;
        if (!_isLocked)
        {
            OnDoorUnlocked?.Invoke();
        }
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
            _moveCoroutine = null;
        }

        Quaternion endRot = _closedRotation * Quaternion.Euler(0f, targetAngle, 0f);

        if (!isActiveAndEnabled || _doorSpeed <= 0f)
        {
            _hinge.localRotation = endRot;
            _isOpen = targetOpenState;
            _isMoving = false;
            return;
        }

        _moveCoroutine = StartCoroutine(MoveDoorRoutine(endRot, targetOpenState));
    }
    private IEnumerator MoveDoorRoutine(Quaternion endRot, bool targetOpenState)
    {
        _isMoving = true;

        Quaternion startRot = _hinge.localRotation;
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