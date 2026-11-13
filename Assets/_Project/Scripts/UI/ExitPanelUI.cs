using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class ExitPanelUI : MonoBehaviour
{
    [Header("Referencias de Paneles")]
    [SerializeField] private PauseMenuUI _pausePanelUI;
    [SerializeField] private OptionsPanelUI _optionsPanelUI;
    [SerializeField] private GameObject _quitPanel;
    [Header("Botones")]
    [SerializeField] private Button _quitButton;
    [SerializeField] private Button _leaveGameButton;
    [SerializeField] private Button _continueButton;
    [Header("Escena de Salida")]
    [SerializeField] private string sceneName;
    [Header("LeanTween Slide Settings")]
    [SerializeField] private float _transitionDuration = 0.35f;
    [SerializeField] private LeanTweenType _easeIn = LeanTweenType.easeOutQuint;
    [SerializeField] private LeanTweenType _easeOut = LeanTweenType.easeInOutQuint;

    private bool _isTransitioning = false;
    private RectTransform _panelRect;
    private Vector2 _targetPos;
    private void Awake()
    {
        if (_pausePanelUI == null) _pausePanelUI = FindAnyObjectByType<PauseMenuUI>();
        if (_optionsPanelUI == null) _optionsPanelUI = FindAnyObjectByType<OptionsPanelUI>();

        if (_quitPanel != null)
        {
            _panelRect = _quitPanel.GetComponent<RectTransform>();
            _targetPos = _panelRect.anchoredPosition;
            _quitPanel.SetActive(false);
        }

        if (_leaveGameButton != null) _leaveGameButton.onClick.AddListener(LeaveGame);
        if (_continueButton != null) _continueButton.onClick.AddListener(ClosePanel);
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
        if (_continueButton != null) _continueButton.onClick.RemoveAllListeners();
        if (_quitButton != null) _quitButton.onClick.RemoveAllListeners();
    }
    private void EscapeClosePanel(bool isPauseOpen)
    {
        if (!isPauseOpen && _quitPanel != null && _quitPanel.activeInHierarchy)
        {
            StopAllCoroutines();
            StartCoroutine(ClosingPanel(restoreParentFocus: false));
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
        StartCoroutine(ClosingPanel(restoreParentFocus: true));
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
    private float GetHiddenLeftX(RectTransform rect)
    {
        if (rect == null) return -1000f;
        Canvas canvas = rect.GetComponentInParent<Canvas>();
        float canvasWidth = canvas != null ? canvas.GetComponent<RectTransform>().rect.width : 2560f;
        float anchorXInCanvas = canvasWidth * rect.anchorMin.x;
        float rightEdgeOffset = rect.rect.width * (1f - rect.pivot.x);
        return -anchorXInCanvas - rightEdgeOffset - 20f;
    }
    private IEnumerator OpeningPanel()
    {
        _isTransitioning = true;
        EnableQuitButton(false);

        if (_quitPanel != null) _quitPanel.SetActive(true);
        FirstSelectButton(_continueButton);

        if (_pausePanelUI != null) _pausePanelUI.EnableResumeButton(false);
        if (_optionsPanelUI != null) _optionsPanelUI.EnableOptionsButton(false);

        if (_panelRect != null)
        {
            LeanTween.cancel(_panelRect.gameObject);
            float hiddenX = GetHiddenLeftX(_panelRect);
            _panelRect.anchoredPosition = new Vector2(hiddenX, _targetPos.y);

            LeanTween.moveX(_panelRect, _targetPos.x, _transitionDuration)
                .setEase(_easeIn)
                .setIgnoreTimeScale(true);
        }

        yield return new WaitForSecondsRealtime(_transitionDuration);
        _isTransitioning = false;
    }
    private IEnumerator ClosingPanel(bool restoreParentFocus)
    {
        _isTransitioning = true;

        if (_panelRect != null)
        {
            LeanTween.cancel(_panelRect.gameObject);
            float hiddenX = GetHiddenLeftX(_panelRect);

            LeanTween.moveX(_panelRect, hiddenX, _transitionDuration)
                .setEase(_easeOut)
                .setIgnoreTimeScale(true);
        }

        yield return new WaitForSecondsRealtime(_transitionDuration);

        EnableQuitButton(true);
        if (restoreParentFocus)
        {
            if (_pausePanelUI != null) _pausePanelUI.EnableResumeButton(true);
            if (_optionsPanelUI != null) _optionsPanelUI.EnableOptionsButton(true);
            FirstSelectButton(_quitButton);
        }

        if (_quitPanel != null) _quitPanel.SetActive(false);
        _isTransitioning = false;
    }
}