using System;
using UnityEngine;
using UnityEngine.UI;
[Serializable] public class DelayedHealthBar
{
    [Header("Imágenes (Filled Horizontal)")]
    [Tooltip("Barra roja principal que refleja la vida actual.")]
    [SerializeField] private Image _redFillImage;

    [Tooltip("Barra blanca secundaria ubicada debajo de la roja que muestra el daño reciente.")]
    [SerializeField] private Image _whiteTrailImage;

    [Header("Tiempos y Velocidades")]
    [Tooltip("Si está activo, la barra roja baja al instante al recibir daño.")]
    [SerializeField] private bool _instantRedDrop = true;
    [Min(1f)][SerializeField] private float _redLerpSpeed = 25f;

    [Tooltip("Segundos que la barra blanca se mantiene quieta tras cada impacto recibido.")]
    [Min(0f)][SerializeField] private float _trailDelay = 0.85f;

    [Tooltip("Velocidad (en unidades de fill por segundo) con la que baja la barra blanca tras la pausa.")]
    [Min(0.05f)][SerializeField] private float _whiteDrainSpeed = 0.45f;

    private float _targetFill = 1f;
    private float _whiteFill = 1f;
    private float _delayTimer = 0f;
    /// <summary>
    /// Fija ambas barras al instante (usar al inicializar, reaparecer o cambiar de objetivo).
    /// </summary>
    public void SetInstant(float normalizedHealth)
    {
        normalizedHealth = Mathf.Clamp01(normalizedHealth);
        _targetFill = normalizedHealth;
        _whiteFill = normalizedHealth;
        _delayTimer = 0f;

        if (_redFillImage != null)
            _redFillImage.fillAmount = normalizedHealth;

        if (_whiteTrailImage != null)
            _whiteTrailImage.fillAmount = normalizedHealth;
    }
    /// <summary>
    /// Actualiza la vida objetivo. Si bajó respecto al valor anterior, congela la barra blanca y reinicia la espera.
    /// </summary>
    public void SetTarget(float normalizedHealth)
    {
        normalizedHealth = Mathf.Clamp01(normalizedHealth);

        if (normalizedHealth < _targetFill - 0.0001f)
        {
            float currentVisualRed = _redFillImage != null ? _redFillImage.fillAmount : _targetFill;
            _whiteFill = Mathf.Max(_whiteFill, currentVisualRed);
            _delayTimer = _trailDelay;
        }

        _targetFill = normalizedHealth;
    }
    /// <summary>
    /// Actualiza la interpolación de ambas barras en cada frame.
    /// </summary>
    public void Tick(float deltaTime)
    {
        float currentRed = _targetFill;

        if (_redFillImage != null)
        {
            currentRed = _instantRedDrop
                ? _targetFill
                : Mathf.Lerp(_redFillImage.fillAmount, _targetFill, deltaTime * _redLerpSpeed);

            if (Mathf.Abs(currentRed - _targetFill) < 0.001f)
                currentRed = _targetFill;

            _redFillImage.fillAmount = currentRed;
        }

        if (_whiteFill < currentRed)
        {
            _whiteFill = currentRed;
            _delayTimer = 0f;
        }
        else if (_whiteFill > _targetFill)
        {
            if (_delayTimer > 0f)
            {
                _delayTimer -= deltaTime;
            }
            else
            {
                _whiteFill = Mathf.MoveTowards(_whiteFill, _targetFill, _whiteDrainSpeed * deltaTime);
            }
        }

        if (_whiteTrailImage != null)
        {
            _whiteTrailImage.fillAmount = _whiteFill;
        }
    }
}