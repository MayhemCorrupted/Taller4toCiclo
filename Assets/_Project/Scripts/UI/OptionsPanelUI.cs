using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class OptionsMenuUI : MonoBehaviour, IPanel
{
    [SerializeField] private PauseMenuUI _pauseMenuUI;

    [SerializeField] private GameObject _categoryContainer; 
    [SerializeField] private GameObject _configContainer;

    [SerializeField] private Button _graphicsButton;
    [SerializeField] private Button _audioButton;
    [SerializeField] private Button _controlsButton;
    [SerializeField] private Button _accessibilityButton;
    [SerializeField] private Button _returnToMainPanelButton;

    [SerializeField] private Button _returnToOptionsButton;

    [SerializeField] private GameObject _graphicsSection;
    [SerializeField] private GameObject _audioSection;
    [SerializeField] private GameObject _controlsSection;
    [SerializeField] private GameObject _accessibilitySection;

    [SerializeField] private Animator _optionsAnimator;
    [SerializeField] private Animator _configAnimator;

    private readonly int _showHash = Animator.StringToHash("Show");
    private readonly int _hideHash = Animator.StringToHash("Hide");
    private void Awake()
    {
        if (_pauseMenuUI == null) _pauseMenuUI = FindAnyObjectByType<PauseMenuUI>();

        if (_graphicsButton != null) _graphicsButton.onClick.AddListener(() => OpenSpecificConfig(_graphicsSection));
        if (_audioButton != null) _audioButton.onClick.AddListener(() => OpenSpecificConfig(_audioSection));
        if (_controlsButton != null) _controlsButton.onClick.AddListener(() => OpenSpecificConfig(_controlsSection));
        if (_accessibilityButton != null) _accessibilityButton.onClick.AddListener(() => OpenSpecificConfig(_accessibilitySection));

        if (_returnToMainPanelButton != null) _returnToMainPanelButton.onClick.AddListener(ReturnToPauseMenu);
        if (_returnToOptionsButton != null) _returnToOptionsButton.onClick.AddListener(ReturnToCategories);
    }
    private void OnEnable()
    {
        StartCoroutine(OpeningPanel());
    }
    private void OnDestroy()
    {
        if (_graphicsButton != null) _graphicsButton.onClick.RemoveAllListeners();
        if (_audioButton != null) _audioButton.onClick.RemoveAllListeners();
        if (_controlsButton != null) _controlsButton.onClick.RemoveAllListeners();
        if (_accessibilityButton != null) _accessibilityButton.onClick.RemoveAllListeners();

        if (_returnToMainPanelButton != null) _returnToMainPanelButton.onClick.RemoveAllListeners();
        if (_returnToOptionsButton != null) _returnToOptionsButton.onClick.RemoveAllListeners();
    }
    public void OpenPanel()
    {
        gameObject.SetActive(true);
    }
    public void ClosePanel()
    {
        StartCoroutine(ClosingPanel());
    }
    private void ReturnToPauseMenu()
    {
        ClosePanel();
    }
    private void OpenSpecificConfig(GameObject panelToOpen)
    {
        StartCoroutine(OpeningConfigPanel(panelToOpen));
    }
    private void ReturnToCategories()
    {
        StartCoroutine(ClosingConfigPanel());
    }
    private void HideAllSubPanels()
    {
        if (_graphicsSection != null) _graphicsSection.SetActive(false);
        if (_audioSection != null) _audioSection.SetActive(false);
        if (_controlsSection != null) _controlsSection.SetActive(false);
        if (_accessibilitySection != null) _accessibilitySection.SetActive(false);
    }

    private void SelectButton(Button btn)
    {
        if (btn != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(btn.gameObject);
        }
    }
    private IEnumerator OpeningPanel()
    {
        if (_categoryContainer != null) _categoryContainer.SetActive(true);
        if (_configContainer != null) _configContainer.SetActive(false);
        HideAllSubPanels();

        if (_optionsAnimator != null) _optionsAnimator.SetTrigger(_showHash);

        yield return null;
        SelectButton(_graphicsButton);
    }
    private IEnumerator ClosingPanel()
    {
        if (_optionsAnimator != null)
        {
            _optionsAnimator.SetTrigger(_hideHash);
            yield return null;

            AnimatorStateInfo stateInfo = _optionsAnimator.GetCurrentAnimatorStateInfo(0);
            yield return new WaitForSecondsRealtime(stateInfo.length);
        }

        if (_pauseMenuUI != null) _pauseMenuUI.SetMainButtonsState(true);
        gameObject.SetActive(false);
    }
    private IEnumerator OpeningConfigPanel(GameObject panelToOpen)
    {
        if (_categoryContainer != null) _categoryContainer.SetActive(false);
        if (_configContainer != null) _configContainer.SetActive(true);

        HideAllSubPanels();
        if (panelToOpen != null) panelToOpen.SetActive(true);

        if (_configAnimator != null)
        {
            _configAnimator.SetTrigger(_showHash);
            yield return null;
        }

        SelectButton(_returnToOptionsButton);
    }
    private IEnumerator ClosingConfigPanel()
    {
        if (_configAnimator != null)
        {
            _configAnimator.SetTrigger(_hideHash);
            yield return null;

            AnimatorStateInfo stateInfo = _configAnimator.GetCurrentAnimatorStateInfo(0);
            yield return new WaitForSecondsRealtime(stateInfo.length);
        }

        if (_configContainer != null) _configContainer.SetActive(false);
        HideAllSubPanels();
        if (_categoryContainer != null) _categoryContainer.SetActive(true);

        SelectButton(_graphicsButton);
    }
}
