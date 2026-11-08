using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInputs))]
[RequireComponent(typeof(PlayerFSM))]
[RequireComponent(typeof(PlayerStats))]
[RequireComponent(typeof(PlayerMovement))]
public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private WeaponHitbox _weaponHitbox;
    [SerializeField] private float _baseLightDamage = 21f;
    [SerializeField] private float _baseHeavyDamage = 46.2f;
    [SerializeField] private float _lockOnRadius = 15f;
    [SerializeField] private float _lightAttackImpulse = 3f;
    [SerializeField] private float _heavyAttackImpulse = 4.5f;
    [SerializeField] private LayerMask _enemyLayer;

    private Transform _lockOnTarget;
    private PlayerInputs _playerInputs;
    private PlayerFSM _playerFSM;
    private PlayerStats _playerStats;
    private PlayerMovement _playerMovement;
    private Camera _mainCamera;
    readonly private Collider[] _hitEnemiesBuffer = new Collider[15];

    private bool _isWeaponActive = false;
    public Transform LockOnTarget => _lockOnTarget;
    private void Awake()
    {
        _playerInputs = GetComponent<PlayerInputs>();
        _playerFSM = GetComponent<PlayerFSM>();
        _playerStats = GetComponent<PlayerStats>();
        _playerMovement = GetComponent<PlayerMovement>();
        _mainCamera = Camera.main;
    }
    private void OnEnable()
    {
        _playerInputs.OnLockOn += ToggleLockOn;
    }
    private void OnDisable()
    {
        _playerInputs.OnLockOn -= ToggleLockOn;
    }
    private void Update()
    {
        if (UIManager.Instance.CurrentState == GameState.Gameplay)
        {
            CheckLockTargetValid();
            CombatRotation();
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

        if (_playerFSM.CurrentState == PlayerState.Attacking)
        {
            _playerFSM.ChangeState(PlayerState.Idle);
        }
    }
    public void ActivateWeapon(bool isHeavyAttack)
    {
        _isWeaponActive = true;

        float baseDamage = isHeavyAttack ? _baseHeavyDamage : _baseLightDamage;
        float vesaniaMultiplier = 1f + _playerStats.VesaniaDamageBonus;
        float grossDamage = (baseDamage * _playerStats.DamageMultiplier) * vesaniaMultiplier;

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
            if (_playerFSM.CurrentState == PlayerState.Skill || _playerFSM.CurrentState == PlayerState.Blocking)
            {
                SnapToMouse();
            }
        }
    }
}