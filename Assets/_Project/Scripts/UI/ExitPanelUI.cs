using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ExitPanelUI : MonoBehaviour
{
    [SerializeField] private Animator _exitAnimator;
    [SerializeField] private PauseMenuUI _pausePanel;
    [SerializeField] private Button _leaveGameButton;
    [SerializeField] private Button _cancelButton;
    [SerializeField] private string sceneName;

    private readonly int _hideHash = Animator.StringToHash("Hide");
    private readonly int _showHash = Animator.StringToHash("Show");

    private void Awake()
    {
        if (_pausePanel == null) _pausePanel = FindAnyObjectByType<PauseMenuUI>();
        if (_leaveGameButton != null) _leaveGameButton.onClick.AddListener(LeaveGame);
        if (_cancelButton != null) _cancelButton.onClick.AddListener(CancelLeave);
    }
    private void OnEnable()
    {
        StartCoroutine(OpeningPanel());
    }
    private void OnDestroy()
    {
        if (_leaveGameButton != null) _leaveGameButton.onClick.RemoveAllListeners();
        if (_cancelButton != null) _cancelButton.onClick.RemoveAllListeners();
    }
    private void LeaveGame()
    {
        if (!string.IsNullOrEmpty(sceneName)) SceneManager.LoadScene(sceneName);
        else Debug.LogWarning("Falta colocar el nombre de la escena");
    }
    private void CancelLeave()
    {
        StartCoroutine(ClosingPanel());
    }
    private IEnumerator OpeningPanel()
    {
        _exitAnimator.SetTrigger(_showHash);
        yield return null;
    }
    private IEnumerator ClosingPanel()
    {
        _exitAnimator.SetTrigger(_hideHash);
        yield return null;
        AnimatorStateInfo stateInfo = _exitAnimator.GetCurrentAnimatorStateInfo(0);
        yield return new WaitForSecondsRealtime(stateInfo.length);
        _pausePanel.SetMainButtonsState(true);
        gameObject.SetActive(false);
    }
}
