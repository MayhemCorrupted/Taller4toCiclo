using UnityEngine;
using System;
public class PlayerAnimator : MonoBehaviour
{
    public event Action OnStandUpCompleted;
    public bool HasWeapon { get; set; } = true;
    public bool IsBlockHitPlaying => _isBlockHitPlaying;
    public bool IsLaying { get; private set; }
    public bool IsStandingUp { get; private set; }
    [Header("Referencias")]
    [SerializeField] private Animator _animator;
    [Header("Configuración de Layers")]
    [Tooltip("Nombre del Layer de Combate en el Animator (si no se encuentra, usará el índice 1).")]
    [SerializeField] private string _combatLayerName = "Combat";
    [Tooltip("Nombre exacto del Layer en el Animator dedicado a Laying y StandUp.")]
    [SerializeField] private string _layingLayerName = "Laying";
    [Tooltip("Si está activo, el jugador comenzará la escena tirado en el suelo con el layer de Laying en weight 1.")]
    [SerializeField] private bool _startLaying = false;
    private PlayerFSM _playerFSM;
    private PlayerInputs _playerInputs;
    private PlayerCombat _playerCombat;
    private PlayerHealth _playerHealth;
    private PlayerStats _playerStats;
    private InventoryManager _inventoryManager;
    private Transform _mainCameraTransform;

    private readonly int _speedHash = Animator.StringToHash("Speed");
    private readonly int _inputXHash = Animator.StringToHash("InputX");
    private readonly int _inputYHash = Animator.StringToHash("InputY");
    private readonly int _isLockedOnHash = Animator.StringToHash("IsLockedOn");
    private readonly int _hasWeaponHash = Animator.StringToHash("HasWeapon");

    private readonly int _lightAttackHash = Animator.StringToHash("LightAttack");
    private readonly int _heavyAttackHash = Animator.StringToHash("HeavyAttack");

    private readonly int _dodgeHash = Animator.StringToHash("Dodge");
    private readonly int _isBlockingHash = Animator.StringToHash("IsBlocking");
    private readonly int _skillTriggerHash = Animator.StringToHash("SkillTrigger");
    private readonly int _skillIDHash = Animator.StringToHash("SkillID");
    private readonly int _blockHitHash = Animator.StringToHash("BlockHit");

    private readonly int _layingBoolHash = Animator.StringToHash("Laying");
    private readonly int _standUpHash = Animator.StringToHash("StandUp");

    private readonly int _hurtHash = Animator.StringToHash("Hurt");
    private readonly int _itemHash = Animator.StringToHash("Item");
    private readonly int _knockoutHash = Animator.StringToHash("Knockout");
    private readonly int _permadeathHash = Animator.StringToHash("Permadeath");

    private float _currentInputX;
    private float _currentInputY;
    private Transform _playerRoot;
    public float DodgeNormalizedTime { get; private set; }
    private int _remainingIframes;
    private bool _areIframesActive = false;

    private bool _isBlockHitPlaying = false;
    private bool _hasEnteredBlockHitState = false;
    private int _blockHitLayerIndex = 1;
    private float _blockHitSafetyTimer = 0f;
    private const float MaxBlockHitLockDuration = 1.2f;

    private bool _isHurtPlaying = false;
    private float _hurtSafetyTimer = 0f;
    private const float MaxHurtLockDuration = 1.2f;

    private int _combatLayerIndex = 1;
    private int _layingLayerIndex = -1;
    private void Awake()
    {
        _playerFSM = GetComponentInParent<PlayerFSM>();
        _playerInputs = GetComponentInParent<PlayerInputs>();
        _playerCombat = GetComponentInParent<PlayerCombat>();
        _playerHealth = GetComponentInParent<PlayerHealth>();
        _playerStats = GetComponentInParent<PlayerStats>();

        _inventoryManager = FindAnyObjectByType<InventoryManager>();

        _playerRoot = _playerFSM != null ? _playerFSM.transform : transform;
        if (Camera.main != null) _mainCameraTransform = Camera.main.transform;

        if (_playerCombat != null) HasWeapon = _playerCombat.IsWeaponActive;
        if (_animator != null)
        {
            _animator.SetBool(_hasWeaponHash, HasWeapon);
            ResolveLayerIndices();
            SetCombatLayerWeight(0f);
            SetLayingLayerWeight(0f);
        }
    }
    private void Start()
    {
        ResolveLayerIndices();
        SetCombatLayerWeight(0f);

        if (_startLaying)
        {
            StartLaying();
        }
        else
        {
            StopLaying();
        }
    }
    private void OnEnable()
    {
        if (_playerInputs != null)
        {
            _playerInputs.OnLightAttack += TriggerLightAttack;
            _playerInputs.OnHeavyAttack += TriggerHeavyAttack;
        }

        if (_playerFSM != null)
        {
            _playerFSM.OnStateChanged += HandleFSMStateChanged;
        }

        if (_playerHealth != null)
        {
            _playerHealth.OnHurt += TriggerHurt;
            _playerHealth.OnBlockHit += TriggerBlockHit;
        }

        if (_playerCombat != null)
        {
            _playerCombat.OnWeaponActiveChanged += HandleWeaponActiveChanged;
        }
    }
    private void OnDisable()
    {
        if (_playerInputs != null)
        {
            _playerInputs.OnLightAttack -= TriggerLightAttack;
            _playerInputs.OnHeavyAttack -= TriggerHeavyAttack;
        }

        if (_playerFSM != null)
        {
            _playerFSM.OnStateChanged -= HandleFSMStateChanged;
        }

        if (_playerHealth != null)
        {
            _playerHealth.OnHurt -= TriggerHurt;
            _playerHealth.OnBlockHit -= TriggerBlockHit;
        }

        if (_playerCombat != null)
        {
            _playerCombat.OnWeaponActiveChanged -= HandleWeaponActiveChanged;
        }
    }
    private void ResolveLayerIndices()
    {
        if (_animator == null) return;

        _layingLayerIndex = -1;
        string targetLaying = _layingLayerName != null ? _layingLayerName.Trim() : string.Empty;

        if (!string.IsNullOrEmpty(targetLaying))
        {
            _layingLayerIndex = _animator.GetLayerIndex(targetLaying);
            if (_layingLayerIndex < 0)
            {
                for (int i = 1; i < _animator.layerCount; i++)
                {
                    if (string.Equals(_animator.GetLayerName(i).Trim(), targetLaying, System.StringComparison.OrdinalIgnoreCase))
                    {
                        _layingLayerIndex = i;
                        break;
                    }
                }
            }
        }

        if (_layingLayerIndex < 0 && _animator.layerCount > 2)
        {
            _layingLayerIndex = _animator.layerCount - 1;
        }

        int resolvedCombat = -1;
        string targetCombat = _combatLayerName != null ? _combatLayerName.Trim() : string.Empty;
        if (!string.IsNullOrEmpty(targetCombat))
        {
            resolvedCombat = _animator.GetLayerIndex(targetCombat);
            if (resolvedCombat < 0)
            {
                for (int i = 1; i < _animator.layerCount; i++)
                {
                    if (string.Equals(_animator.GetLayerName(i).Trim(), targetCombat, System.StringComparison.OrdinalIgnoreCase))
                    {
                        resolvedCombat = i;
                        break;
                    }
                }
            }
        }

        if (resolvedCombat > 0 && resolvedCombat != _layingLayerIndex)
        {
            _combatLayerIndex = resolvedCombat;
        }
        else if (_animator.layerCount > 1 && _layingLayerIndex != 1)
        {
            _combatLayerIndex = 1;
        }
        else
        {
            _combatLayerIndex = 0;
        }

        _blockHitLayerIndex = _combatLayerIndex;
    }
    private void SetCombatLayerWeight(float weight)
    {
        if (_animator == null) return;

        if (_combatLayerIndex > 0 && _combatLayerIndex < _animator.layerCount && _combatLayerIndex != _layingLayerIndex)
        {
            _animator.SetLayerWeight(_combatLayerIndex, weight);
        }
    }
    private void SetLayingLayerWeight(float weight)
    {
        if (_animator == null) return;

        if (_layingLayerIndex < 0)
        {
            ResolveLayerIndices();
        }

        if (_layingLayerIndex > 0 && _layingLayerIndex < _animator.layerCount)
        {
            _animator.SetLayerWeight(_layingLayerIndex, weight);
        }
    }
    private void HandleWeaponActiveChanged(bool isWeaponActive)
    {
        HasWeapon = isWeaponActive;
        if (_animator != null)
        {
            _animator.SetBool(_hasWeaponHash, HasWeapon);
        }
    }
    public void StartLaying()
    {
        SetLaying(true);
    }
    public void StopLaying()
    {
        IsStandingUp = false;
        SetLaying(false);
    }
    public void DeactivateLayingLayer()
    {
        StopLaying();
    }
    public void SetLaying(bool laying)
    {
        if (_animator == null) return;

        FinishBlockHit();
        FinishHurt();
        IsLaying = laying;

        if (laying)
        {
            IsStandingUp = false;

            _animator.ResetTrigger(_standUpHash);
            _animator.SetBool(_layingBoolHash, true);
            SetCombatLayerWeight(0f);
            SetLayingLayerWeight(1f);

            if (_playerFSM != null)
            {
                _playerFSM.ChangeState(PlayerState.StandUp);
            }
        }
        else
        {
            _animator.SetBool(_layingBoolHash, false);

            if (!IsStandingUp)
            {
                _animator.ResetTrigger(_standUpHash);
                SetLayingLayerWeight(0f);

                if (_playerFSM != null && _playerFSM.CurrentState == PlayerState.StandUp)
                {
                    _playerFSM.ChangeState(PlayerState.Idle);
                }
                OnStandUpCompleted?.Invoke();
            }
        }
    }
    public void TriggerStandUp()
    {
        if (_animator == null) return;

        FinishBlockHit();
        FinishHurt();

        IsLaying = false;
        IsStandingUp = true;

        ResolveLayerIndices();
        SetCombatLayerWeight(0f);
        SetLayingLayerWeight(1f);

        if (_playerFSM != null)
        {
            _playerFSM.ChangeState(PlayerState.StandUp);
        }

        _animator.SetBool(_layingBoolHash, false);
        _animator.ResetTrigger(_standUpHash);
        _animator.SetTrigger(_standUpHash);
    }
    private void CompleteStandUp()
    {
        bool wasStandingUp = IsStandingUp;
        IsStandingUp = false;

        if (_animator != null)
        {
            _animator.ResetTrigger(_standUpHash);
        }

        if (!IsLaying)
        {
            SetLayingLayerWeight(0f);

            if (_playerFSM != null && _playerFSM.CurrentState == PlayerState.StandUp)
            {
                _playerFSM.ChangeState(PlayerState.Idle);
            }
            if (wasStandingUp)
            {
                OnStandUpCompleted?.Invoke();
            }
        }
    }
    private void Update()
    {
        TrackBlockHitProgress();
        TrackHurtProgress();
        SyncMovement();
        TrackDodgeProgress();
    }
    private void FixedUpdate()
    {
        if (_areIframesActive)
        {
            _remainingIframes--;
            if (_remainingIframes <= 0)
            {
                CancelIframes();
            }
        }
    }
    private void TrackHurtProgress()
    {
        if (!_isHurtPlaying) return;

        _hurtSafetyTimer += Time.deltaTime;
        if (_hurtSafetyTimer >= MaxHurtLockDuration)
        {
            FinishHurt();
        }
    }
    private void FinishHurt()
    {
        _isHurtPlaying = false;
        _hurtSafetyTimer = 0f;
    }
    private void TrackBlockHitProgress()
    {
        if (!_isBlockHitPlaying) return;

        _blockHitSafetyTimer += Time.deltaTime;
        if (_blockHitSafetyTimer >= MaxBlockHitLockDuration || _animator == null)
        {
            FinishBlockHit();
            return;
        }

        int layer = (_blockHitLayerIndex >= 0 && _blockHitLayerIndex < _animator.layerCount) ? _blockHitLayerIndex : 0;
        AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(layer);
        bool inTransition = _animator.IsInTransition(layer);

        if (stateInfo.IsName("BlockHit") || stateInfo.IsTag("BlockHit"))
        {
            _hasEnteredBlockHitState = true;

            if (!stateInfo.loop && stateInfo.normalizedTime >= 0.95f)
            {
                FinishBlockHit();
            }
        }
        else if (_hasEnteredBlockHitState && !inTransition)
        {
            FinishBlockHit();
        }
    }
    private void FinishBlockHit()
    {
        _isBlockHitPlaying = false;
        _hasEnteredBlockHitState = false;
        _blockHitSafetyTimer = 0f;
    }
    private void TrackDodgeProgress()
    {
        if (_playerFSM != null && _playerFSM.CurrentState == PlayerState.Dodging && _animator != null)
        {
            int combatLayer = (_combatLayerIndex >= 0 && _combatLayerIndex < _animator.layerCount) ? _combatLayerIndex : 0;
            AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(combatLayer);

            if (!stateInfo.IsName("Dodge"))
            {
                stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            }

            DodgeNormalizedTime = Mathf.Clamp01(stateInfo.normalizedTime);
        }
        else
        {
            DodgeNormalizedTime = 0f;
        }
    }
    private void SyncMovement()
    {
        if (_animator == null || _playerFSM == null) return;

        if (IsLaying || IsStandingUp)
        {
            SetLayingLayerWeight(1f);

            if (_playerFSM.CurrentState != PlayerState.StandUp &&
                _playerFSM.CurrentState != PlayerState.Knockout &&
                _playerFSM.CurrentState != PlayerState.Death)
            {
                _playerFSM.ChangeState(PlayerState.StandUp);
            }
        }
        else
        {
            SetLayingLayerWeight(0f);
        }

        bool inCombatAction = _playerFSM.CurrentState == PlayerState.Attacking ||
                              _playerFSM.CurrentState == PlayerState.Dodging ||
                              _playerFSM.CurrentState == PlayerState.Skill ||
                              _playerFSM.CurrentState == PlayerState.Blocking ||
                              _playerFSM.CurrentState == PlayerState.UsingItem ||
                              _playerFSM.CurrentState == PlayerState.Hurt ||
                              _playerFSM.CurrentState == PlayerState.Knockout ||
                              _playerFSM.CurrentState == PlayerState.Death ||
                              _isBlockHitPlaying ||
                              _isHurtPlaying;

        if (_combatLayerIndex > 0 && _combatLayerIndex < _animator.layerCount && _combatLayerIndex != _layingLayerIndex)
        {
            if (inCombatAction)
            {
                _animator.SetLayerWeight(_combatLayerIndex, 1f);
            }
            else
            {
                float currentWeight = _animator.GetLayerWeight(_combatLayerIndex);
                float nextWeight = Mathf.Lerp(currentWeight, 0f, Time.deltaTime * 15f);
                if (nextWeight < 0.01f) nextWeight = 0f;
                _animator.SetLayerWeight(_combatLayerIndex, nextWeight);
            }
        }

        if (_playerCombat != null)
        {
            HasWeapon = _playerCombat.IsWeaponActive;
        }

        float currentSpeed = 0f;
        if (!IsLaying && !IsStandingUp)
        {
            if (_playerFSM.CurrentState == PlayerState.Walking) currentSpeed = 0.5f;
            else if (_playerFSM.CurrentState == PlayerState.Running) currentSpeed = 1f;
        }

        bool isBlockingOrRecovering = _playerFSM.CurrentState == PlayerState.Blocking || _isBlockHitPlaying;

        _animator.SetFloat(_speedHash, currentSpeed, 0.1f, Time.deltaTime);
        _animator.SetBool(_isBlockingHash, isBlockingOrRecovering);
        _animator.SetBool(_hasWeaponHash, HasWeapon);

        bool isLockedOn = _playerCombat != null && _playerCombat.LockOnTarget != null;
        _animator.SetBool(_isLockedOnHash, isLockedOn);

        if (isLockedOn && _playerInputs != null && !IsLaying && !IsStandingUp)
        {
            Vector2 moveInput = _playerInputs.MoveInput;
            Vector3 inputDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
            float targetX = 0f;
            float targetY = 0f;

            if (inputDirection.magnitude >= 0.1f)
            {
                float targetAngle = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg;
                if (_mainCameraTransform != null)
                {
                    targetAngle += _mainCameraTransform.eulerAngles.y;
                }

                Vector3 worldMoveDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;

                targetX = Vector3.Dot(worldMoveDirection, _playerRoot.right) * (currentSpeed > 0 ? currentSpeed : 0.5f);
                targetY = Vector3.Dot(worldMoveDirection, _playerRoot.forward) * (currentSpeed > 0 ? currentSpeed : 0.5f);
            }

            _currentInputX = Mathf.Lerp(_currentInputX, targetX, Time.deltaTime * 10f);
            _currentInputY = Mathf.Lerp(_currentInputY, targetY, Time.deltaTime * 10f);

            _animator.SetFloat(_inputXHash, _currentInputX);
            _animator.SetFloat(_inputYHash, _currentInputY);
        }
        else
        {
            _currentInputX = Mathf.Lerp(_currentInputX, 0f, Time.deltaTime * 10f);
            _currentInputY = Mathf.Lerp(_currentInputY, 0f, Time.deltaTime * 10f);
            _animator.SetFloat(_inputXHash, _currentInputX);
            _animator.SetFloat(_inputYHash, _currentInputY);
        }
    }
    private void HandleFSMStateChanged(PlayerState newState)
    {
        if (_animator == null) return;

        if (newState != PlayerState.Blocking)
        {
            FinishBlockHit();
        }

        if (newState == PlayerState.Knockout || newState == PlayerState.Death)
        {
            IsLaying = false;
            IsStandingUp = false;
            _animator.SetBool(_layingBoolHash, false);
            _animator.ResetTrigger(_standUpHash);
            SetLayingLayerWeight(0f);
            SetCombatLayerWeight(1f);
        }

        if (newState == PlayerState.Dodging)
        {
            SetCombatLayerWeight(1f);
            _animator.SetTrigger(_dodgeHash);
        }
        else if (newState == PlayerState.Skill)
        {
            SetCombatLayerWeight(1f);
            if (_playerCombat != null)
            {
                _animator.SetInteger(_skillIDHash, _playerCombat.CurrentSkillID);
            }
            _animator.SetTrigger(_skillTriggerHash);
        }
        else if (newState == PlayerState.UsingItem)
        {
            SetCombatLayerWeight(1f);
            _animator.SetTrigger(_itemHash);
        }
        else if (newState == PlayerState.Knockout)
        {
            SetCombatLayerWeight(1f);
            _animator.SetTrigger(_knockoutHash);
        }
        else if (newState == PlayerState.Death)
        {
            SetCombatLayerWeight(1f);
            _animator.SetTrigger(_permadeathHash);
        }
    }
    private bool CanAttack()
    {
        if (_playerFSM == null) return false;
        if (IsLaying || IsStandingUp) return false;
        if (_playerInputs != null && !_playerInputs.AreGameplayInputsEnabled) return false;
        if (_playerCombat != null && !_playerCombat.IsWeaponActive) return false;

        return _playerFSM.CurrentState == PlayerState.Idle ||
               _playerFSM.CurrentState == PlayerState.Walking ||
               _playerFSM.CurrentState == PlayerState.Running;
    }
    private void TriggerLightAttack()
    {
        if (CanAttack() && _playerCombat != null && _animator != null)
        {
            if (_playerCombat.TryLightAttack())
            {
                SetCombatLayerWeight(1f);
                _animator.SetTrigger(_lightAttackHash);
            }
        }
    }
    private void TriggerHeavyAttack()
    {
        if (CanAttack() && _playerCombat != null && _animator != null)
        {
            if (_playerCombat.TryHeavyAttack())
            {
                SetCombatLayerWeight(1f);
                _animator.SetTrigger(_heavyAttackHash);
            }
        }
    }
    private void TriggerHurt()
    {
        FinishBlockHit();
        IsLaying = false;
        if (_animator != null) _animator.SetBool(_layingBoolHash, false);
        CompleteStandUp();

        _isHurtPlaying = true;
        _hurtSafetyTimer = 0f;
        SetCombatLayerWeight(1f);

        if (_animator != null)
        {
            _animator.ResetTrigger(_hurtHash);
            _animator.SetTrigger(_hurtHash);
        }
    }
    private void TriggerBlockHit()
    {
        if (_animator == null) return;

        _blockHitLayerIndex = (_combatLayerIndex >= 0 && _combatLayerIndex < _animator.layerCount) ? _combatLayerIndex : 0;
        _isBlockHitPlaying = true;
        _hasEnteredBlockHitState = false;
        _blockHitSafetyTimer = 0f;

        SetCombatLayerWeight(1f);
        _animator.ResetTrigger(_blockHitHash);
        _animator.SetTrigger(_blockHitHash);
    }
    public void AE_ApplyItemEffect()
    {
        if (_inventoryManager != null)
        {
            _inventoryManager.ApplyConsumableEffect();
        }
    }
    public void AE_ExecuteSkill()
    {
        if (_playerCombat != null) _playerCombat.ExecuteSkill();
    }
    public void AE_ActivateLightHitbox()
    {
        if (_playerCombat != null) _playerCombat.ActivateWeapon(false);
    }
    public void AE_ActivateHeavyHitbox()
    {
        if (_playerCombat != null) _playerCombat.ActivateWeapon(true);
    }
    public void AE_DeactivateHitbox()
    {
        if (_playerCombat != null) _playerCombat.DeactivateWeapon();
    }
    public void AE_StartIframes()
    {
        if (_playerStats != null && !_areIframesActive)
        {
            _remainingIframes = _playerStats.Iframes;
            _areIframesActive = true;
            if (_playerHealth != null) _playerHealth.IsDodgeInvulnerable = true;
        }
    }
    private void CancelIframes()
    {
        if (!_areIframesActive) return;

        _areIframesActive = false;
        if (_playerHealth != null) _playerHealth.IsDodgeInvulnerable = false;
    }
    public void OnAnimationStateExit(ActionStateType type)
    {
        if (_animator == null || _playerFSM == null) return;

        switch (type)
        {
            case ActionStateType.Attack:
                if (_playerCombat != null) _playerCombat.EndAttackRoutine();
                break;
            case ActionStateType.StandUp:
                CompleteStandUp();
                break;
            case ActionStateType.Dodge:
                CancelIframes();
                goto case ActionStateType.UsingItem;
            case ActionStateType.Hurt:
                FinishHurt();
                goto case ActionStateType.UsingItem;
            case ActionStateType.Skill:
            case ActionStateType.UsingItem:
                _animator.ResetTrigger(_dodgeHash);
                _animator.ResetTrigger(_skillTriggerHash);
                _animator.ResetTrigger(_hurtHash);
                _animator.ResetTrigger(_itemHash);

                if (_playerFSM.CurrentState == PlayerState.Dodging ||
                    _playerFSM.CurrentState == PlayerState.Skill ||
                    _playerFSM.CurrentState == PlayerState.Hurt ||
                    _playerFSM.CurrentState == PlayerState.UsingItem)
                {
                    _playerFSM.ChangeState(PlayerState.Idle);
                }
                break;
            case ActionStateType.Knockout:
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.ProcessPlayerDeath();
                }
                break;
            case ActionStateType.Death:
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.ProcessPlayerDeath();
                }
                break;
        }
    }
    public void RespawnPlayer()
    {
        FinishBlockHit();
        FinishHurt();
        IsLaying = false;
        if (_animator != null) _animator.SetBool(_layingBoolHash, false);
        CompleteStandUp();
        SetLayingLayerWeight(0f);
        SetCombatLayerWeight(0f);

        if (_animator != null)
        {
            _animator.ResetTrigger(_knockoutHash);
            _animator.ResetTrigger(_permadeathHash);
            _animator.ResetTrigger(_hurtHash);
            _animator.ResetTrigger(_itemHash);
            _animator.ResetTrigger(_blockHitHash);
            _animator.ResetTrigger(_standUpHash);
        }

        if (_playerFSM != null && (_playerFSM.CurrentState == PlayerState.Knockout || _playerFSM.CurrentState == PlayerState.Death))
        {
            _playerFSM.ChangeState(PlayerState.Idle);
        }
    }
}