using UnityEngine;
using System.Collections;
public class DoorController : MonoBehaviour, IInteractable
{
    [Header("Door Settings")]
    [SerializeField] private Transform _hinge;
    [SerializeField] private float _openAngle = 90f;
    [SerializeField] private float _doorSpeed = 0.5f;

    [Header("Direction Restrictions")]
    [SerializeField] private bool _revertDirection = false;
    [SerializeField][Range(-1, 1)] private int _forcedDirection = 0;

    private Quaternion _closedRotation;
    private bool _isOpen = false;
    private bool _isMoving = false;
    private Transform _playerTransform;
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
            OpenDoor();
        }
        else
        {
            CloseDoor();
        }
    }
    private void OpenDoor()
    {
        int side = CalculateSide();
        float targetAngle = side * _openAngle;
        StartCoroutine(MoveDoorRoutine(targetAngle));
    }

    private void CloseDoor()
    {
        StartCoroutine(MoveDoorRoutine(0f));
    }

    private int CalculateSide()
    {
        if (_forcedDirection != 0) return _forcedDirection;

        if (_playerTransform == null) return 1;

        Vector3 localPlayerPos = transform.InverseTransformPoint(_playerTransform.position);

        int side = localPlayerPos.z >= 0 ? 1 : -1;

        if (_revertDirection) side *= -1;

        return side;
    }
    private IEnumerator MoveDoorRoutine(float targetAngle)
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
        _isOpen = (targetAngle != 0f);
        _isMoving = false;
    }
}
