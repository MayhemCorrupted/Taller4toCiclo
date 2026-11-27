using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
public class EnemyWorldHealthHUD : MonoBehaviour
{
    [Header("Referencias del Enemigo")]
    [SerializeField] private EntityBrain _brain;
    [SerializeField] private EntityCombat _combat;
    [SerializeField] private Health _health;
    [Header("Elementos UI World-Space")]
    [SerializeField] private Canvas _worldCanvas;
    [SerializeField] private GameObject _worldHudContainer;
    [SerializeField] private DelayedHealthBar _healthBar = new();
    [SerializeField] private TextMeshProUGUI _healthPercentageText;

    [Header("Posicionamiento y Visibilidad")]
    [SerializeField] private Transform _anchorTarget;
    [SerializeField] private Vector3 _verticalOffset = new(0f, 2.2f, 0f);
    [FormerlySerializedAs("_onlyShowWhenDamagedOrAlerted")]
    [SerializeField] private bool _onlyShowWhenLockedOn = true;

    [Header("Ajustes Visuales")]
    [SerializeField] private bool _faceCamera = true;
    private Camera _mainCamera;
    private PlayerCombat _playerCombat;
    private Transform _enemyRoot;
    private bool _isEnabledForThisEnemy = false;
    private bool _isInitialized = false;
    private void Awake()
    {
        if (_brain == null) _brain = GetComponentInParent<EntityBrain>();
        if (_combat == null) _combat = GetComponentInParent<EntityCombat>();
        if (_health == null) _health = GetComponentInParent<Health>();
        if (_worldCanvas == null) _worldCanvas = GetComponentInChildren<Canvas>(true);

        _enemyRoot = _health != null ? _health.transform
            : (_combat != null ? _combat.transform
            : (_brain != null ? _brain.transform : transform.root));

        _mainCamera = Camera.main;
        BindPlayerCombat();

        if (_worldCanvas != null)
        {
            _worldCanvas.renderMode = RenderMode.WorldSpace;
            if (_mainCamera != null) _worldCanvas.worldCamera = _mainCamera;
        }
    }
    private void Start()
    {
        InitializeFromEntityData();
    }
    private void OnDestroy()
    {
        if (_health != null)
        {
            _health.OnHealthChanged -= HandleHealthChanged;
            _health.OnDeath -= HandleDeath;
        }

        if (_playerCombat != null)
        {
            _playerCombat.OnLockOnTargetChanged -= HandleLockOnTargetChanged;
        }
    }
    private void BindPlayerCombat()
    {
        if (_playerCombat != null) return;

        _playerCombat = FindAnyObjectByType<PlayerCombat>();
        if (_playerCombat != null)
        {
            _playerCombat.OnLockOnTargetChanged -= HandleLockOnTargetChanged;
            _playerCombat.OnLockOnTargetChanged += HandleLockOnTargetChanged;
        }
    }
    public void InitializeFromEntityData()
    {
        EntityData data = _brain != null ? _brain.Data : (_combat != null ? _combat.Data : null);
        if (data == null)
        {
            if (_worldHudContainer != null) _worldHudContainer.SetActive(false);
            return;
        }

        _isInitialized = true;
        _isEnabledForThisEnemy = (data.healthBarMode == EnemyHealthBarMode.WorldSpace);

        if (!_isEnabledForThisEnemy)
        {
            if (_worldHudContainer != null) _worldHudContainer.SetActive(false);
            return;
        }

        if (_health != null)
        {
            _health.OnHealthChanged -= HandleHealthChanged;
            _health.OnDeath -= HandleDeath;
            _health.OnHealthChanged += HandleHealthChanged;
            _health.OnDeath += HandleDeath;
            UpdateVisuals(instant: true);
        }

        RefreshVisibility();
    }
    private void LateUpdate()
    {
        if (!_isInitialized)
        {
            InitializeFromEntityData();
            if (!_isInitialized) return;
        }

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

        _healthBar.Tick(Time.deltaTime);
    }
    private bool IsThisEnemyLockedOn()
    {
        if (_playerCombat == null)
            BindPlayerCombat();

        if (_playerCombat == null || _playerCombat.LockOnTarget == null || _enemyRoot == null)
            return false;

        return _playerCombat.LockOnTarget.IsChildOf(_enemyRoot);
    }
    private void RefreshVisibility()
    {
        if (_worldHudContainer == null) return;

        if (!_onlyShowWhenLockedOn)
        {
            if (!_worldHudContainer.activeSelf) _worldHudContainer.SetActive(true);
            return;
        }
        bool shouldShow = IsThisEnemyLockedOn();

        if (_worldHudContainer.activeSelf != shouldShow)
            _worldHudContainer.SetActive(shouldShow);
    }
    private void HandleLockOnTargetChanged(Transform newTarget)
    {
        if (!_isInitialized || !_isEnabledForThisEnemy) return;
        RefreshVisibility();
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

        float targetRatio = Mathf.Clamp01(_health.CurrentHealth / _health.MaxHealth);

        if (instant)
            _healthBar.SetInstant(targetRatio);
        else
            _healthBar.SetTarget(targetRatio);

        if (_healthPercentageText != null)
        {
            int percentage = _health.CurrentHealth > 0f
                ? Mathf.Max(1, Mathf.RoundToInt(targetRatio * 100f))
                : 0;

            _healthPercentageText.text = $"{percentage}%";
        }
    }
}