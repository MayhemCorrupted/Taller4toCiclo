using UnityEngine;
using UnityEngine.InputSystem;

public class TEMPplayerAttack : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActionAsset;
    [SerializeField] private Color damagedColor = Color.red;
    [SerializeField] private Collider attackCollider;
    private InputAction attackAction;

    private Rigidbody enemyRb;
    void Start()
    {
        attackAction = inputActionAsset.FindAction("Attack");
    }
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            enemyRb = other.GetComponent<Rigidbody>();
            if (enemyRb != null)
            {
                Vector3 forceDirection = other.transform.position - transform.position;
                forceDirection.y = 0; 
                forceDirection.Normalize();
                float forceMagnitude = 10f; 
                if (attackAction.triggered)
                {
                    enemyRb.AddForce(forceDirection * forceMagnitude, ForceMode.Impulse);
                }
            }
        }
    }
}
