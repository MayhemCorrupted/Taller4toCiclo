using System;
using UnityEngine;

public enum EnemyState { Patroling, Chasing, Attacking, Hurt }

[RequireComponent(typeof(Health))]
public class EntityBrain : MonoBehaviour
{
    [SerializeField] private EntityData _data;
    [Header("Sub-components")]
    [SerializeField] private Health _health;
    [SerializeField] private EntityCombat _combat;
    [SerializeField] private EntityMovement _movement;

    public event Action<EnemyState> OnStateChanged;
    private EnemyState _currentState = EnemyState.Patroling;
    public EnemyState CurrentState => _currentState;

    private void Awake()
    {
        if (_health == null) _health = GetComponent<Health>();
        if (_combat == null) _combat = GetComponent<EntityCombat>();
        if (_movement == null) _movement = GetComponent<EntityMovement>();

        if (_data != null)
        {
            if (_health != null) _health.SetHealthValues(_data.health, _data.defense);

            if (_combat != null)
            {
                _combat.SetData(_data);
            }
            if (_movement != null) _movement.SetSpeed(_data.speed);
        }
        else
        {
            Debug.LogWarning($"EntityBrain en {gameObject.name}: No se ha asignado el Asset EntityData.");
        }
    }

    private void Update()
    {
        if (_currentState == EnemyState.Hurt) return;

        if (_combat == null)
        {
            ChangeState(EnemyState.Patroling);
            return;
        }

        if (_combat.IsPerformingAttack)
        {
            return;
        }

        if (_combat.IsInAttackRange)
            ChangeState(EnemyState.Attacking);
        else if (_combat.CanSeeTarget)
            ChangeState(EnemyState.Chasing);
        else
            ChangeState(EnemyState.Patroling);
    }
    public void ChangeState(EnemyState state)
    {
        if (_currentState == state) return;
        _currentState = state;
        OnStateChanged?.Invoke(state);
    }
}