using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using Unity.Cinemachine;
[Serializable] public struct CameraShakeProfile
{
    [Tooltip("Amplitud máxima del movimiento de cámara (AmplitudeGain).")]
    [Min(0f)] public float intensity;

    [Tooltip("Velocidad de vibración (FrequencyGain).")]
    [Min(0f)] public float frequency;

    [Tooltip("Duración total del shake en segundos.")]
    [Min(0.01f)] public float duration;
}
public class CameraShakeManager : MonoBehaviour
{
    public static CameraShakeManager Instance { get; private set; }
    [Header("Referencia Cinemachine (Opcional si se auto-detecta)")]
    [SerializeField] private CinemachineBasicMultiChannelPerlin _noiseComponent;
    [Header("Perfiles de Impacto")]
    [SerializeField] private CameraShakeProfile _lightHitProfile = new()
    {
        intensity = 1.2f,
        frequency = 2.5f,
        duration = 0.14f
    };
    [SerializeField] private CameraShakeProfile _heavyHitProfile = new()
    {
        intensity = 2.6f,
        frequency = 3.2f,
        duration = 0.24f
    };
    [SerializeField] private CameraShakeProfile _skillHitProfile = new()
    {
        intensity = 3.2f,
        frequency = 3.5f,
        duration = 0.28f
    };
    [Header("Mejoras de Feeling")]
    [Tooltip("Curva de decaimiento (Eje X: 0 a 1 tiempo normalizado | Eje Y: multiplicador de intensidad).")]
    [SerializeField] private AnimationCurve _falloffCurve = new(
        new Keyframe(0f, 1f, 0f, -2f),
        new Keyframe(1f, 0f, 0f, 0f)
    );
    [Tooltip("Si está activo, un shake más débil no sobrescribirá uno más fuerte que siga activo.")]
    [SerializeField] private bool _protectStrongerShake = true;
    [Tooltip("Usa tiempo sin escalar (recomendado si usas hitstop o slow-motion).")]
    [SerializeField] private bool _useUnscaledTime = true;
    private Coroutine _shakeCoroutine;
    private float _currentActiveIntensity;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ResolveNoiseComponent();
    }
    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        StopShake();
    }
    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _currentActiveIntensity = 0f;
        _shakeCoroutine = null;
        ResolveNoiseComponent();
    }
    private void ResolveNoiseComponent()
    {
        if (_noiseComponent != null) return;

        _noiseComponent = FindAnyObjectByType<CinemachineBasicMultiChannelPerlin>();
        if (_noiseComponent != null)
        {
            _noiseComponent.AmplitudeGain = 0f;
            _noiseComponent.FrequencyGain = 0f;
        }
    }
    public void ShakeLightHit() => Shake(_lightHitProfile);
    public void ShakeHeavyHit() => Shake(_heavyHitProfile);
    public void ShakeSkillHit() => Shake(_skillHitProfile);
    public void Shake(CameraShakeProfile profile)
    {
        Shake(profile.intensity, profile.frequency, profile.duration);
    }
    public void Shake(float intensity, float frequency, float duration)
    {
        if (intensity <= 0f || duration <= 0f) return;

        if (_noiseComponent == null)
        {
            ResolveNoiseComponent();
            if (_noiseComponent == null) return;
        }

        if (_protectStrongerShake && _shakeCoroutine != null && intensity < _currentActiveIntensity)
            return;

        if (_shakeCoroutine != null)
            StopCoroutine(_shakeCoroutine);

        _shakeCoroutine = StartCoroutine(ShakeRoutine(intensity, frequency, duration));
    }
    public void StopShake()
    {
        if (_shakeCoroutine != null)
        {
            StopCoroutine(_shakeCoroutine);
            _shakeCoroutine = null;
        }

        _currentActiveIntensity = 0f;

        if (_noiseComponent != null)
        {
            _noiseComponent.AmplitudeGain = 0f;
            _noiseComponent.FrequencyGain = 0f;
        }
    }
    private IEnumerator ShakeRoutine(float maxIntensity, float frequency, float duration)
    {
        _noiseComponent.FrequencyGain = frequency;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (_noiseComponent == null) yield break;

            elapsed += _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / duration);
            float curveFactor = _falloffCurve != null ? _falloffCurve.Evaluate(normalizedTime) : (1f - normalizedTime);

            _currentActiveIntensity = maxIntensity * Mathf.Max(0f, curveFactor);
            _noiseComponent.AmplitudeGain = _currentActiveIntensity;

            yield return null;
        }

        _currentActiveIntensity = 0f;
        _shakeCoroutine = null;

        if (_noiseComponent != null)
        {
            _noiseComponent.AmplitudeGain = 0f;
            _noiseComponent.FrequencyGain = 0f;
        }
    }
}
