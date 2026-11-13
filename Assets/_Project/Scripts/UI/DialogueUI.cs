using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization;
using UnityEngine.UI;
public class DialogueUI : MonoBehaviour
{
    [SerializeField] private GameObject _playerHUD;
    [SerializeField] private GameObject _dialoguePanel;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private TextMeshProUGUI _speakerNameTMP;
    [SerializeField] private TextMeshProUGUI _dialogueTextTMP;
    [SerializeField] private Image _speakerPortrait;
    [SerializeField] private GameObject _optionsContainer;
    [SerializeField] private Button[] _optionButtons;
    [SerializeField] private TextMeshProUGUI[] _optionTMP;

    [SerializeField] private TextMeshProUGUI _interactionPromptTMP;
    [SerializeField] private LocalizedString _skipTypingLocalizedString;
    [SerializeField] private LocalizedString _nextLineLocalizedString;
    [Header("Tiempos y Animaciones")]
    [SerializeField] private float _fadePanelDuration = 0.3f;
    [SerializeField] private float _skipPromptDelay = 1.5f;
    [SerializeField] private float _nextPromptDelay = 0.2f;
    [SerializeField][Range(0.01f, 0.1f)] private float defaultTextSpeed = 0.03f;
    public bool IsTyping { get; private set; }
    private DialogueLine? _currentLine = null;
    private Coroutine _typingCoroutine;
    private Coroutine _promptCoroutine;
    private float _currentTextSpeed;
    private string _currentInteractKeyName = "E";
    private float _typingPercentage = 0f;
    private void Awake()
    {
        if (_canvasGroup == null && _dialoguePanel != null)
            _canvasGroup = _dialoguePanel.GetComponent<CanvasGroup>();

        if (_dialoguePanel != null) _dialoguePanel.SetActive(false);
        HideAllOptions();
        if (_interactionPromptTMP != null) _interactionPromptTMP.gameObject.SetActive(false);
    }
    private void OnDisable()
    {
        if (_promptCoroutine != null) StopCoroutine(_promptCoroutine);
        if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);
        LeanTween.cancel(gameObject);
    }
    private void OnDestroy()
    {
        UnsubscribeCurrentLine();
    }
    public void OpenUI(Action onComplete)
    {
        if (_playerHUD != null) _playerHUD.SetActive(false);
        if (_dialoguePanel != null) _dialoguePanel.SetActive(true);
        HideAllOptions();

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
            LeanTween.alphaCanvas(_canvasGroup, 1f, _fadePanelDuration)
                     .setEaseOutCubic()
                     .setOnComplete(() => onComplete?.Invoke());
        }
        else onComplete?.Invoke();
    }
    public void CloseUI(Action onComplete)
    {
        UnsubscribeCurrentLine();
        HideAllOptions();

        if (_promptCoroutine != null) StopCoroutine(_promptCoroutine);
        if (_interactionPromptTMP != null) _interactionPromptTMP.gameObject.SetActive(false);

        if (_canvasGroup != null)
        {
            LeanTween.alphaCanvas(_canvasGroup, 0f, _fadePanelDuration)
                     .setEaseInCubic()
                     .setOnComplete(() => FinalizeClose(onComplete));
        }
        else FinalizeClose(onComplete);
    }
    private void FinalizeClose(Action onComplete)
    {
        if (_dialoguePanel != null) _dialoguePanel.SetActive(false);
        if (_playerHUD != null) _playerHUD.SetActive(true);
        if (_dialogueTextTMP != null) _dialogueTextTMP.text = string.Empty;
        onComplete?.Invoke();
    }
    public void UpdateInteractKeyName(string keyName)
    {
        _currentInteractKeyName = keyName;
    }
    private void UnsubscribeCurrentLine()
    {
        LanguageManager.Instance.UnbindText(_speakerNameTMP);
        LanguageManager.Instance.UnbindText(_dialogueTextTMP);
        _currentLine = null;
    }
    public void ShowLine(DialogueLine line)
    {
        UnsubscribeCurrentLine();
        _currentLine = line;
        _typingPercentage = 0f;
        IsTyping = false;

        if (_dialogueTextTMP != null)
        {
            _dialogueTextTMP.text = string.Empty;
            _dialogueTextTMP.maxVisibleCharacters = 0;
        }

        if (_speakerPortrait != null)
        {
            _speakerPortrait.sprite = line.SpeakerSprite;
            _speakerPortrait.gameObject.SetActive(line.SpeakerSprite != null);
        }

        _currentTextSpeed = line.CustomTextSpeed > 0 ? line.CustomTextSpeed : defaultTextSpeed;

        LanguageManager.Instance.BindText(_speakerNameTMP, line.SpeakerName);
        LanguageManager.Instance.BindText(_dialogueTextTMP, line.DialogueText, (text) =>
        {
            if (_dialogueTextTMP.text == text && IsTyping) return;

            if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);

            _dialogueTextTMP.text = text;
            _dialogueTextTMP.maxVisibleCharacters = 0; 

            _typingPercentage = 0f;
            _typingCoroutine = StartCoroutine(TypewriterRoutine());
        });
    }
    private IEnumerator TypewriterRoutine()
    {
        if (this == null || _dialogueTextTMP == null) yield break;

        IsTyping = true;
        UpdateInteractionPrompt();

        _dialogueTextTMP.ForceMeshUpdate();
        int totalChars = _dialogueTextTMP.textInfo.characterCount;
        int visibleCount = 0;

        while (visibleCount < totalChars)
        {
            visibleCount++;
            _dialogueTextTMP.maxVisibleCharacters = visibleCount;

            _typingPercentage = (float)visibleCount / Mathf.Max(1, totalChars);

            yield return new WaitForSeconds(_currentTextSpeed);
        }

        _typingPercentage = 1f;
        IsTyping = false;
        SkipTypingEffect();
    }
    public void SkipTypingEffect()
    {
        if (this == null || _dialogueTextTMP == null) return;
        if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);

        _typingPercentage = 1f;

        _dialogueTextTMP.maxVisibleCharacters = 99999;

        IsTyping = false;
        UpdateInteractionPrompt();
    }
    public void ShowOptions(DialogueOption[] options, Action<DialogueSequence> onOptionSelected)
    {
        HideAllOptions();

        if (_promptCoroutine != null) StopCoroutine(_promptCoroutine);
        if (_interactionPromptTMP != null) _interactionPromptTMP.gameObject.SetActive(false);
        if (_optionsContainer != null) _optionsContainer.SetActive(true);

        for (int i = 0; i < options.Length; i++)
        {
            if (i >= _optionButtons.Length) break;
            int index = i;
            _optionButtons[index].gameObject.SetActive(true);

            LanguageManager.Instance.BindText(_optionTMP[index], options[index].OptionText);

            _optionButtons[index].onClick.RemoveAllListeners();
            _optionButtons[index].onClick.AddListener(() =>
            {
                onOptionSelected?.Invoke(options[index].NextSequence);
            });
        }

        if (options.Length > 0 && _optionButtons[0] != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(_optionButtons[0].gameObject);
        }
    }
    public void HideAllOptions()
    {
        if (_optionsContainer != null) _optionsContainer.SetActive(false);

        for (int i = 0; i < _optionButtons.Length; i++)
        {
            if (_optionTMP != null && i < _optionTMP.Length)
            {
                LanguageManager.Instance.UnbindText(_optionTMP[i]);
            }

            if (_optionButtons != null && i < _optionButtons.Length && _optionButtons[i] != null)
            {
                _optionButtons[i].gameObject.SetActive(false);
                _optionButtons[i].onClick.RemoveAllListeners();
            }
        }
    }
    private void UpdateInteractionPrompt()
    {
        if (this == null || _interactionPromptTMP == null) return;

        if (_promptCoroutine != null) StopCoroutine(_promptCoroutine);
        _interactionPromptTMP.gameObject.SetActive(false);

        float delay = IsTyping ? _skipPromptDelay : _nextPromptDelay;
        _promptCoroutine = StartCoroutine(ShowPromptRoutine(delay));
    }
    private IEnumerator ShowPromptRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);

        LocalizedString targetString = IsTyping ? _skipTypingLocalizedString : _nextLineLocalizedString;

        LanguageManager.Instance.BindText(_interactionPromptTMP, targetString, (text) =>
        {
            _interactionPromptTMP.text = text.Replace("{key}", _currentInteractKeyName);
            _interactionPromptTMP.gameObject.SetActive(true);
        });
    }
}