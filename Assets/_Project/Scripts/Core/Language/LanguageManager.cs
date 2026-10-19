using System;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
[Serializable]
public struct LocalizedTMP
{
    public LocalizedString localizedText;
    public TextMeshProUGUI localizedTMP;
    [HideInInspector] public LocalizedString.ChangeHandler changeHandler;
}

public class LanguageManager : MonoBehaviour
{
    public static LanguageManager Instance { get; private set; }
    [SerializeField] private LocalizedTMP[] _localizedUIElements;
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        for (int i = 0; i < _localizedUIElements.Length; i++)
        {
            int index = i;
            _localizedUIElements[index].changeHandler = (translatedText) =>
            {
                if (_localizedUIElements[index].localizedTMP != null)
                {
                    _localizedUIElements[index].localizedTMP.text = translatedText;
                }
            };

            if (_localizedUIElements[index].localizedText != null)
            {
                _localizedUIElements[index].localizedText.StringChanged += _localizedUIElements[index].changeHandler;
            }
        }
    }
    private void OnDestroy()
    {
        for (int i = 0; i < _localizedUIElements.Length; i++)
        {
            if (_localizedUIElements[i].localizedText != null && _localizedUIElements[i].changeHandler != null)
            {
                _localizedUIElements[i].localizedText.StringChanged -= _localizedUIElements[i].changeHandler;
            }
        }
    }
    public void ChangeLanguage(int localeIndex)
    {
        if (!LocalizationSettings.InitializationOperation.IsDone) return;

        if (localeIndex >= 0 && localeIndex < LocalizationSettings.AvailableLocales.Locales.Count)
        {
            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[localeIndex];
        }
        else
        {
            Debug.LogWarning($"El índice de idioma {localeIndex} no es válido o no está configurado.");
        }
    }
}
