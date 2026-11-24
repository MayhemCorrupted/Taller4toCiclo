using UnityEngine;
[RequireComponent(typeof(Collider))]
public class IsometricCameraTrigger : MonoBehaviour
{
    [Header("Activación")]
    [SerializeField] private string _playerTag = "Player";
    [Tooltip("Si es true, el trigger solo se activará una vez.")]
    [SerializeField] private bool _oneShot = false;
    [Tooltip("Si es true, al salir del collider se restauran los valores previos.")]
    [SerializeField] private bool _revertOnExit = true;

    [Header("1. Rotación en Eje Y (Yaw)")]
    [SerializeField] private bool _modifyYaw = false;
    [Tooltip("Ángulo Y objetivo (en grados) o incremento si 'Is Relative Yaw' está activo.")]
    [SerializeField] private float _targetYawAngle = 45f;
    [SerializeField] private bool _isRelativeYaw = false;
    [Tooltip("Tiempo de suavizado (-1 usa el valor por defecto del Manager).")]
    [SerializeField] private float _yawSmoothTime = -1f;

    [Header("2. Tamaño Ortográfico (Zoom)")]
    [SerializeField] private bool _modifyOrthographicSize = false;
    [SerializeField] private float _targetOrthographicSize = 8f;
    [SerializeField] private bool _isRelativeOrthoSize = false;
    [Tooltip("Tiempo de suavizado (-1 usa el valor por defecto del Manager).")]
    [SerializeField] private float _orthoSmoothTime = -1f;

    [Header("3. Damping Temporal")]
    [SerializeField] private bool _modifyDamping = false;
    [SerializeField] private Vector3 _temporaryDamping = new(1.2f, 1.2f, 1.2f);
    [Tooltip("Duración en segundos (0 = se mantiene mientras estés dentro o hasta restaurar).")]
    [Min(0f)][SerializeField] private float _dampingDuration = 0f;

    [Header("4. Tracking Target Temporal")]
    [SerializeField] private bool _modifyTrackingTarget = false;
    [SerializeField] private Transform _temporaryTarget;
    [SerializeField] private Vector3 _targetWorldOffset = Vector3.zero;
    [Tooltip("Duración en segundos antes de volver al jugador (0 = hasta salir del trigger).")]
    [Min(0f)][SerializeField] private float _focusDuration = 2.5f;
    private bool _hasTriggered;
    private float _previousYaw;
    private float _previousOrthoSize;
    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(_playerTag)) return;
        ActivateCameraChanges();
    }
    private void OnTriggerExit(Collider other)
    {
        if (!_revertOnExit || !other.CompareTag(_playerTag)) return;
        RevertCameraChanges();
    }
    /// <summary>
    /// Puede llamarse desde el Trigger físico o desde eventos de StateManager / DialogueManager.
    /// </summary>
    public void ActivateCameraChanges()
    {
        if (_oneShot && _hasTriggered) return;
        if (IsometricCameraManager.Instance == null) return;

        _hasTriggered = true;
        IsometricCameraManager cam = IsometricCameraManager.Instance;

        if (_modifyYaw)
        {
            _previousYaw = cam.TargetYaw;
            if (_isRelativeYaw)
                cam.RotateYawBy(_targetYawAngle, _yawSmoothTime);
            else
                cam.SetYawAngle(_targetYawAngle, _yawSmoothTime);
        }

        if (_modifyOrthographicSize)
        {
            _previousOrthoSize = cam.CurrentOrthographicSize;
            if (_isRelativeOrthoSize)
                cam.AddOrthographicSize(_targetOrthographicSize, _orthoSmoothTime);
            else
                cam.SetOrthographicSize(_targetOrthographicSize, _orthoSmoothTime);
        }

        if (_modifyDamping)
        {
            cam.SetTemporaryDamping(_temporaryDamping, _dampingDuration);
        }

        if (_modifyTrackingTarget && _temporaryTarget != null)
        {
            cam.SetTemporaryTrackingTarget(
                _temporaryTarget,
                _focusDuration,
                _targetWorldOffset,
                useCustomDamping: _modifyDamping,
                customDamping: _temporaryDamping
            );
        }
    }
    public void RevertCameraChanges()
    {
        if (IsometricCameraManager.Instance == null) return;

        IsometricCameraManager cam = IsometricCameraManager.Instance;

        if (_modifyYaw)
        {
            cam.SetYawAngle(_previousYaw, _yawSmoothTime);
        }

        if (_modifyOrthographicSize)
        {
            cam.SetOrthographicSize(_previousOrthoSize, _orthoSmoothTime);
        }

        if (_modifyDamping && _dampingDuration <= 0f)
        {
            cam.ResetDamping();
        }

        if (_modifyTrackingTarget && _focusDuration <= 0f)
        {
            cam.ClearTemporaryTrackingTarget();
        }
    }
}