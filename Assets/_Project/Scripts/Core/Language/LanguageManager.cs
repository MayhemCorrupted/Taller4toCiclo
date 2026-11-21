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

    private readonly Dictionary<TMP_Text, BindingInfo> _activeBindings = new();
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }
    private void OnDestroy()
    {
        if (Instance != this) return;

        foreach (var kvp in _activeBindings)
        {
            if (kvp.Value.LocString != null && kvp.Value.Handler != null)
            {
                kvp.Value.LocString.StringChanged -= kvp.Value.Handler;
            }
        }

        _activeBindings.Clear();
        Instance = null;
    }
    public void RegisterElements(LocalizedTMP[] elements)
    {
        if (elements == null) return;

        foreach (var element in elements)
        {
            BindText(element.localizedTMP, element.localizedText);
        }
    }
    public void UnregisterElements(LocalizedTMP[] elements)
    {
        if (elements == null) return;

        foreach (var element in elements)
        {
            UnbindText(element.localizedTMP);
        }
    }
    public void BindText(TMP_Text uiText, LocalizedString locString, Action<string> onUpdate = null)
    {
        if (uiText == null || locString == null) return;

        UnbindText(uiText);

        if (locString.IsEmpty) return;

        void Handler(string translatedText)
        {
            if (uiText == null)
            {
                UnbindText(uiText);
                return;
            }

            if (onUpdate != null)
            {
                onUpdate.Invoke(translatedText);
            }
            else
            {
                uiText.text = translatedText;
            }
        }

        locString.StringChanged += Handler;
        _activeBindings[uiText] = new BindingInfo { LocString = locString, Handler = Handler };
    }
    public void UnbindText(TMP_Text uiText)
    {
        if (ReferenceEquals(uiText, null)) return;

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

        var locales = LocalizationSettings.AvailableLocales.Locales;
        if (localeIndex >= 0 && localeIndex < locales.Count)
        {
            LocalizationSettings.SelectedLocale = locales[localeIndex];
        }
        else
        {
            GameLogger.LogWarning($"[LanguageManager] El índice de idioma {localeIndex} no es válido.");
        }
    }
}