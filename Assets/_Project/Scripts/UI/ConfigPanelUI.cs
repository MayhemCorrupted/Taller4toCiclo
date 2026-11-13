using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public class ConfigPanelUI : MonoBehaviour
{
    [Header("Referencias de Paneles")]
    [SerializeField] private OptionsPanelUI _optionsPanelUI;
    [SerializeField] private PauseMenuUI _pausePanelUI;
    [SerializeField] private GameObject _configPanel;

    [Header("Secciones")]
    [SerializeField] private GameObject _graphicsSection;
    [SerializeField] private GameObject _audioSection;
    [SerializeField] private GameObject _controlsSection;
    [SerializeField] private GameObject _accessibilitySection;

    [Header("Navegación")]
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
        if (_optionsPanelUI == null) _optionsPanelUI = FindAnyObjectByType<OptionsPanelUI>();
        if (_pausePanelUI == null) _pausePanelUI = FindAnyObjectByType<PauseMenuUI>();
        if (_returnButton != null)
        {
            _returnBtnDefaultScale = _returnButton.transform.localScale;
            _returnButton.onClick.AddListener(CloseConfigPanel);
        }

        if (_configPanel != null)
        {
            _panelRect = _configPanel.GetComponent<RectTransform>();
            _targetPos = _panelRect.anchoredPosition;
            _configPanel.SetActive(false);
        }
    }
    private void OnEnable()
    {
        if (_pausePanelUI != null) _pausePanelUI.IsOpen += HandleGlobalClose;
        if (_optionsPanelUI != null) _optionsPanelUI.IsOpen += HandleGlobalClose;
    }
    private void OnDisable()
    {
        if (_pausePanelUI != null) _pausePanelUI.IsOpen -= HandleGlobalClose;
        if (_optionsPanelUI != null) _optionsPanelUI.IsOpen -= HandleGlobalClose;
    }
    private void OnDestroy()
    {
        if (_returnButton != null) _returnButton.onClick.RemoveAllListeners();
    }
    private void HandleGlobalClose(bool isParentOpen)
    {
        if (!isParentOpen && _configPanel != null && _configPanel.activeInHierarchy)
        {
            StopAllCoroutines();
            StartCoroutine(ClosingConfigPanel(restoreOptionsFocus: false));
        }
    }
    public void OpenConfigPanel(OptionSelected optionToOpen)
    {
        if (_isTransitioning) return;
        StartCoroutine(OpeningConfigPanel(optionToOpen));
    }
    public void CloseConfigPanel()
    {
        if (_isTransitioning) return;
        StartCoroutine(ClosingConfigPanel(restoreOptionsFocus: true));
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
    private void HideAllSubPanels()
    {
        if (_graphicsSection != null) _graphicsSection.SetActive(false);
        if (_audioSection != null) _audioSection.SetActive(false);
        if (_controlsSection != null) _controlsSection.SetActive(false);
        if (_accessibilitySection != null) _accessibilitySection.SetActive(false);
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
    private IEnumerator OpeningConfigPanel(OptionSelected optionToOpen)
    {
        _isTransitioning = true;
        IsOpen?.Invoke(true);

        if (_configPanel != null) _configPanel.SetActive(true);
        HideAllSubPanels();

        switch (optionToOpen)
        {
            case OptionSelected.graphics: if (_graphicsSection != null) _graphicsSection.SetActive(true); break;
            case OptionSelected.audio: if (_audioSection != null) _audioSection.SetActive(true); break;
            case OptionSelected.controls: if (_controlsSection != null) _controlsSection.SetActive(true); break;
            case OptionSelected.accessibility: if (_accessibilitySection != null) _accessibilitySection.SetActive(true); break;
        }

        if (_returnButton != null)
        {
            LeanTween.cancel(_returnButton.gameObject);
            _returnButton.gameObject.SetActive(true);
            _returnButton.transform.localScale = _returnBtnDefaultScale;
            _returnButton.interactable = true;
        }
        SelectButton(_returnButton);

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
    private IEnumerator ClosingConfigPanel(bool restoreOptionsFocus)
    {
        _isTransitioning = true;
        IsOpen?.Invoke(false);

        if (_returnButton != null)
        {
            LeanTween.cancel(_returnButton.gameObject);
            _returnButton.interactable = false;
            LeanTween.scale(_returnButton.gameObject, Vector3.zero, _fastReturnBtnDuration)
                .setEaseInQuint()
                .setIgnoreTimeScale(true)
                .setOnComplete(() =>
                {
                    if (_returnButton != null) _returnButton.gameObject.SetActive(false);
                });
        }

        if (_panelRect != null)
        {
            LeanTween.cancel(_panelRect.gameObject);
            float hiddenX = GetHiddenLeftX(_panelRect);

            LeanTween.moveX(_panelRect, hiddenX, _transitionDuration)
                .setEase(_easeOut)
                .setIgnoreTimeScale(true);
        }

        yield return new WaitForSecondsRealtime(_transitionDuration);

        HideAllSubPanels();

        if (restoreOptionsFocus && _optionsPanelUI != null)
        {
            _optionsPanelUI.OnConfigClosed();
        }

        if (_configPanel != null) _configPanel.SetActive(false);
        _isTransitioning = false;
    }
}