using UnityEngine;
using UnityEngine.InputSystem;
public class DialogueManager : MonoBehaviour
{
    private DialogueUI _dialogueUI;
    private PlayerInputs _playerInputs;

    private DialogueSequence _currentSequence;
    private int _currentLineIndex;
    private bool _isDialogueActive = false;
    private bool _isWaitingForChoice = false;
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

        if (UIManager.Instance != null) UIManager.Instance.SetGameState(GameState.Dialogue);

        UpdateUIInteractKey();
        LoadSequence(sequence);

        _dialogueUI.OpenUI(() =>
        {
            _dialogueUI.ShowLine(_currentSequence.Lines[_currentLineIndex]);
        });
    }
    private void LoadSequence(DialogueSequence sequence)
    {
        _currentSequence = sequence;
        _currentLineIndex = 0;
        _isDialogueActive = true;
        _isWaitingForChoice = false;
    }
    private void AdvanceDialogue()
    {
        if (!_isDialogueActive || _isWaitingForChoice) return;

        UpdateUIInteractKey();

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
                if (_currentSequence.Options != null && _currentSequence.Options.Length > 0)
                {
                    _isWaitingForChoice = true;
                    _dialogueUI.ShowOptions(_currentSequence.Options, HandleOptionSelected);
                }
                else
                {
                    EndDialogue();
                }
            }
        }
    }
    private void HandleOptionSelected(DialogueSequence nextSequence)
    {
        _dialogueUI.HideAllOptions();
        if (nextSequence != null)
        {
            LoadSequence(nextSequence);
            _dialogueUI.ShowLine(_currentSequence.Lines[_currentLineIndex]);
        }
        else
        {
            EndDialogue();
        }
    }
    private void EndDialogue()
    {
        _dialogueUI.CloseUI(() =>
        {
            _isDialogueActive = false;
            _currentSequence = null;
            _isWaitingForChoice = false;

            if (UIManager.Instance != null) UIManager.Instance.SetGameState(GameState.Gameplay);
        });
    }
    private void UpdateUIInteractKey()
    {
        if (_playerInputs == null || _playerInputs.InputSystemAsset == null || _dialogueUI == null) return;

        InputAction interactAction = _playerInputs.InputSystemAsset.Gameplay.Interact;
        string controlScheme = _playerInputs.CurrentDeviceMode switch
        {
            "Mobile_Touch" => "Touch",
            "PC_Gamepad" => "Gamepad",
            _ => "KeyboardMouse",
        };
        string finalKeyName = "E"; 
        for (int i = 0; i < interactAction.bindings.Count; i++)
        {
            if (interactAction.bindings[i].groups.Contains(controlScheme))
            {
                finalKeyName = interactAction.GetBindingDisplayString(i);
                break;
            }
        }
        _dialogueUI.UpdateInteractKeyName(finalKeyName);
    }
}