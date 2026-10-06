using UnityEngine;
using UnityEngine.InputSystem;
[RequireComponent(typeof(PlayerInputs))]
[RequireComponent(typeof(PlayerFSM))]
[RequireComponent(typeof(PlayerStats))]
public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private WeaponHitbox _weaponHitbox;
    [SerializeField] private float _baseLightDamage = 21;
    [SerializeField] private float _baseHeavyDamage = 46.2f;
    [SerializeField] private float _lockOnRadius = 15;
    [SerializeField] private LayerMask _enemyLayer;

    private Transform _lockOnTarget;
    private PlayerInputs _playerInputs;
    private PlayerFSM _playerFSM;
    private PlayerStats _playerStats;
    private Camera _mainCamera;
    private Collider[] _hitEnemiesBuffer = new Collider[15];
    private void Awake()
    {
        _playerInputs = GetComponent<PlayerInputs>();
        _playerFSM = GetComponent<PlayerFSM>();
        _playerStats = GetComponent<PlayerStats>();
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
        CheckLockDistance();
        CombatRotation();
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
    private void CheckLockDistance()
    {
        if (_lockOnTarget == null) return;
        if (Vector3.Distance(transform.position, _lockOnTarget.position) > _lockOnRadius)
        {
            _lockOnTarget = null;
        }
    }
    private void CombatRotation()
    {
        if (_playerFSM.CurrentState != PlayerState.Attacking &&
            _playerFSM.CurrentState != PlayerState.Skill &&
            _playerFSM.CurrentState != PlayerState.Blocking)
        {
            return;
        }
        if (_lockOnTarget != null)
        {
            Vector3 directionToTarget = _lockOnTarget.position - transform.position;
            directionToTarget.y = 0;
            transform.rotation = Quaternion.LookRotation(directionToTarget);
        }
        else if (_playerInputs.CurrentDeviceMode == "PC_KeyboardMouse" && _mainCamera != null)
        {
            Ray ray = _mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            Plane groundPlane = new(Vector3.up, transform.position);
            if (groundPlane.Raycast (ray, out float rayDistance))
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
    public void ActivateWeapon(bool isHeavyAttack)
    {
        float baseDamage = isHeavyAttack ? _baseHeavyDamage : _baseLightDamage;
        float grossDamage = (baseDamage * _playerStats.DamageMultiplier) + _playerStats.VesaniaDamageBonus;
        _weaponHitbox.SetDamage(grossDamage);
        _weaponHitbox.EnableHitbox();
    }
    public void DeactivateWeapon()
    {
        _weaponHitbox.DisableHitbox();
    }
}
