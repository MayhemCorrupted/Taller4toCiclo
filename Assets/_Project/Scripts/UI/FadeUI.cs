using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public struct FadeEvents
{
    public string eventName;
    [Min(0f)] public float fadeInDuration;
    [Min(0f)] public float fadeStayDuration;
    [Min(0f)] public float fadeOutDuration;
    [Tooltip("Se invoca al INICIAR la secuencia, justo antes de que el panel empiece a aparecer (Alpha 0 -> 1).")]
    public UnityEvent OnSequenceStart;
    [Tooltip("Se invoca al FINALIZAR el Fade In (Alpha 1).")]
    public UnityEvent OnFadeInComplete;
    [Tooltip("Se invoca al terminar el Fade In y completar la duración prolongada de espera, ANTES del Fade Out.")]
    public UnityEvent OnDurationFade;
    [Tooltip("Se invoca cuando el Fade Out termina y el panel ya es invisible (Alpha 0).")]
    public UnityEvent OnSequenceComplete;
}

public class FadeUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private GameObject _panelObject;

    [Header("Settings")]
    [SerializeField] private bool _disablePanelOnComplete = true;
    [Tooltip("Si es verdadero, el fade funcionará incluso si el juego está pausado (Time.timeScale = 0).")]
    [SerializeField] private bool _useUnscaledTime = true;
    [SerializeField] private float _defaultTransitionDuration = 0.45f;

    [Header("Fade Sequences")]
    [SerializeField] private FadeEvents[] _fadeSettings;
    private Coroutine _currentFadeRoutine;
    private void Awake()
    {
        if (_canvasGroup == null) _canvasGroup = GetComponentInChildren<CanvasGroup>(true);
        if (_panelObject == null && _canvasGroup != null) _panelObject = _canvasGroup.gameObject;
    }
    public void StartSequence(string sequenceName)
    {
        FadeEvents selectedEvents = default;
        bool eventFound = false;

        if (_fadeSettings != null)
        {
            foreach (var eventGroup in _fadeSettings)
            {
                if (eventGroup.eventName == sequenceName)
                {
                    selectedEvents = eventGroup;
                    eventFound = true;
                    break;
                }
            }
        }

        if (!eventFound)
        {
            GameLogger.LogWarning($"FadeUI: No se encontró la secuencia '{sequenceName}' en {gameObject.name}.");
            return;
        }

        ForceStopFade();
        _currentFadeRoutine = StartCoroutine(FadeRoutine(selectedEvents));
    }
    /// <summary>
    /// Oscurece la pantalla (Alpha 0 -> 1) y espera a que termine. Ideal antes de SceneManager.LoadSceneAsync.
    /// </summary>
    public Task FadeInAsync(float duration = -1f)
    {
        float targetDuration = duration < 0f ? _defaultTransitionDuration : duration;
        TaskCompletionSource<bool> tcs = new();

        ForceStopFade();
        _currentFadeRoutine = StartCoroutine(SingleFadeRoutine(0f, 1f, targetDuration, disableOnFinish: false, () =>
        {
            tcs.TrySetResult(true);
        }));

        return tcs.Task;
    }
    /// <summary>
    /// Aclara la pantalla (Alpha 1 -> 0) y desactiva el bloqueo al finalizar. Ideal tras completar la carga de escena.
    /// </summary>
    public Task FadeOutAsync(float duration = -1f)
    {
        float targetDuration = duration < 0f ? _defaultTransitionDuration : duration;
        TaskCompletionSource<bool> tcs = new();

        ForceStopFade();
        _currentFadeRoutine = StartCoroutine(SingleFadeRoutine(1f, 0f, targetDuration, disableOnFinish: _disablePanelOnComplete, () =>
        {
            tcs.TrySetResult(true);
        }));

        return tcs.Task;
    }
    private IEnumerator SingleFadeRoutine(float startAlpha, float endAlpha, float duration, bool disableOnFinish, Action onComplete)
    {
        if (_panelObject != null) _panelObject.SetActive(true);
        if (_canvasGroup != null)
        {
            _canvasGroup.blocksRaycasts = true;
            if (duration > 0f)
                yield return StartCoroutine(FadeCanvasGroup(startAlpha, endAlpha, duration));
            else
                _canvasGroup.alpha = endAlpha;
        }

        if (disableOnFinish)
        {
            if (_canvasGroup != null) _canvasGroup.blocksRaycasts = false;
            if (_panelObject != null) _panelObject.SetActive(false);
        }

        _currentFadeRoutine = null;
        onComplete?.Invoke();
    }
    private IEnumerator FadeRoutine(FadeEvents activeEvents)
    {
        if (_panelObject != null) _panelObject.SetActive(true);
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = true;
        }

        activeEvents.OnSequenceStart?.Invoke();

        if (activeEvents.fadeInDuration > 0f)
            yield return StartCoroutine(FadeCanvasGroup(0f, 1f, activeEvents.fadeInDuration));
        else if (_canvasGroup != null)
            _canvasGroup.alpha = 1f;

        activeEvents.OnFadeInComplete?.Invoke();

        if (activeEvents.fadeStayDuration > 0f)
        {
            if (_useUnscaledTime)
                yield return new WaitForSecondsRealtime(activeEvents.fadeStayDuration);
            else
                yield return new WaitForSeconds(activeEvents.fadeStayDuration);
        }

        activeEvents.OnDurationFade?.Invoke();

        if (activeEvents.fadeOutDuration > 0f)
            yield return StartCoroutine(FadeCanvasGroup(1f, 0f, activeEvents.fadeOutDuration));
        else if (_canvasGroup != null)
            _canvasGroup.alpha = 0f;

        activeEvents.OnSequenceComplete?.Invoke();

        if (_disablePanelOnComplete && _panelObject != null)
        {
            if (_canvasGroup != null) _canvasGroup.blocksRaycasts = false;
            _panelObject.SetActive(false);
        }

        _currentFadeRoutine = null;
    }
    private IEnumerator FadeCanvasGroup(float startAlpha, float targetAlpha, float duration)
    {
        if (_canvasGroup == null) yield break;

        float elapsedTime = 0f;
        _canvasGroup.alpha = startAlpha;

        while (elapsedTime < duration)
        {
            elapsedTime += _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            _canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        _canvasGroup.alpha = targetAlpha;
    }
    public void ForceStopFade()
    {
        if (_currentFadeRoutine != null)
        {
            StopCoroutine(_currentFadeRoutine);
            _currentFadeRoutine = null;
        }
    }
}