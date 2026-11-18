using System;
using UnityEngine;
using UnityEngine.Events;
public enum EnemyState { Patroling, Chasing, Attacking, Hurt, Agonizing, Death }
public enum DetectedRangeBand { PersonalSpace, Short, Medium, Long, OutOfRange }
[RequireComponent(typeof(Health))]
public class EntityBrain : MonoBehaviour
{
    [SerializeField] private EntityData _data;

    [Header("Sub-componentes")]
    [SerializeField] private Health _health;
    [SerializeField] private EntityCombat _combat;
    [SerializeField] private EntityMovement _movement;

    [Header("Detección Aggro y Pérdida de Objetivo")]
    [Min(1.0f)][SerializeField] private float _aggroRadius = 10.0f;
    [Min(0f)][SerializeField] private float _lossDetectionBuffer = 2.0f;
    [Min(0.1f)][SerializeField] private float _lossDetectionDuration = 3.0f;

    [Header("Bandas de Ataque y Espacio Personal")]
    [Min(0.2f)][SerializeField] private float _personalSpaceRadius = 1.6f;
    [Min(0.5f)][SerializeField] private float _shortRangeRadius = 4.5f;
    [Min(1.0f)][SerializeField] private float _mediumRangeRadius = 8.5f;
    [Min(1.5f)][SerializeField] private float _longRangeRadius = 13.0f;

    [Header("Depuración Visual (Solo Editor)")]
    [SerializeField] private bool _showRangeGizmos = true;

    [Header("Agonía Pasiva")]
    [Min(0.1f)][SerializeField] private float _passiveAgonizeCheckInterval = 1.0f;

    [Header("Eventos de Estado")]
    public UnityEvent OnDeathAnimationFinishedEvent;
    public event Action<EnemyState> OnStateChanged;
    public event Action OnHurtTriggered;
    public event Action OnDeathAnimationCompleted;
    private EnemyState _currentState = EnemyState.Patroling;
    private float _agonizeCooldownTimer = 0f;
    private float _passiveAgonizeTimer = 0f;
    private bool _deathAnimationNotified = false;
    private int _activeHurtAnimationCount = 0;
    public EnemyState CurrentState => _currentState;
    public EntityData Data => _data;
    public float AggroRadius => _aggroRadius;
    public float LossDetectionBuffer => _lossDetectionBuffer;
    public float MaxDetectionRadius => _aggroRadius;
    public float LossDetectionRadius => _aggroRadius + _lossDetectionBuffer;
    public float LossDetectionDuration => _lossDetectionDuration;
    public float PersonalSpaceRadius => _personalSpaceRadius;
    public float ShortRangeRadius => _shortRangeRadius;
    public float MediumRangeRadius => _mediumRangeRadius;
    public float LongRangeRadius => _longRangeRadius;
    public bool IsHurtAnimationActive => _activeHurtAnimationCount > 0;
    public bool CanTriggerAgonize =>
        _data != null &&
        _data.canAgonize &&
        _agonizeCooldownTimer <= 0f &&
        _currentState != EnemyState.Death &&
        _currentState != EnemyState.Agonizing &&
        (_health == null || _health.CurrentHealth > 0f);
    private void OnValidate()
    {
        _shortRangeRadius = Mathf.Max(_personalSpaceRadius + 0.3f, _shortRangeRadius);
        _mediumRangeRadius = Mathf.Max(_shortRangeRadius + 0.5f, _mediumRangeRadius);
        _longRangeRadius = Mathf.Max(_mediumRangeRadius + 0.5f, _longRangeRadius);
    }
    private void Awake()
    {
        if (_health == null) TryGetComponent(out _health);
        if (_combat == null) TryGetComponent(out _combat);
        if (_movement == null) TryGetComponent(out _movement);

        if (_data != null)
        {
            if (_health != null) _health.SetHealthValues(_data.health, _data.defense);
            if (_combat != null) _combat.SetData(_data);
            if (_movement != null)
            {
                _movement.SetData(_data);
                _movement.SetSpeed(_data.moveSpeed);
            }
        }
        else
        {
            Debug.LogWarning($"EntityBrain en {gameObject.name}: No se ha asignado el Asset EntityData.");
        }
    }
    private void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnVesaniaChanged += HandleVesaniaChanged;
    }
    private void OnEnable()
    {
        if (_health != null)
        {
            _health.OnDeath += HandleDeath;
            _health.OnTakeDamage += HandleDamageTaken;
        }
    }
    private void OnDisable()
    {
        if (_health != null)
        {
            _health.OnDeath -= HandleDeath;
            _health.OnTakeDamage -= HandleDamageTaken;
        }
    }
    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnVesaniaChanged -= HandleVesaniaChanged;
    }
    public DetectedRangeBand EvaluateDistanceBand(float distance)
    {
        if (distance <= _personalSpaceRadius) return DetectedRangeBand.PersonalSpace;
        if (distance <= _shortRangeRadius) return DetectedRangeBand.Short;
        if (distance <= _mediumRangeRadius) return DetectedRangeBand.Medium;
        if (distance <= _longRangeRadius) return DetectedRangeBand.Long;
        return DetectedRangeBand.OutOfRange;
    }
    public float GetOptimalRadiusForCategory(AttackRangeCategory category)
    {
        return category switch
        {
            AttackRangeCategory.Close => Mathf.Lerp(_personalSpaceRadius, _shortRangeRadius, 0.5f),
            AttackRangeCategory.Medium => Mathf.Lerp(_shortRangeRadius, _mediumRangeRadius, 0.5f),
            AttackRangeCategory.Far => Mathf.Lerp(_mediumRangeRadius, _longRangeRadius, 0.5f),
            _ => Mathf.Lerp(_personalSpaceRadius, _shortRangeRadius, 0.5f)
        };
    }
    private void HandleVesaniaChanged(int newVesaniaLevel)
    {
        if (_data == null) return;
        _data.GetAgonizeParameters(newVesaniaLevel, out _, out float newCooldown);
        if (_agonizeCooldownTimer > newCooldown)
            _agonizeCooldownTimer = newCooldown;
    }
    private void GetCurrentVesaniaAgonizeParams(out float probability, out float cooldown)
    {
        int currentVesania = GameManager.Instance != null ? GameManager.Instance.CurrentVesania : 1;
        if (_data != null)
        {
            _data.GetAgonizeParameters(currentVesania, out probability, out cooldown);
        }
        else
        {
            probability = 0f;
            cooldown = 0f;
        }
    }
    private void Update()
    {
        if (_currentState == EnemyState.Death) return;

        if (_currentState != EnemyState.Agonizing && _agonizeCooldownTimer > 0f)
        {
            _agonizeCooldownTimer = Mathf.Max(0f, _agonizeCooldownTimer - Time.deltaTime);
        }

        if (CanTriggerAgonize)
        {
            _passiveAgonizeTimer += Time.deltaTime;
            if (_passiveAgonizeTimer >= _passiveAgonizeCheckInterval)
            {
                _passiveAgonizeTimer = 0f;
                if (TryRollAgonize()) return;
            }
        }
        else
        {
            _passiveAgonizeTimer = 0f;
        }

        if (_currentState == EnemyState.Hurt || _currentState == EnemyState.Agonizing)
            return;

        if (_combat == null)
        {
            ChangeState(EnemyState.Patroling);
            return;
        }

        if (_combat.IsPerformingAttack)
            return;

        if (_combat.CanAttackFromCurrentPosition)
            ChangeState(EnemyState.Attacking);
        else if (_combat.HasTarget)
            ChangeState(EnemyState.Chasing);
        else
            ChangeState(EnemyState.Patroling);
    }
    private void HandleDamageTaken(float damage)
    {
        if (_health != null && _health.CurrentHealth <= 0f)
        {
            HandleDeath();
            return;
        }

        if (_combat != null && !_combat.HasTarget)
        {
            _combat.ForceAlertNearestTarget();
        }

        if (TryRollAgonize())
            return;

        if (_currentState == EnemyState.Agonizing || _currentState == EnemyState.Death)
            return;

        bool canStun = _data == null || _data.isStunnable;
        bool hasArmor = _combat != null && _combat.IsArmorActive;

        if (canStun && !hasArmor)
        {
            TriggerHurt();
        }
    }
    private bool TryRollAgonize()
    {
        if (!CanTriggerAgonize) return false;

        GetCurrentVesaniaAgonizeParams(out float probability, out _);
        if (UnityEngine.Random.value <= probability)
        {
            TriggerAgonize();
            return true;
        }

        return false;
    }
    public void TriggerAgonize()
    {
        if (!CanTriggerAgonize) return;
        _passiveAgonizeTimer = 0f;
        _activeHurtAnimationCount = 0;
        ChangeState(EnemyState.Agonizing);
    }
    public void TriggerHurt()
    {
        if (_currentState == EnemyState.Death || _currentState == EnemyState.Agonizing) return;

        if (_currentState != EnemyState.Hurt)
            ChangeState(EnemyState.Hurt);

        OnHurtTriggered?.Invoke();
    }
    public void NotifyHurtAnimationStarted()
    {
        if (_currentState == EnemyState.Death || _currentState == EnemyState.Agonizing) return;

        _activeHurtAnimationCount++;
        if (_currentState != EnemyState.Hurt)
            ChangeState(EnemyState.Hurt);
    }
    public void NotifyHurtAnimationEnded()
    {
        _activeHurtAnimationCount = Mathf.Max(0, _activeHurtAnimationCount - 1);

        if (_activeHurtAnimationCount == 0 && _currentState == EnemyState.Hurt)
        {
            if (_movement == null || !_movement.IsKnockbackActive)
                EndHurt();
        }
    }
    public void EndHurt()
    {
        if (_currentState != EnemyState.Hurt) return;

        _activeHurtAnimationCount = 0;

        if (_combat != null && _combat.CanAttackFromCurrentPosition)
            ChangeState(EnemyState.Attacking);
        else if (_combat != null && _combat.HasTarget)
            ChangeState(EnemyState.Chasing);
        else
            ChangeState(EnemyState.Patroling);
    }
    public void EndAgonize()
    {
        if (_currentState != EnemyState.Agonizing) return;

        GetCurrentVesaniaAgonizeParams(out _, out float cooldown);
        _agonizeCooldownTimer = cooldown;
        _passiveAgonizeTimer = 0f;

        if (_combat != null && _combat.CanAttackFromCurrentPosition)
            ChangeState(EnemyState.Attacking);
        else if (_combat != null && _combat.HasTarget)
            ChangeState(EnemyState.Chasing);
        else
            ChangeState(EnemyState.Patroling);
    }
    public void NotifyDeathAnimationFinished()
    {
        if (_deathAnimationNotified) return;
        _deathAnimationNotified = true;

        OnDeathAnimationCompleted?.Invoke();
        OnDeathAnimationFinishedEvent?.Invoke();
    }
    private void HandleDeath()
    {
        _activeHurtAnimationCount = 0;
        ChangeState(EnemyState.Death);
    }
    public void ChangeState(EnemyState state)
    {
        if (_currentState == state) return;
        if (_currentState == EnemyState.Death) return;

        _currentState = state;
        OnStateChanged?.Invoke(state);
    }
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!_showRangeGizmos) return;

        Vector3 center = transform.position + Vector3.up * 0.05f;

        DrawGizmoRing(center, _personalSpaceRadius, new Color(1f, 0.15f, 0.15f, 0.85f));
        DrawGizmoRing(center, _shortRangeRadius, new Color(1f, 0.55f, 0f, 0.85f));
        DrawGizmoRing(center, _mediumRangeRadius, new Color(1f, 0.9f, 0.1f, 0.85f));
        DrawGizmoRing(center, _longRangeRadius, new Color(0f, 0.85f, 1f, 0.85f));

        DrawGizmoRing(center + Vector3.up * 0.02f, _aggroRadius, new Color(0.2f, 1f, 0.35f, 0.9f));
        DrawDashedGizmoRing(center + Vector3.up * 0.04f, _aggroRadius + _lossDetectionBuffer, new Color(0.95f, 0.2f, 0.95f, 0.95f));

        if (Application.isPlaying && _combat != null && _combat.HasTarget)
        {
            Gizmos.color = _combat.IsLosingTarget ? Color.magenta : Color.green;
            Gizmos.DrawLine(center + Vector3.up * 0.5f, _combat.TargetLocation + Vector3.up * 0.5f);
        }
    }
    private void DrawGizmoRing(Vector3 center, float radius, Color color)
    {
        if (radius <= 0f) return;
        Gizmos.color = color;
        const int segments = 48;
        float step = 360f / segments;
        Vector3 prev = center + new Vector3(radius, 0f, 0f);

        for (int i = 1; i <= segments; i++)
        {
            float rad = i * step * Mathf.Deg2Rad;
            Vector3 next = center + new Vector3(Mathf.Cos(rad) * radius, 0f, Mathf.Sin(rad) * radius);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
    private void DrawDashedGizmoRing(Vector3 center, float radius, Color color)
    {
        if (radius <= 0f) return;
        Gizmos.color = color;
        const int segments = 48;
        float step = 360f / segments;

        for (int i = 0; i < segments; i += 2)
        {
            float radA = i * step * Mathf.Deg2Rad;
            float radB = (i + 1) * step * Mathf.Deg2Rad;
            Vector3 pA = center + new Vector3(Mathf.Cos(radA) * radius, 0f, Mathf.Sin(radA) * radius);
            Vector3 pB = center + new Vector3(Mathf.Cos(radB) * radius, 0f, Mathf.Sin(radB) * radius);
            Gizmos.DrawLine(pA, pB);
        }
    }
#endif
}