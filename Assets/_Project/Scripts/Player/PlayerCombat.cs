using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System;
[RequireComponent(typeof(PlayerInputs))]
[RequireComponent(typeof(PlayerFSM))]
[RequireComponent(typeof(PlayerStats))]
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerStamina))]
[RequireComponent(typeof(PlayerHealth))]
public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private WeaponHitbox _weaponHitbox;
    [SerializeField] private float _baseWeaponDamage = 20f;
    [SerializeField] private float _lockOnRadius = 15f;
    [SerializeField] private float _lightAttackImpulse = 3f;
    [SerializeField] private float _heavyAttackImpulse = 4.5f;
    [SerializeField] private LayerMask _enemyLayer;

    [Header("Skills Settings")]
    [SerializeField] private GameObject _rangedProjectilePrefab;
    [SerializeField] private Transform _projectileSpawnPoint;
    [SerializeField] private float _areaSkillRadius = 4f;
    [SerializeField] private float _distanceSelfDamage = 2.5f;

    [Header("Burst Settings")]
    [SerializeField] private int _burstCount = 3;
    [SerializeField] private float _burstDelay = 0.15f;
    [SerializeField] private float _burstSpreadAngle = 15f;
    [Header("Skills Cooldowns")]
    [SerializeField] private float _distanceSkillCooldownDuration = 3f;
    [SerializeField] private float _areaSkillCooldownDuration = 5f;
    private float _distanceSkillTimer = 0f;
    private float _areaSkillTimer = 0f;
    private Transform _lockOnTarget;
    private PlayerInputs _playerInputs;
    private PlayerFSM _playerFSM;
    private PlayerStats _playerStats;
    private PlayerMovement _playerMovement;
    private PlayerStamina _playerStamina;
    private PlayerHealth _playerHealth;
    private Camera _mainCamera;
    private readonly Collider[] _hitEnemiesBuffer = new Collider[15];
    private bool _isWeaponActive = false;
    public Transform LockOnTarget => _lockOnTarget;
    public int CurrentSkillID { get; private set; } = 0;
    public event Action<int> OnSkillSwapped;
    private void Awake()
    {
        _playerInputs = GetComponent<PlayerInputs>();
        _playerFSM = GetComponent<PlayerFSM>();
        _playerStats = GetComponent<PlayerStats>();
        _playerMovement = GetComponent<PlayerMovement>();
        _playerStamina = GetComponent<PlayerStamina>();
        _playerHealth = GetComponent<PlayerHealth>();
        _mainCamera = Camera.main;
    }
    private void OnEnable()
    {
        _playerInputs.OnLockOn += ToggleLockOn;
        _playerInputs.OnSwap += SwapSkill;
    }
    private void OnDisable()
    {
        _playerInputs.OnLockOn -= ToggleLockOn;
        _playerInputs.OnSwap -= SwapSkill;
    }
    private void Update()
    {
        UpdateCooldowns();

        if (UIManager.Instance.CurrentState == GameState.Gameplay)
        {
            CheckLockTargetValid();
            CombatRotation();
        }
    }
    private void UpdateCooldowns()
    {
        if (_distanceSkillTimer > 0f) _distanceSkillTimer -= Time.deltaTime;
        if (_areaSkillTimer > 0f) _areaSkillTimer -= Time.deltaTime;
    }
    public float GetSkillCooldownPercentage(int skillID)
    {
        if (skillID == 0) return _distanceSkillCooldownDuration > 0f ? Mathf.Clamp01(_distanceSkillTimer / _distanceSkillCooldownDuration) : 0f;
        return _areaSkillCooldownDuration > 0f ? Mathf.Clamp01(_areaSkillTimer / _areaSkillCooldownDuration) : 0f;
    }
    public void SwapSkill()
    {
        CurrentSkillID = CurrentSkillID == 0 ? 1 : 0;

        OnSkillSwapped?.Invoke(CurrentSkillID);
    }
    public bool IsSkillOnCooldown(int skillID)
    {
        return skillID == 0 ? _distanceSkillTimer > 0f : _areaSkillTimer > 0f;
    }

    public bool TrySkillAttack()
    {
        if (UIManager.Instance.CurrentState != GameState.Gameplay) return false;

        if (_playerFSM.CurrentState != PlayerState.Idle &&
            _playerFSM.CurrentState != PlayerState.Walking &&
            _playerFSM.CurrentState != PlayerState.Running) return false;

        if (IsSkillOnCooldown(CurrentSkillID)) return false;

        if (CurrentSkillID == 0)
        {
            if (!_playerStamina.HasEnoughStamina(15f) || _playerHealth.CurrentHealth <= 20f) return false;
        }
        else
        {
            if (!_playerStamina.HasEnoughStamina(60f)) return false;
        }

        if (CurrentSkillID == 0)
        {
            _playerStamina.UseStamina(15f);
            _playerHealth.ConsumeHealth(_distanceSelfDamage);
            _distanceSkillTimer = _distanceSkillCooldownDuration;
        }
        else
        {
            _playerStamina.UseStamina(60f);
            _areaSkillTimer = _areaSkillCooldownDuration;
        }

        if (_lockOnTarget == null) SnapToMouse();

        _playerFSM.ChangeState(PlayerState.Skill);
        return true;
    }

    private float CalculateGrossDamage(float attackMultiplier)
    {
        float sumBonus = _playerStats.FuerzaMult + _playerStats.VesaniaDamageBonus + _playerStats.ConsumableDamageBonus;
        return _baseWeaponDamage * attackMultiplier * (1f + sumBonus);
    }

    public void ExecuteSkill()
    {
        if (CurrentSkillID == 0)
        {
            float damage = CalculateGrossDamage(2.5f);
            StartCoroutine(ShootBurstRoutine(damage));
        }
        else if (CurrentSkillID == 1)
        {
            float damage = CalculateGrossDamage(3.0f);
            int hitCount = Physics.OverlapSphereNonAlloc(transform.position, _areaSkillRadius, _hitEnemiesBuffer, _enemyLayer);

            for (int i = 0; i < hitCount; i++)
            {
                if (_hitEnemiesBuffer[i].TryGetComponent(out Health enemyHealth))
                {
                    enemyHealth.TakeDamage(damage);
                }
            }
        }
    }

    private IEnumerator ShootBurstRoutine(float damage)
    {
        for (int i = 0; i < _burstCount; i++)
        {
            if (_rangedProjectilePrefab != null && _projectileSpawnPoint != null)
            {
                float angle = 0f;
                if (i > 0)
                {
                    float directionMultiplier = (i % 2 != 0) ? 1f : -1f;
                    angle = Mathf.Ceil(i / 2f) * _burstSpreadAngle * directionMultiplier;
                }

                Quaternion spreadRotation = transform.rotation * Quaternion.Euler(0f, angle, 0f);
                Vector3 shootDirection = spreadRotation * Vector3.forward;

                GameObject proj = Instantiate(_rangedProjectilePrefab, _projectileSpawnPoint.position, spreadRotation);
                if (proj.TryGetComponent(out PlayerProjectile projectile))
                {
                    projectile.Init(shootDirection, damage);
                }
            }
            yield return new WaitForSeconds(_burstDelay);
        }
    }

    public bool TryLightAttack()
    {
        if (UIManager.Instance.CurrentState != GameState.Gameplay) return false;

        if (_playerFSM.CurrentState != PlayerState.Idle &&
            _playerFSM.CurrentState != PlayerState.Walking &&
            _playerFSM.CurrentState != PlayerState.Running) return false;

        if (_lockOnTarget == null) SnapToMouse();

        _playerFSM.ChangeState(PlayerState.Attacking);
        return true;
    }

    public bool TryHeavyAttack()
    {
        if (UIManager.Instance.CurrentState != GameState.Gameplay) return false;

        if (_playerFSM.CurrentState != PlayerState.Idle &&
            _playerFSM.CurrentState != PlayerState.Walking &&
            _playerFSM.CurrentState != PlayerState.Running) return false;

        if (_lockOnTarget == null) SnapToMouse();

        _playerFSM.ChangeState(PlayerState.Attacking);
        return true;
    }
    public void EndAttackRoutine()
    {
        DeactivateWeapon();

        if (_playerFSM.CurrentState == PlayerState.Attacking || _playerFSM.CurrentState == PlayerState.Skill)
        {
            _playerFSM.ChangeState(PlayerState.Idle);
        }
    }
    public void ActivateWeapon(bool isHeavyAttack)
    {
        _isWeaponActive = true;

        float attackMultiplier = isHeavyAttack ? 2.2f : 1.0f;
        float grossDamage = CalculateGrossDamage(attackMultiplier);

        if (_weaponHitbox != null)
        {
            _weaponHitbox.SetDamage(grossDamage);
            _weaponHitbox.EnableHitbox();
        }

        float impulseForce = isHeavyAttack ? _heavyAttackImpulse : _lightAttackImpulse;
        if (_playerMovement != null)
        {
            _playerMovement.ApplyCombatImpulse(impulseForce);
        }
    }
    public void DeactivateWeapon()
    {
        _isWeaponActive = false;
        if (_weaponHitbox != null) _weaponHitbox.DisableHitbox();
    }
    private void ToggleLockOn()
    {
        if (_lockOnTarget != null)
        {
            _lockOnTarget = null;
            return;
        }

        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, _lockOnRadius, _hitEnemiesBuffer, _enemyLayer);
        float closestDistance = Mathf.Infinity;
        Transform closestEnemy = null;
        for (int i = 0; i < hitCount; i++)
        {
            Collider enemy = _hitEnemiesBuffer[i];
            float distance = Vector3.Distance(transform.position, enemy.transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = enemy.transform;
            }
        }
        _lockOnTarget = closestEnemy;
    }
    private void CheckLockTargetValid()
    {
        if (_lockOnTarget == null) return;

        Health targetHealth = _lockOnTarget.GetComponent<Health>();
        if (targetHealth != null && targetHealth.CurrentHealth <= 0)
        {
            _lockOnTarget = null;
            return;
        }

        if (Vector3.Distance(transform.position, _lockOnTarget.position) > _lockOnRadius)
        {
            _lockOnTarget = null;
        }
    }
    private void SnapToMouse()
    {
        if (_playerInputs.CurrentDeviceMode == "PC_KeyboardMouse" && _mainCamera != null)
        {
            Ray ray = _mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            Plane groundPlane = new(Vector3.up, transform.position);
            if (groundPlane.Raycast(ray, out float rayDistance))
            {
                Vector3 point = ray.GetPoint(rayDistance);
                Vector3 directionToMouse = point - transform.position;
                directionToMouse.y = 0;
                if (directionToMouse.sqrMagnitude > 0.1f)
                {
                    transform.rotation = Quaternion.LookRotation(directionToMouse);
                }
            }
        }
    }
    private void CombatRotation()
    {
        if (_playerFSM.CurrentState == PlayerState.Dodging) return;
        if (_lockOnTarget != null)
        {
            Vector3 directionToTarget = _lockOnTarget.position - transform.position;
            directionToTarget.y = 0;

            if (directionToTarget.sqrMagnitude > 0.1f)
            {
                transform.rotation = Quaternion.LookRotation(directionToTarget);
            }
        }
        else
        {
            if (_playerFSM.CurrentState == PlayerState.Skill || _playerFSM.CurrentState == PlayerState.Blocking || _playerFSM.CurrentState == PlayerState.Attacking)
            {
                SnapToMouse();
            }
        }
    }
}