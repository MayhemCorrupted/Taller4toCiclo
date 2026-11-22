using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public enum SectionOpened { Exit, Options, Configurations, Main }
public class PauseMenuUI : MonoBehaviour, IPanel
{
    [Header("Contenedor Principal (Fade)")]
    [SerializeField] private GameObject _mainContainer;
    [SerializeField] private CanvasGroup _mainCanvasGroup;

    [Header("Contenedor de Selección Principal (Slide)")]
    [SerializeField] private GameObject _selectionContainer;

    [Header("Sub-Paneles Apilables")]
    [SerializeField] private OptionsPanelUI _optionsPanelUI;
    [SerializeField] private ConfigPanelUI _configPanelUI;
    [SerializeField] private ExitPanelUI _exitPanelUI;

    [Header("Botones de Selection Container")]
    [SerializeField] private Button _resumeButton;
    [SerializeField] private Button _optionsButton;
    [SerializeField] private Button _quitButton;

    [Header("Animación (Fade y Slide)")]
    [SerializeField] private float _fadeDuration = 0.25f;
    [SerializeField] private float _slideDuration = 0.3f;
    [SerializeField] private bool _slideFromRight = false;
    [SerializeField] private LeanTweenType _easeIn = LeanTweenType.easeOutQuint;
    [SerializeField] private LeanTweenType _easeOut = LeanTweenType.easeInOutQuint;
    public Action<bool> IsOpen;
    public SectionOpened CurrentSection { get; private set; } = SectionOpened.Main;
    public bool IsMenuOpen => _isMenuOpen;

    private bool _isMenuOpen = false;
    private bool _isTransitioning = false;
    private RectTransform _selectionRect;
    private Vector2 _selectionTargetPos;
    private void Awake()
    {
        if (_optionsPanelUI == null) _optionsPanelUI = GetComponentInChildren<OptionsPanelUI>(true);
        if (_configPanelUI == null) _configPanelUI = GetComponentInChildren<ConfigPanelUI>(true);
        if (_exitPanelUI == null) _exitPanelUI = GetComponentInChildren<ExitPanelUI>(true);

        if (_mainContainer != null)
        {
            if (_mainCanvasGroup == null)
                _mainCanvasGroup = _mainContainer.GetComponent<CanvasGroup>();
            if (_mainCanvasGroup == null)
                _mainCanvasGroup = _mainContainer.AddComponent<CanvasGroup>();

            _mainCanvasGroup.alpha = 0f;
            _mainContainer.SetActive(false);
        }

        if (_selectionContainer != null)
        {
            _selectionRect = _selectionContainer.GetComponent<RectTransform>();
            _selectionTargetPos = _selectionRect.anchoredPosition;
            _selectionContainer.SetActive(false);
        }

        if (_resumeButton != null) _resumeButton.onClick.AddListener(ResumeGame);
        if (_optionsButton != null) _optionsButton.onClick.AddListener(OpenOptions);
        if (_quitButton != null) _quitButton.onClick.AddListener(OpenExit);

        EnforceLayerOrder();
        IsOpen?.Invoke(false);
    }
    private void OnDestroy()
    {
        if (_resumeButton != null) _resumeButton.onClick.RemoveAllListeners();
        if (_optionsButton != null) _optionsButton.onClick.RemoveAllListeners();
        if (_quitButton != null) _quitButton.onClick.RemoveAllListeners();

        if (_mainContainer != null) LeanTween.cancel(_mainContainer);
        if (_selectionRect != null) LeanTween.cancel(_selectionRect.gameObject);
    }
    /// <summary>
    /// Mantiene el orden de profundidad de atrás hacia adelante:
    /// Config (fondo) -> Exit / Options (medio) -> Selection (frente).
    /// </summary>
    public void EnforceLayerOrder()
    {
        if (_configPanelUI != null && _configPanelUI.PanelRect != null)
            _configPanelUI.PanelRect.SetAsLastSibling();

        if (_exitPanelUI != null && _exitPanelUI.PanelRect != null)
            _exitPanelUI.PanelRect.SetAsLastSibling();

        if (_optionsPanelUI != null && _optionsPanelUI.PanelRect != null)
            _optionsPanelUI.PanelRect.SetAsLastSibling();

        if (_selectionRect != null)
            _selectionRect.SetAsLastSibling();
    }
    public void OpenPanel()
    {
        if (_isTransitioning || _isMenuOpen) return;
        StartCoroutine(OpenMenuRoutine());
    }

    public void ClosePanel()
    {
        if (_isTransitioning || !_isMenuOpen) return;
        StartCoroutine(CloseMenuRoutine());
    }
    private void ResumeGame()
    {
        if (_isTransitioning || !_isMenuOpen) return;

        if (UIManager.Instance != null)
            UIManager.Instance.SetGameState(GameState.Gameplay);

        ClosePanel();
    }
    public void OpenOptions()
    {
        if (_isTransitioning || !_isMenuOpen || _optionsPanelUI == null) return;
        if (CurrentSection == SectionOpened.Options) return;

        StartCoroutine(OpenOptionsRoutine());
    }
    public void OpenExit()
    {
        if (_isTransitioning || !_isMenuOpen || _exitPanelUI == null) return;
        if (CurrentSection == SectionOpened.Exit) return;

        StartCoroutine(OpenExitRoutine());
    }
    public void OpenConfigurations(OptionSelected optionToOpen)
    {
        if (_isTransitioning || !_isMenuOpen || _configPanelUI == null) return;

        if (CurrentSection == SectionOpened.Configurations)
        {
            _configPanelUI.SwitchSection(optionToOpen);
            return;
        }

        if (CurrentSection != SectionOpened.Options) return;
        StartCoroutine(OpenConfigurationsRoutine(optionToOpen));
    }
    public void ReturnToOptions()
    {
        if (_isTransitioning || !_isMenuOpen || CurrentSection != SectionOpened.Configurations) return;
        if (_optionsPanelUI == null || _configPanelUI == null) return;

        StartCoroutine(CloseConfigurationsRoutine());
    }
    public void ReturnToMain()
    {
        if (_isTransitioning || !_isMenuOpen || CurrentSection == SectionOpened.Main) return;
        StartCoroutine(CloseTopToMainRoutine());
    }
    private IEnumerator OpenMenuRoutine()
    {
        _isTransitioning = true;
        _isMenuOpen = true;
        CurrentSection = SectionOpened.Main;
        IsOpen?.Invoke(true);

        if (_optionsPanelUI != null) _optionsPanelUI.HideImmediate();
        if (_configPanelUI != null) _configPanelUI.HideImmediate();
        if (_exitPanelUI != null) _exitPanelUI.HideImmediate();

        if (_mainContainer != null)
        {
            _mainContainer.SetActive(true);
            if (_mainCanvasGroup != null)
            {
                LeanTween.cancel(_mainContainer);
                _mainCanvasGroup.alpha = 0f;
                LeanTween.alphaCanvas(_mainCanvasGroup, 1f, _fadeDuration)
                    .setEaseOutCubic()
                    .setIgnoreTimeScale(true);
            }
        }

        EnforceLayerOrder();
        SlideInSelection(_resumeButton);

        float waitTime = Mathf.Max(_fadeDuration, _slideDuration);
        yield return new WaitForSecondsRealtime(waitTime);

        _isTransitioning = false;
    }
    private IEnumerator CloseMenuRoutine()
    {
        _isTransitioning = true;
        _isMenuOpen = false;
        IsOpen?.Invoke(false);

        float maxSlideOutTime = SlideOutSelection();

        if (_optionsPanelUI != null && _optionsPanelUI.IsVisible)
            maxSlideOutTime = Mathf.Max(maxSlideOutTime, _optionsPanelUI.SlideOut());

        if (_configPanelUI != null && _configPanelUI.IsVisible)
            maxSlideOutTime = Mathf.Max(maxSlideOutTime, _configPanelUI.SlideOut());

        if (_exitPanelUI != null && _exitPanelUI.IsVisible)
            maxSlideOutTime = Mathf.Max(maxSlideOutTime, _exitPanelUI.SlideOut());

        if (_mainContainer != null && _mainCanvasGroup != null)
        {
            LeanTween.cancel(_mainContainer);
            LeanTween.alphaCanvas(_mainCanvasGroup, 0f, _fadeDuration)
                .setEaseInCubic()
                .setIgnoreTimeScale(true);
        }

        float waitTime = Mathf.Max(_fadeDuration, maxSlideOutTime);
        yield return new WaitForSecondsRealtime(waitTime);

        if (_selectionContainer != null) _selectionContainer.SetActive(false);
        if (_optionsPanelUI != null) _optionsPanelUI.HideImmediate();
        if (_configPanelUI != null) _configPanelUI.HideImmediate();
        if (_exitPanelUI != null) _exitPanelUI.HideImmediate();
        if (_mainContainer != null) _mainContainer.SetActive(false);

        CurrentSection = SectionOpened.Main;
        _isTransitioning = false;
    }
    private IEnumerator OpenOptionsRoutine()
    {
        _isTransitioning = true;

        if (CurrentSection == SectionOpened.Configurations && _configPanelUI != null && _configPanelUI.IsVisible)
        {
            float outDuration = _configPanelUI.SlideOut();
            yield return new WaitForSecondsRealtime(outDuration);
            _configPanelUI.HideImmediate();

            CurrentSection = SectionOpened.Options;
            _optionsPanelUI.FocusLastSelected();
            _isTransitioning = false;
            yield break;
        }

        if (_exitPanelUI != null && _exitPanelUI.IsVisible)
        {
            float outDuration = _exitPanelUI.SlideOut();
            yield return new WaitForSecondsRealtime(outDuration);
            _exitPanelUI.HideImmediate();
        }

        EnforceLayerOrder();
        CurrentSection = SectionOpened.Options;
        float inDuration = _optionsPanelUI.SlideIn(restoreLastSelection: false);
        yield return new WaitForSecondsRealtime(inDuration);

        _isTransitioning = false;
    }
    private IEnumerator OpenExitRoutine()
    {
        _isTransitioning = true;

        float outDuration = 0f;
        if (_configPanelUI != null && _configPanelUI.IsVisible)
            outDuration = Mathf.Max(outDuration, _configPanelUI.SlideOut());
        if (_optionsPanelUI != null && _optionsPanelUI.IsVisible)
            outDuration = Mathf.Max(outDuration, _optionsPanelUI.SlideOut());

        if (outDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(outDuration);
            if (_configPanelUI != null) _configPanelUI.HideImmediate();
            if (_optionsPanelUI != null) _optionsPanelUI.HideImmediate();
        }

        EnforceLayerOrder();
        CurrentSection = SectionOpened.Exit;
        float inDuration = _exitPanelUI.SlideIn();
        yield return new WaitForSecondsRealtime(inDuration);

        _isTransitioning = false;
    }
    private IEnumerator OpenConfigurationsRoutine(OptionSelected optionToOpen)
    {
        _isTransitioning = true;

        EnforceLayerOrder();
        CurrentSection = SectionOpened.Configurations;
        float inDuration = _configPanelUI.SlideIn(optionToOpen);
        yield return new WaitForSecondsRealtime(inDuration);

        _isTransitioning = false;
    }
    private IEnumerator CloseConfigurationsRoutine()
    {
        _isTransitioning = true;

        float outDuration = _configPanelUI.SlideOut();
        yield return new WaitForSecondsRealtime(outDuration);
        _configPanelUI.HideImmediate();

        CurrentSection = SectionOpened.Options;
        _optionsPanelUI.FocusLastSelected();

        _isTransitioning = false;
    }
    private IEnumerator CloseTopToMainRoutine()
    {
        _isTransitioning = true;
        Button buttonToFocus = _resumeButton;

        if (CurrentSection == SectionOpened.Configurations)
        {
            float outDuration = 0f;
            if (_configPanelUI != null && _configPanelUI.IsVisible)
                outDuration = Mathf.Max(outDuration, _configPanelUI.SlideOut());
            if (_optionsPanelUI != null && _optionsPanelUI.IsVisible)
                outDuration = Mathf.Max(outDuration, _optionsPanelUI.SlideOut());

            yield return new WaitForSecondsRealtime(outDuration);
            if (_configPanelUI != null) _configPanelUI.HideImmediate();
            if (_optionsPanelUI != null) _optionsPanelUI.HideImmediate();
            buttonToFocus = _optionsButton;
        }
        else if (CurrentSection == SectionOpened.Options && _optionsPanelUI != null)
        {
            float outDuration = _optionsPanelUI.SlideOut();
            yield return new WaitForSecondsRealtime(outDuration);
            _optionsPanelUI.HideImmediate();
            buttonToFocus = _optionsButton;
        }
        else if (CurrentSection == SectionOpened.Exit && _exitPanelUI != null)
        {
            float outDuration = _exitPanelUI.SlideOut();
            yield return new WaitForSecondsRealtime(outDuration);
            _exitPanelUI.HideImmediate();
            buttonToFocus = _quitButton;
        }

        CurrentSection = SectionOpened.Main;
        SelectButton(buttonToFocus);
        _isTransitioning = false;
    }
    private void SlideInSelection(Button focusButton)
    {
        if (_selectionContainer == null) return;

        _selectionContainer.SetActive(true);
        SelectButton(focusButton);

        if (_selectionRect != null)
        {
            LeanTween.cancel(_selectionRect.gameObject);
            float hiddenX = GetOffscreenX(_selectionRect, _slideFromRight);
            _selectionRect.anchoredPosition = new Vector2(hiddenX, _selectionTargetPos.y);

            LeanTween.moveX(_selectionRect, _selectionTargetPos.x, _slideDuration)
                .setEase(_easeIn)
                .setIgnoreTimeScale(true);
        }
    }
    private float SlideOutSelection()
    {
        if (_selectionContainer == null || !_selectionContainer.activeSelf || _selectionRect == null)
            return 0f;

        LeanTween.cancel(_selectionRect.gameObject);
        float hiddenX = GetOffscreenX(_selectionRect, _slideFromRight);

        LeanTween.moveX(_selectionRect, hiddenX, _slideDuration)
            .setEase(_easeOut)
            .setIgnoreTimeScale(true);

        return _slideDuration;
    }
    public static void SelectButton(Button btn)
    {
        if (btn != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(btn.gameObject);
        }
    }
    public static float GetOffscreenX(RectTransform rect, bool fromRight = false, float margin = 40f)
    {
        if (rect == null) return fromRight ? 2000f : -2000f;

        Canvas rootCanvas = rect.GetComponentInParent<Canvas>();
        if (rootCanvas != null) rootCanvas = rootCanvas.rootCanvas;

        RectTransform parentRect = rect.parent as RectTransform;
        if (rootCanvas == null || parentRect == null)
            return fromRight ? 2000f : -2000f;

        RectTransform canvasRect = rootCanvas.GetComponent<RectTransform>();
        float anchorRefX = rect.localPosition.x - rect.anchoredPosition.x;

        if (!fromRight)
        {
            Vector3 canvasLeftWorld = canvasRect.TransformPoint(new Vector3(canvasRect.rect.xMin, 0f, 0f));
            float canvasLeftLocalX = parentRect.InverseTransformPoint(canvasLeftWorld).x;
            float rightEdgeFromPivot = rect.rect.width * (1f - rect.pivot.x);
            float targetPivotLocalX = canvasLeftLocalX - rightEdgeFromPivot - margin;
            return targetPivotLocalX - anchorRefX;
        }
        else
        {
            Vector3 canvasRightWorld = canvasRect.TransformPoint(new Vector3(canvasRect.rect.xMax, 0f, 0f));
            float canvasRightLocalX = parentRect.InverseTransformPoint(canvasRightWorld).x;
            float leftEdgeFromPivot = rect.rect.width * rect.pivot.x;
            float targetPivotLocalX = canvasRightLocalX + leftEdgeFromPivot + margin;
            return targetPivotLocalX - anchorRefX;
        }
    }
}