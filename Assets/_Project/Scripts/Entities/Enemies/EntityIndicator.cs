using System;
using System.Collections;
using UnityEngine;
public class EntityIndicator : MonoBehaviour
{
    [SerializeField] private MeshRenderer _renderer;
    [SerializeField] private Color _startColor = new(0.5f, 0f, 0f, 0.4f);
    [SerializeField] private Color _endColor = new(1f, 0.2f, 0.2f, 0.8f);
    [SerializeField] private Color _imminentAttackColor = new(1f, 0f, 0f, 1f);
    [Range(0.5f, 1f)][SerializeField] private float _imminentPhaseStartPercent = 0.85f;

    private Material _material;
    private Health _targetHealth;
    private Action _onDeathCallback;
    private Coroutine _indicatorCoroutine;
    private void Awake()
    {
        if (_renderer == null) _renderer = GetComponentInChildren<MeshRenderer>();

        if (_renderer != null)
        {
            _material = new Material(_renderer.sharedMaterial);
            _renderer.material = _material;
            _material.color = _startColor;
        }
    }
    public void Init(Health enemyHealth)
    {
        _targetHealth = enemyHealth;
        if (_targetHealth != null)
        {
            _onDeathCallback = () => Destroy(gameObject);
            _targetHealth.OnDeath += _onDeathCallback;
        }
    }
    private void OnDestroy()
    {
        if (_targetHealth != null)
        {
            _targetHealth.OnDeath -= _onDeathCallback;
        }

        if (_material != null)
        {
            Destroy(_material);
        }
    }
    public void AnimateIndicator(float duration)
    {
        if (!gameObject.activeInHierarchy) return;

        if (_indicatorCoroutine != null)
            StopCoroutine(_indicatorCoroutine);

        _indicatorCoroutine = StartCoroutine(IndicatorRoutine(duration));
    }
    public void TriggerImminentAttackColor()
    {
        if (_indicatorCoroutine != null)
        {
            StopCoroutine(_indicatorCoroutine);
            _indicatorCoroutine = null;
        }

        if (_material != null)
            _material.color = _imminentAttackColor;
    }
    private IEnumerator IndicatorRoutine(float duration)
    {
        float timer = 0f;
        float safeDuration = Mathf.Max(duration, 0.001f);
        float imminentThreshold = Mathf.Clamp(_imminentPhaseStartPercent, 0.01f, 0.99f);

        while (timer < safeDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / safeDuration);

            if (_material != null)
            {
                if (t < imminentThreshold)
                {
                    float chargeProgress = t / imminentThreshold;
                    _material.color = Color.Lerp(_startColor, _endColor, chargeProgress);
                }
                else
                {
                    float imminentProgress = (t - imminentThreshold) / (1f - imminentThreshold);
                    _material.color = Color.Lerp(_endColor, _imminentAttackColor, imminentProgress);
                }
            }

            yield return null;
        }

        if (_material != null)
            _material.color = _imminentAttackColor;

        _indicatorCoroutine = null;
    }
}