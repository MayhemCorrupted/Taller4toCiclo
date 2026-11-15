using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class EnemyWorldHealthHUD : MonoBehaviour
{
    [Header("Referencias del Enemigo")]
    [SerializeField] private EntityBrain _brain;
    [SerializeField] private Health _health;

    [Header("Elementos UI World-Space")]
    [Tooltip("Contenedor raíz del World-Space Canvas sobre la cabeza del enemigo.")]
    [SerializeField] private GameObject _worldHudContainer;
    [Tooltip("Imagen de relleno (Image Type = Filled).")]
    [SerializeField] private Image _healthFillImage;
    [Tooltip("Texto con el porcentaje de vida (ej: 100%).")]
    [SerializeField] private TextMeshProUGUI _healthPercentageText;

    [Header("Ajustes Visuales")]
    [SerializeField] private float _lerpSpeed = 12f;
    [SerializeField] private bool _faceCamera = true;

    private Camera _mainCamera;
    private float _targetFillAmount = 1f;
    private bool _isEnabledForThisEnemy = false;
    private void Awake()
    {
        if (_brain == null) _brain = GetComponentInParent<EntityBrain>();
        if (_health == null) _health = GetComponentInParent<Health>();
        _mainCamera = Camera.main;
    }
    private void Start()
    {
        EntityData data = _brain != null ? _brain.Data : null;
        _isEnabledForThisEnemy = (data != null && data.healthBarMode == EnemyHealthBarMode.WorldSpace);

        if (_worldHudContainer != null)
            _worldHudContainer.SetActive(_isEnabledForThisEnemy);

        if (_isEnabledForThisEnemy && _health != null)
        {
            _health.OnHealthChanged += HandleHealthChanged;
            _health.OnDeath += HandleDeath;
            UpdateVisuals(instant: true);
        }
    }
    private void OnDestroy()
    {
        if (_health != null)
        {
            _health.OnHealthChanged -= HandleHealthChanged;
            _health.OnDeath -= HandleDeath;
        }
    }
    private void LateUpdate()
    {
        if (!_isEnabledForThisEnemy || _worldHudContainer == null || !_worldHudContainer.activeSelf)
            return;

        if (_faceCamera && _mainCamera != null)
        {
            transform.rotation = _mainCamera.transform.rotation;
        }

        if (_healthFillImage != null)
        {
            _healthFillImage.fillAmount = Mathf.Lerp(_healthFillImage.fillAmount, _targetFillAmount, Time.deltaTime * _lerpSpeed);
        }
    }
    private void HandleHealthChanged()
    {
        UpdateVisuals(instant: false);
    }
    private void HandleDeath()
    {
        if (_worldHudContainer != null)
            _worldHudContainer.SetActive(false);
    }
    private void UpdateVisuals(bool instant)
    {
        if (_health == null || _health.MaxHealth <= 0f) return;

        _targetFillAmount = Mathf.Clamp01(_health.CurrentHealth / _health.MaxHealth);

        if (instant && _healthFillImage != null)
            _healthFillImage.fillAmount = _targetFillAmount;

        if (_healthPercentageText != null)
        {
            int percentage = _health.CurrentHealth > 0f
                ? Mathf.Max(1, Mathf.RoundToInt(_targetFillAmount * 100f))
                : 0;

            _healthPercentageText.text = $"{percentage}%";
        }
    }
}