using UnityEngine;
using System.Collections;
[CreateAssetMenu(fileName = "EntityMeleeAttack", menuName = "Rotbound/Entity/Attack/EntityMeleeAttack")]
public class EntityMeleeAttack : EntityAttack
{
    [Header("MELEE PROPERTIES")]
    public bool isRadial;
    public float radius;
    public Vector3 hitboxMeasurements = Vector3.one;
    public float verticalOffset = 0.0f;
    private readonly Collider[] _hitBuffer = new Collider[10];
    public override IEnumerator Execute(Transform origin, Transform target, MonoBehaviour runner)
    {
        Vector3 originPos = origin.position + (Vector3.up * verticalOffset);
        int hitCount;

        if (isRadial)
        {
            hitCount = Physics.OverlapSphereNonAlloc(originPos, radius, _hitBuffer);
        }
        else
        {
            Vector3 boxCenter = originPos + (origin.forward * radius);
            hitCount = Physics.OverlapBoxNonAlloc(boxCenter, hitboxMeasurements * 0.5f, _hitBuffer, origin.rotation);
        }

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = _hitBuffer[i];

            if (hit.TryGetComponent(out PlayerHealth playerHealth))
            {
                playerHealth.TakeDamage(damage);

                if (hit.TryGetComponent(out PlayerMovement playerMov))
                {
                    Vector3 knockbackDir = (hit.transform.position - origin.position).normalized;
                    playerMov.ApplyKnockback(knockbackDir, knockback);
                    yield return null;
                }
            }
        }
    }
}