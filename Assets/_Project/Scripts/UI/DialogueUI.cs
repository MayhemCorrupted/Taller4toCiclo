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
    private float _typingPercentage = 0f;
    private void Awake()
    {
        if (_dialoguePanel != null) _dialoguePanel.SetActive(false);
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

        if (_speakerPortrait != null)
        {
            _speakerPortrait.sprite = line.SpeakerSprite;
            _speakerPortrait.gameObject.SetActive(line.SpeakerSprite != null);
        }

        _currentTextSpeed = line.CustomTextSpeed > 0 ? line.CustomTextSpeed : defaultTextSpeed;

        LanguageManager.Instance.BindText(_speakerNameTMP, line.SpeakerName);
        LanguageManager.Instance.BindText(_dialogueTextTMP, line.DialogueText, (text) =>
        {
            _dialogueTextTMP.ForceMeshUpdate();

            if (!IsTyping && _typingPercentage >= 1f)
            {
                SkipTypingEffect();
            }
            else if (!IsTyping && _typingPercentage == 0f)
            {
                if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);
                _typingCoroutine = StartCoroutine(TypewriterRoutine());
            }
        });
    }
    private IEnumerator TypewriterRoutine()
    {
        if (this == null || _dialogueTextTMP == null) yield break;

        IsTyping = true;
        UpdateInteractionPrompt();

        while (_typingPercentage < 1f)
        {
            if (this == null || _dialogueTextTMP == null) yield break;

            _dialogueTextTMP.ForceMeshUpdate();
            TMP_TextInfo textInfo = _dialogueTextTMP.textInfo;

            int totalVisibleChars = 0;
            for (int i = 0; i < textInfo.characterCount; i++)
            {
                if (textInfo.characterInfo[i].isVisible) totalVisibleChars++;
            }

            int charsToShow = Mathf.FloorToInt(_typingPercentage * totalVisibleChars);
            int visibleCount = 0;

            for (int i = 0; i < textInfo.characterCount; i++)
            {
                if (!textInfo.characterInfo[i].isVisible) continue;
                SetCharAlpha(i, visibleCount < charsToShow ? (byte)255 : (byte)0);
                visibleCount++;
            }

            _dialogueTextTMP.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);

            float totalTime = _currentTextSpeed * totalVisibleChars;
            if (totalTime > 0) _typingPercentage += Time.deltaTime / totalTime;
            else _typingPercentage = 1f;

            yield return null;
        }

        IsTyping = false;
        SkipTypingEffect();
    }
    public void SkipTypingEffect()
    {
        if (this == null || _dialogueTextTMP == null) return;
        if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);

        _typingPercentage = 1f;
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