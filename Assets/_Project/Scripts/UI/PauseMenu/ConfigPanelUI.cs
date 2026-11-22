using UnityEngine;
using UnityEngine.UI;
public class ConfigPanelUI : MonoBehaviour
{
    [Header("Controlador Principal")]
    [SerializeField] private PauseMenuUI _pauseMenuUI;
    [SerializeField] private GameObject _configContainer;

    [Header("Secciones de Configuración")]
    [SerializeField] private GameObject _graphicsSection;
    [SerializeField] private GameObject _audioSection;
    [SerializeField] private GameObject _controlsSection;
    [SerializeField] private GameObject _accessibilitySection;

    [Header("Navegación")]
    [SerializeField] private Button _returnButton;

    [Header("LeanTween Slide Settings")]
    [SerializeField] private float _transitionDuration = 0.3f;
    [SerializeField] private bool _slideFromRight = false;
    [SerializeField] private LeanTweenType _easeIn = LeanTweenType.easeOutQuint;
    [SerializeField] private LeanTweenType _easeOut = LeanTweenType.easeInOutQuint;
    public bool IsVisible => _configContainer != null && _configContainer.activeSelf;
    public RectTransform PanelRect => _panelRect;

    private RectTransform _panelRect;
    private Vector2 _targetPos;
    private void Awake()
    {
        if (_pauseMenuUI == null) _pauseMenuUI = GetComponentInParent<PauseMenuUI>(true);

        if (_configContainer != null)
        {
            _panelRect = _configContainer.GetComponent<RectTransform>();
            _targetPos = _panelRect.anchoredPosition;
            _configContainer.SetActive(false);
        }

        if (_returnButton != null)
            _returnButton.onClick.AddListener(RequestReturnToOptions);

        HideAllSections();
    }
    private void OnDestroy()
    {
        if (_returnButton != null) _returnButton.onClick.RemoveAllListeners();
        if (_panelRect != null) LeanTween.cancel(_panelRect.gameObject);
    }
    private void RequestReturnToOptions()
    {
        if (_pauseMenuUI != null)
            _pauseMenuUI.ReturnToOptions();
    }
    public float SlideIn(OptionSelected optionToOpen)
    {
        if (_configContainer == null) return 0f;

        SwitchSection(optionToOpen);
        _configContainer.SetActive(true);

        if (_panelRect != null)
        {
            LeanTween.cancel(_panelRect.gameObject);

            float hiddenX = PauseMenuUI.GetOffscreenX(_panelRect, _slideFromRight);
            _panelRect.anchoredPosition = new Vector2(hiddenX, _targetPos.y);

            LeanTween.moveX(_panelRect, _targetPos.x, _transitionDuration)
                .setEase(_easeIn)
                .setIgnoreTimeScale(true);
        }

        return _transitionDuration;
    }
    public void SwitchSection(OptionSelected optionToOpen)
    {
        HideAllSections();
        ActivateSection(optionToOpen);
        PauseMenuUI.SelectButton(_returnButton);
    }
    public float SlideOut()
    {
        if (_configContainer == null || !_configContainer.activeSelf || _panelRect == null)
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
        HideAllSections();
        if (_configContainer != null) _configContainer.SetActive(false);
    }
    private void ActivateSection(OptionSelected optionToOpen)
    {
        switch (optionToOpen)
        {
            case OptionSelected.graphics:
                if (_graphicsSection != null) _graphicsSection.SetActive(true);
                break;
            case OptionSelected.audio:
                if (_audioSection != null) _audioSection.SetActive(true);
                break;
            case OptionSelected.controls:
                if (_controlsSection != null) _controlsSection.SetActive(true);
                break;
            case OptionSelected.accessibility:
                if (_accessibilitySection != null) _accessibilitySection.SetActive(true);
                break;
        }
    }
    private void HideAllSections()
    {
        if (_graphicsSection != null) _graphicsSection.SetActive(false);
        if (_audioSection != null) _audioSection.SetActive(false);
        if (_controlsSection != null) _controlsSection.SetActive(false);
        if (_accessibilitySection != null) _accessibilitySection.SetActive(false);
    }
}