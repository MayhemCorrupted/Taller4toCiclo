using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
public enum IsometricCameraState
{
    Exploration,
    LockOn,
    Override
}
[DefaultExecutionOrder(-50)]
public class IsometricCameraManager : MonoBehaviour
{
    public static IsometricCameraManager Instance { get; private set; }

    public event Action<IsometricCameraState> OnCameraStateChanged;
    public event Action<float> OnYawChanged;

    [Header("Referencias Principales")]
    [SerializeField] private CinemachineCamera _cinemachineCamera;
    [Tooltip("Transform raíz del jugador. Si se deja vacío, se buscará por Tag 'Player'.")]
    [SerializeField] private Transform _playerTarget;
    [Tooltip("Offset adicional del pivote (dejar en 0,0,0 si CinemachinePositionComposer ya tiene Target Offset configurado).")]
    [SerializeField] private Vector3 _basePivotOffset = Vector3.zero;

    [Header("Rotación Isométrica (Eje Y)")]
    [Tooltip("Tiempo de suavizado al rotar la cámara en Y.")]
    [Min(0.01f)][SerializeField] private float _defaultYawSmoothTime = 0.35f;

    [Header("Offset Base por Mirada del Jugador (Exploración)")]
    [SerializeField] private bool _enablePlayerLookOffset = true;
    [Tooltip("Distancia en unidades de mundo hacia adelante de donde mira el jugador.")]
    [Min(0f)][SerializeField] private float _playerLookOffsetDistance = 1.6f;
    [Tooltip("Suavizado de cámara cuando el jugador gira su dirección de mirada.")]
    [Min(0.01f)][SerializeField] private float _lookOffsetSmoothTime = 0.25f;

    [Header("Offset Circular Superpuesto por Mouse (PC_KeyboardMouse)")]
    [SerializeField] private bool _enableMouseCircularOffset = true;
    [Tooltip("Radio máximo adicional que el cursor desplaza la cámara sobre el offset de mirada.")]
    [Min(0f)][SerializeField] private float _mouseMaxOffsetRadius = 2.8f;
    [Tooltip("Zona muerta central (0 a 1) para mantener solo el offset de mirada cuando el cursor está centrado.")]
    [Range(0f, 0.5f)][SerializeField] private float _mouseCenterDeadzone = 0.12f;
    [Min(0.01f)][SerializeField] private float _mouseOffsetSmoothTime = 0.18f;

    [Header("Offset Circular Superpuesto por Stick Derecho (PC_Gamepad)")]
    [SerializeField] private bool _enableGamepadStickOffset = true;
    [Tooltip("Radio máximo adicional que el stick derecho desplaza la cámara sobre el offset de mirada.")]
    [Min(0f)][SerializeField] private float _gamepadMaxOffsetRadius = 2.8f;
    [Tooltip("Zona muerta radial del stick derecho para evitar drift.")]
    [Range(0.05f, 0.5f)][SerializeField] private float _gamepadStickDeadzone = 0.15f;
    [Min(0.01f)][SerializeField] private float _gamepadOffsetSmoothTime = 0.16f;
    [SerializeField] private bool _invertGamepadY = false;

    [Header("Límite Combinado en Exploración")]
    [Tooltip("Distancia máxima total al sumar el offset de mirada + offset del mouse/stick (0 = sin límite adicional).")]
    [Min(0f)][SerializeField] private float _maxCombinedExploratoryOffset = 4.0f;

    [Header("Offset de Fijado (Lock-On)")]
    [Tooltip("Proporción del punto medio entre jugador y enemigo (0.5 = centro exacto entre ambos).")]
    [Range(0.1f, 0.9f)][SerializeField] private float _lockOnMidpointWeight = 0.45f;
    [Tooltip("Distancia máxima en unidades de mundo que puede desplazarse el foco hacia el enemigo fijado.")]
    [Min(0f)][SerializeField] private float _maxLockOnOffset = 4.5f;
    [Min(0.01f)][SerializeField] private float _lockOnOffsetSmoothTime = 0.22f;

    [Header("Tamaño Ortográfico (Zoom)")]
    [Min(0.01f)][SerializeField] private float _defaultOrthoSmoothTime = 0.4f;
    [SerializeField] private float _minOrthoSize = 3f;
    [SerializeField] private float _maxOrthoSize = 20f;

    [Header("Detección de Teletransporte")]
    [Tooltip("Si el jugador se mueve más de esta distancia en un frame, la cámara hace warp instantáneo.")]
    [SerializeField] private float _warpDistanceThreshold = 6f;

    public IsometricCameraState CurrentState { get; private set; } = IsometricCameraState.Exploration;
    public float CurrentYaw => _currentYaw;
    public float TargetYaw => _targetYaw;
    public float CurrentOrthographicSize => _currentOrthoSize;
    public Transform ActiveTrackingTarget => _overrideTrackingTarget != null ? _overrideTrackingTarget : _playerTarget;

    private CinemachinePositionComposer _positionComposer;
    private CinemachineFollow _followComponent;
    private PlayerCombat _playerCombat;
    private PlayerInputs _playerInputs;

    private Transform _followProxy;
    private Vector3 _lastPlayerPosition;
    private bool _hasLastPlayerPosition;

    private float _fixedPitch;
    private float _initialYaw;
    private float _currentYaw;
    private float _targetYaw;
    private float _yawVelocity;
    private float _activeYawSmoothTime;

    private Vector3 _currentWorldOffset;
    private Vector3 _offsetVelocity;

    private float _initialOrthoSize;
    private float _currentOrthoSize;
    private float _targetOrthoSize;
    private float _orthoVelocity;
    private float _activeOrthoSmoothTime;

    private Vector3 _defaultDamping = new(0.5f, 0.5f, 0.5f);
    private Coroutine _tempDampingCoroutine;

    private Transform _overrideTrackingTarget;
    private Vector3 _overrideTargetOffset;
    private bool _hasTargetLinkedDamping;
    private Coroutine _tempTargetCoroutine;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        ResolveReferences();
        InitializeFollowProxy();
        CaptureInitialCameraSettings();
    }
    private void OnEnable()
    {
        if (_playerCombat != null)
        {
            _playerCombat.OnLockOnTargetChanged += HandleLockOnTargetChanged;
        }
    }
    private void OnDisable()
    {
        if (_playerCombat != null)
        {
            _playerCombat.OnLockOnTargetChanged -= HandleLockOnTargetChanged;
        }
    }
    private void OnDestroy()
    {
        if (_followProxy != null)
            Destroy(_followProxy.gameObject);

        if (Instance == this)
            Instance = null;
    }
    private void ResolveReferences()
    {
        if (_cinemachineCamera == null)
            _cinemachineCamera = FindAnyObjectByType<CinemachineCamera>(FindObjectsInactive.Include);

        if (_cinemachineCamera != null)
        {
            _positionComposer = _cinemachineCamera.GetComponent<CinemachinePositionComposer>();
            _followComponent = _cinemachineCamera.GetComponent<CinemachineFollow>();
        }

        if (_playerTarget == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) _playerTarget = playerObj.transform;
        }

        if (_playerTarget != null)
        {
            _playerCombat = _playerTarget.GetComponent<PlayerCombat>();
            if (_playerCombat == null)
                _playerCombat = _playerTarget.GetComponentInChildren<PlayerCombat>();

            _playerInputs = _playerTarget.GetComponent<PlayerInputs>();
            if (_playerInputs == null)
                _playerInputs = _playerTarget.GetComponentInChildren<PlayerInputs>();
        }
    }
    private void InitializeFollowProxy()
    {
        GameObject proxyObj = new("IsometricCamera_FollowProxy");
        _followProxy = proxyObj.transform;
        _followProxy.rotation = Quaternion.identity;

        if (_playerTarget != null)
        {
            _followProxy.position = _playerTarget.position + _basePivotOffset;
            _lastPlayerPosition = _playerTarget.position;
            _hasLastPlayerPosition = true;
        }

        if (_cinemachineCamera != null)
        {
            _cinemachineCamera.Target.TrackingTarget = _followProxy;
        }
    }
    private void CaptureInitialCameraSettings()
    {
        if (_cinemachineCamera == null) return;

        Vector3 initialEuler = _cinemachineCamera.transform.eulerAngles;
        _fixedPitch = initialEuler.x;
        _initialYaw = initialEuler.y;
        _currentYaw = _initialYaw;
        _targetYaw = _initialYaw;
        _activeYawSmoothTime = _defaultYawSmoothTime;

        _initialOrthoSize = _cinemachineCamera.Lens.OrthographicSize;
        _currentOrthoSize = _initialOrthoSize;
        _targetOrthoSize = _initialOrthoSize;
        _activeOrthoSmoothTime = _defaultOrthoSmoothTime;

        if (_positionComposer != null)
        {
            _defaultDamping = _positionComposer.Damping;
        }
        else if (_followComponent != null)
        {
            _defaultDamping = _followComponent.TrackerSettings.PositionDamping;
        }
    }
    private void LateUpdate()
    {
        if (_cinemachineCamera == null) return;

        EvaluateCameraState();
        UpdateYawRotation();
        UpdateOrthographicSize();
        UpdateFollowProxyPosition();
    }
    #region Máquina de Estados y Seguimiento
    private void HandleLockOnTargetChanged(Transform newLockTarget)
    {
        EvaluateCameraState();
    }
    private void EvaluateCameraState()
    {
        IsometricCameraState desiredState;

        if (_overrideTrackingTarget != null)
        {
            desiredState = IsometricCameraState.Override;
        }
        else if (_playerCombat != null && _playerCombat.LockOnTarget != null)
        {
            desiredState = IsometricCameraState.LockOn;
        }
        else
        {
            desiredState = IsometricCameraState.Exploration;
        }

        if (CurrentState != desiredState)
        {
            CurrentState = desiredState;
            OnCameraStateChanged?.Invoke(CurrentState);
        }
    }
    private void UpdateFollowProxyPosition()
    {
        if (_followProxy == null) return;

        if (CurrentState == IsometricCameraState.Override && _overrideTrackingTarget != null)
        {
            _currentWorldOffset = Vector3.SmoothDamp(
                _currentWorldOffset,
                _overrideTargetOffset,
                ref _offsetVelocity,
                _lockOnOffsetSmoothTime
            );

            _followProxy.position = _overrideTrackingTarget.position + _currentWorldOffset;
            _hasLastPlayerPosition = false;
            return;
        }

        if (_playerTarget == null) return;

        if (_hasLastPlayerPosition)
        {
            Vector3 playerDelta = _playerTarget.position - _lastPlayerPosition;
            if (playerDelta.sqrMagnitude >= _warpDistanceThreshold * _warpDistanceThreshold)
            {
                _currentWorldOffset = Vector3.zero;
                _offsetVelocity = Vector3.zero;
                _followProxy.position = _playerTarget.position + _basePivotOffset;
                _cinemachineCamera.OnTargetObjectWarped(_followProxy, playerDelta);
            }
        }
        _lastPlayerPosition = _playerTarget.position;
        _hasLastPlayerPosition = true;

        Vector3 desiredOffset;
        float smoothTime;

        if (CurrentState == IsometricCameraState.LockOn && _playerCombat != null && _playerCombat.LockOnTarget != null)
        {
            desiredOffset = CalculateLockOnOffset(_playerCombat.LockOnTarget);
            smoothTime = _lockOnOffsetSmoothTime;
        }
        else
        {
            CalculateExploratoryCircularOffset(out desiredOffset, out smoothTime);
        }

        _currentWorldOffset = Vector3.SmoothDamp(
            _currentWorldOffset,
            desiredOffset,
            ref _offsetVelocity,
            smoothTime
        );

        _followProxy.position = _playerTarget.position + _basePivotOffset + _currentWorldOffset;
    }
    private Vector3 CalculatePlayerLookOffset()
    {
        if (!_enablePlayerLookOffset || _playerTarget == null || _playerLookOffsetDistance <= 0f)
            return Vector3.zero;

        Vector3 forwardXZ = _playerTarget.forward;
        forwardXZ.y = 0f;

        if (forwardXZ.sqrMagnitude < 0.0001f)
            return Vector3.zero;

        return forwardXZ.normalized * _playerLookOffsetDistance;
    }
    private void CalculateExploratoryCircularOffset(out Vector3 desiredOffset, out float smoothTime)
    {
        Vector3 baseLookOffset = CalculatePlayerLookOffset();
        Vector3 inputOverlayOffset = Vector3.zero;
        smoothTime = _lookOffsetSmoothTime;

        bool canReadPlayerAim = (_playerInputs == null || _playerInputs.AreGameplayInputsEnabled) &&
                                (UIManager.Instance == null || UIManager.Instance.CurrentState == GameState.Gameplay);

        if (canReadPlayerAim)
        {
            string deviceMode = _playerInputs != null ? _playerInputs.CurrentDeviceMode : "PC_KeyboardMouse";

            if (deviceMode == "PC_Gamepad")
            {
                inputOverlayOffset = CalculateGamepadStickOffset();
                if (inputOverlayOffset.sqrMagnitude > 0.0001f)
                    smoothTime = _gamepadOffsetSmoothTime;
            }
            else if (deviceMode == "PC_KeyboardMouse")
            {
                inputOverlayOffset = CalculateMouseCircularOffset();
                if (inputOverlayOffset.sqrMagnitude > 0.0001f)
                    smoothTime = _mouseOffsetSmoothTime;
            }
        }

        Vector3 combinedOffset = baseLookOffset + inputOverlayOffset;

        if (_maxCombinedExploratoryOffset > 0f)
        {
            combinedOffset = Vector3.ClampMagnitude(combinedOffset, _maxCombinedExploratoryOffset);
        }

        desiredOffset = combinedOffset;
    }
    private Vector3 CalculateGamepadStickOffset()
    {
        if (!_enableGamepadStickOffset || Gamepad.current == null)
            return Vector3.zero;

        Vector2 rawStick = Gamepad.current.rightStick.ReadValue();
        if (_invertGamepadY)
            rawStick.y = -rawStick.y;

        Vector2 clampedStick = Vector2.ClampMagnitude(rawStick, 1f);
        float magnitude = clampedStick.magnitude;

        if (magnitude <= _gamepadStickDeadzone)
            return Vector3.zero;

        float remappedMagnitude = Mathf.InverseLerp(_gamepadStickDeadzone, 1f, magnitude);
        Vector2 circularInput = clampedStick.normalized * remappedMagnitude;

        return ProjectCircularInputToWorld(circularInput, _gamepadMaxOffsetRadius);
    }
    private Vector3 CalculateMouseCircularOffset()
    {
        if (!_enableMouseCircularOffset || Mouse.current == null)
            return Vector3.zero;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        float halfWidth = Mathf.Max(1f, Screen.width * 0.5f);
        float halfHeight = Mathf.Max(1f, Screen.height * 0.5f);

        Vector2 centered = new(
            (mousePos.x - halfWidth) / halfWidth,
            (mousePos.y - halfHeight) / halfHeight
        );
        centered = Vector2.ClampMagnitude(centered, 1f);

        float magnitude = centered.magnitude;
        if (magnitude <= _mouseCenterDeadzone)
            return Vector3.zero;

        float remappedMagnitude = Mathf.InverseLerp(_mouseCenterDeadzone, 1f, magnitude);
        Vector2 circularInput = centered.normalized * remappedMagnitude;

        return ProjectCircularInputToWorld(circularInput, _mouseMaxOffsetRadius);
    }
    private Vector3 ProjectCircularInputToWorld(Vector2 circularInput, float maxRadius)
    {
        Quaternion yawRotation = Quaternion.Euler(0f, _currentYaw, 0f);
        Vector3 rightXZ = yawRotation * Vector3.right;
        Vector3 forwardXZ = yawRotation * Vector3.forward;

        return (rightXZ * circularInput.x + forwardXZ * circularInput.y) * maxRadius;
    }
    private Vector3 CalculateLockOnOffset(Transform enemyTarget)
    {
        if (_playerTarget == null || enemyTarget == null)
            return Vector3.zero;

        Vector3 toEnemy = enemyTarget.position - _playerTarget.position;
        toEnemy.y = 0f;

        Vector3 weightedOffset = toEnemy * _lockOnMidpointWeight;
        return Vector3.ClampMagnitude(weightedOffset, _maxLockOnOffset);
    }
    #endregion
    #region Rotación en Eje Y (Yaw)
    private void UpdateYawRotation()
    {
        _currentYaw = Mathf.SmoothDampAngle(_currentYaw, _targetYaw, ref _yawVelocity, _activeYawSmoothTime);
        _cinemachineCamera.transform.rotation = Quaternion.Euler(_fixedPitch, _currentYaw, 0f);
    }
    public void SetYawAngle(float targetYawAngle, float customSmoothTime = -1f)
    {
        _targetYaw = Mathf.Repeat(targetYawAngle, 360f);
        _activeYawSmoothTime = customSmoothTime > 0f ? customSmoothTime : _defaultYawSmoothTime;
        OnYawChanged?.Invoke(_targetYaw);
    }
    public void RotateYawBy(float deltaDegrees, float customSmoothTime = -1f)
    {
        SetYawAngle(_targetYaw + deltaDegrees, customSmoothTime);
    }
    public void ResetYawAngle(float customSmoothTime = -1f)
    {
        SetYawAngle(_initialYaw, customSmoothTime);
    }
    #endregion
    #region Tamaño Ortográfico (Zoom)
    private void UpdateOrthographicSize()
    {
        _currentOrthoSize = Mathf.SmoothDamp(_currentOrthoSize, _targetOrthoSize, ref _orthoVelocity, _activeOrthoSmoothTime);

        LensSettings lens = _cinemachineCamera.Lens;
        if (!Mathf.Approximately(lens.OrthographicSize, _currentOrthoSize))
        {
            lens.OrthographicSize = _currentOrthoSize;
            _cinemachineCamera.Lens = lens;
        }
    }
    public void SetOrthographicSize(float targetSize, float customSmoothTime = -1f)
    {
        _targetOrthoSize = Mathf.Clamp(targetSize, _minOrthoSize, _maxOrthoSize);
        _activeOrthoSmoothTime = customSmoothTime > 0f ? customSmoothTime : _defaultOrthoSmoothTime;
    }
    public void AddOrthographicSize(float deltaSize, float customSmoothTime = -1f)
    {
        SetOrthographicSize(_targetOrthoSize + deltaSize, customSmoothTime);
    }
    public void ResetOrthographicSize(float customSmoothTime = -1f)
    {
        SetOrthographicSize(_initialOrthoSize, customSmoothTime);
    }
    #endregion
    #region Damping Temporal
    public void SetTemporaryDamping(Vector3 temporaryDamping, float duration = 0f)
    {
        if (_tempDampingCoroutine != null)
        {
            StopCoroutine(_tempDampingCoroutine);
            _tempDampingCoroutine = null;
        }

        ApplyDampingInternal(temporaryDamping);

        if (duration > 0f)
        {
            _tempDampingCoroutine = StartCoroutine(RestoreDampingAfterDelay(duration));
        }
    }
    public void ResetDamping()
    {
        if (_tempDampingCoroutine != null)
        {
            StopCoroutine(_tempDampingCoroutine);
            _tempDampingCoroutine = null;
        }

        ApplyDampingInternal(_defaultDamping);
    }
    private void ApplyDampingInternal(Vector3 damping)
    {
        if (_positionComposer != null)
        {
            _positionComposer.Damping = damping;
        }
        else if (_followComponent != null)
        {
            _followComponent.TrackerSettings.PositionDamping = damping;
        }
    }
    private IEnumerator RestoreDampingAfterDelay(float duration)
    {
        yield return new WaitForSeconds(duration);
        _tempDampingCoroutine = null;
        ApplyDampingInternal(_defaultDamping);
    }
    #endregion
    #region Tracking Target Temporal
    public void SetTemporaryTrackingTarget(Transform newTarget, float duration = 0f, Vector3 worldOffset = default, bool useCustomDamping = false, Vector3 customDamping = default)
    {
        if (newTarget == null)
        {
            ClearTemporaryTrackingTarget();
            return;
        }

        if (_tempTargetCoroutine != null)
        {
            StopCoroutine(_tempTargetCoroutine);
            _tempTargetCoroutine = null;
        }

        _overrideTrackingTarget = newTarget;
        _overrideTargetOffset = worldOffset;
        _offsetVelocity = Vector3.zero;

        if (useCustomDamping)
        {
            _hasTargetLinkedDamping = true;
            SetTemporaryDamping(customDamping, 0f);
        }

        EvaluateCameraState();

        if (duration > 0f)
        {
            _tempTargetCoroutine = StartCoroutine(ClearTemporaryTargetAfterDelay(duration));
        }
    }
    public void ClearTemporaryTrackingTarget()
    {
        if (_tempTargetCoroutine != null)
        {
            StopCoroutine(_tempTargetCoroutine);
            _tempTargetCoroutine = null;
        }

        _overrideTrackingTarget = null;
        _overrideTargetOffset = Vector3.zero;
        _offsetVelocity = Vector3.zero;

        if (_hasTargetLinkedDamping)
        {
            _hasTargetLinkedDamping = false;
            ResetDamping();
        }

        EvaluateCameraState();
    }
    private IEnumerator ClearTemporaryTargetAfterDelay(float duration)
    {
        yield return new WaitForSeconds(duration);
        _tempTargetCoroutine = null;
        ClearTemporaryTrackingTarget();
    }

    #endregion
}