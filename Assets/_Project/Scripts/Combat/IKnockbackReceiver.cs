using UnityEngine;

public interface IKnockbackReceiver
{
    void ApplyKnockback(Vector3 direction, float force);
}
