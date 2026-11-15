using System;
using UnityEngine;
using UnityEngine.Events;
public enum EnemyState { Patroling, Chasing, Attacking, Hurt, Agonizing, Death }
[RequireComponent(typeof(Health))]
public class EntityBrain : MonoBehaviour
{
    [SerializeField] private EntityData _data;
    [Header("Sub-components")]
    [SerializeField] private Health _health;
    [SerializeField] private EntityCombat _combat;
    [SerializeField] private EntityMovement _movement;
    [Header("Configuración de Chequeo Pasivo de Agonize")]
    [Tooltip("Cada cuántos segundos se evalúa la probabilidad de Agonize sin necesidad de recibir golpes.")]
    [Min(0.1f)][SerializeField] private float _passiveAgonizeCheckInterval = 1.0f;
    [Header("Eventos de Estado (Inspector)")]
    [Tooltip("Se dispara cuando la animación de muerte del enemigo termina de reproducirse.")]
    public UnityEvent OnDeathAnimationFinishedEvent;
    public event Action<EnemyState> OnStateChanged;
    public event Action OnHurtTriggered;
    public event Action OnDeathAnimationCompleted;
    private EnemyState _currentState = EnemyState.Patroling;
    private float _agonizeCooldownTimer = 0f;
    private float _passiveAgonizeTimer = 0f;
    private bool _deathAnimationNotified = false;
    public EnemyState CurrentState => _currentState;
    public EntityData Data => _data;
    public bool CanTriggerAgonize =>
        _data != null &&
        _data.canAgonize &&
        _agonizeCooldownTimer <= 0f &&
        _currentState != EnemyState.Death &&
        _currentState != EnemyState.Agonizing &&
        (_health == null || _health.CurrentHealth > 0f);
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
                _movement.SetSpeed(_data.speed);
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

        if (_combat.IsInAttackRange)
            ChangeState(EnemyState.Attacking);
        else if (_combat.CanSeeTarget)
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
        ChangeState(EnemyState.Agonizing);
    }
    public void TriggerHurt()
    {
        if (_currentState == EnemyState.Death || _currentState == EnemyState.Agonizing) return;

        if (_currentState != EnemyState.Hurt)
            ChangeState(EnemyState.Hurt);

        OnHurtTriggered?.Invoke();
    }
    public void EndHurt()
    {
        if (_currentState != EnemyState.Hurt) return;

        if (_combat != null && _combat.IsInAttackRange)
            ChangeState(EnemyState.Attacking);
        else if (_combat != null && _combat.CanSeeTarget)
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

        if (_combat != null && _combat.IsInAttackRange)
            ChangeState(EnemyState.Attacking);
        else if (_combat != null && _combat.CanSeeTarget)
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
        ChangeState(EnemyState.Death);
    }
    public void ChangeState(EnemyState state)
    {
        if (_currentState == state) return;
        if (_currentState == EnemyState.Death) return;

        _currentState = state;
        OnStateChanged?.Invoke(state);
    }
}