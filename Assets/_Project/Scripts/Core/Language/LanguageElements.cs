using UnityEngine;
public class LanguageElements : MonoBehaviour
{
    [SerializeField] LocalizedTMP[] _localizedObjects;
    private void Start()
    {
        LanguageManager.Instance.RegisterElements(_localizedObjects);
    }
    private void OnDestroy()
    {
        if (LanguageManager.Instance != null)
        {
            LanguageManager.Instance.UnregisterElements(_localizedObjects);
        }
    }
}