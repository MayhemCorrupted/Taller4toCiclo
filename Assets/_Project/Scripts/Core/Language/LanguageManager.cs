using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
[Serializable] public struct LocalizedTMP
{
    public string localizedName;
    public LocalizedString localizedText;
    public TextMeshProUGUI localizedTMP;
    [HideInInspector] public LocalizedString.ChangeHandler changeHandler;
}
public class LanguageManager : MonoBehaviour
{
    public static LanguageManager Instance { get; private set; }
    private class BindingInfo
    {
        public LocalizedString LocString;
        public LocalizedString.ChangeHandler Handler;
    }
    private readonly Dictionary<TextMeshProUGUI, BindingInfo> _activeBindings = new();
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    public void RegisterElements(LocalizedTMP[] elements)
    {
        foreach (var element in elements)
        {
            BindText(element.localizedTMP, element.localizedText);
        }
    }
    public void UnregisterElements(LocalizedTMP[] elements)
    {
        foreach (var element in elements)
        {
            UnbindText(element.localizedTMP);
        }
    }
    public void BindText(TextMeshProUGUI uiText, LocalizedString locString, Action<string> onUpdate = null)
    {
        if (uiText == null || locString == null) return;

        UnbindText(uiText);

        void handler(string translatedText)
        {
            if (uiText != null) uiText.text = translatedText;
            onUpdate?.Invoke(translatedText);
        }

        locString.StringChanged += handler;
        _activeBindings[uiText] = new BindingInfo { LocString = locString, Handler = handler };
    }
    public void UnbindText(TextMeshProUGUI uiText)
    {
        if (uiText == null) return;

        if (_activeBindings.TryGetValue(uiText, out BindingInfo info))
        {
            if (info.LocString != null && info.Handler != null)
            {
                info.LocString.StringChanged -= info.Handler;
            }
            _activeBindings.Remove(uiText);
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
            Debug.LogWarning($"El índice de idioma {localeIndex} no es válido.");
        }
    }
}
