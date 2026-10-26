using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class PauseMenuUI : MonoBehaviour, IPanel
{
    [SerializeField] private Animator _menuAnimator;

    [SerializeField] private GameObject _mainPauseContainer;
    [SerializeField] private GameObject _optionsContainer;
    [SerializeField] private GameObject _quitContainer;

    [SerializeField] private Button _resumeButton;
    [SerializeField] private Button _optionsButton;
    [SerializeField] private Button _quitButton;

    private readonly int _showHash = Animator.StringToHash("Show");
    private readonly int _hideHash = Animator.StringToHash("Hide");
    private void Awake()
    {
        if (_resumeButton != null) _resumeButton.onClick.AddListener(ResumeGame);
        if (_optionsButton != null) _optionsButton.onClick.AddListener(OpenOptionsContainer);
        if (_quitButton != null) _quitButton.onClick.AddListener(OpenExitContainer);

        if (_optionsContainer != null) _optionsContainer.SetActive(false);
        if (_quitContainer != null) _quitContainer.SetActive(false);
        if (_mainPauseContainer != null) _mainPauseContainer.SetActive(false);
    }
    private void OnDestroy()
    {
        if (_resumeButton != null) _resumeButton.onClick.RemoveAllListeners();
        if (_optionsButton != null) _optionsButton.onClick.RemoveAllListeners();
        if (_quitButton != null) _quitButton.onClick.RemoveAllListeners();
    }
    public void OpenPanel()
    {
        StartCoroutine(OpeningPanel());
    }
    public void ClosePanel()
    {
        StartCoroutine(ClosingPanel());
    }
    private void ResumeGame()
    {
        UIManager.Instance.SetGameState(GameState.Gameplay);
        ClosePanel();
    }
    private void OpenExitContainer()
    {
        if (_quitContainer != null) _quitContainer.SetActive(true);
        SetMainButtonsState(false);
    }
    private void OpenOptionsContainer()
    {
        if (_optionsContainer != null) _optionsContainer.SetActive(true);
        SetMainButtonsState(false);
    }
    public void SetMainButtonsState(bool state)
    {
        if (_resumeButton != null) _resumeButton.interactable = state;
        if (_optionsButton != null) _optionsButton.interactable = state;
        if (_quitButton != null) _quitButton.interactable = state;
    }
    private void FirstSelectButton(Button btn)
    {
        if (btn != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(btn.gameObject);
        }
    }
    private IEnumerator ClosingPanel()
    {
        _menuAnimator.SetTrigger(_hideHash);
        yield return null;
        AnimatorStateInfo stateInfo = _menuAnimator.GetCurrentAnimatorStateInfo(0);
        yield return new WaitForSeconds(stateInfo.length);
        if (_mainPauseContainer != null) _mainPauseContainer.SetActive(false);
    }
    private IEnumerator OpeningPanel()
    {
        if (_optionsContainer != null) _optionsContainer.SetActive(false);
        if (_quitContainer != null) _quitContainer.SetActive(false);
        if (_mainPauseContainer != null) _mainPauseContainer.SetActive(true);
        SetMainButtonsState(true);
        _menuAnimator.SetTrigger(_showHash);
        FirstSelectButton(_resumeButton);
        yield return null;
    }
}