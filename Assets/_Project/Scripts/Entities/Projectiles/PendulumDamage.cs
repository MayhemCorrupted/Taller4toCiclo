using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class PendulumDamage : MonoBehaviour
{
    [Header("Daño")]
    [SerializeField] private float damage = 35f;

    [Header("Empuje (Knockback)")]
    [SerializeField] private bool applyKnockback = true;
    [SerializeField] private float knockbackForce = 12f;
    [SerializeField] private float knockbackDuration = 0.2f;
    [Range(0f, 1f)]
    [SerializeField] private float blockKnockbackMultiplier = 0.5f;

    private Vector3 lastPosition;
    private Vector3 swingVelocity;

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;

        Rigidbody rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    private void Start()
    {
        lastPosition = transform.position;
    }

    private void LateUpdate()
    {
        if (Time.deltaTime > 0f)
        {
            swingVelocity = (transform.position - lastPosition) / Time.deltaTime;
        }
        lastPosition = transform.position;
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (other.TryGetComponent<PlayerHealth>(out PlayerHealth playerHealth))
        {
            bool wasBlocking = other.TryGetComponent<PlayerFSM>(out PlayerFSM fsm) && fsm.CurrentState == PlayerState.Blocking;
            bool hitConnected = playerHealth.TakeDamage(damage);

            if (hitConnected && applyKnockback)
            {
                if (other.TryGetComponent<CharacterController>(out CharacterController cc))
                {
                    Vector3 pushDir = CalculatePushDirection(other.transform.position);
                    float finalForce = wasBlocking ? knockbackForce * blockKnockbackMultiplier : knockbackForce;

                    StartCoroutine(ApplyKnockbackRoutine(cc, pushDir, finalForce));
                }
            }
        }
    }

    private Vector3 CalculatePushDirection(Vector3 playerPosition)
    {
        Vector3 horizontalVelocity = new Vector3(swingVelocity.x, 0f, swingVelocity.z);

        if (horizontalVelocity.sqrMagnitude < 0.05f)
        {
            Vector3 awayFromBlade = playerPosition - transform.position;
            awayFromBlade.y = 0f;
            return awayFromBlade.sqrMagnitude > 0.001f ? awayFromBlade.normalized : transform.right;
        }

        return horizontalVelocity.normalized;
    }

    private IEnumerator ApplyKnockbackRoutine(CharacterController cc, Vector3 direction, float force)
    {
        float timer = 0f;

        while (timer < knockbackDuration && cc != null && cc.enabled)
        {
            float currentForce = Mathf.Lerp(force, 0f, timer / knockbackDuration);
            cc.Move(direction * currentForce * Time.deltaTime);

            timer += Time.deltaTime;
            yield return null;
        }
    }
}