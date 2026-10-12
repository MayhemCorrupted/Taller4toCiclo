using UnityEngine;
public class DialogueManager : MonoBehaviour
{
    private DialogueUI _dialogueUI;
    private PlayerInputs _playerInputs;

    private DialogueSequence _currentSequence;
    private int _currentLineIndex;
    private bool _isDialogueActive = false;
    private void Awake()
    {
        _playerInputs = FindAnyObjectByType<PlayerInputs>();
        _dialogueUI = FindAnyObjectByType<DialogueUI>();
    }
    private void OnEnable()
    {
        if (_playerInputs != null) _playerInputs.OnInteract += AdvanceDialogue;
    }
    private void OnDisable()
    {
        if (_playerInputs != null) _playerInputs.OnInteract -= AdvanceDialogue;
    }
    public void StartDialogue(DialogueSequence sequence)
    {
        if (sequence == null || sequence.Lines.Length == 0 || _isDialogueActive) return;
        _currentSequence = sequence;
        _currentLineIndex = 0;
        _isDialogueActive = true;

        _dialogueUI.OpenUI(() =>
        {
            _dialogueUI.ShowLine(_currentSequence.Lines[_currentLineIndex]);
        });
    }
    private void AdvanceDialogue()
    {
        if (!_isDialogueActive) return;
        if (_dialogueUI.IsTyping)
        {
            _dialogueUI.SkipTypingEffect();
        }
        else
        {
            _currentLineIndex++;

            if (_currentLineIndex < _currentSequence.Lines.Length)
            {
                _dialogueUI.ShowLine(_currentSequence.Lines[_currentLineIndex]);
            }
            else
            {
                _dialogueUI.CloseUI(() =>
                {
                    _isDialogueActive = false;
                    _currentSequence = null;
                });
            }
        }
    }
}