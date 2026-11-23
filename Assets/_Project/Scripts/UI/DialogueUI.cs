using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization;
using UnityEngine.UI;
[Serializable] public struct DialogueUIPreset
{
    [Tooltip("ID único para invocar esta UI (ej: 'Pensamiento', 'Cartel', 'Cinematica').")]
    public string id;

    [Header("Panel Principal")]
    public GameObject panel;
    public CanvasGroup canvasGroup;

    [Header("Textos y Retrato")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI bodyText;
    public Image portrait;

    [Header("Opciones de Respuesta")]
    public GameObject optionsPanel;
    public Button[] optionButtons;
    public TextMeshProUGUI[] optionTexts;

    [Header("Aviso de Interacción")]
    public TextMeshProUGUI promptText;
    [Tooltip("Si se deja vacío, usa el texto de la UI por defecto.")]
    public LocalizedString skipText;
    [Tooltip("Si se deja vacío, usa el texto de la UI por defecto.")]
    public LocalizedString nextText;

    [Header("Velocidad y Tiempos (Segundos)")]
    [Tooltip("Segundos por carácter. 0.03 = normal | 0.15 = lento | 0.5 = muy lento.")]
    [Range(0.01f, 0.5f)] public float textSpeed;
    [Tooltip("Espera antes de mostrar el aviso para saltar texto (recomendado: 1.5).")]
    [Min(0f)] public float skipDelay;
    [Tooltip("Espera antes de mostrar el aviso para continuar (recomendado: 0.2).")]
    [Min(0f)] public float nextDelay;

    [Header("Pausas por Puntuación (Segundos Extra)")]
    [Tooltip("Pausa en coma, punto y coma o dos puntos (, ; :) (recomendado: 0.15).")]
    [Min(0f)] public float commaPause;
    [Tooltip("Pausa en punto o puntos suspensivos (. …) (recomendado: 0.3).")]
    [Min(0f)] public float periodPause;
    [Tooltip("Pausa en exclamación o interrogación (! ?) (recomendado: 0.25).")]
    [Min(0f)] public float signPause;
}
public class DialogueUI : MonoBehaviour
{
    [Header("HUD del Jugador")]
    [SerializeField] private GameObject _playerHUD;

    [Header("UI por Defecto")]
    [SerializeField] private DialogueUIPreset _defaultUI;

    [Header("UIs Personalizadas")]
    [SerializeField] private DialogueUIPreset[] _customUIs;

    [Header("Animación de Panel")]
    [SerializeField] private float _fadeDuration = 0.3f;

    public bool IsTyping { get; private set; }
    public string ActiveUIID => _activeUI.id;

    private DialogueUIPreset _activeUI;
    private Coroutine _typingCoroutine;
    private Coroutine _promptCoroutine;
    private float _currentTextSpeed;
    private string _interactKeyName = "E";
    private bool _canStartTyping = true;
    private bool _hasPendingTyping = false;
    private void Reset()
    {
        _defaultUI = new DialogueUIPreset
        {
            id = "Default",
            textSpeed = 0.03f,
            skipDelay = 1.5f,
            nextDelay = 0.2f,
            commaPause = 0.15f,
            periodPause = 0.3f,
            signPause = 0.25f
        };
    }
    private void OnValidate()
    {
        if (_defaultUI.textSpeed <= 0f)
            _defaultUI.textSpeed = 0.03f;

        if (_customUIs == null) return;

        for (int i = 0; i < _customUIs.Length; i++)
        {
            if (_customUIs[i].textSpeed <= 0f)
                _customUIs[i].textSpeed = 0.03f;
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
        if (preset.textSpeed <= 0f)
            preset.textSpeed = 0.03f;

        if (preset.panel != null)
        {
            if (preset.canvasGroup == null)
                preset.canvasGroup = preset.panel.GetComponent<CanvasGroup>();

            if (preset.canvasGroup == null)
                preset.canvasGroup = preset.panel.AddComponent<CanvasGroup>();
        }

        if (preset.canvasGroup != null)
            preset.canvasGroup.alpha = 0f;

        if (preset.panel != null)
            preset.panel.SetActive(false);

        HidePresetOptions(preset);

        if (preset.promptText != null)
            preset.promptText.gameObject.SetActive(false);
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
            if (string.Equals(_customUIs[i].id?.Trim(), trimmedID, StringComparison.OrdinalIgnoreCase))
            {
                _activeUI = _customUIs[i];
                return;
            }
        }

        GameLogger.LogWarning($"[DialogueUI] No se encontró una UI con el ID '{uiID}'. Se usará la UI por defecto.");
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
        if (customPreset.textSpeed <= 0f)
            customPreset.textSpeed = 0.03f;

        _activeUI = customPreset;
    }
    public void SwitchActiveUI(string uiID)
    {
        GameObject previousPanel = _activeUI.panel;
        UnsubscribeCurrentLine();
        HideAllOptions();

        if (_promptCoroutine != null) StopCoroutine(_promptCoroutine);
        if (_activeUI.promptText != null) _activeUI.promptText.gameObject.SetActive(false);

        if (string.IsNullOrWhiteSpace(uiID))
            UseDefaultUI();
        else
            UseCustomUI(uiID);

        if (previousPanel != null && previousPanel != _activeUI.panel)
            previousPanel.SetActive(false);

        if (_activeUI.panel != null && !_activeUI.panel.activeSelf)
        {
            if (_activeUI.canvasGroup != null) _activeUI.canvasGroup.alpha = 1f;
            _activeUI.panel.SetActive(true);
        }
    }
    public void OpenUI(Action onComplete)
    {
        if (_playerHUD != null) _playerHUD.SetActive(false);

        DeactivateAllPanelsExcept(_activeUI.panel);
        HideAllOptions();

        if (_activeUI.promptText != null)
            _activeUI.promptText.gameObject.SetActive(false);

        if (_activeUI.canvasGroup != null)
        {
            LeanTween.cancel(_activeUI.canvasGroup.gameObject);
            _activeUI.canvasGroup.alpha = 0f;

            if (_activeUI.panel != null)
                _activeUI.panel.SetActive(true);

            LeanTween.alphaCanvas(_activeUI.canvasGroup, 1f, _fadeDuration)
                     .setEaseOutCubic()
                     .setIgnoreTimeScale(true)
                     .setOnComplete(() => onComplete?.Invoke());
        }
        else
        {
            if (_activeUI.panel != null)
                _activeUI.panel.SetActive(true);

            onComplete?.Invoke();
        }
    }
    public void CloseUI(Action onComplete, float customFadeDuration = -1f)
    {
        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
            _typingCoroutine = null;
        }

        UnsubscribeCurrentLine();
        HideAllOptions();

        if (_promptCoroutine != null)
        {
            StopCoroutine(_promptCoroutine);
            _promptCoroutine = null;
        }

        if (_activeUI.promptText != null)
            _activeUI.promptText.gameObject.SetActive(false);

        float duration = customFadeDuration >= 0f ? customFadeDuration : _fadeDuration;

        if (_activeUI.canvasGroup != null && duration > 0f)
        {
            LeanTween.cancel(_activeUI.canvasGroup.gameObject);
            LeanTween.alphaCanvas(_activeUI.canvasGroup, 0f, duration)
                     .setEaseInCubic()
                     .setIgnoreTimeScale(true)
                     .setOnComplete(() => FinalizeClose(onComplete));
        }
        else
        {
            if (_activeUI.canvasGroup != null)
                _activeUI.canvasGroup.alpha = 0f;

            FinalizeClose(onComplete);
        }
    }
    private void FinalizeClose(Action onComplete)
    {
        if (_activeUI.panel != null) _activeUI.panel.SetActive(false);
        if (_playerHUD != null) _playerHUD.SetActive(true);

        if (_activeUI.nameText != null) _activeUI.nameText.text = string.Empty;
        if (_activeUI.bodyText != null) _activeUI.bodyText.text = string.Empty;
        if (_activeUI.portrait != null)
        {
            _activeUI.portrait.sprite = null;
            _activeUI.portrait.gameObject.SetActive(false);
        }

        _canStartTyping = true;
        _hasPendingTyping = false;
        _activeUI = _defaultUI;

        onComplete?.Invoke();
    }
    private void DeactivateAllPanelsExcept(GameObject activePanel)
    {
        if (_defaultUI.panel != null && _defaultUI.panel != activePanel)
            _defaultUI.panel.SetActive(false);

        if (_customUIs == null) return;
        for (int i = 0; i < _customUIs.Length; i++)
        {
            if (_customUIs[i].panel != null && _customUIs[i].panel != activePanel)
                _customUIs[i].panel.SetActive(false);
        }
    }
    public void UpdateInteractKeyName(string keyName)
    {
        _interactKeyName = keyName;
    }
    private void UnsubscribeCurrentLine()
    {
        if (LanguageManager.Instance == null) return;

        if (_activeUI.nameText != null)
            LanguageManager.Instance.UnbindText(_activeUI.nameText);

        if (_activeUI.bodyText != null)
            LanguageManager.Instance.UnbindText(_activeUI.bodyText);
    }
    /// <summary>
    /// Carga retrato, nombre y texto localizado. Si startTyping es false, deja el texto preparado
    /// con 0 caracteres visibles hasta que se llame a StartTyping() al finalizar el fade.
    /// </summary>
    public void ShowLine(DialogueLine line, bool startTyping = true)
    {
        UnsubscribeCurrentLine();

        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
            _typingCoroutine = null;
        }

        if (_promptCoroutine != null)
        {
            StopCoroutine(_promptCoroutine);
            _promptCoroutine = null;
        }

        if (_activeUI.promptText != null)
            _activeUI.promptText.gameObject.SetActive(false);

        IsTyping = false;
        _canStartTyping = startTyping;
        _hasPendingTyping = false;

        if (_activeUI.nameText != null)
            _activeUI.nameText.text = string.Empty;

        if (_activeUI.bodyText != null)
        {
            _activeUI.bodyText.text = string.Empty;
            _activeUI.bodyText.maxVisibleCharacters = 0;
        }

        if (_activeUI.portrait != null)
        {
            _activeUI.portrait.sprite = line.SpeakerSprite;
            _activeUI.portrait.gameObject.SetActive(line.SpeakerSprite != null);
        }

        float presetSpeed = _activeUI.textSpeed > 0f ? _activeUI.textSpeed : 0.03f;
        _currentTextSpeed = line.CustomTextSpeed > 0f ? line.CustomTextSpeed : presetSpeed;

        if (_activeUI.nameText != null && LanguageManager.Instance != null)
        {
            LanguageManager.Instance.BindText(_activeUI.nameText, line.SpeakerName);
        }

        if (_activeUI.bodyText != null && LanguageManager.Instance != null)
        {
            LanguageManager.Instance.BindText(_activeUI.bodyText, line.DialogueText, (text) =>
            {
                if (_activeUI.bodyText.text == text && IsTyping) return;

                if (_typingCoroutine != null)
                {
                    StopCoroutine(_typingCoroutine);
                    _typingCoroutine = null;
                }

                _activeUI.bodyText.text = text;
                _activeUI.bodyText.maxVisibleCharacters = 0;

                if (_canStartTyping)
                {
                    _hasPendingTyping = false;
                    _typingCoroutine = StartCoroutine(TypewriterRoutine());
                }
                else
                {
                    _hasPendingTyping = true;
                }
            });
        }
    }
    /// <summary>
    /// Inicia el efecto de máquina de escribir una vez que el fade de apertura ha terminado.
    /// </summary>
    public void StartTyping()
    {
        _canStartTyping = true;

        if (_hasPendingTyping)
        {
            _hasPendingTyping = false;
            if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);
            _typingCoroutine = StartCoroutine(TypewriterRoutine());
        }
        else if (!IsTyping)
        {
            UpdateInteractionPrompt();
        }
    }
    private IEnumerator TypewriterRoutine()
    {
        if (this == null || _activeUI.bodyText == null) yield break;

        IsTyping = true;
        UpdateInteractionPrompt();

        _activeUI.bodyText.ForceMeshUpdate();
        TMP_TextInfo textInfo = _activeUI.bodyText.textInfo;
        int totalChars = textInfo.characterCount;
        int visibleCount = 0;

        while (visibleCount < totalChars)
        {
            visibleCount++;
            _activeUI.bodyText.maxVisibleCharacters = visibleCount;

            float waitDuration = _currentTextSpeed;

            if (visibleCount < totalChars)
            {
                char currentChar = textInfo.characterInfo[visibleCount - 1].character;
                char nextChar = textInfo.characterInfo[visibleCount].character;
                waitDuration += GetPunctuationDelay(currentChar, nextChar);
            }

            yield return new WaitForSeconds(waitDuration);
        }

        IsTyping = false;
        _typingCoroutine = null;
        UpdateInteractionPrompt();
    }
    private float GetPunctuationDelay(char currentChar, char nextChar)
    {
        if (IsPausePunctuation(nextChar))
            return 0f;

        return currentChar switch
        {
            ',' or ';' or ':' => _activeUI.commaPause,
            '.' or '…' => _activeUI.periodPause,
            '!' or '?' => _activeUI.signPause,
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
        if (this == null || _activeUI.bodyText == null) return;

        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
            _typingCoroutine = null;
        }

        _hasPendingTyping = false;
        _activeUI.bodyText.maxVisibleCharacters = 99999;
        IsTyping = false;
        UpdateInteractionPrompt();
    }
    public void ShowOptions(DialogueOption[] options, Action<DialogueSequence> onOptionSelected)
    {
        HidePresetOptions(_activeUI);

        if (_promptCoroutine != null) StopCoroutine(_promptCoroutine);
        if (_activeUI.promptText != null) _activeUI.promptText.gameObject.SetActive(false);
        if (_activeUI.optionsPanel != null) _activeUI.optionsPanel.SetActive(true);

        if (_activeUI.optionButtons == null) return;

        for (int i = 0; i < options.Length; i++)
        {
            if (i >= _activeUI.optionButtons.Length || _activeUI.optionButtons[i] == null) break;
            int index = i;
            _activeUI.optionButtons[index].gameObject.SetActive(true);

            if (LanguageManager.Instance != null && _activeUI.optionTexts != null && index < _activeUI.optionTexts.Length && _activeUI.optionTexts[index] != null)
            {
                LanguageManager.Instance.BindText(_activeUI.optionTexts[index], options[index].OptionText);
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
        if (preset.optionsPanel != null) preset.optionsPanel.SetActive(false);
        if (preset.optionButtons == null) return;

        for (int i = 0; i < preset.optionButtons.Length; i++)
        {
            if (LanguageManager.Instance != null && preset.optionTexts != null && i < preset.optionTexts.Length && preset.optionTexts[i] != null)
            {
                LanguageManager.Instance.UnbindText(preset.optionTexts[i]);
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
        if (this == null || _activeUI.promptText == null) return;

        if (_promptCoroutine != null) StopCoroutine(_promptCoroutine);
        _activeUI.promptText.gameObject.SetActive(false);

        float delay = IsTyping ? _activeUI.skipDelay : _activeUI.nextDelay;
        _promptCoroutine = StartCoroutine(ShowPromptRoutine(delay));
    }
    private IEnumerator ShowPromptRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (_activeUI.promptText == null || LanguageManager.Instance == null) yield break;

        LocalizedString targetString = IsTyping
            ? (!_activeUI.skipText.IsEmpty ? _activeUI.skipText : _defaultUI.skipText)
            : (!_activeUI.nextText.IsEmpty ? _activeUI.nextText : _defaultUI.nextText);

        LanguageManager.Instance.BindText(_activeUI.promptText, targetString, (text) =>
        {
            if (_activeUI.promptText != null)
            {
                _activeUI.promptText.text = text.Replace("{key}", _interactKeyName);
                _activeUI.promptText.gameObject.SetActive(true);
            }
        });
    }
}