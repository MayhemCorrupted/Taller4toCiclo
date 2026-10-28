using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System;

public class PauseMenuUI : MonoBehaviour, IPanel
{
    [SerializeField] private Animator _menuAnimator;

    [SerializeField] private GameObject _mainPauseContainer;
    [SerializeField] private GameObject _optionsContainer;
    [SerializeField] private GameObject _quitContainer;
    [SerializeField] private GameObject _ConfigContainer;

    [SerializeField] private Button _resumeButton;

    private readonly int _showHash = Animator.StringToHash("Show");
    private readonly int _hideHash = Animator.StringToHash("Hide");
    public Action<bool> IsOpen;
    private bool _isTransitioning = false;
    private void Awake()
    {
        IsOpen?.Invoke(false);
        if (_resumeButton != null) _resumeButton.onClick.AddListener(ResumeGame);
        if (_optionsContainer != null) _optionsContainer.SetActive(false);
        if (_quitContainer != null) _quitContainer.SetActive(false);
        if (_mainPauseContainer != null) _mainPauseContainer.SetActive(false);
        if (_ConfigContainer != null) _ConfigContainer.SetActive(false);
    }
    private void OnDestroy()
    {
        if (_resumeButton != null) _resumeButton.onClick.RemoveAllListeners();
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
    private void ResumeGame()
    {
        if (_isTransitioning) return;
        UIManager.Instance.SetGameState(GameState.Gameplay);
        ClosePanel();
    }
    public void EnableResumeButton(bool state)
    {
        if (_resumeButton != null) _resumeButton.interactable = state;
    }
    private void FirstSelectButton(Button btn)
    {
        if (btn != null )
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(btn.gameObject);
        }
    }
    private IEnumerator ClosingPanel()
    {
        _isTransitioning = true;
        IsOpen?.Invoke(false);
        _menuAnimator.SetTrigger(_hideHash);
        yield return null;
        AnimatorStateInfo stateInfo = _menuAnimator.GetCurrentAnimatorStateInfo(0);
        yield return new WaitForSeconds(stateInfo.length);
        if (_mainPauseContainer != null) _mainPauseContainer.SetActive(false);
        _isTransitioning = false;
    }
    private IEnumerator OpeningPanel()
    {
        _isTransitioning = true;
        IsOpen?.Invoke(true);
        if (_optionsContainer != null) _optionsContainer.SetActive(false);
        if (_quitContainer != null) _quitContainer.SetActive(false);
        if (_ConfigContainer != null) _ConfigContainer.SetActive(false);
        if (_mainPauseContainer != null) _mainPauseContainer.SetActive(true);
        EnableResumeButton(true);
        _menuAnimator.SetTrigger(_showHash);
        FirstSelectButton(_resumeButton);
        yield return null;
        _isTransitioning = false;
    }
}