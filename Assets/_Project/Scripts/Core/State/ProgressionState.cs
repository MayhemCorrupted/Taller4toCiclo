using System;
using UnityEngine;
using UnityEngine.Events;
[Serializable] public struct ProgressionState
{
    public int EntitiesKilled;
    public int TotalEntities;
    public int InteractionsCompleted;
    public int TotalInteractions;
    public readonly float CombatPercentage => TotalEntities == 0 ? 0f : Mathf.Clamp01((float)EntitiesKilled / TotalEntities) * 100f;
    public readonly float InteractionPercentage => TotalInteractions == 0 ? 0f : Mathf.Clamp01((float)InteractionsCompleted / TotalInteractions) * 100f;
    public readonly float TotalProgressionPercentage => (CombatPercentage + InteractionPercentage) / 2f;
}
[Serializable] public struct StateProgressionStep
{
    [Tooltip("El valor exacto de 'currentState' para que este evento ocurra.")]
    public int requiredStateValue;
    public UnityEvent onStateTriggered;
}
[Serializable] public struct ProgressionMilestone
{
    [Tooltip("Porcentaje total de progresión (0 a 100) necesario para activar este evento.")]
    [Range(0f, 100f)] public float requiredPercentage;
    public UnityEvent onMilestoneReached;
    [HideInInspector] public bool hasBeenTriggered;
}