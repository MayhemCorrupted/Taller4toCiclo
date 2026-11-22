using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class ExitPanelUI : MonoBehaviour
{
    [Header("Controlador Principal")]
    [SerializeField] private PauseMenuUI _pauseMenuUI;
    [SerializeField] private GameObject _exitContainer;

    [Header("Botones de Confirmación")]
    [SerializeField] private Button _leaveGameButton;
    [SerializeField] private Button _continueButton;

    [Header("Escena de Menú Principal")]
    [SerializeField] private string _mainMenuSceneName;

    [Header("LeanTween Slide Settings")]
    [SerializeField] private float _transitionDuration = 0.3f;
    [SerializeField] private bool _slideFromRight = false;
    [SerializeField] private LeanTweenType _easeIn = LeanTweenType.easeOutQuint;
    [SerializeField] private LeanTweenType _easeOut = LeanTweenType.easeInOutQuint;

    public bool IsVisible => _exitContainer != null && _exitContainer.activeSelf;
    public RectTransform PanelRect => _panelRect;

    private RectTransform _panelRect;
    private Vector2 _targetPos;
    private void Awake()
    {
        if (_pauseMenuUI == null) _pauseMenuUI = GetComponentInParent<PauseMenuUI>(true);

        if (_exitContainer != null)
        {
            _panelRect = _exitContainer.GetComponent<RectTransform>();
            _targetPos = _panelRect.anchoredPosition;
            _exitContainer.SetActive(false);
        }

        if (_leaveGameButton != null) _leaveGameButton.onClick.AddListener(LeaveGame);
        if (_continueButton != null) _continueButton.onClick.AddListener(RequestReturnToMain);
    }
    private void OnDestroy()
    {
        if (_leaveGameButton != null) _leaveGameButton.onClick.RemoveAllListeners();
        if (_continueButton != null) _continueButton.onClick.RemoveAllListeners();
        if (_panelRect != null) LeanTween.cancel(_panelRect.gameObject);
    }
    private void LeaveGame()
    {
        if (string.IsNullOrWhiteSpace(_mainMenuSceneName))
        {
            GameLogger.LogWarning("[ExitPanelUI] Falta asignar el nombre de la escena de menú principal.");
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(_mainMenuSceneName);
    }
    private void RequestReturnToMain()
    {
        if (_pauseMenuUI != null)
            _pauseMenuUI.ReturnToMain();
    }
    public float SlideIn()
    {
        if (_exitContainer == null) return 0f;

        _exitContainer.SetActive(true);

        if (_panelRect != null)
        {
            LeanTween.cancel(_panelRect.gameObject);

            float hiddenX = PauseMenuUI.GetOffscreenX(_panelRect, _slideFromRight);
            _panelRect.anchoredPosition = new Vector2(hiddenX, _targetPos.y);

            LeanTween.moveX(_panelRect, _targetPos.x, _transitionDuration)
                .setEase(_easeIn)
                .setIgnoreTimeScale(true);
        }

        PauseMenuUI.SelectButton(_continueButton);
        return _transitionDuration;
    }
    public float SlideOut()
    {
        if (_exitContainer == null || !_exitContainer.activeSelf || _panelRect == null)
            return 0f;

        LeanTween.cancel(_panelRect.gameObject);
        float hiddenX = PauseMenuUI.GetOffscreenX(_panelRect, _slideFromRight);

        LeanTween.moveX(_panelRect, hiddenX, _transitionDuration)
            .setEase(_easeOut)
            .setIgnoreTimeScale(true);

        return _transitionDuration;
    }
    public void HideImmediate()
    {
        if (_panelRect != null) LeanTween.cancel(_panelRect.gameObject);
        if (_exitContainer != null) _exitContainer.SetActive(false);
    }
}