using System.Collections;
using UnityEngine;

public class VolumetricFogArea : MonoBehaviour
{
	[SerializeField][Range(0.0f, 1.0f)] private float defaultDensity = 0.0f;
	[SerializeField][Range(0.0f, 1.0f)] private float targetDensity = 0.03f;
	[SerializeField][Min(0.0f)] private float transitionDuration = 1.0f;

	private Coroutine fogCoroutine;

    private void Start()
    {
        RenderSettings.fog = true;
        RenderSettings.fogDensity = defaultDensity;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            FogTransition(targetDensity);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            FogTransition(defaultDensity);
    }

    private void FogTransition(float finalDensity)
    {
        if (fogCoroutine != null)
            StopCoroutine(fogCoroutine);

        fogCoroutine = StartCoroutine(FadeFog(finalDensity));
    }

    private IEnumerator FadeFog(float finalDensity)
    {
        float startDensity = RenderSettings.fogDensity;
        float elapsed = 0.0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            RenderSettings.fogDensity = Mathf.Lerp(startDensity, finalDensity, elapsed / transitionDuration);
            yield return null;
        }

        RenderSettings.fogDensity = finalDensity;
        fogCoroutine = null;
    }
}
