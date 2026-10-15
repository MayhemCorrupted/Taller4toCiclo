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

    [SerializeField] private TextMeshProUGUI _speakerNameTMP;
    [SerializeField] private TextMeshProUGUI _dialogueTextTMP;
    [SerializeField] private Image _speakerPortrait;

    [SerializeField] private GameObject _optionsContainer;
    [SerializeField] private Button[] _optionButtons;
    [SerializeField] private TextMeshProUGUI[] _optionTMP;

    [SerializeField] private TextMeshProUGUI _interactionPromptTMP;
    [SerializeField] private LocalizedString _skipTypingLocalizedString;
    [SerializeField] private LocalizedString _nextLineLocalizedString;

    [SerializeField] private float _skipPromptDelay = 1.5f;
    [SerializeField] private float _nextPromptDelay = 0.2f;

    [SerializeField][Range(0.01f, 0.1f)] private float defaultTextSpeed = 0.03f;
    public bool IsTyping { get; private set; }
    private DialogueLine? _currentLine = null;
    private Coroutine _typingCoroutine;
    private Coroutine _promptCoroutine;
    private float _currentTextSpeed;
    private string _currentInteractKeyName = "";

    private LocalizedString[] _activeOptionStrings;
    private LocalizedString.ChangeHandler[] _activeOptionHandlers;
    private void Awake()
    {
        if (_dialoguePanel != null) _dialoguePanel.SetActive(false);
        _activeOptionStrings = new LocalizedString[_optionButtons.Length];
        _activeOptionHandlers = new LocalizedString.ChangeHandler[_optionButtons.Length];

        for (int i = 0; i < _optionButtons.Length; i++)
        {
            int index = i;
            _activeOptionHandlers[i] = (text) =>
            {
                if (this == null || index >= _optionTMP.Length || _optionTMP[index] == null) return;
                _optionTMP[index].text = text;
            };
        }
        HideAllOptions();
        if (_interactionPromptTMP != null) _interactionPromptTMP.gameObject.SetActive(false);
    }
    private void OnDisable()
    {
        if (_promptCoroutine != null) StopCoroutine(_promptCoroutine);
        if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);
    }
    private void OnDestroy()
    {
        UnsubscribeCurrentLine();

        if (_activeOptionStrings != null)
        {
            for (int i = 0; i < _activeOptionStrings.Length; i++)
            {
                if (_activeOptionStrings[i] != null)
                {
                    _activeOptionStrings[i].StringChanged -= _activeOptionHandlers[i];
                    _activeOptionStrings[i] = null;
                }
            }
        }
    }
    public void OpenUI(Action onComplete)
    {
        if (_playerHUD != null) _playerHUD.SetActive(false);
        if (_dialoguePanel != null) _dialoguePanel.SetActive(true);
        HideAllOptions();
        onComplete?.Invoke();
    }
    public void CloseUI(Action onComplete)
    {
        UnsubscribeCurrentLine();
        HideAllOptions();

        if (_promptCoroutine != null) StopCoroutine(_promptCoroutine);
        if (_interactionPromptTMP != null) _interactionPromptTMP.gameObject.SetActive(false);
        if (_dialoguePanel != null) _dialoguePanel.SetActive(false);
        if (_playerHUD != null) _playerHUD.SetActive(true);

        onComplete?.Invoke();
    }
    public void UpdateInteractKeyName(string keyName)
    {
        _currentInteractKeyName = keyName;
    }
    private void UnsubscribeCurrentLine()
    {
        if (_currentLine.HasValue)
        {
            _currentLine.Value.SpeakerName.StringChanged -= UpdateSpeakerName;
            _currentLine.Value.DialogueText.StringChanged -= UpdateDialogueText;
            _currentLine = null;
        }
    }
    public void ShowLine(DialogueLine line)
    {
        UnsubscribeCurrentLine();
        _currentLine = line;

        if (_speakerPortrait != null)
        {
            _speakerPortrait.sprite = line.SpeakerSprite;
            _speakerPortrait.gameObject.SetActive(line.SpeakerSprite != null);
        }

        _currentTextSpeed = line.CustomTextSpeed > 0 ? line.CustomTextSpeed : defaultTextSpeed;

        line.SpeakerName.StringChanged += UpdateSpeakerName;
        line.DialogueText.StringChanged += UpdateDialogueText;
    }
    private void UpdateSpeakerName(string localizedName)
    {
        if (this == null || _speakerNameTMP == null) return;
        _speakerNameTMP.text = localizedName;
    }
    private void UpdateDialogueText(string localizedText)
    {
        if (this == null || _dialogueTextTMP == null) return;
        _dialogueTextTMP.text = localizedText;

        if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);
        _typingCoroutine = StartCoroutine(TypewriterRoutine());
    }
    private IEnumerator TypewriterRoutine()
    {
        if (this == null || _dialogueTextTMP == null) yield break;

        _dialogueTextTMP.ForceMeshUpdate();
        TMP_TextInfo textInfo = _dialogueTextTMP.textInfo;

        for (int i = 0; i < textInfo.characterCount; i++) SetCharAlpha(i, 0);
        _dialogueTextTMP.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);

        IsTyping = true;
        UpdateInteractionPrompt();

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            if (!textInfo.characterInfo[i].isVisible) continue;
            if (this == null || _dialogueTextTMP == null) yield break;

            SetCharAlpha(i, 255);
            _dialogueTextTMP.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
            yield return new WaitForSeconds(_currentTextSpeed);
        }

        IsTyping = false;
        UpdateInteractionPrompt();
    }
    public void SkipTypingEffect()
    {
        if (this == null || _dialogueTextTMP == null) return;
        if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);

        TMP_TextInfo textInfo = _dialogueTextTMP.textInfo;
        for (int i = 0; i < textInfo.characterCount; i++)
        {
            if (!textInfo.characterInfo[i].isVisible) continue;
            SetCharAlpha(i, 255);
        }

        _dialogueTextTMP.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
        IsTyping = false;
        UpdateInteractionPrompt();
    }
    private void SetCharAlpha(int charIndex, byte alphaValue)
    {
        if (this == null || _dialogueTextTMP == null) return;
        TMP_CharacterInfo charInfo = _dialogueTextTMP.textInfo.characterInfo[charIndex];
        if (!charInfo.isVisible) return;

        int materialIndex = charInfo.materialReferenceIndex;
        int vertexIndex = charInfo.vertexIndex;

        Color32[] newVertexColors = _dialogueTextTMP.textInfo.meshInfo[materialIndex].colors32;

        newVertexColors[vertexIndex + 0].a = alphaValue;
        newVertexColors[vertexIndex + 1].a = alphaValue;
        newVertexColors[vertexIndex + 2].a = alphaValue;
        newVertexColors[vertexIndex + 3].a = alphaValue;
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

            var localizedRef = options[index].OptionText;
            _activeOptionStrings[index] = localizedRef;
            localizedRef.StringChanged += _activeOptionHandlers[index];

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
            if (_activeOptionStrings != null && _activeOptionStrings[i] != null)
            {
                _activeOptionStrings[i].StringChanged -= _activeOptionHandlers[i];
                _activeOptionStrings[i] = null;
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
        var asyncOperation = targetString.GetLocalizedStringAsync();

        yield return asyncOperation;

        if (this == null || _interactionPromptTMP == null) yield break;

        string localizedText = asyncOperation.Result;
        _interactionPromptTMP.text = localizedText.Replace("{key}", _currentInteractKeyName);
        _interactionPromptTMP.gameObject.SetActive(true);
    }
}