using System.Collections;
using UnityEngine;

public class EnemyDetection : MonoBehaviour
{
    [Min(0.01f)] public float radius;

    [SerializeField] private LayerMask targetLayer;
    [SerializeField] private LayerMask wallLayer;

    private bool canSeeTarget = false;
    private Vector3 targetLocation = Vector3.zero;

    private void OnDrawGizmos()
    {
#if UNITY_EDITOR
        Gizmos.color = Color.red;
        UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.up, radius);
#endif
    }

    public void Init()
    {
        StartCoroutine(CRDetection());
    }

    private IEnumerator CRDetection()
    {
        while (true)
        {
            yield return null;
            Detect();
        }
    }

    private void Detect()
    {
        // ...
    }
}
