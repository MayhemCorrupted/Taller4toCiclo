using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
public enum OptionSelected { graphics, audio, controls, accessibility }
public class OptionsPanelUI : MonoBehaviour
{
    [SerializeField] private PauseMenuUI _pauseMenuUI;
    [SerializeField] private ExitPanelUI _exitPanelUI;
    [SerializeField] private ConfigPanelUI _configPanelUI;

    [SerializeField] private GameObject _optionsPanel;

    [SerializeField] private Button _graphicsSectionButton;
    [SerializeField] private Button _audioSectionButton;
    [SerializeField] private Button _controlsSectionButton;
    [SerializeField] private Button _accessibilitySectionButton;

    [SerializeField] private Button _optionsButton;
    [SerializeField] private Button _returnButton; 
    [SerializeField] private Animator _optionsAnimator;

    public Action<bool> IsOpen;
    private bool _isTransitioning = false;

    private readonly int _showHash = Animator.StringToHash("Show");
    private readonly int _hideHash = Animator.StringToHash("Hide");
    private void Start()
    {
        if (_pauseMenuUI == null) _pauseMenuUI = FindAnyObjectByType<PauseMenuUI>();
        if (_exitPanelUI == null) _exitPanelUI = FindAnyObjectByType<ExitPanelUI>();
        if (_configPanelUI == null) _configPanelUI = FindAnyObjectByType<ConfigPanelUI>();

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

        if (_returnButton != null) _returnButton.onClick.RemoveAllListeners();
    }
    private void EscapeClosePanel(bool isPauseOpen)
    {
        if (!isPauseOpen && _optionsPanel.activeInHierarchy && !_isTransitioning)
        {
            StartCoroutine(ClosingPanel());
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
        StartCoroutine(ClosingPanel());
    }
    private void OpenSpecificConfig(OptionSelected optionToOpen)
    {
        if (_isTransitioning) return;
        EnableReturnButton(false);
        EnableSectionButtons(false);
        EnableOptionsButton(false);
        _configPanelUI.OpenConfigPanel(optionToOpen);
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
    private IEnumerator OpeningPanel()
    {
        _isTransitioning = true;
        IsOpen?.Invoke(true);
        _optionsPanel.SetActive(true);
        _optionsAnimator.SetTrigger(_showHash);
        _pauseMenuUI.EnableResumeButton(false);
        _exitPanelUI.EnableQuitButton(false);
        EnableOptionsButton(false);
        EnableReturnButton(true);
        EnableSectionButtons(true);
        SelectButton(_graphicsSectionButton);
        yield return null;
        _isTransitioning = false;
    }
    private IEnumerator ClosingPanel()
    {
        _isTransitioning = true;
        IsOpen?.Invoke(false);
        if (_optionsAnimator != null)
        {
            _optionsAnimator.SetTrigger(_hideHash);
            yield return null;

            AnimatorStateInfo stateInfo = _optionsAnimator.GetCurrentAnimatorStateInfo(0);
            yield return new WaitForSecondsRealtime(stateInfo.length);
        }

        EnableOptionsButton(true);
        _pauseMenuUI.EnableResumeButton(true);
        _exitPanelUI.EnableQuitButton(true);
        EnableOptionsButton(true);
        _optionsPanel.SetActive(false);
        _isTransitioning = false;
    }
}