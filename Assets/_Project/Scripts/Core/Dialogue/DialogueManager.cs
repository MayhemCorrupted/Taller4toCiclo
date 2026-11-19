using System;
using UnityEngine;
using UnityEngine.InputSystem;
public class DialogueManager : MonoBehaviour
{
    public event Action<string> OnDialogueEventTriggered;

    private DialogueUI _dialogueUI;
    private PlayerInputs _playerInputs;
    private DialogueSequence _currentSequence;
    private int _currentLineIndex;
    private bool _isDialogueActive = false;
    private bool _isWaitingForChoice = false;
    private bool _isTransitioning = false;
    private string _pendingUIID = null;
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
    public void SetPendingUI(string uiID)
    {
        _pendingUIID = uiID;
    }
    public void StartDialogue(DialogueSequence sequence)
    {
        if (!CanStartDialogue(sequence)) return;

        string targetUI = !string.IsNullOrWhiteSpace(_pendingUIID)
            ? _pendingUIID
            : sequence.id;

        _pendingUIID = null;

        if (!string.IsNullOrWhiteSpace(targetUI))
        {
            _dialogueUI.UseCustomUI(targetUI);
        }
        else
        {
            _dialogueUI.UseDefaultUI();
        }

        BeginDialogueInternal(sequence);
    }
    public void StartCustomDialogue(DialogueSequence sequence)
    {
        if (!CanStartDialogue(sequence)) return;

        _pendingUIID = null;

        if (!string.IsNullOrWhiteSpace(sequence.id))
            _dialogueUI.UseCustomUI(sequence.id);
        else
            _dialogueUI.UseCustomUI(0);

        BeginDialogueInternal(sequence);
    }
    public void StartDialogue(DialogueSequence sequence, string uiID)
    {
        if (!CanStartDialogue(sequence)) return;

        _pendingUIID = null;
        _dialogueUI.UseCustomUI(uiID);
        BeginDialogueInternal(sequence);
    }
    public void StartDialogue(DialogueSequence sequence, DialogueUIPreset customUI)
    {
        if (!CanStartDialogue(sequence)) return;

        _pendingUIID = null;
        _dialogueUI.UseCustomUI(customUI);
        BeginDialogueInternal(sequence);
    }
    private bool CanStartDialogue(DialogueSequence sequence)
    {
        return sequence != null &&
               sequence.Lines != null &&
               sequence.Lines.Length > 0 &&
               !_isDialogueActive &&
               !_isTransitioning;
    }
    private void BeginDialogueInternal(DialogueSequence sequence)
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetDialogueOptionsVisible(false);
            UIManager.Instance.SetGameState(GameState.Dialogue);
        }

        UpdateUIInteractKey();
        LoadSequence(sequence);
        _isTransitioning = true;

        _dialogueUI.OpenUI(() =>
        {
            _isTransitioning = false;
            if (_isDialogueActive && _currentSequence != null && _currentLineIndex < _currentSequence.Lines.Length)
            {
                _dialogueUI.ShowLine(_currentSequence.Lines[_currentLineIndex]);
            }
        });
    }
    private void LoadSequence(DialogueSequence sequence)
    {
        _currentSequence = sequence;
        _currentLineIndex = 0;
        _isDialogueActive = true;
        _isWaitingForChoice = false;

        if (UIManager.Instance != null)
            UIManager.Instance.SetDialogueOptionsVisible(false);
    }
    private void AdvanceDialogue()
    {
        if (!_isDialogueActive || _isWaitingForChoice || _isTransitioning) return;
        if (_currentSequence == null || _currentSequence.Lines == null) return;

        UpdateUIInteractKey();

        if (_dialogueUI.IsTyping)
        {
            _dialogueUI.SkipTypingEffect();
            return;
        }

        if (_currentLineIndex < 0 || _currentLineIndex >= _currentSequence.Lines.Length)
        {
            EndDialogue();
            return;
        }

        DialogueSequence sequenceBeforeEvent = _currentSequence;
        string currentLineEvent = _currentSequence.Lines[_currentLineIndex].LineEndEventID;

        if (!string.IsNullOrEmpty(currentLineEvent))
        {
            OnDialogueEventTriggered?.Invoke(currentLineEvent);

            if (_currentSequence != sequenceBeforeEvent || !_isDialogueActive) return;
        }

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
                if (UIManager.Instance != null)
                    UIManager.Instance.SetDialogueOptionsVisible(true);

                _dialogueUI.ShowOptions(_currentSequence.Options, HandleOptionSelected);
            }
            else
            {
                EndDialogue();
            }
        }
    }
    private void HandleOptionSelected(DialogueSequence nextSequence)
    {
        _isWaitingForChoice = false;
        if (UIManager.Instance != null)
            UIManager.Instance.SetDialogueOptionsVisible(false);

        _dialogueUI.HideAllOptions();

        if (nextSequence != null && nextSequence.Lines != null && nextSequence.Lines.Length > 0)
        {
            DialogueSequence sequenceBeforeEvent = _currentSequence;
            string sequenceEvent = _currentSequence != null ? _currentSequence.SequenceEndEventID : null;

            if (!string.IsNullOrEmpty(sequenceEvent))
            {
                OnDialogueEventTriggered?.Invoke(sequenceEvent);
                if (_currentSequence != sequenceBeforeEvent) return;
            }

            if (_currentSequence != null &&
                !string.Equals(_currentSequence.id?.Trim(), nextSequence.id?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                _dialogueUI.SwitchActiveUI(nextSequence.id);
            }

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
        if (!_isDialogueActive && _isTransitioning) return;

        DialogueSequence finishedSequence = _currentSequence;

        _isDialogueActive = false;
        _isWaitingForChoice = false;
        _currentSequence = null;
        _isTransitioning = true;

        if (UIManager.Instance != null)
            UIManager.Instance.SetDialogueOptionsVisible(false);

        string sequenceEvent = finishedSequence != null ? finishedSequence.SequenceEndEventID : null;
        if (!string.IsNullOrEmpty(sequenceEvent))
        {
            OnDialogueEventTriggered?.Invoke(sequenceEvent);
        }

        if (_isDialogueActive) return;

        _dialogueUI.CloseUI(() =>
        {
            _isTransitioning = false;
            if (!_isDialogueActive && UIManager.Instance != null)
            {
                UIManager.Instance.SetGameState(GameState.Gameplay);
            }
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