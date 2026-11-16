using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class GameOverUI : MonoBehaviour, IPanel
{
    [Header("Referencias de UI")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private GameObject _panelObject;

    [Header("Sub-Paneles de Resultado")]
    [Tooltip("Contenedor visual que se encenderá cuando el jugador gane.")]
    [SerializeField] private GameObject _victoryPanel;
    [Tooltip("Contenedor visual que se encenderá cuando el jugador pierda.")]
    [SerializeField] private GameObject _defeatPanel;

    [Header("Configuración de Fade In")]
    [Tooltip("Tiempo de espera opcional antes de empezar a oscurecer/mostrar la pantalla.")]
    [Min(0f)][SerializeField] private float _startDelay = 0f;
    [Tooltip("Duración del Fade In (Alpha 0 -> 1).")]
    [Min(0f)][SerializeField] private float _fadeInDuration = 1f;
    [Tooltip("Si es verdadero, el fade funcionará aunque Time.timeScale sea 0.")]
    [SerializeField] private bool _useUnscaledTime = true;

    [Header("Eventos")]
    [Tooltip("Se dispara justo al comenzar la secuencia de fin de partida.")]
    public UnityEvent OnGameOverOpenStart;
    [Tooltip("Se dispara cuando el Fade In termina (Alpha = 1) y los botones ya son interactuables.")]
    public UnityEvent OnGameOverFadeInComplete;
    public bool IsOpen => _isOpen;
    private Coroutine _fadeInCoroutine;
    private bool _isOpen;
    private void Awake()
    {
        if (_canvasGroup == null)
            _canvasGroup = GetComponentInChildren<CanvasGroup>(true);

        if (_panelObject == null && _canvasGroup != null)
            _panelObject = _canvasGroup.gameObject;

        ResetPanelVisuals();
    }
    public void OpenVictoryPanel()
    {
        OpenOutcomePanel(isVictory: true);
    }
    public void OpenDefeatPanel()
    {
        OpenOutcomePanel(isVictory: false);
    }
    public void OpenPanel()
    {
        OpenDefeatPanel();
    }
    private void OpenOutcomePanel(bool isVictory)
    {
        if (_isOpen) return;
        _isOpen = true;

        if (_victoryPanel != null)
            _victoryPanel.SetActive(isVictory);

        if (_defeatPanel != null)
            _defeatPanel.SetActive(!isVictory);

        if (UIManager.Instance != null && UIManager.Instance.CurrentState != GameState.GameOver)
        {
            UIManager.Instance.SetGameState(GameState.GameOver);
        }

        if (_fadeInCoroutine != null)
        {
            StopCoroutine(_fadeInCoroutine);
        }

        _fadeInCoroutine = StartCoroutine(FadeInRoutine());
    }
    public void ClosePanel()
    {
        if (_fadeInCoroutine != null)
        {
            StopCoroutine(_fadeInCoroutine);
            _fadeInCoroutine = null;
        }

        _isOpen = false;
        ResetPanelVisuals();

        if (UIManager.Instance != null && UIManager.Instance.CurrentState == GameState.GameOver)
        {
            UIManager.Instance.SetGameState(GameState.Gameplay);
        }
    }
    private IEnumerator FadeInRoutine()
    {
        if (_panelObject != null && !_panelObject.activeSelf)
            _panelObject.SetActive(true);

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = true;
        }

        OnGameOverOpenStart?.Invoke();

        if (_startDelay > 0f)
        {
            if (_useUnscaledTime)
                yield return new WaitForSecondsRealtime(_startDelay);
            else
                yield return new WaitForSeconds(_startDelay);
        }

        if (_canvasGroup != null && _fadeInDuration > 0f)
        {
            float elapsedTime = 0f;

            while (elapsedTime < _fadeInDuration)
            {
                elapsedTime += _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / _fadeInDuration);
                _canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
                yield return null;
            }
        }

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 1f;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
        }

        _fadeInCoroutine = null;
        OnGameOverFadeInComplete?.Invoke();
    }
    private void ResetPanelVisuals()
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }

        if (_victoryPanel != null)
            _victoryPanel.SetActive(false);

        if (_defeatPanel != null)
            _defeatPanel.SetActive(false);

        if (_panelObject != null && _panelObject != gameObject)
        {
            _panelObject.SetActive(false);
        }
    }
}