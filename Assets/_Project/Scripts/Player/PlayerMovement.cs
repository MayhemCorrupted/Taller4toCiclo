using UnityEngine;
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInputs))]
[RequireComponent(typeof(PlayerFSM))]
[RequireComponent(typeof(PlayerStats))]
[RequireComponent(typeof(PlayerStamina))]
[RequireComponent(typeof(PlayerCombat))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float _walkSpeed = 5f;
    [SerializeField] private float _runSpeed = 10f;
    [Header("Dash Settings")]
    [SerializeField] private float _dashSpeed = 12f;
    [Tooltip("La curva determina el multiplicador de velocidad sincronizado con la animación. Eje X = % Animación (0 a 1), Eje Y = Multiplicador (1 a 0).")]
    [SerializeField] private AnimationCurve _dashCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
    [Space]
    [SerializeField] private float _accelerationRate = 5f;
    [SerializeField] private float _decelerationRate = 12f;
    [SerializeField] [Range(0.05f, 0.5f)] private float _turnSmoothTime = 0.1f;
    [SerializeField] private float _gravity = -9.81f;
    [SerializeField] private float _combatFriction = 15f;
    [SerializeField] private float _blockedKnockbackMultiplier = 0.35f;

    private float _turnSmoothVelocity;
    private Vector3 _dashDirection;
    private Vector3 _verticalVelocity;
    private Vector3 _currentVelocity;

    private CharacterController _characterController;
    private PlayerInputs _playerInputs;
    private PlayerStats _playerStats;
    private PlayerFSM _playerFSM;
    private PlayerStamina _playerStamina;
    private PlayerCombat _playerCombat;
    private PlayerAnimator _playerAnimator;
    private Transform _mainCameraTransform;
    public Vector3 CurrentVelocity => _characterController.velocity;
    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _playerInputs = GetComponent<PlayerInputs>();
        _playerStats = GetComponent<PlayerStats>();
        _playerFSM = GetComponent<PlayerFSM>();
        _playerStamina = GetComponent<PlayerStamina>();
        _playerCombat = GetComponent<PlayerCombat>();
        _playerAnimator = GetComponentInChildren<PlayerAnimator>();
        if (Camera.main != null) _mainCameraTransform = Camera.main.transform;
    }
    private void Update()
    {
        if (UIManager.Instance.CurrentState == GameState.Gameplay) HandleMovement();
    }
    private void HandleMovement()
    {
        if (_playerFSM.CurrentState == PlayerState.Dodging)
        {
            if (_dashDirection == Vector3.zero)
            {
                Vector2 input = _playerInputs.MoveInput;
                Vector3 inputDirection = new Vector3(input.x, 0f, input.y).normalized;

                if (inputDirection.magnitude >= 0.1f)
                {
                    float targetAngle = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg;
                    if (_mainCameraTransform != null)
                        targetAngle += _mainCameraTransform.eulerAngles.y;

                    _dashDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
                }
                else
                {
                    _dashDirection = transform.forward;
                }

                if (_dashDirection != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(_dashDirection);
                }
            }

            float normalizedTime = _playerAnimator != null ? _playerAnimator.DodgeNormalizedTime : 0f;
            float currentDashSpeed = _dashSpeed * _dashCurve.Evaluate(normalizedTime);

            currentDashSpeed *= _playerStats.SpeedMultiplier;

            Vector2 currentInput = _playerInputs.MoveInput;
            if (currentInput.magnitude >= 0.1f)
            {
                bool hasSufficientStamina = _playerStamina.CurrentStamina > 0;
                float baseTargetSpeed = (_playerInputs.InputRun && hasSufficientStamina) ? _runSpeed : _walkSpeed;
                float intendedSpeed = baseTargetSpeed * _playerStats.SpeedMultiplier;

                currentDashSpeed = Mathf.Max(currentDashSpeed, intendedSpeed);
            }
            _characterController.Move(currentDashSpeed * Time.deltaTime * _dashDirection);
            Gravity();
            return;
        }

        _dashDirection = Vector3.zero;

        if (_playerFSM.CurrentState != PlayerState.Walking &&
            _playerFSM.CurrentState != PlayerState.Running &&
            _playerFSM.CurrentState != PlayerState.Idle)
        {
            _currentVelocity = Vector3.Lerp(_currentVelocity, Vector3.zero, _combatFriction * Time.deltaTime);
            if (_currentVelocity.magnitude > 0.01f)
            {
                _characterController.Move(_currentVelocity * Time.deltaTime);
            }
            Gravity();
            return;
        }

        Vector2 moveInput = _playerInputs.MoveInput;
        Vector3 moveDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
        Vector3 targetVelocity = Vector3.zero;

        if (moveDirection.magnitude >= 0.1f)
        {
            bool hasSufficientStamina = _playerStamina.CurrentStamina > 0;
            float baseSpeed = (_playerFSM.CurrentState == PlayerState.Running && hasSufficientStamina) ? _runSpeed : _walkSpeed;
            float targetSpeed = baseSpeed * _playerStats.SpeedMultiplier;

            float targetAngle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;
            if (_mainCameraTransform != null)
            {
                targetAngle += _mainCameraTransform.eulerAngles.y;
            }
            Vector3 adjustedDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            targetVelocity = adjustedDirection * targetSpeed;

            if (_playerCombat.LockOnTarget == null)
            {
                float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _turnSmoothVelocity, _turnSmoothTime);
                transform.rotation = Quaternion.Euler(0f, angle, 0f);
            }
        }

        float currentRate = moveDirection.magnitude > 0.1f ? _accelerationRate : _decelerationRate;
        _currentVelocity = Vector3.Lerp(_currentVelocity, targetVelocity, Time.deltaTime * currentRate);

        if (_currentVelocity.magnitude > 0.01f)
        {
            _characterController.Move(_currentVelocity * Time.deltaTime);
        }

        Gravity();
    }
    public void ApplyCombatImpulse(float force)
    {
        _currentVelocity = transform.forward * force;
    }
    public void ApplyKnockback(Vector3 direction, float force)
    {
        if (_playerFSM.CurrentState == PlayerState.Dodging ||
            _playerFSM.CurrentState == PlayerState.Hurt ||
            _playerFSM.CurrentState == PlayerState.Knockout ||
            _playerFSM.CurrentState == PlayerState.Death)
        {
            return;
        }

        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return;

        float appliedForce = force;
        if (_playerFSM.CurrentState == PlayerState.Blocking)
        {
            appliedForce *= _blockedKnockbackMultiplier;
        }

        _currentVelocity = direction.normalized * appliedForce;
    }
    private void Gravity()
    {
        if (_characterController.isGrounded && _verticalVelocity.y < 0)
        {
            _verticalVelocity.y = -2f;
        }
        _verticalVelocity.y += _gravity * Time.deltaTime;
        _characterController.Move(_verticalVelocity * Time.deltaTime);
    }
}