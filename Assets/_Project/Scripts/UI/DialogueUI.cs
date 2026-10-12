using TMPro;
using UnityEngine;
using System;
using System.Collections;

public class DialogueUI : MonoBehaviour
{
    [SerializeField] private GameObject _playerHUD;
    [SerializeField] private GameObject _dialoguePanel;
    [SerializeField] private TextMeshProUGUI _speakerNameText;
    [SerializeField] private TextMeshProUGUI _dialogueTextText;

    [SerializeField] private float _textSpeed = 0.05f;
    public bool IsTyping { get; private set; }
    private DialogueLine? _currentLine = null;
    private Coroutine _typingCoroutine;
    private void Awake()
    {
        _dialoguePanel.SetActive(false);
    }
    public void OpenUI(Action onComplete)
    {
        _playerHUD.SetActive(false);
        _dialoguePanel.SetActive(true);
        onComplete?.Invoke();
    }
    public void CloseUI(Action onComplete)
    {
        UnsubscribeCurrentLine();

        _dialoguePanel.SetActive(false);
        _playerHUD.SetActive(true);
        onComplete?.Invoke();
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

        line.SpeakerName.StringChanged += UpdateSpeakerName;
        line.DialogueText.StringChanged += UpdateDialogueText;
    }
    private void UpdateSpeakerName(string localizedName)
    {
        _speakerNameText.text = localizedName;
    }
    private void UpdateDialogueText(string localizedText)
    {
        _dialogueTextText.text = localizedText;

        if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);
        _typingCoroutine = StartCoroutine(TypewriterRoutine());
    }
    private IEnumerator TypewriterRoutine()
    {
        _dialogueTextText.ForceMeshUpdate();
        TMP_TextInfo textInfo = _dialogueTextText.textInfo;

        for (int i = 0; i < textInfo.characterCount; i++) SetCharAlpha(i, 0);
        _dialogueTextText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);

        IsTyping = true;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            if (!textInfo.characterInfo[i].isVisible) continue;

            SetCharAlpha(i, 255);
            _dialogueTextText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
            yield return new WaitForSeconds(_textSpeed);
        }

        IsTyping = false;
    }
    public void SkipTypingEffect()
    {
        if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);

        TMP_TextInfo textInfo = _dialogueTextText.textInfo;
        for (int i = 0; i < textInfo.characterCount; i++)
        {
            if (!textInfo.characterInfo[i].isVisible) continue;
            SetCharAlpha(i, 255);
        }

        _dialogueTextText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
        IsTyping = false;
    }
    private void SetCharAlpha(int charIndex, byte alphaValue)
    {
        TMP_CharacterInfo charInfo = _dialogueTextText.textInfo.characterInfo[charIndex];
        if (!charInfo.isVisible) return;

        int materialIndex = charInfo.materialReferenceIndex;
        int vertexIndex = charInfo.vertexIndex;

        Color32[] newVertexColors = _dialogueTextText.textInfo.meshInfo[materialIndex].colors32;

        newVertexColors[vertexIndex + 0].a = alphaValue;
        newVertexColors[vertexIndex + 1].a = alphaValue;
        newVertexColors[vertexIndex + 2].a = alphaValue;
        newVertexColors[vertexIndex + 3].a = alphaValue;
    }
}
