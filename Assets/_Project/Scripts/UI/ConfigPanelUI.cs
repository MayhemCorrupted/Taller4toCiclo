using System;
using System.Collections;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ConfigPanelUI : MonoBehaviour
{
    [SerializeField] private OptionsPanelUI _optionsPanelUI;
    [SerializeField] private PauseMenuUI _pausePanelUI;
    [SerializeField] private GameObject _configPanel;

    [SerializeField] private GameObject _graphicsSection;
    [SerializeField] private GameObject _audioSection;
    [SerializeField] private GameObject _controlsSection;
    [SerializeField] private GameObject _accessibilitySection;

    [SerializeField] private Button _returnButton;
    [SerializeField] private Animator _configAnimator;
    
    public Action<bool> IsOpen;
    private bool _isTransitioning = false;

    private readonly int _showHash = Animator.StringToHash("Show");
    private readonly int _hideHash = Animator.StringToHash("Hide");
    private void Awake()
    {
        if (_optionsPanelUI == null) _optionsPanelUI = FindAnyObjectByType<OptionsPanelUI>();
        if (_returnButton != null) _returnButton.onClick.AddListener(CloseConfigPanel);
    }
    private void OnEnable()
    {
        if (_pausePanelUI != null) _pausePanelUI.IsOpen += HandleOptionsClose;
    }
    private void OnDisable()
    {
        if (_pausePanelUI != null) _pausePanelUI.IsOpen -= HandleOptionsClose;
    }
    private void OnDestroy()
    {
        if (_returnButton != null) _returnButton.onClick.RemoveAllListeners();
    }
    private void HandleOptionsClose(bool isOptionsOpen)
    {
        if (!isOptionsOpen && _configPanel.activeInHierarchy && !_isTransitioning)
        {
            CloseConfigPanel();
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
        StartCoroutine(ClosingConfigPanel());
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
        _returnButton.interactable = state;
    }
    private void HideAllSubPanels()
    {
        if (_graphicsSection != null) _graphicsSection.SetActive(false);
        if (_audioSection != null) _audioSection.SetActive(false);
        if (_controlsSection != null) _controlsSection.SetActive(false);
        if (_accessibilitySection != null) _accessibilitySection.SetActive(false);
    }
    private IEnumerator OpeningConfigPanel(OptionSelected optionToOpen)
    {
        _isTransitioning = true;
        IsOpen?.Invoke(true);
        if (_configPanel != null) _configPanel.SetActive(true);
        HideAllSubPanels();
        switch (optionToOpen)
        {
            case OptionSelected.graphics:
                if (_graphicsSection != null) _graphicsSection.SetActive(true);
                break;
            case OptionSelected.audio:
                if (_audioSection != null) _audioSection.SetActive(true);
                break;
            case OptionSelected.controls:
                if (_controlsSection != null) _controlsSection.SetActive(true);
                break;
            case OptionSelected.accessibility:
                if (_accessibilitySection != null) _accessibilitySection.SetActive(true);
                break;
        }
        _configAnimator.SetTrigger(_showHash);
        EnableReturnButton(true);
        SelectButton(_returnButton);
        AnimatorStateInfo stateInfo = _configAnimator.GetCurrentAnimatorStateInfo(0);
        yield return new WaitForSecondsRealtime(stateInfo.length);
        _isTransitioning = false;
    }
    private IEnumerator ClosingConfigPanel()
    {
        _isTransitioning = true;
        IsOpen?.Invoke(false);
        if (_configAnimator != null)
        {
            _configAnimator.SetTrigger(_hideHash);
            yield return null;

            AnimatorStateInfo stateInfo = _configAnimator.GetCurrentAnimatorStateInfo(0);
            yield return new WaitForSecondsRealtime(stateInfo.length);
        }

        EnableReturnButton(false);
        HideAllSubPanels();
        
        _optionsPanelUI.EnableSectionButtons(true);
        _optionsPanelUI.EnableReturnButton(true);
        _optionsPanelUI.RestoreSelectedButton();
        if (_configPanel != null) _configPanel.SetActive(false);
        _isTransitioning = false;
    }
}