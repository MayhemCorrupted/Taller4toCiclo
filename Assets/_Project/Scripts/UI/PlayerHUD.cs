using UnityEngine;
using UnityEngine.UI;
public class PlayerHUD : MonoBehaviour
{
    [Header("Visibilidad Dormida (Recomendado)")]
    [Tooltip("Opcional: Si se asigna, el HUD se oculta con alpha=0 fuera de Gameplay sin desactivar el GameObject, manteniéndose siempre sincronizado.")]
    [SerializeField] private CanvasGroup _hudCanvasGroup;
    [SerializeField] private bool _hideOutsideGameplay = true;

    [Header("Barras de Estado")]
    [SerializeField] private DelayedHealthBar _healthBar = new();
    [SerializeField] private Image _staminaFillImage;
    [SerializeField] private float _staminaLerpSpeed = 10f;

    [Header("Interfaz de Habilidades (Ataques)")]
    [Tooltip("Contenedor padre de la UI de ataques/habilidades que se ocultará cuando IsWeaponActive sea falso.")]
    [SerializeField] private GameObject _attacksContainer;
    [SerializeField] private Image _skillIconImage;
    [SerializeField] private Image _skillCooldownFill;
    [SerializeField] private Sprite _distanceSkillSprite;
    [SerializeField] private Sprite _areaSkillSprite;

    [Header("Animación Cambio de Habilidad (LeanTween)")]
    [Tooltip("El impulso de escala al cambiar de habilidad (ej: x=1.3, y=1.3 para que crezca y vuelva a 1)")]
    [SerializeField] private Vector3 _skillSwapPunchScale = new(1.3f, 1.3f, 1f);
    [Tooltip("La distancia en Y hacia la que salta rápido al cambiar (ej: 15)")]
    [SerializeField] private float _skillSwapPunchDistanceY = 15f;
    [SerializeField] private float _skillSwapTweenDuration = 0.25f;

    [Header("Interfaz de Consumibles")]
    [SerializeField] private Image _consumableIconImage;

    private PlayerHealth _playerHealth;
    private PlayerStamina _playerStamina;
    private PlayerStats _playerStats;
    private PlayerCombat _playerCombat;
    private InventoryManager _inventoryManager;

    private float _targetStaminaFill;
    private float _originalSkillIconY;
    private bool _hasCapturedOriginalY;
    private bool _lastDormantHiddenState;
    private void Awake()
    {
        ResolveReferences();

        if (_hudCanvasGroup == null)
            _hudCanvasGroup = GetComponentInChildren<CanvasGroup>();

        if (_skillIconImage != null && !_hasCapturedOriginalY)
        {
            _originalSkillIconY = _skillIconImage.rectTransform.anchoredPosition.y;
            _hasCapturedOriginalY = true;
        }
    }
    private void OnEnable()
    {
        ResolveReferences();

        if (_playerHealth != null) _playerHealth.OnHealthChanged += UpdateHealthBar;
        if (_playerStamina != null) _playerStamina.OnStaminaChanged += UpdateStaminaBar;

        if (_playerCombat != null)
        {
            _playerCombat.OnSkillSwapped += HandleSkillSwappedAnimated;
            _playerCombat.OnWeaponActiveChanged += HandleWeaponActiveChanged;
        }

        if (_inventoryManager != null) _inventoryManager.OnConsumableEquipped += HandleConsumableEquipped;

        SyncAllHUDData();
    }
    private void OnDisable()
    {
        if (_playerHealth != null) _playerHealth.OnHealthChanged -= UpdateHealthBar;
        if (_playerStamina != null) _playerStamina.OnStaminaChanged -= UpdateStaminaBar;

        if (_playerCombat != null)
        {
            _playerCombat.OnSkillSwapped -= HandleSkillSwappedAnimated;
            _playerCombat.OnWeaponActiveChanged -= HandleWeaponActiveChanged;
        }

        if (_inventoryManager != null) _inventoryManager.OnConsumableEquipped -= HandleConsumableEquipped;
    }
    private void Start()
    {
        SyncAllHUDData();
    }
    private void Update()
    {
        UpdateDormantVisibility();

        float dt = Time.unscaledDeltaTime;

        _healthBar.Tick(dt);

        if (_staminaFillImage != null)
            _staminaFillImage.fillAmount = Mathf.Lerp(_staminaFillImage.fillAmount, _targetStaminaFill, dt * _staminaLerpSpeed);

        if (_playerCombat != null && _skillCooldownFill != null)
        {
            _skillCooldownFill.fillAmount = _playerCombat.GetSkillCooldownPercentage(_playerCombat.CurrentSkillID);
        }
    }
    private void ResolveReferences()
    {
        if (_playerHealth == null) _playerHealth = FindAnyObjectByType<PlayerHealth>();
        if (_playerStamina == null) _playerStamina = FindAnyObjectByType<PlayerStamina>();
        if (_playerStats == null) _playerStats = FindAnyObjectByType<PlayerStats>();
        if (_playerCombat == null) _playerCombat = FindAnyObjectByType<PlayerCombat>();
        if (_inventoryManager == null) _inventoryManager = FindAnyObjectByType<InventoryManager>();
    }
    public void SyncAllHUDData()
    {
        ResolveReferences();

        if (_skillIconImage != null && !_hasCapturedOriginalY)
        {
            _originalSkillIconY = _skillIconImage.rectTransform.anchoredPosition.y;
            _hasCapturedOriginalY = true;
        }

        UpdateStaminaBar();

        if (_playerHealth != null && _playerHealth.MaxHealth > 0f)
        {
            _healthBar.SetInstant(_playerHealth.CurrentHealth / _playerHealth.MaxHealth);
        }

        if (_staminaFillImage != null) _staminaFillImage.fillAmount = _targetStaminaFill;

        if (_playerCombat != null)
        {
            HandleWeaponActiveChanged(_playerCombat.IsWeaponActive);
            ApplySkillSprite(_playerCombat.CurrentSkillID, playPunchAnimation: false);
        }

        if (_inventoryManager != null)
        {
            HandleConsumableEquipped(_inventoryManager.EquippedConsumable, _inventoryManager.ConsumableCount);
        }

        UpdateDormantVisibility(force: true);
    }
    private void UpdateDormantVisibility(bool force = false)
    {
        if (_hudCanvasGroup == null || !_hideOutsideGameplay) return;

        bool shouldHide = UIManager.Instance != null && UIManager.Instance.CurrentState != GameState.Gameplay;
        if (!force && shouldHide == _lastDormantHiddenState) return;

        _lastDormantHiddenState = shouldHide;
        _hudCanvasGroup.alpha = shouldHide ? 0f : 1f;
        _hudCanvasGroup.interactable = !shouldHide;
        _hudCanvasGroup.blocksRaycasts = !shouldHide;
    }
    private void HandleWeaponActiveChanged(bool isWeaponActive)
    {
        if (_attacksContainer != null)
        {
            _attacksContainer.SetActive(isWeaponActive);
        }
    }
    private void UpdateHealthBar()
    {
        if (_playerHealth == null || _playerHealth.MaxHealth <= 0f) return;
        _healthBar.SetTarget(_playerHealth.CurrentHealth / _playerHealth.MaxHealth);
    }
    private void UpdateStaminaBar()
    {
        if (_playerStamina == null || _playerStats == null || _playerStats.MaxStamina <= 0f) return;
        _targetStaminaFill = _playerStamina.CurrentStamina / _playerStats.MaxStamina;
    }
    private void HandleSkillSwappedAnimated(int skillID)
    {
        bool isGameplay = UIManager.Instance == null || UIManager.Instance.CurrentState == GameState.Gameplay;
        ApplySkillSprite(skillID, playPunchAnimation: isGameplay && gameObject.activeInHierarchy);
    }
    private void ApplySkillSprite(int skillID, bool playPunchAnimation)
    {
        if (_skillIconImage == null) return;

        _skillIconImage.sprite = (skillID == 0) ? _distanceSkillSprite : _areaSkillSprite;

        LeanTween.cancel(_skillIconImage.gameObject);
        _skillIconImage.rectTransform.localScale = Vector3.one;

        Vector2 currentAnchoredPos = _skillIconImage.rectTransform.anchoredPosition;
        _skillIconImage.rectTransform.anchoredPosition = new Vector2(currentAnchoredPos.x, _originalSkillIconY);

        if (playPunchAnimation)
        {
            LeanTween.scale(_skillIconImage.rectTransform, _skillSwapPunchScale, _skillSwapTweenDuration)
                .setEasePunch();
            float targetY = _originalSkillIconY + _skillSwapPunchDistanceY;
            LeanTween.moveY(_skillIconImage.rectTransform, targetY, _skillSwapTweenDuration)
                .setEasePunch();
        }
    }
    private void HandleConsumableEquipped(ConsumableItem item, int count)
    {
        if (_consumableIconImage == null) return;

        if (item == null || count <= 0)
        {
            _consumableIconImage.enabled = false;
        }
        else
        {
            _consumableIconImage.sprite = item.Icon;
            _consumableIconImage.enabled = true;
        }
    }
}