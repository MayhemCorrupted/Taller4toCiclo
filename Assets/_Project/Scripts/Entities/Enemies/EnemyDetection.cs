using System.Collections;
using UnityEngine;

public class EnemyDetection : MonoBehaviour
{
	[SerializeField] private LayerMask targetLayer;
	[SerializeField] private LayerMask wallLayer;

	[SerializeField] private float checkInterval = 0.1f;

    private float detectionRadius;
    private float attackRadius;
    private float loseTargetRadius;

    private Transform target = null;
	private bool canSeeTarget = false;
	private bool isInAttackRange = false;
	private WaitForSeconds rangeEvalInterval;


	public bool CanSeeTarget => canSeeTarget;

	public bool IsInAttackRange => isInAttackRange;

	public Vector3 TargetLocation => target ? target.position : Vector3.positiveInfinity;
	public Transform TargetTransform => target;

	public float DetectionRadius => detectionRadius;
	public float AttackRadius => attackRadius;
	public float LoseTargetRadius => loseTargetRadius;

	private void Awake()
	{
		rangeEvalInterval = new WaitForSeconds(checkInterval);
	}

	private void OnDrawGizmos()
	{
#if UNITY_EDITOR
		Gizmos.color = Color.white;
		UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.up, detectionRadius);

		Gizmos.color = Color.orange;
		UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.up, loseTargetRadius);

		Gizmos.color = Color.red;
		UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.up, attackRadius);
#endif
	}

	public void Init(float detRadius, float atkRadius, float losRadius)
	{
		detectionRadius = detRadius;
		attackRadius = atkRadius;
		loseTargetRadius = losRadius;

		StartCoroutine(CRDetection());
	}

	private IEnumerator CRDetection()
	{
		while (true)
		{
			yield return rangeEvalInterval;
			Detect();
		}
	}

	private void Detect()
	{
		if (!canSeeTarget)
		{
			Collider[] rangeCheck = Physics.OverlapSphere(transform.position, detectionRadius, targetLayer);

			if (rangeCheck.Length > 0)
			{
				Transform _target = rangeCheck[0].transform;

				if (HasClearLineOfSight(_target, detectionRadius))
				{
					target = _target;
					canSeeTarget = true;
					CheckAttackRange();
				}
			}
		}
		else
		{
			if (!target)
			{
				ResetDetectionData();
				return;
			}

			float distance = Vector3.Distance(transform.position, target.position);

			if (distance > loseTargetRadius || !HasClearLineOfSight(target, distance))
				ResetDetectionData();
			else
				isInAttackRange = distance <= attackRadius;
		}
	}

	private void ResetDetectionData()
	{
		target = null;
		canSeeTarget = false;
		isInAttackRange = false;
	}

	private bool HasClearLineOfSight(Transform target, float maxDistance)
	{
		Vector3 direction = (target.position - transform.position).normalized;

		return !Physics.Raycast(transform.position, direction, maxDistance, wallLayer);
	}

	private void CheckAttackRange()
	{
		if (!target) return;

		float distance = Vector3.Distance(transform.position, target.position);
		isInAttackRange = distance <= attackRadius;
	}
}
