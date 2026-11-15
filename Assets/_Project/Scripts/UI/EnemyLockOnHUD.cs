using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class EnemyLockOnHUD : MonoBehaviour
{
    [Header("Referencias UI Enemigo")]
    [Tooltip("Contenedor padre de la barra de vida en el Canvas principal.")]
    [SerializeField] private GameObject _enemyHudContainer;
    [Tooltip("Imagen configurada en Image Type = Filled.")]
    [SerializeField] private Image _enemyHealthFillImage;
    [Tooltip("Texto que mostrará el porcentaje de vida (ej: 85%).")]
    [SerializeField] private TextMeshProUGUI _healthPercentageText;
    [SerializeField] private float _lerpSpeed = 12f;

    private PlayerCombat _playerCombat;
    private readonly List<EntityCombat> _detectingScreenUIEnemies = new();
    private EntityCombat _displayedEnemy;
    private Health _displayedEnemyHealth;
    private float _targetFillAmount;
    private void Awake()
    {
        _playerCombat = FindAnyObjectByType<PlayerCombat>();

        if (_enemyHudContainer != null)
            _enemyHudContainer.SetActive(false);
    }
    private void OnEnable()
    {
        if (_playerCombat != null)
            _playerCombat.OnLockOnTargetChanged += HandleLockOnChanged;

        EntityCombat.OnAnyEnemyTargetDetected += HandleEnemyDetectedPlayer;
        EntityCombat.OnAnyEnemyTargetLost += HandleEnemyLostPlayer;
    }
    private void OnDisable()
    {
        if (_playerCombat != null)
            _playerCombat.OnLockOnTargetChanged -= HandleLockOnChanged;

        EntityCombat.OnAnyEnemyTargetDetected -= HandleEnemyDetectedPlayer;
        EntityCombat.OnAnyEnemyTargetLost -= HandleEnemyLostPlayer;
    }
    private void Update()
    {
        for (int i = _detectingScreenUIEnemies.Count - 1; i >= 0; i--)
        {
            EntityCombat enemy = _detectingScreenUIEnemies[i];
            if (enemy == null || !enemy.CanSeeTarget)
            {
                _detectingScreenUIEnemies.RemoveAt(i);
            }
        }

        RefreshDisplayedTarget();

        if (_displayedEnemyHealth == null || _enemyHudContainer == null || !_enemyHudContainer.activeSelf)
            return;

        UpdateHealthValues(instant: false);
    }
    private bool UsesScreenUI(EntityCombat enemy)
    {
        if (enemy == null) return false;
        return enemy.Data == null || enemy.Data.healthBarMode == EnemyHealthBarMode.ScreenUI;
    }
    private void HandleEnemyDetectedPlayer(EntityCombat enemy)
    {
        if (!UsesScreenUI(enemy)) return;

        if (!_detectingScreenUIEnemies.Contains(enemy))
            _detectingScreenUIEnemies.Add(enemy);

        RefreshDisplayedTarget();
    }
    private void HandleEnemyLostPlayer(EntityCombat enemy)
    {
        if (_detectingScreenUIEnemies.Remove(enemy))
        {
            RefreshDisplayedTarget();
        }
    }
    private void HandleLockOnChanged(Transform lockedTarget)
    {
        RefreshDisplayedTarget();
    }
    private void RefreshDisplayedTarget()
    {
        EntityCombat chosenEnemy = null;

        if (_playerCombat != null && _playerCombat.LockOnTarget != null)
        {
            EntityCombat lockedCombat = _playerCombat.LockOnTarget.GetComponentInParent<EntityCombat>();
            if (UsesScreenUI(lockedCombat))
            {
                chosenEnemy = lockedCombat;
            }
        }

        if (chosenEnemy == null && _detectingScreenUIEnemies.Count > 0)
        {
            chosenEnemy = _detectingScreenUIEnemies[0];
        }

        if (chosenEnemy == _displayedEnemy) return;

        _displayedEnemy = chosenEnemy;
        _displayedEnemyHealth = _displayedEnemy != null ? _displayedEnemy.GetComponent<Health>() : null;

        if (_displayedEnemyHealth != null && _displayedEnemyHealth.CurrentHealth > 0f)
        {
            if (_enemyHudContainer != null)
                _enemyHudContainer.SetActive(true);

            UpdateHealthValues(instant: true);
        }
        else if (_enemyHudContainer != null)
        {
            _enemyHudContainer.SetActive(false);
        }
    }
    private void UpdateHealthValues(bool instant)
    {
        if (_displayedEnemyHealth.MaxHealth <= 0f) return;

        _targetFillAmount = Mathf.Clamp01(_displayedEnemyHealth.CurrentHealth / _displayedEnemyHealth.MaxHealth);

        if (_enemyHealthFillImage != null)
        {
            _enemyHealthFillImage.fillAmount = instant
                ? _targetFillAmount
                : Mathf.Lerp(_enemyHealthFillImage.fillAmount, _targetFillAmount, Time.deltaTime * _lerpSpeed);
        }

        if (_healthPercentageText != null)
        {
            int percentage = _displayedEnemyHealth.CurrentHealth > 0f
                ? Mathf.Max(1, Mathf.RoundToInt(_targetFillAmount * 100f))
                : 0;

            _healthPercentageText.text = $"{percentage}%";
        }
    }
}