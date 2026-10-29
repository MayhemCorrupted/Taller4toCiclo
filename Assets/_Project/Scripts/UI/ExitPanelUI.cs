using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ExitPanelUI : MonoBehaviour
{
    [SerializeField] private PauseMenuUI _pausePanelUI;
    [SerializeField] private OptionsPanelUI _optionsPanelUI;

    [SerializeField] private Animator _quitAnimator;
    
    [SerializeField] private GameObject _quitPanel;
    
    [SerializeField] private Button _quitButton;
    [SerializeField] private Button _leaveGameButton;
    [SerializeField] private Button _ContinueButton;
    
    [SerializeField] private string sceneName;

    private readonly int _hideHash = Animator.StringToHash("Hide");
    private readonly int _showHash = Animator.StringToHash("Show");

    private bool _isTransitioning = false;
    private void Start()
    {
        if (_pausePanelUI == null) _pausePanelUI = FindAnyObjectByType<PauseMenuUI>();
        if (_leaveGameButton != null) _leaveGameButton.onClick.AddListener(LeaveGame);
        if (_ContinueButton != null) _ContinueButton.onClick.AddListener(ClosePanel);
        if (_quitButton != null) _quitButton.onClick.AddListener(OpenPanel);
    }
    private void OnEnable()
    {
        EnableQuitButton(true);
        if (_pausePanelUI != null) _pausePanelUI.IsOpen += EscapeClosePanel;
    }
    private void OnDisable()
    {
        if (_pausePanelUI != null) _pausePanelUI.IsOpen -= EscapeClosePanel;
    }
    private void OnDestroy()
    {
        if (_leaveGameButton != null) _leaveGameButton.onClick.RemoveAllListeners();
        if (_ContinueButton != null) _ContinueButton.onClick.RemoveAllListeners();
    }
    private void EscapeClosePanel(bool isPauseOpen)
    {
        if (!isPauseOpen && _quitPanel.activeInHierarchy && !_isTransitioning)
        {
            StartCoroutine(ClosingPanel());
        }
    }
    private void LeaveGame()
    {
        if (_isTransitioning) return;
        if (!string.IsNullOrEmpty(sceneName)) SceneManager.LoadScene(sceneName);
        else Debug.LogWarning("Falta colocar el nombre de la escena, pero funciona el botón :)");
    }
    private void OpenPanel()
    {
        if (_isTransitioning) return;
        StartCoroutine(OpeningPanel());
    }
    private void ClosePanel()
    {
        if (_isTransitioning) return;
        StartCoroutine(ClosingPanel());
    }
    private void FirstSelectButton(Button btn)
    {
        if (btn != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(btn.gameObject);
        }
    }
    public void EnableQuitButton(bool state)
    {
        if (_quitButton != null) _quitButton.interactable = state;
    }
    private IEnumerator OpeningPanel()
    {
        _isTransitioning = true;
        EnableQuitButton(false);
        _quitPanel.SetActive(true);
        FirstSelectButton(_ContinueButton);
        _pausePanelUI.EnableResumeButton(false);
        _optionsPanelUI.EnableOptionsButton(false);
        _quitAnimator.SetTrigger(_showHash);
        yield return null;
        _isTransitioning = false;
    }
    private IEnumerator ClosingPanel()
    {
        _isTransitioning = true;
        _quitAnimator.SetTrigger(_hideHash);
        yield return null;
        AnimatorStateInfo stateInfo = _quitAnimator.GetCurrentAnimatorStateInfo(0);
        yield return new WaitForSecondsRealtime(stateInfo.length);
        EnableQuitButton(true);
        _pausePanelUI.EnableResumeButton(true);
        _optionsPanelUI.EnableOptionsButton(true);
        FirstSelectButton(_quitButton);
        _quitPanel.SetActive(false);
        _isTransitioning = false;
    }
}
