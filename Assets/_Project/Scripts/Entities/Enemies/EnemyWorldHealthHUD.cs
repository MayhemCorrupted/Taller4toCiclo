using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class EnemyWorldHealthHUD : MonoBehaviour
{
    [Header("Referencias del Enemigo")]
    [SerializeField] private EntityBrain _brain;
    [SerializeField] private EntityCombat _combat;
    [SerializeField] private Health _health;

    [Header("Elementos UI World-Space")]
    [SerializeField] private Canvas _worldCanvas;
    [SerializeField] private GameObject _worldHudContainer;
    [SerializeField] private Image _healthFillImage;
    [SerializeField] private TextMeshProUGUI _healthPercentageText;

    [Header("Posicionamiento y Visibilidad")]
    [SerializeField] private Transform _anchorTarget;
    [SerializeField] private Vector3 _verticalOffset = new(0f, 2.2f, 0f);
    [SerializeField] private bool _onlyShowWhenDamagedOrAlerted = false;

    [Header("Ajustes Visuales")]
    [SerializeField] private float _lerpSpeed = 12f;
    [SerializeField] private bool _faceCamera = true;

    private Camera _mainCamera;
    private float _targetFillAmount = 1f;
    private bool _isEnabledForThisEnemy = false;
    private void Awake()
    {
        if (_brain == null) _brain = GetComponentInParent<EntityBrain>();
        if (_combat == null) _combat = GetComponentInParent<EntityCombat>();
        if (_health == null) _health = GetComponentInParent<Health>();
        if (_worldCanvas == null) _worldCanvas = GetComponentInChildren<Canvas>(true);

        _mainCamera = Camera.main;

        if (_worldCanvas != null)
        {
            _worldCanvas.renderMode = RenderMode.WorldSpace;
            if (_mainCamera != null) _worldCanvas.worldCamera = _mainCamera;
        }
    }
    private void Start()
    {
        EntityData data = _brain != null ? _brain.Data : null;
        _isEnabledForThisEnemy = (data != null && data.healthBarMode == EnemyHealthBarMode.WorldSpace);

        if (!_isEnabledForThisEnemy)
        {
            if (_worldHudContainer != null) _worldHudContainer.SetActive(false);
            return;
        }

        if (_health != null)
        {
            _health.OnHealthChanged += HandleHealthChanged;
            _health.OnDeath += HandleDeath;
            UpdateVisuals(instant: true);
        }

        RefreshVisibility();
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
        if (!_isEnabledForThisEnemy || _worldHudContainer == null)
            return;

        if (_health != null && _health.CurrentHealth <= 0f)
        {
            if (_worldHudContainer.activeSelf) _worldHudContainer.SetActive(false);
            return;
        }

        RefreshVisibility();

        if (!_worldHudContainer.activeSelf)
            return;

        if (_anchorTarget != null)
        {
            transform.position = _anchorTarget.position + _verticalOffset;
        }

        if (_faceCamera)
        {
            if (_mainCamera == null) _mainCamera = Camera.main;
            if (_mainCamera != null)
                transform.rotation = _mainCamera.transform.rotation;
        }

        if (_healthFillImage != null)
        {
            _healthFillImage.fillAmount = Mathf.Lerp(_healthFillImage.fillAmount, _targetFillAmount, Time.deltaTime * _lerpSpeed);
        }
    }
    private void RefreshVisibility()
    {
        if (_worldHudContainer == null) return;

        if (!_onlyShowWhenDamagedOrAlerted)
        {
            if (!_worldHudContainer.activeSelf) _worldHudContainer.SetActive(true);
            return;
        }

        bool isDamaged = _health != null && _health.CurrentHealth < _health.MaxHealth;
        bool isAlerted = _combat != null && _combat.CanSeeTarget;
        bool shouldShow = isDamaged || isAlerted;

        if (_worldHudContainer.activeSelf != shouldShow)
            _worldHudContainer.SetActive(shouldShow);
    }
    private void HandleHealthChanged()
    {
        UpdateVisuals(instant: false);
        RefreshVisibility();
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