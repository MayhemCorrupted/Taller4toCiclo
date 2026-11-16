using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization;
using UnityEngine.UI;
[Serializable] public struct DialogueUIPreset
{
    [Tooltip("Identificador único para invocar esta UI (ej: 'Pensamiento', 'Cartel', 'Cinematica').")]
    public string uiID;

    [Header("Panel Principal")]
    public GameObject dialoguePanel;
    public CanvasGroup canvasGroup;

    [Header("Textos y Retrato")]
    public TextMeshProUGUI speakerNameTMP;
    public TextMeshProUGUI dialogueTextTMP;
    public Image speakerPortrait;

    [Header("Opciones de Respuesta")]
    public GameObject optionsContainer;
    public Button[] optionButtons;
    public TextMeshProUGUI[] optionTMP;

    [Header("Prompt de Interacción")]
    public TextMeshProUGUI interactionPromptTMP;
    [Tooltip("Opcional: Si se deja vacío, usará el LocalizedString de la UI por defecto.")]
    public LocalizedString skipTypingLocalizedString;
    [Tooltip("Opcional: Si se deja vacío, usará el LocalizedString de la UI por defecto.")]
    public LocalizedString nextLineLocalizedString;

    [Header("Tiempos y Velocidad de Texto")]
    [Tooltip("Velocidad base por carácter en esta UI (recomendado: 0.03).")]
    [Range(0.01f, 0.1f)] public float defaultTextSpeed;
    [Tooltip("Segundos antes de mostrar el aviso para saltar el tipeo (recomendado: 1.5).")]
    [Min(0f)] public float skipPromptDelay;
    [Tooltip("Segundos antes de mostrar el aviso para pasar a la siguiente línea (recomendado: 0.2).")]
    [Min(0f)] public float nextPromptDelay;

    [Header("Pausas por Puntuación (Segundos Extra)")]
    [Tooltip("Pausa adicional al detectar comas o punto y coma (, ; :) (recomendado: 0.15).")]
    [Min(0f)] public float commaPauseDelay;
    [Tooltip("Pausa adicional al detectar puntos (.) (recomendado: 0.3).")]
    [Min(0f)] public float periodPauseDelay;
    [Tooltip("Pausa adicional al detectar signos de exclamación o interrogación (! ?) (recomendado: 0.25).")]
    [Min(0f)] public float specialSignPauseDelay;
}
public class DialogueUI : MonoBehaviour
{
    [Header("HUD del Jugador")]
    [SerializeField] private GameObject _playerHUD;

    [Header("Configuración de UI por Defecto")]
    [SerializeField] private DialogueUIPreset _defaultUI;

    [Header("Configuraciones de UI Personalizadas")]
    [SerializeField] private DialogueUIPreset[] _customUIs;

    [Header("Animación de Panel")]
    [SerializeField] private float _fadePanelDuration = 0.3f;

    public bool IsTyping { get; private set; }
    public string ActiveUIID => _activeUI.uiID;

    private DialogueUIPreset _activeUI;
    private DialogueLine? _currentLine = null;
    private Coroutine _typingCoroutine;
    private Coroutine _promptCoroutine;
    private float _currentTextSpeed;
    private string _currentInteractKeyName = "E";
    private float _typingPercentage = 0f;
    private void Reset()
    {
        _defaultUI = new DialogueUIPreset
        {
            uiID = "Default",
            defaultTextSpeed = 0.03f,
            skipPromptDelay = 1.5f,
            nextPromptDelay = 0.2f,
            commaPauseDelay = 0.15f,
            periodPauseDelay = 0.3f,
            specialSignPauseDelay = 0.25f
        };
    }
    private void OnValidate()
    {
        if (_defaultUI.defaultTextSpeed <= 0f)
            _defaultUI.defaultTextSpeed = 0.03f;

        if (_customUIs != null)
        {
            for (int i = 0; i < _customUIs.Length; i++)
            {
                if (_customUIs[i].defaultTextSpeed <= 0f)
                    _customUIs[i].defaultTextSpeed = 0.03f;
            }
        }
    }
    private void Awake()
    {
        InitializePreset(ref _defaultUI);

        if (_customUIs != null)
        {
            for (int i = 0; i < _customUIs.Length; i++)
            {
                InitializePreset(ref _customUIs[i]);
            }
        }

        _activeUI = _defaultUI;
    }
    private void InitializePreset(ref DialogueUIPreset preset)
    {
        if (preset.defaultTextSpeed <= 0f)
            preset.defaultTextSpeed = 0.03f;

        if (preset.canvasGroup == null && preset.dialoguePanel != null)
            preset.canvasGroup = preset.dialoguePanel.GetComponent<CanvasGroup>();

        if (preset.dialoguePanel != null)
            preset.dialoguePanel.SetActive(false);

        HidePresetOptions(preset);

        if (preset.interactionPromptTMP != null)
            preset.interactionPromptTMP.gameObject.SetActive(false);
    }
    private void OnDisable()
    {
        if (_promptCoroutine != null) StopCoroutine(_promptCoroutine);
        if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);
        LeanTween.cancel(gameObject);
        if (_activeUI.canvasGroup != null) LeanTween.cancel(_activeUI.canvasGroup.gameObject);
    }
    private void OnDestroy()
    {
        UnsubscribeCurrentLine();
    }
    public void UseDefaultUI()
    {
        _activeUI = _defaultUI;
    }
    public void UseCustomUI(string uiID)
    {
        if (string.IsNullOrWhiteSpace(uiID) || _customUIs == null)
        {
            _activeUI = _defaultUI;
            return;
        }

        string trimmedID = uiID.Trim();
        for (int i = 0; i < _customUIs.Length; i++)
        {
            if (string.Equals(_customUIs[i].uiID?.Trim(), trimmedID, StringComparison.OrdinalIgnoreCase))
            {
                _activeUI = _customUIs[i];
                return;
            }
        }

        Debug.LogWarning($"[DialogueUI] No se encontró una UI con el ID '{uiID}'. Se usará la UI por defecto.");
        _activeUI = _defaultUI;
    }
    public void UseCustomUI(int customIndex)
    {
        if (_customUIs == null || customIndex < 0 || customIndex >= _customUIs.Length)
        {
            _activeUI = _defaultUI;
            return;
        }

        _activeUI = _customUIs[customIndex];
    }
    public void UseCustomUI(DialogueUIPreset customPreset)
    {
        if (customPreset.defaultTextSpeed <= 0f)
            customPreset.defaultTextSpeed = 0.03f;

        _activeUI = customPreset;
    }
    public void SwitchActiveUI(string uiID)
    {
        GameObject previousPanel = _activeUI.dialoguePanel;
        UnsubscribeCurrentLine();
        HideAllOptions();

        if (_promptCoroutine != null) StopCoroutine(_promptCoroutine);
        if (_activeUI.interactionPromptTMP != null) _activeUI.interactionPromptTMP.gameObject.SetActive(false);

        if (string.IsNullOrWhiteSpace(uiID))
            UseDefaultUI();
        else
            UseCustomUI(uiID);

        if (previousPanel != null && previousPanel != _activeUI.dialoguePanel)
        {
            previousPanel.SetActive(false);
        }

        if (_activeUI.dialoguePanel != null && !_activeUI.dialoguePanel.activeSelf)
        {
            _activeUI.dialoguePanel.SetActive(true);
            if (_activeUI.canvasGroup != null) _activeUI.canvasGroup.alpha = 1f;
        }
    }
    public void OpenUI(Action onComplete)
    {
        if (_playerHUD != null) _playerHUD.SetActive(false);

        DeactivateAllPanelsExcept(_activeUI.dialoguePanel);

        if (_activeUI.dialoguePanel != null) _activeUI.dialoguePanel.SetActive(true);
        HideAllOptions();

        if (_activeUI.canvasGroup != null)
        {
            LeanTween.cancel(_activeUI.canvasGroup.gameObject);
            _activeUI.canvasGroup.alpha = 0f;
            LeanTween.alphaCanvas(_activeUI.canvasGroup, 1f, _fadePanelDuration)
                     .setEaseOutCubic()
                     .setOnComplete(() => onComplete?.Invoke());
        }
        else
        {
            onComplete?.Invoke();
        }
    }
    public void CloseUI(Action onComplete)
    {
        UnsubscribeCurrentLine();
        HideAllOptions();

        if (_promptCoroutine != null) StopCoroutine(_promptCoroutine);
        if (_activeUI.interactionPromptTMP != null) _activeUI.interactionPromptTMP.gameObject.SetActive(false);

        if (_activeUI.canvasGroup != null)
        {
            LeanTween.cancel(_activeUI.canvasGroup.gameObject);
            LeanTween.alphaCanvas(_activeUI.canvasGroup, 0f, _fadePanelDuration)
                     .setEaseInCubic()
                     .setOnComplete(() => FinalizeClose(onComplete));
        }
        else
        {
            FinalizeClose(onComplete);
        }
    }
    private void FinalizeClose(Action onComplete)
    {
        if (_activeUI.dialoguePanel != null) _activeUI.dialoguePanel.SetActive(false);
        if (_playerHUD != null) _playerHUD.SetActive(true);
        if (_activeUI.dialogueTextTMP != null) _activeUI.dialogueTextTMP.text = string.Empty;

        _activeUI = _defaultUI;

        onComplete?.Invoke();
    }
    private void DeactivateAllPanelsExcept(GameObject activePanel)
    {
        if (_defaultUI.dialoguePanel != null && _defaultUI.dialoguePanel != activePanel)
            _defaultUI.dialoguePanel.SetActive(false);

        if (_customUIs == null) return;
        for (int i = 0; i < _customUIs.Length; i++)
        {
            if (_customUIs[i].dialoguePanel != null && _customUIs[i].dialoguePanel != activePanel)
                _customUIs[i].dialoguePanel.SetActive(false);
        }
    }
    public void UpdateInteractKeyName(string keyName)
    {
        _currentInteractKeyName = keyName;
    }
    private void UnsubscribeCurrentLine()
    {
        if (LanguageManager.Instance != null)
        {
            if (_activeUI.speakerNameTMP != null)
                LanguageManager.Instance.UnbindText(_activeUI.speakerNameTMP);

            if (_activeUI.dialogueTextTMP != null)
                LanguageManager.Instance.UnbindText(_activeUI.dialogueTextTMP);
        }
        _currentLine = null;
    }
    public void ShowLine(DialogueLine line)
    {
        UnsubscribeCurrentLine();
        _currentLine = line;
        _typingPercentage = 0f;
        IsTyping = false;

        if (_activeUI.dialogueTextTMP != null)
        {
            _activeUI.dialogueTextTMP.text = string.Empty;
            _activeUI.dialogueTextTMP.maxVisibleCharacters = 0;
        }

        if (_activeUI.speakerPortrait != null)
        {
            _activeUI.speakerPortrait.sprite = line.SpeakerSprite;
            _activeUI.speakerPortrait.gameObject.SetActive(line.SpeakerSprite != null);
        }

        float presetTextSpeed = _activeUI.defaultTextSpeed > 0f ? _activeUI.defaultTextSpeed : 0.03f;
        _currentTextSpeed = line.CustomTextSpeed > 0f ? line.CustomTextSpeed : presetTextSpeed;

        if (_activeUI.speakerNameTMP != null && LanguageManager.Instance != null)
        {
            LanguageManager.Instance.BindText(_activeUI.speakerNameTMP, line.SpeakerName);
        }

        if (_activeUI.dialogueTextTMP != null && LanguageManager.Instance != null)
        {
            LanguageManager.Instance.BindText(_activeUI.dialogueTextTMP, line.DialogueText, (text) =>
            {
                if (_activeUI.dialogueTextTMP.text == text && IsTyping) return;

                if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);

                _activeUI.dialogueTextTMP.text = text;
                _activeUI.dialogueTextTMP.maxVisibleCharacters = 0;

                _typingPercentage = 0f;
                _typingCoroutine = StartCoroutine(TypewriterRoutine());
            });
        }
    }
    private IEnumerator TypewriterRoutine()
    {
        if (this == null || _activeUI.dialogueTextTMP == null) yield break;

        IsTyping = true;
        UpdateInteractionPrompt();

        _activeUI.dialogueTextTMP.ForceMeshUpdate();
        TMP_TextInfo textInfo = _activeUI.dialogueTextTMP.textInfo;
        int totalChars = textInfo.characterCount;
        int visibleCount = 0;

        while (visibleCount < totalChars)
        {
            visibleCount++;
            _activeUI.dialogueTextTMP.maxVisibleCharacters = visibleCount;

            _typingPercentage = (float)visibleCount / Mathf.Max(1, totalChars);

            float waitDuration = _currentTextSpeed;

            if (visibleCount < totalChars)
            {
                char currentChar = textInfo.characterInfo[visibleCount - 1].character;
                char nextChar = textInfo.characterInfo[visibleCount].character;
                waitDuration += GetPunctuationDelay(currentChar, nextChar);
            }

            yield return new WaitForSeconds(waitDuration);
        }

        _typingPercentage = 1f;
        IsTyping = false;
        SkipTypingEffect();
    }
    private float GetPunctuationDelay(char currentChar, char nextChar)
    {
        if (IsPausePunctuation(nextChar))
            return 0f;

        return currentChar switch
        {
            ',' or ';' or ':' => _activeUI.commaPauseDelay,
            '.' or '…' => _activeUI.periodPauseDelay,
            '!' or '?' => _activeUI.specialSignPauseDelay,
            _ => 0f,
        };
    }
    private static bool IsPausePunctuation(char c)
    {
        return c == ',' || c == ';' || c == ':' ||
               c == '.' || c == '…' ||
               c == '!' || c == '?';
    }
    public void SkipTypingEffect()
    {
        if (this == null || _activeUI.dialogueTextTMP == null) return;
        if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);

        _typingPercentage = 1f;
        _activeUI.dialogueTextTMP.maxVisibleCharacters = 99999;

        IsTyping = false;
        UpdateInteractionPrompt();
    }
    public void ShowOptions(DialogueOption[] options, Action<DialogueSequence> onOptionSelected)
    {
        HidePresetOptions(_activeUI);

        if (_promptCoroutine != null) StopCoroutine(_promptCoroutine);
        if (_activeUI.interactionPromptTMP != null) _activeUI.interactionPromptTMP.gameObject.SetActive(false);
        if (_activeUI.optionsContainer != null) _activeUI.optionsContainer.SetActive(true);

        if (_activeUI.optionButtons == null) return;

        for (int i = 0; i < options.Length; i++)
        {
            if (i >= _activeUI.optionButtons.Length || _activeUI.optionButtons[i] == null) break;
            int index = i;
            _activeUI.optionButtons[index].gameObject.SetActive(true);

            if (LanguageManager.Instance != null && _activeUI.optionTMP != null && index < _activeUI.optionTMP.Length && _activeUI.optionTMP[index] != null)
            {
                LanguageManager.Instance.BindText(_activeUI.optionTMP[index], options[index].OptionText);
            }

            _activeUI.optionButtons[index].onClick.RemoveAllListeners();
            _activeUI.optionButtons[index].onClick.AddListener(() =>
            {
                onOptionSelected?.Invoke(options[index].NextSequence);
            });
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetDialogueOptionsVisible(options.Length > 0);
        }

        if (options.Length > 0 && _activeUI.optionButtons.Length > 0 && _activeUI.optionButtons[0] != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(_activeUI.optionButtons[0].gameObject);
        }
    }
    public void HideAllOptions()
    {
        HidePresetOptions(_activeUI);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetDialogueOptionsVisible(false);
        }
    }
    private void HidePresetOptions(DialogueUIPreset preset)
    {
        if (preset.optionsContainer != null) preset.optionsContainer.SetActive(false);
        if (preset.optionButtons == null) return;

        for (int i = 0; i < preset.optionButtons.Length; i++)
        {
            if (LanguageManager.Instance != null && preset.optionTMP != null && i < preset.optionTMP.Length && preset.optionTMP[i] != null)
            {
                LanguageManager.Instance.UnbindText(preset.optionTMP[i]);
            }

            if (preset.optionButtons[i] != null)
            {
                preset.optionButtons[i].gameObject.SetActive(false);
                preset.optionButtons[i].onClick.RemoveAllListeners();
            }
        }
    }
    private void UpdateInteractionPrompt()
    {
        if (this == null || _activeUI.interactionPromptTMP == null) return;

        if (_promptCoroutine != null) StopCoroutine(_promptCoroutine);
        _activeUI.interactionPromptTMP.gameObject.SetActive(false);

        float delay = IsTyping ? _activeUI.skipPromptDelay : _activeUI.nextPromptDelay;
        _promptCoroutine = StartCoroutine(ShowPromptRoutine(delay));
    }
    private IEnumerator ShowPromptRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (_activeUI.interactionPromptTMP == null || LanguageManager.Instance == null) yield break;

        LocalizedString targetString = IsTyping
            ? (!_activeUI.skipTypingLocalizedString.IsEmpty ? _activeUI.skipTypingLocalizedString : _defaultUI.skipTypingLocalizedString)
            : (!_activeUI.nextLineLocalizedString.IsEmpty ? _activeUI.nextLineLocalizedString : _defaultUI.nextLineLocalizedString);

        LanguageManager.Instance.BindText(_activeUI.interactionPromptTMP, targetString, (text) =>
        {
            if (_activeUI.interactionPromptTMP != null)
            {
                _activeUI.interactionPromptTMP.text = text.Replace("{key}", _currentInteractKeyName);
                _activeUI.interactionPromptTMP.gameObject.SetActive(true);
            }
        });
    }
}