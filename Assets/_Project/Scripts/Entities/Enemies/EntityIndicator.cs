using UnityEngine;
using System;
using System.Collections;

public class EntityIndicator : MonoBehaviour
{
    [SerializeField] private MeshRenderer _renderer;
    [SerializeField] private Color _startColor = new(0.5f, 0f, 0f, 0.4f);
    [SerializeField] private Color _endColor = new(1f, 0.2f, 0.2f, 0.8f);
    private Material _material;
    private Health _targetHealth;
    private Action _onDeathCallback;
    private void Awake()
    {
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
    }
    public void AnimateIndicator(float duration)
    {
        if (gameObject.activeInHierarchy)
            StartCoroutine(IndicatorRoutine(duration));
    }
    private IEnumerator IndicatorRoutine(float duration)
    {
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            if (_material != null)
                _material.color = Color.Lerp(_startColor, _endColor, t);

            yield return null;
        }
    }
}