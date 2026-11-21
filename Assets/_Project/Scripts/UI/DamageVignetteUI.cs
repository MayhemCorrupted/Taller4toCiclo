using UnityEngine;

public class DamageVignetteUI : MonoBehaviour
{
    [Header("Referencias UI y Jugador")]
    [SerializeField] private CanvasGroup _vignetteCanvasGroup;
    [Tooltip("Opcional: Si se deja vacío, busca automáticamente al PlayerHealth en la escena.")]
    [SerializeField] private PlayerHealth _playerHealth;

    [Header("Tiempos de Fade (Segundos)")]
    [Tooltip("Velocidad con la que aparece el destello al recibir el impacto.")]
    [Min(0.01f)][SerializeField] private float _fadeInDuration = 0.08f;

    [Tooltip("Duración del fade out cuando el jugador tiene la vida casi llena (ej: 0.5s).")]
    [Min(0.05f)][SerializeField] private float _minFadeOutDuration = 0.5f;

    [Tooltip("Duración del fade out cuando el jugador está casi muerto / crítico (ej: 2.2s).")]
    [Min(0.05f)][SerializeField] private float _maxFadeOutDuration = 2.2f;

    [Header("Opacidad (Alpha) según Vida")]
    [Tooltip("Opacidad máxima del efecto cuando el jugador tiene vida alta.")]
    [Range(0.1f, 1f)][SerializeField] private float _minPeakAlpha = 0.55f;

    [Tooltip("Opacidad máxima del efecto cuando el jugador está en vida crítica.")]
    [Range(0.1f, 1f)][SerializeField] private float _maxPeakAlpha = 0.95f;

    [Header("Persistencia al Morir")]
    [Tooltip("Opacidad fija que se mantiene en pantalla desde que muere hasta que reaparece.")]
    [Range(0.1f, 1f)][SerializeField] private float _deathAlpha = 1f;
    [Min(0.01f)][SerializeField] private float _deathFadeInDuration = 0.25f;
    [Min(0.01f)][SerializeField] private float _respawnFadeOutDuration = 0.45f;

    private bool _isDeadLocked;
    private void Awake()
    {
        if (_vignetteCanvasGroup == null)
            _vignetteCanvasGroup = GetComponentInChildren<CanvasGroup>();

        if (_vignetteCanvasGroup != null)
        {
            _vignetteCanvasGroup.alpha = 0f;
            _vignetteCanvasGroup.blocksRaycasts = false;
            _vignetteCanvasGroup.interactable = false;
            _vignetteCanvasGroup.gameObject.SetActive(false);
        }
    }
    private void Start()
    {
        ResolvePlayerHealth();
        SubscribeEvents();
    }
    private void OnDestroy()
    {
        UnsubscribeEvents();

        if (_vignetteCanvasGroup != null)
            LeanTween.cancel(_vignetteCanvasGroup.gameObject);
    }
    private void ResolvePlayerHealth()
    {
        if (_playerHealth != null) return;
        _playerHealth = FindAnyObjectByType<PlayerHealth>();
    }
    private void SubscribeEvents()
    {
        if (_playerHealth != null)
        {
            _playerHealth.OnTakeDamage += HandlePlayerDamaged;
            _playerHealth.OnHealthChanged += HandleHealthChanged;
            _playerHealth.OnHealed += HandlePlayerHealed;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerRespawned += HandlePlayerRespawned;
        }
    }
    private void UnsubscribeEvents()
    {
        if (_playerHealth != null)
        {
            _playerHealth.OnTakeDamage -= HandlePlayerDamaged;
            _playerHealth.OnHealthChanged -= HandleHealthChanged;
            _playerHealth.OnHealed -= HandlePlayerHealed;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerRespawned -= HandlePlayerRespawned;
        }
    }

    private void HandlePlayerDamaged(float damageAmount)
    {
        if (_vignetteCanvasGroup == null || _playerHealth == null) return;

        if (_playerHealth.CurrentHealth <= 0f)
        {
            LockDeathVignette();
            return;
        }

        float healthRatio = _playerHealth.MaxHealth > 0f
            ? Mathf.Clamp01(_playerHealth.CurrentHealth / _playerHealth.MaxHealth)
            : 0f;

        float scaledFadeOut = Mathf.Lerp(_maxFadeOutDuration, _minFadeOutDuration, healthRatio);
        float scaledPeakAlpha = Mathf.Lerp(_maxPeakAlpha, _minPeakAlpha, healthRatio);

        PlayDamageFlash(scaledPeakAlpha, scaledFadeOut);
    }

    private void HandleHealthChanged()
    {
        if (_playerHealth == null) return;

        if (_playerHealth.CurrentHealth <= 0f && !_isDeadLocked)
        {
            LockDeathVignette();
        }
        else if (_isDeadLocked && _playerHealth.CurrentHealth > 0f)
        {
            ClearDeathVignette();
        }
    }
    private void HandlePlayerHealed()
    {
        if (_isDeadLocked && _playerHealth != null && _playerHealth.CurrentHealth > 0f)
        {
            ClearDeathVignette();
        }
    }
    private void HandlePlayerRespawned()
    {
        ClearDeathVignette();
    }
    private void PlayDamageFlash(float peakAlpha, float fadeOutDuration)
    {
        if (_isDeadLocked) return;

        GameObject targetObj = _vignetteCanvasGroup.gameObject;
        LeanTween.cancel(targetObj);
        targetObj.SetActive(true);

        LeanTween.alphaCanvas(_vignetteCanvasGroup, peakAlpha, _fadeInDuration)
                 .setEaseOutQuad()
                 .setIgnoreTimeScale(true)
                 .setOnComplete(() =>
                 {
                     if (_isDeadLocked || _vignetteCanvasGroup == null) return;

                     LeanTween.alphaCanvas(_vignetteCanvasGroup, 0f, fadeOutDuration)
                              .setEaseInOutSine()
                              .setIgnoreTimeScale(true)
                              .setOnComplete(() =>
                              {
                                  if (!_isDeadLocked && _vignetteCanvasGroup != null)
                                  {
                                      _vignetteCanvasGroup.gameObject.SetActive(false);
                                  }
                              });
                 });
    }
    private void LockDeathVignette()
    {
        if (_vignetteCanvasGroup == null) return;

        _isDeadLocked = true;
        GameObject targetObj = _vignetteCanvasGroup.gameObject;
        LeanTween.cancel(targetObj);
        targetObj.SetActive(true);

        LeanTween.alphaCanvas(_vignetteCanvasGroup, _deathAlpha, _deathFadeInDuration)
                 .setEaseOutCubic()
                 .setIgnoreTimeScale(true);
    }
    private void ClearDeathVignette()
    {
        if (_vignetteCanvasGroup == null) return;

        _isDeadLocked = false;
        GameObject targetObj = _vignetteCanvasGroup.gameObject;
        LeanTween.cancel(targetObj);

        if (!targetObj.activeInHierarchy)
        {
            _vignetteCanvasGroup.alpha = 0f;
            return;
        }

        LeanTween.alphaCanvas(_vignetteCanvasGroup, 0f, _respawnFadeOutDuration)
                 .setEaseOutSine()
                 .setIgnoreTimeScale(true)
                 .setOnComplete(() =>
                 {
                     if (!_isDeadLocked && _vignetteCanvasGroup != null)
                     {
                         _vignetteCanvasGroup.gameObject.SetActive(false);
                     }
                 });
    }
}
