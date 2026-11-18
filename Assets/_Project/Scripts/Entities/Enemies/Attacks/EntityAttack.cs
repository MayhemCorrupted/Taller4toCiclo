using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
public enum ArmorType { None, Superarmor, Hyperarmor }
public abstract class EntityAttack : ScriptableObject
{
    [Header("Daño y Empuje")]
    [Min(0.0f)] public float damage = 10f;
    [Min(0.0f)] public float knockback = 5f;

    [Header("Telegrafiado, Indicador y Seguimiento")]
    public GameObject indicatorPrefab;
    [Min(0.0f)] public float telegraphDuration = 0.8f;
    [Range(0f, 1f)] public float trackTargetUntil = 0.85f;
    public LayerMask groundLayer = ~0;
    public LayerMask wallLayer;

    [Header("Efectos Visuales (VFX al Accionar)")]
    public GameObject impactVfxPrefab;
    public Vector3 vfxOffset = Vector3.zero;
    public Vector3 vfxScale = Vector3.one;
    [Min(0f)] public float vfxLifetime = 2.0f;
    public bool alignVfxWithAttacker = true;

    [Header("Sincronización con Animación")]
    public string animationTrigger;
    [Range(0f, 1f)] public float damageImpactPercent = 0.45f;
    [Min(0.05f)] public float fallbackAnimationDuration = 1.0f;
    [Min(0.0f)] public float activeMotionDuration = 0.4f;

    [Header("Armadura durante el Ataque")]
    public bool overrideBaseArmor = false;
    public ArmorType armorState = ArmorType.None;
    [Range(0f, 1f)] public float armorActivationPercent = 0.0f;
    [Range(0f, 1f)] public float hyperarmorDamageMultiplier = 0.5f;
    private readonly Dictionary<int, List<GameObject>> _activeIndicatorsByInstance = new();
    public virtual void Prepare(Transform origin, Transform target) { }
    public virtual void UpdatePreparation(Transform origin, Transform target, float normalizedTime) { }
    public abstract IEnumerator Execute(Transform origin, Transform target, MonoBehaviour runner);
    public virtual void CancelAttack()
    {
        ClearAllIndicators();
    }
    public virtual void CancelAttackFor(Transform origin)
    {
        ClearIndicatorsFor(origin);
    }
    public void SetIndicatorsImminentFor(Transform origin)
    {
        Transform root = GetRootTransform(origin);
        if (root == null) return;

        if (_activeIndicatorsByInstance.TryGetValue(root.GetInstanceID(), out List<GameObject> list))
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null && list[i].TryGetComponent(out EntityIndicator indicator))
                {
                    indicator.TriggerImminentAttackColor();
                }
            }
        }
    }
    protected GameObject SpawnImpactVfx(Vector3 basePosition, Quaternion directionRotation)
    {
        if (impactVfxPrefab == null) return null;

        Quaternion finalRot = alignVfxWithAttacker ? directionRotation : Quaternion.identity;
        Vector3 finalPos = basePosition + (finalRot * vfxOffset);

        GameObject vfx = Instantiate(impactVfxPrefab, finalPos, finalRot);
        vfx.transform.localScale = Vector3.Scale(vfx.transform.localScale, vfxScale);

        if (vfxLifetime > 0f)
            Destroy(vfx, vfxLifetime);

        return vfx;
    }
    protected GameObject SpawnIndicatorFor(Transform origin, Vector3 position, Quaternion rotation, Vector3 scale, float duration)
    {
        if (indicatorPrefab == null || origin == null) return null;

        GameObject ind = Instantiate(indicatorPrefab, position, rotation);
        ind.transform.localScale = scale;

        if (ind.TryGetComponent(out EntityIndicator indicatorComp))
        {
            origin.TryGetComponent(out Health entityHealth);
            indicatorComp.Init(entityHealth);
            indicatorComp.AnimateIndicator(Mathf.Max(0.05f, duration));
        }

        int key = origin.GetInstanceID();
        if (!_activeIndicatorsByInstance.TryGetValue(key, out List<GameObject> list))
        {
            list = new List<GameObject>();
            _activeIndicatorsByInstance[key] = list;
        }
        list.Add(ind);
        return ind;
    }
    protected List<GameObject> GetIndicatorsFor(Transform origin)
    {
        if (origin == null) return null;
        _activeIndicatorsByInstance.TryGetValue(origin.GetInstanceID(), out List<GameObject> list);
        return list;
    }
    protected void ClearIndicatorsFor(Transform origin)
    {
        if (origin == null) return;
        int key = origin.GetInstanceID();
        if (_activeIndicatorsByInstance.TryGetValue(key, out List<GameObject> list))
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null) Destroy(list[i]);
            }
            list.Clear();
            _activeIndicatorsByInstance.Remove(key);
        }
    }
    protected void ClearAllIndicators()
    {
        foreach (var kvp in _activeIndicatorsByInstance)
        {
            for (int i = 0; i < kvp.Value.Count; i++)
            {
                if (kvp.Value[i] != null) Destroy(kvp.Value[i]);
            }
        }
        _activeIndicatorsByInstance.Clear();
    }
    protected Transform GetRootTransform(Transform origin)
    {
        if (origin == null) return null;
        EntityBrain parentBrain = origin.GetComponentInParent<EntityBrain>();
        return parentBrain != null ? parentBrain.transform : origin;
    }
    protected void RotateRootTowards(Transform origin, Vector3 targetPos)
    {
        Transform root = GetRootTransform(origin);
        if (root == null) return;

        Vector3 lookDir = targetPos - root.position;
        lookDir.y = 0f;
        if (lookDir.sqrMagnitude > 0.001f)
        {
            root.rotation = Quaternion.LookRotation(lookDir.normalized);
        }
    }
    protected Vector3 GetTelegraphOrigin(Transform origin)
    {
        Transform rootTransform = GetRootTransform(origin);
        if (rootTransform == null) return Vector3.zero;
        return GetGroundPosition(rootTransform.position, rootTransform);
    }
    protected Vector3 GetGroundPosition(Vector3 referencePosition, Transform attackerRoot = null)
    {
        Vector3 rayOrigin = referencePosition + Vector3.up * 1.5f;
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 6f, groundLayer, QueryTriggerInteraction.Ignore))
        {
            return hit.point + Vector3.up * 0.03f;
        }

        if (attackerRoot != null && attackerRoot.TryGetComponent(out Collider rootCollider))
        {
            return new Vector3(referencePosition.x, rootCollider.bounds.min.y + 0.03f, referencePosition.z);
        }

        return new Vector3(referencePosition.x, referencePosition.y + 0.03f, referencePosition.z);
    }
    protected Vector3 GetSafeChargeDestination(Transform root, Vector3 direction, float desiredDistance, float agentRadius)
    {
        Vector3 flatDir = new Vector3(direction.x, 0f, direction.z).normalized;
        if (flatDir.sqrMagnitude < 0.001f) flatDir = root.forward;

        float safeDistance = desiredDistance;
        Vector3 castOrigin = root.position + Vector3.up * 0.6f;

        if (Physics.SphereCast(castOrigin, Mathf.Max(0.2f, agentRadius * 0.85f), flatDir, out RaycastHit wallHit, desiredDistance, wallLayer, QueryTriggerInteraction.Ignore))
        {
            safeDistance = Mathf.Max(0f, wallHit.distance - agentRadius);
        }

        Vector3 rawEnd = root.position + flatDir * safeDistance;
        if (NavMesh.SamplePosition(rawEnd, out NavMeshHit navHit, 2.0f, NavMesh.AllAreas))
        {
            return new Vector3(navHit.position.x, root.position.y, navHit.position.z);
        }

        return rawEnd;
    }
    protected IEnumerator WaitForImpactTiming(Transform origin)
    {
        if (damageImpactPercent <= 0f) yield break;

        Transform root = GetRootTransform(origin);
        Animator animator = root != null ? root.GetComponentInChildren<Animator>() : null;
        EntityCombat combat = root != null ? root.GetComponent<EntityCombat>() : null;

        if (animator == null || string.IsNullOrEmpty(animationTrigger) || combat == null)
        {
            yield return new WaitForSeconds(fallbackAnimationDuration * damageImpactPercent);
            yield break;
        }

        float enterTimeout = 0.35f;
        float enterTimer = 0f;
        while (!combat.IsAttackAnimationPlaying && enterTimer < enterTimeout)
        {
            enterTimer += Time.deltaTime;
            yield return null;
        }

        if (!combat.IsAttackAnimationPlaying)
        {
            yield return new WaitForSeconds(fallbackAnimationDuration * damageImpactPercent);
            yield break;
        }

        yield return null;

        float safetyTimer = 0f;
        float maxSafetyTime = Mathf.Max(fallbackAnimationDuration * 2.5f, 3f);

        while (combat.IsAttackAnimationPlaying && safetyTimer < maxSafetyTime)
        {
            safetyTimer += Time.deltaTime;
            AnimatorStateInfo stateInfo = animator.IsInTransition(0)
                ? animator.GetNextAnimatorStateInfo(0)
                : animator.GetCurrentAnimatorStateInfo(0);

            if (stateInfo.IsTag("Attack") || safetyTimer >= 0.05f)
            {
                float progress = stateInfo.loop ? (stateInfo.normalizedTime % 1f) : Mathf.Clamp01(stateInfo.normalizedTime);
                if (progress >= damageImpactPercent)
                {
                    yield break;
                }
            }

            yield return null;
        }
    }
    protected bool ApplyDamageAndKnockback(Collider hitCollider, Vector3 knockbackDirection, Vector3 fallbackOrigin)
    {
        if (hitCollider == null) return false;

        PlayerHealth playerHealth = hitCollider.GetComponentInParent<PlayerHealth>();
        if (playerHealth == null) return false;

        if (!playerHealth.TakeDamage(damage))
            return false;

        if (knockback > 0f && playerHealth.TryGetComponent<PlayerMovement>(out var playerMov))
        {
            Vector3 flatDir = knockbackDirection;
            flatDir.y = 0f;

            if (flatDir.sqrMagnitude < 0.001f)
            {
                flatDir = playerHealth.transform.position - fallbackOrigin;
                flatDir.y = 0f;
            }

            if (flatDir.sqrMagnitude > 0.001f)
            {
                playerMov.ApplyKnockback(flatDir.normalized, knockback);
            }
        }

        return true;
    }
}