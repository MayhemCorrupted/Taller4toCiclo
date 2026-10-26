using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
public class OptionsMenuUI : MonoBehaviour, IPanel
{
    [SerializeField] private PauseMenuUI _pauseMenuUI;

    [SerializeField] private GameObject _categoryContainer;
    [SerializeField] private GameObject _configGlobalContainer;

    [SerializeField] private Button _graphicsButton;
    [SerializeField] private Button _audioButton;
    [SerializeField] private Button _controlsButton;
    [SerializeField] private Button _accessibilityButton;
    [SerializeField] private Button _returnToPauseButton;

    [SerializeField] private Button _returnToCategoryButton;

    [SerializeField] private GameObject _graphicsPanel;
    [SerializeField] private GameObject _audioPanel;
    [SerializeField] private GameObject _controlsPanel;
    [SerializeField] private GameObject _accessibilityPanel;

    [Header("Animación")]
    [SerializeField] private Animator _optionsAnimator;
    private readonly int _showHash = Animator.StringToHash("Show");
    private readonly int _hideHash = Animator.StringToHash("Hide");
    private void Awake()
    {
        if (_graphicsButton != null) _graphicsButton.onClick.AddListener(() => OpenSpecificConfig(_graphicsPanel));
        if (_audioButton != null) _audioButton.onClick.AddListener(() => OpenSpecificConfig(_audioPanel));
        if (_controlsButton != null) _controlsButton.onClick.AddListener(() => OpenSpecificConfig(_controlsPanel));
        if (_accessibilityButton != null) _accessibilityButton.onClick.AddListener(() => OpenSpecificConfig(_accessibilityPanel));

        if (_returnToPauseButton != null) _returnToPauseButton.onClick.AddListener(ReturnToPauseMenu);
        if (_returnToCategoryButton != null) _returnToCategoryButton.onClick.AddListener(ReturnToCategories);
    }
    private void OnDestroy()
    {
        if (_graphicsButton != null) _graphicsButton.onClick.RemoveAllListeners();
        if (_audioButton != null) _audioButton.onClick.RemoveAllListeners();
        if (_controlsButton != null) _controlsButton.onClick.RemoveAllListeners();
        if (_accessibilityButton != null) _accessibilityButton.onClick.RemoveAllListeners();

        if (_returnToPauseButton != null) _returnToPauseButton.onClick.RemoveAllListeners();
        if (_returnToCategoryButton != null) _returnToCategoryButton.onClick.RemoveAllListeners();
    }
    public void OpenPanel()
    {
        gameObject.SetActive(true);
        if (_optionsAnimator != null) _optionsAnimator.SetTrigger(_showHash);

        if (_categoryContainer != null) _categoryContainer.SetActive(true);
        if (_configGlobalContainer != null) _configGlobalContainer.SetActive(false);
        HideAllSubPanels();

        SelectButton(_graphicsButton);
    }
    public void ClosePanel()
    {
        if (_optionsAnimator != null) _optionsAnimator.SetTrigger(_hideHash);
        gameObject.SetActive(false);
    }
    private void OpenSpecificConfig(GameObject panelToOpen)
    {
        if (_categoryContainer != null) _categoryContainer.SetActive(false);

        if (_configGlobalContainer != null) _configGlobalContainer.SetActive(true);

        HideAllSubPanels();
        if (panelToOpen != null) panelToOpen.SetActive(true);

        SelectButton(_returnToCategoryButton);
    }
    private void ReturnToCategories()
    {
        if (_configGlobalContainer != null) _configGlobalContainer.SetActive(false);
        HideAllSubPanels();

        if (_categoryContainer != null) _categoryContainer.SetActive(true);

        SelectButton(_graphicsButton);
    }
    private void ReturnToPauseMenu()
    {
        ClosePanel();
        if (_pauseMenuUI != null) _pauseMenuUI.ReturnFromOptions();
    }
    private void HideAllSubPanels()
    {
        if (_graphicsPanel != null) _graphicsPanel.SetActive(false);
        if (_audioPanel != null) _audioPanel.SetActive(false);
        if (_controlsPanel != null) _controlsPanel.SetActive(false);
        if (_accessibilityPanel != null) _accessibilityPanel.SetActive(false);
    }
    private void SelectButton(Button btn)
    {
        if (btn != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(btn.gameObject);
        }
    }
}