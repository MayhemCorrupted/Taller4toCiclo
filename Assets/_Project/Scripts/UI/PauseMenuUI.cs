using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PauseMenuUI : MonoBehaviour, IPanel
{
    [Header("Conexiones de UI")]
    [SerializeField] private OptionsMenuUI _optionsMenuUI; 
    [Header("Contenedores")]
    [SerializeField] private GameObject _mainPauseContainer;
    [SerializeField] private GameObject _quitContainer;
    [Header("Botones Principales")]
    [SerializeField] private Button _resumeButton;
    [SerializeField] private Button _optionsButton;
    [SerializeField] private Button _quitButton;
    [Header("Botones Salida")]
    [SerializeField] private Button _confirmQuitButton;
    [SerializeField] private Button _cancelQuitButton;
    [Header("Animación")]
    [SerializeField] private Animator _pauseAnimator;
    private readonly int _showHash = Animator.StringToHash("Show");
    private readonly int _hideHash = Animator.StringToHash("Hide");
    private void Awake()
    {
        if (_resumeButton != null) _resumeButton.onClick.AddListener(ResumeGame);
        if (_optionsButton != null) _optionsButton.onClick.AddListener(OpenOptions);
        if (_quitButton != null) _quitButton.onClick.AddListener(ShowQuitConfirmation);

        if (_confirmQuitButton != null) _confirmQuitButton.onClick.AddListener(QuitGame);
        if (_cancelQuitButton != null) _cancelQuitButton.onClick.AddListener(HideQuitConfirmation);

        if (_quitContainer != null) _quitContainer.SetActive(false);
        if (_optionsMenuUI != null) _optionsMenuUI.gameObject.SetActive(false);
    }
    private void OnDestroy()
    {
        if (_resumeButton != null) _resumeButton.onClick.RemoveAllListeners();
        if (_optionsButton != null) _optionsButton.onClick.RemoveAllListeners();
        if (_quitButton != null) _quitButton.onClick.RemoveAllListeners();
        if (_confirmQuitButton != null) _confirmQuitButton.onClick.RemoveAllListeners();
        if (_cancelQuitButton != null) _cancelQuitButton.onClick.RemoveAllListeners();
    }
    public void OpenPanel()
    {
        gameObject.SetActive(true);
        if (_mainPauseContainer != null) _mainPauseContainer.SetActive(true);
        if (_quitContainer != null) _quitContainer.SetActive(false);

        if (_optionsMenuUI != null) _optionsMenuUI.ClosePanel();

        if (_pauseAnimator != null) _pauseAnimator.SetTrigger(_showHash);

        SelectButton(_resumeButton);
        Time.timeScale = 0f;
    }
    public void ClosePanel()
    {
        if (_pauseAnimator != null) _pauseAnimator.SetTrigger(_hideHash);

        gameObject.SetActive(false);
        Time.timeScale = 1f;
    }

    private void ResumeGame()
    {
        UIManager.Instance.SetGameState(GameState.Gameplay);
        ClosePanel();
    }

    private void OpenOptions()
    {
        if (_mainPauseContainer != null) _mainPauseContainer.SetActive(false);
        if (_optionsMenuUI != null) _optionsMenuUI.OpenPanel();
    }
    public void ReturnFromOptions()
    {
        if (_mainPauseContainer != null) _mainPauseContainer.SetActive(true);
        SelectButton(_optionsButton);
    }
    private void ShowQuitConfirmation()
    {
        if (_mainPauseContainer != null) _mainPauseContainer.SetActive(false);
        if (_quitContainer != null) _quitContainer.SetActive(true);
        SelectButton(_cancelQuitButton);
    }
    private void HideQuitConfirmation()
    {
        if (_quitContainer != null) _quitContainer.SetActive(false);
        if (_mainPauseContainer != null) _mainPauseContainer.SetActive(true);
        SelectButton(_quitButton);
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
    private void SelectButton(Button btn)
    {
        if (btn != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(btn.gameObject);
        }
    }
}