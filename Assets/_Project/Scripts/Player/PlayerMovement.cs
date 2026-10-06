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
    [SerializeField] private float _dashSpeed = 12f;
    [SerializeField] private float _accelerationRate = 5f;
    [SerializeField] private float _turnSmoothTime = 0.1f;
    [SerializeField] private float _gravity = -9.81f;

    private float _turnSmoothVelocity;
    private float _currentSpeed;
    private Vector3 _dashDirection;
    private Vector3 _velocity;
    private Vector3 _currentMoveDirection;

    private CharacterController _characterController;
    private PlayerInputs _playerInputs;
    private PlayerStats _playerStats;
    private PlayerFSM _playerFSM;
    private PlayerStamina _playerStamina;
    private PlayerCombat _playerCombat;
    private Transform _mainCameraTransform;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _playerInputs = GetComponent<PlayerInputs>();
        _playerStats = GetComponent<PlayerStats>();
        _playerFSM = GetComponent<PlayerFSM>();
        _playerStamina = GetComponent<PlayerStamina>();
        _playerCombat = GetComponent<PlayerCombat>();
        if (Camera.main != null) _mainCameraTransform = Camera.main.transform;
    }
    private void Update()
    {
        HandleMovement();
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
                    {
                        targetAngle += _mainCameraTransform.eulerAngles.y;
                    }

                    _dashDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
                }
                else
                {
                    _dashDirection = transform.forward;
                }
            }

            _characterController.Move(_dashSpeed * Time.deltaTime * _dashDirection);
            return;
        }
        _dashDirection = Vector3.zero;

        if (_playerFSM.CurrentState != PlayerState.Walking &&
            _playerFSM.CurrentState != PlayerState.Running &&
            _playerFSM.CurrentState != PlayerState.Idle)
        {
            _currentSpeed = Mathf.Lerp(_currentSpeed, 0, Time.deltaTime * (_accelerationRate * 2));

            if (_currentSpeed > 0.01f)
            {
                _characterController.Move(transform.forward * (_currentSpeed * Time.deltaTime));
            }
            Gravity();
            return;
        }

        Vector2 moveInput = _playerInputs.MoveInput;
        Vector3 moveDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
        float targetSpeed = 0;

        if (moveDirection.magnitude >= 0.1f)
        {
            bool hasSufficientStamina = _playerStamina.CurrentStamina > 0;
            float baseSpeed = (_playerFSM.CurrentState == PlayerState.Running && hasSufficientStamina) ? _runSpeed : _walkSpeed;
            targetSpeed = baseSpeed * _playerStats.SpeedMultiplier;

            if (_playerCombat.LockOnTarget != null)
            {
                _currentMoveDirection = (transform.right * moveInput.x + transform.forward * moveInput.y).normalized;
            }
            else
            {
                float targetAngle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;
                if (_mainCameraTransform != null)
                {
                    targetAngle += _mainCameraTransform.eulerAngles.y;
                }
                _currentMoveDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
                float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _turnSmoothVelocity, _turnSmoothTime);
                transform.rotation = Quaternion.Euler(0f, angle, 0f);
            }
        }

        _currentSpeed = Mathf.Lerp(_currentSpeed, targetSpeed, Time.deltaTime * _accelerationRate);
       
        if (_currentSpeed > 0.01f && _currentMoveDirection != Vector3.zero)
        {
            _characterController.Move(_currentMoveDirection * (_currentSpeed * Time.deltaTime));
        }

        Gravity();
    }
    private void Gravity()
    {
        if (_characterController.isGrounded && _velocity.y < 0)
        {
            _velocity.y = -2f;
        }
        _velocity.y += _gravity * Time.deltaTime;
        _characterController.Move(_velocity * Time.deltaTime);
    }
}