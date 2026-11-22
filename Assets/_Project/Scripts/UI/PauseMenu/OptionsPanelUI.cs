using UnityEngine;
using UnityEngine.UI;

public enum OptionSelected { graphics, audio, controls, accessibility }

public class OptionsPanelUI : MonoBehaviour
{
    [Header("Controlador Principal")]
    [SerializeField] private PauseMenuUI _pauseMenuUI;
    [SerializeField] private GameObject _optionsSection;

    [Header("Botones de Categoría")]
    [SerializeField] private Button _graphicsSectionButton;
    [SerializeField] private Button _audioSectionButton;
    [SerializeField] private Button _controlsSectionButton;
    [SerializeField] private Button _accessibilitySectionButton;

    [Header("Navegación")]
    [SerializeField] private Button _returnButton;

    [Header("LeanTween Slide Settings")]
    [SerializeField] private float _transitionDuration = 0.3f;
    [SerializeField] private bool _slideFromRight = false;
    [SerializeField] private LeanTweenType _easeIn = LeanTweenType.easeOutQuint;
    [SerializeField] private LeanTweenType _easeOut = LeanTweenType.easeInOutQuint;

    public bool IsVisible => _optionsSection != null && _optionsSection.activeSelf;
    public RectTransform PanelRect => _panelRect;

    private RectTransform _panelRect;
    private Vector2 _targetPos;
    private OptionSelected _lastSelectedOption = OptionSelected.graphics;
    private void Awake()
    {
        if (_pauseMenuUI == null) _pauseMenuUI = GetComponentInParent<PauseMenuUI>(true);

        if (_optionsSection != null)
        {
            _panelRect = _optionsSection.GetComponent<RectTransform>();
            _targetPos = _panelRect.anchoredPosition;
            _optionsSection.SetActive(false);
        }

        if (_graphicsSectionButton != null)
            _graphicsSectionButton.onClick.AddListener(() => RequestConfigSection(OptionSelected.graphics));
        if (_audioSectionButton != null)
            _audioSectionButton.onClick.AddListener(() => RequestConfigSection(OptionSelected.audio));
        if (_controlsSectionButton != null)
            _controlsSectionButton.onClick.AddListener(() => RequestConfigSection(OptionSelected.controls));
        if (_accessibilitySectionButton != null)
            _accessibilitySectionButton.onClick.AddListener(() => RequestConfigSection(OptionSelected.accessibility));

        if (_returnButton != null)
            _returnButton.onClick.AddListener(RequestReturnToMain);
    }
    private void OnDestroy()
    {
        if (_graphicsSectionButton != null) _graphicsSectionButton.onClick.RemoveAllListeners();
        if (_audioSectionButton != null) _audioSectionButton.onClick.RemoveAllListeners();
        if (_controlsSectionButton != null) _controlsSectionButton.onClick.RemoveAllListeners();
        if (_accessibilitySectionButton != null) _accessibilitySectionButton.onClick.RemoveAllListeners();
        if (_returnButton != null) _returnButton.onClick.RemoveAllListeners();

        if (_panelRect != null) LeanTween.cancel(_panelRect.gameObject);
    }
    private void RequestConfigSection(OptionSelected selected)
    {
        _lastSelectedOption = selected;
        if (_pauseMenuUI != null)
            _pauseMenuUI.OpenConfigurations(selected);
    }
    private void RequestReturnToMain()
    {
        if (_pauseMenuUI != null)
            _pauseMenuUI.ReturnToMain();
    }
    public float SlideIn(bool restoreLastSelection)
    {
        if (_optionsSection == null) return 0f;

        _optionsSection.SetActive(true);

        if (_panelRect != null)
        {
            LeanTween.cancel(_panelRect.gameObject);

            float hiddenX = PauseMenuUI.GetOffscreenX(_panelRect, _slideFromRight);
            _panelRect.anchoredPosition = new Vector2(hiddenX, _targetPos.y);

            LeanTween.moveX(_panelRect, _targetPos.x, _transitionDuration)
                .setEase(_easeIn)
                .setIgnoreTimeScale(true);
        }

        Button focusTarget = restoreLastSelection ? GetButtonForOption(_lastSelectedOption) : _graphicsSectionButton;
        PauseMenuUI.SelectButton(focusTarget);

        return _transitionDuration;
    }
    public float SlideOut()
    {
        if (_optionsSection == null || !_optionsSection.activeSelf || _panelRect == null)
            return 0f;

        LeanTween.cancel(_panelRect.gameObject);
        float hiddenX = PauseMenuUI.GetOffscreenX(_panelRect, _slideFromRight);

        LeanTween.moveX(_panelRect, hiddenX, _transitionDuration)
            .setEase(_easeOut)
            .setIgnoreTimeScale(true);

        return _transitionDuration;
    }
    public void FocusLastSelected()
    {
        PauseMenuUI.SelectButton(GetButtonForOption(_lastSelectedOption));
    }
    public void HideImmediate()
    {
        if (_panelRect != null) LeanTween.cancel(_panelRect.gameObject);
        if (_optionsSection != null) _optionsSection.SetActive(false);
    }
    private Button GetButtonForOption(OptionSelected option)
    {
        return option switch
        {
            OptionSelected.graphics => _graphicsSectionButton,
            OptionSelected.audio => _audioSectionButton,
            OptionSelected.controls => _controlsSectionButton,
            OptionSelected.accessibility => _accessibilitySectionButton,
            _ => _graphicsSectionButton
        };
    }
}