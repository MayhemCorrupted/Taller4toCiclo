using UnityEngine;
using UnityEngine.UI;
public class PlayerHUD : MonoBehaviour
{
    [Header("Barras de Estado")]
    [SerializeField] private Image _healthFillImage;
    [SerializeField] private Image _staminaFillImage;
    [SerializeField] private float _lerpSpeed = 10f;

    [Header("Interfaz de Habilidades (Ataques)")]
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
    private float _targetHealthFill;
    private float _targetStaminaFill;
    private float _originalSkillIconY;
    private void Awake()
    {
        _playerHealth = FindAnyObjectByType<PlayerHealth>();
        _playerStamina = FindAnyObjectByType<PlayerStamina>();
        _playerStats = FindAnyObjectByType<PlayerStats>();
        _playerCombat = FindAnyObjectByType<PlayerCombat>();
        _inventoryManager = FindAnyObjectByType<InventoryManager>();
    }
    private void OnEnable()
    {
        if (_playerHealth != null) _playerHealth.OnHealthChanged += UpdateHealthBar;
        if (_playerStamina != null) _playerStamina.OnStaminaChanged += UpdateStaminaBar;

        if (_playerCombat != null) _playerCombat.OnSkillSwapped += HandleSkillSwapped;
        if (_inventoryManager != null) _inventoryManager.OnConsumableEquipped += HandleConsumableEquipped;
    }
    private void OnDisable()
    {
        if (_playerHealth != null) _playerHealth.OnHealthChanged -= UpdateHealthBar;
        if (_playerStamina != null) _playerStamina.OnStaminaChanged -= UpdateStaminaBar;

        if (_playerCombat != null) _playerCombat.OnSkillSwapped -= HandleSkillSwapped;
        if (_inventoryManager != null) _inventoryManager.OnConsumableEquipped -= HandleConsumableEquipped;
    }
    private void Start()
    {
        UpdateStaminaBar();
        UpdateHealthBar();

        if (_healthFillImage != null) _healthFillImage.fillAmount = _targetHealthFill;
        if (_staminaFillImage != null) _staminaFillImage.fillAmount = _targetStaminaFill;

        if (_skillIconImage != null)
        {
            _originalSkillIconY = _skillIconImage.rectTransform.anchoredPosition.y;
        }

        if (_playerCombat != null) HandleSkillSwapped(_playerCombat.CurrentSkillID);
        if (_inventoryManager != null) HandleConsumableEquipped(_inventoryManager.EquippedConsumable, _inventoryManager.ConsumableCount);
    }
    private void Update()
    {
        if (_healthFillImage != null)
            _healthFillImage.fillAmount = Mathf.Lerp(_healthFillImage.fillAmount, _targetHealthFill, Time.deltaTime * _lerpSpeed);

        if (_staminaFillImage != null)
            _staminaFillImage.fillAmount = Mathf.Lerp(_staminaFillImage.fillAmount, _targetStaminaFill, Time.deltaTime * _lerpSpeed);

        if (_playerCombat != null && _skillCooldownFill != null)
        {
            _skillCooldownFill.fillAmount = _playerCombat.GetSkillCooldownPercentage(_playerCombat.CurrentSkillID);
        }
    }
    private void UpdateHealthBar()
    {
        if (_playerHealth == null || _playerHealth.MaxHealth <= 0) return;
        _targetHealthFill = _playerHealth.CurrentHealth / _playerHealth.MaxHealth;
    }
    private void UpdateStaminaBar()
    {
        if (_playerStamina == null || _playerStats == null || _playerStats.MaxStamina <= 0) return;
        _targetStaminaFill = _playerStamina.CurrentStamina / _playerStats.MaxStamina;
    }
    private void HandleSkillSwapped(int skillID)
    {
        if (_skillIconImage == null) return;

        _skillIconImage.sprite = (skillID == 0) ? _distanceSkillSprite : _areaSkillSprite;

        LeanTween.cancel(_skillIconImage.gameObject);

        _skillIconImage.rectTransform.localScale = Vector3.one;
        Vector2 currentAnchoredPos = _skillIconImage.rectTransform.anchoredPosition;
        _skillIconImage.rectTransform.anchoredPosition = new Vector2(currentAnchoredPos.x, _originalSkillIconY);

        LeanTween.scale(_skillIconImage.rectTransform, _skillSwapPunchScale, _skillSwapTweenDuration)
            .setEasePunch();
        float targetY = _originalSkillIconY + _skillSwapPunchDistanceY;
        LeanTween.moveY(_skillIconImage.rectTransform, targetY, _skillSwapTweenDuration)
            .setEasePunch();
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