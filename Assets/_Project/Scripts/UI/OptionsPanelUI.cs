using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public enum OptionSelected { graphics, audio, controls, accessibility }
public class OptionsPanelUI : MonoBehaviour
{
    [Header("Referencias de Paneles")]
    [SerializeField] private PauseMenuUI _pauseMenuUI;
    [SerializeField] private ExitPanelUI _exitPanelUI;
    [SerializeField] private ConfigPanelUI _configPanelUI;
    [SerializeField] private GameObject _optionsPanel;
    [Header("Botones de Sección")]
    [SerializeField] private Button _graphicsSectionButton;
    [SerializeField] private Button _audioSectionButton;
    [SerializeField] private Button _controlsSectionButton;
    [SerializeField] private Button _accessibilitySectionButton;
    [Header("Botones de Navegación")]
    [SerializeField] private Button _optionsButton;
    [SerializeField] private Button _returnButton;

    [Header("LeanTween Slide Settings")]
    [SerializeField] private float _transitionDuration = 0.35f;
    [SerializeField] private float _fastReturnBtnDuration = 0.12f;
    [SerializeField] private LeanTweenType _easeIn = LeanTweenType.easeOutQuint;
    [SerializeField] private LeanTweenType _easeOut = LeanTweenType.easeInOutQuint;
    public Action<bool> IsOpen;
    private bool _isTransitioning = false;
    private RectTransform _panelRect;
    private Vector2 _targetPos;
    private Vector3 _returnBtnDefaultScale = Vector3.one;
    private void Awake()
    {
        if (_pauseMenuUI == null) _pauseMenuUI = FindAnyObjectByType<PauseMenuUI>();
        if (_exitPanelUI == null) _exitPanelUI = FindAnyObjectByType<ExitPanelUI>();
        if (_configPanelUI == null) _configPanelUI = FindAnyObjectByType<ConfigPanelUI>();

        if (_optionsPanel != null)
        {
            _panelRect = _optionsPanel.GetComponent<RectTransform>();
            _targetPos = _panelRect.anchoredPosition;
            _optionsPanel.SetActive(false);
        }

        if (_returnButton != null)
            _returnBtnDefaultScale = _returnButton.transform.localScale;

        if (_graphicsSectionButton != null) _graphicsSectionButton.onClick.AddListener(() => OpenSpecificConfig(OptionSelected.graphics));
        if (_audioSectionButton != null) _audioSectionButton.onClick.AddListener(() => OpenSpecificConfig(OptionSelected.audio));
        if (_controlsSectionButton != null) _controlsSectionButton.onClick.AddListener(() => OpenSpecificConfig(OptionSelected.controls));
        if (_accessibilitySectionButton != null) _accessibilitySectionButton.onClick.AddListener(() => OpenSpecificConfig(OptionSelected.accessibility));

        if (_optionsButton != null) _optionsButton.onClick.AddListener(OpenPanel);
        if (_returnButton != null) _returnButton.onClick.AddListener(ClosePanel);
    }
    private void OnEnable()
    {
        EnableOptionsButton(true);
        if (_pauseMenuUI != null) _pauseMenuUI.IsOpen += EscapeClosePanel;
    }
    private void OnDisable()
    {
        if (_pauseMenuUI != null) _pauseMenuUI.IsOpen -= EscapeClosePanel;
    }
    private void OnDestroy()
    {
        if (_graphicsSectionButton != null) _graphicsSectionButton.onClick.RemoveAllListeners();
        if (_audioSectionButton != null) _audioSectionButton.onClick.RemoveAllListeners();
        if (_controlsSectionButton != null) _controlsSectionButton.onClick.RemoveAllListeners();
        if (_accessibilitySectionButton != null) _accessibilitySectionButton.onClick.RemoveAllListeners();

        if (_optionsButton != null) _optionsButton.onClick.RemoveAllListeners();
        if (_returnButton != null) _returnButton.onClick.RemoveAllListeners();
    }
    private void EscapeClosePanel(bool isPauseOpen)
    {
        if (!isPauseOpen && _optionsPanel != null && _optionsPanel.activeInHierarchy)
        {
            StopAllCoroutines();
            StartCoroutine(ClosingPanel(restoreParentFocus: false));
        }
    }
    public void OpenPanel()
    {
        if (_isTransitioning) return;
        StartCoroutine(OpeningPanel());
    }
    public void ClosePanel()
    {
        if (_isTransitioning) return;
        StartCoroutine(ClosingPanel(restoreParentFocus: true));
    }
    private void OpenSpecificConfig(OptionSelected optionToOpen)
    {
        if (_isTransitioning) return;

        SetReturnButtonFastVisibility(false);
        EnableSectionButtons(false);
        EnableOptionsButton(false);

        _configPanelUI.OpenConfigPanel(optionToOpen);
    }
    public void OnConfigClosed()
    {
        SetReturnButtonFastVisibility(true);
        EnableSectionButtons(true);
        RestoreSelectedButton();
    }
    public void SetReturnButtonFastVisibility(bool visible)
    {
        if (_returnButton == null) return;

        LeanTween.cancel(_returnButton.gameObject);
        _returnButton.interactable = visible;

        if (visible)
        {
            _returnButton.gameObject.SetActive(true);
            _returnButton.transform.localScale = Vector3.zero;
            LeanTween.scale(_returnButton.gameObject, _returnBtnDefaultScale, _fastReturnBtnDuration)
                .setEaseOutBack()
                .setIgnoreTimeScale(true);
        }
        else
        {
            LeanTween.scale(_returnButton.gameObject, Vector3.zero, _fastReturnBtnDuration)
                .setEaseInQuint()
                .setIgnoreTimeScale(true)
                .setOnComplete(() =>
                {
                    if (_returnButton != null) _returnButton.gameObject.SetActive(false);
                });
        }
    }
    public void RestoreSelectedButton()
    {
        SelectButton(_graphicsSectionButton);
    }
    private void SelectButton(Button btn)
    {
        if (btn != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(btn.gameObject);
        }
    }
    public void EnableReturnButton(bool state)
    {
        if (_returnButton != null) _returnButton.interactable = state;
    }
    public void EnableOptionsButton(bool state)
    {
        if (_optionsButton != null) _optionsButton.interactable = state;
    }
    public void EnableSectionButtons(bool state)
    {
        if (_audioSectionButton != null) _audioSectionButton.interactable = state;
        if (_graphicsSectionButton != null) _graphicsSectionButton.interactable = state;
        if (_accessibilitySectionButton != null) _accessibilitySectionButton.interactable = state;
        if (_controlsSectionButton != null) _controlsSectionButton.interactable = state;
    }
    private float GetHiddenLeftX(RectTransform rect)
    {
        if (rect == null) return -1000f;
        Canvas canvas = rect.GetComponentInParent<Canvas>();
        float canvasWidth = canvas != null ? canvas.GetComponent<RectTransform>().rect.width : 2560f;
        float anchorXInCanvas = canvasWidth * rect.anchorMin.x;
        float rightEdgeOffset = rect.rect.width * (1f - rect.pivot.x);
        return -anchorXInCanvas - rightEdgeOffset - 20f;
    }
    private IEnumerator OpeningPanel()
    {
        _isTransitioning = true;
        IsOpen?.Invoke(true);

        if (_optionsPanel != null) _optionsPanel.SetActive(true);

        if (_pauseMenuUI != null) _pauseMenuUI.EnableResumeButton(false);
        if (_exitPanelUI != null) _exitPanelUI.EnableQuitButton(false);
        EnableOptionsButton(false);

        if (_returnButton != null)
        {
            LeanTween.cancel(_returnButton.gameObject);
            _returnButton.gameObject.SetActive(true);
            _returnButton.transform.localScale = _returnBtnDefaultScale;
            _returnButton.interactable = true;
        }

        EnableSectionButtons(true);
        SelectButton(_graphicsSectionButton);

        if (_panelRect != null)
        {
            LeanTween.cancel(_panelRect.gameObject);
            float hiddenX = GetHiddenLeftX(_panelRect);
            _panelRect.anchoredPosition = new Vector2(hiddenX, _targetPos.y);

            LeanTween.moveX(_panelRect, _targetPos.x, _transitionDuration)
                .setEase(_easeIn)
                .setIgnoreTimeScale(true);
        }

        yield return new WaitForSecondsRealtime(_transitionDuration);
        _isTransitioning = false;
    }
    private IEnumerator ClosingPanel(bool restoreParentFocus)
    {
        _isTransitioning = true;
        IsOpen?.Invoke(false);
        EnableReturnButton(false);
        EnableSectionButtons(false);

        if (_panelRect != null)
        {
            LeanTween.cancel(_panelRect.gameObject);
            float hiddenX = GetHiddenLeftX(_panelRect);

            LeanTween.moveX(_panelRect, hiddenX, _transitionDuration)
                .setEase(_easeOut)
                .setIgnoreTimeScale(true);
        }

        yield return new WaitForSecondsRealtime(_transitionDuration);

        EnableOptionsButton(true);
        if (restoreParentFocus)
        {
            if (_pauseMenuUI != null) _pauseMenuUI.EnableResumeButton(true);
            if (_exitPanelUI != null) _exitPanelUI.EnableQuitButton(true);
            SelectButton(_optionsButton);
        }

        if (_optionsPanel != null) _optionsPanel.SetActive(false);
        _isTransitioning = false;
    }
}