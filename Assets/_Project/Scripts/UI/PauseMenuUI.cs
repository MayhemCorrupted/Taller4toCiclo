using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PauseMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject _mainPausePanel;
    [SerializeField] private GameObject _optionsPanel;
    [SerializeField] private GameObject _quitConfirmationPopup;

    [SerializeField] private Button _resumeButton;
    [SerializeField] private Button _optionsButton;
    [SerializeField] private Button _quitButton;

    [SerializeField] private Button _confirmQuitButton;
    [SerializeField] private Button _cancelQuitButton;
    private void Awake()
    {
        if (_resumeButton != null) _resumeButton.onClick.AddListener(ResumeGame);
        if (_optionsButton != null) _optionsButton.onClick.AddListener(OpenOptions);
        if (_quitButton != null) _quitButton.onClick.AddListener(ShowQuitConfirmation);

        if (_confirmQuitButton != null) _confirmQuitButton.onClick.AddListener(QuitGame);
        if (_cancelQuitButton != null) _cancelQuitButton.onClick.AddListener(HideQuitConfirmation);

        if (_quitConfirmationPopup != null) _quitConfirmationPopup.SetActive(false);
    }
    private void OnDestroy()
    {
        if (_resumeButton != null) _resumeButton.onClick.RemoveAllListeners();
        if (_optionsButton != null) _optionsButton.onClick.RemoveAllListeners();
        if (_quitButton != null) _quitButton.onClick.RemoveAllListeners();
        if (_confirmQuitButton != null) _confirmQuitButton.onClick.RemoveAllListeners();
        if (_cancelQuitButton != null) _cancelQuitButton.onClick.RemoveAllListeners();
    }
    public void OpenMenu()
    {
        if (_mainPausePanel != null) _mainPausePanel.SetActive(true);
        if (_optionsPanel != null) _optionsPanel.SetActive(false);
        if (_quitConfirmationPopup != null) _quitConfirmationPopup.SetActive(false);

        if (_resumeButton != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(_resumeButton.gameObject);
        }
        Time.timeScale = 0f;
    }
    public void CloseMenu()
    {
        if (_mainPausePanel != null) _mainPausePanel.SetActive(false);
        Time.timeScale = 1f;
    }
    private void ResumeGame()
    {
        UIManager.Instance.SetGameState(GameState.Gameplay);
        CloseMenu();
    }
    private void OpenOptions()
    {
        if (_mainPausePanel != null) _mainPausePanel.SetActive(false);
        if (_optionsPanel != null) _optionsPanel.SetActive(true);
    }
    private void ShowQuitConfirmation()
    {
        if (_mainPausePanel != null) _mainPausePanel.SetActive(false);
        if (_quitConfirmationPopup != null) _quitConfirmationPopup.SetActive(true);

        if (_cancelQuitButton != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(_cancelQuitButton.gameObject);
        }
    }
    private void HideQuitConfirmation()
    {
        if (_quitConfirmationPopup != null) _quitConfirmationPopup.SetActive(false);
        if (_mainPausePanel != null) _mainPausePanel.SetActive(true);

        if (_quitButton != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(_quitButton.gameObject);
        }
    }
    private void QuitGame()
    {
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }
}