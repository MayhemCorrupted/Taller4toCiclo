using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System;
[Serializable] public struct FadeEvents
{
    public string eventName;
    [Min(0f)] public float fadeInDuration;
    [Min(0f)] public float fadeStayDuration;
    [Min(0f)] public float fadeOutDuration;
    [Tooltip("Se invoca al INICIAR la secuencia, justo antes de que el panel empiece a aparecer (Alpha 0 -> 1).")]
    public UnityEvent OnSequenceStart;
    [Tooltip("Se invoca al terminar el Fade In y completar la duración prolongada de espera, ANTES del Fade Out.")]
    public UnityEvent OnDurationFade;
    [Tooltip("Se invoca cuando el Fade Out termina y el panel ya es invisible (Alpha 0).")]
    public UnityEvent OnSequenceComplete;
}

[RequireComponent(typeof(CanvasGroup))]
public class FadeUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private GameObject _panelObject;
    [Header("Settings")]
    [SerializeField] private bool _disablePanelOnComplete = true;
    [Tooltip("Si es verdadero, el fade funcionará incluso si el juego está pausado (Time.timeScale = 0).")]
    [SerializeField] private bool _useUnscaledTime = false;
    [Header("Fade Sequences")]
    [SerializeField] private FadeEvents[] _fadeSettings;
    private Coroutine _currentFadeRoutine;
    private void Awake()
    {
        if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
        if (_panelObject == null) _panelObject = gameObject;
    }
    public void StartSequence(string sequenceName)
    {
        FadeEvents selectedEvents = default;
        bool eventFound = false;

        foreach (var eventGroup in _fadeSettings)
        {
            if (eventGroup.eventName == sequenceName)
            {
                selectedEvents = eventGroup;
                eventFound = true;
                break;
            }
        }

        if (!eventFound)
        {
            Debug.LogWarning($"FadeUI: No se encontró la secuencia '{sequenceName}' en {gameObject.name}.");
            return;
        }

        if (_currentFadeRoutine != null)
        {
            StopCoroutine(_currentFadeRoutine);
        }

        _currentFadeRoutine = StartCoroutine(FadeRoutine(selectedEvents));
    }
    private IEnumerator FadeRoutine(FadeEvents activeEvents)
    {
        _panelObject.SetActive(true);
        _canvasGroup.alpha = 0f;

        activeEvents.OnSequenceStart?.Invoke();

        if (activeEvents.fadeInDuration > 0f)
        {
            yield return StartCoroutine(FadeCanvasGroup(0f, 1f, activeEvents.fadeInDuration));
        }
        else
        {
            _canvasGroup.alpha = 1f;
        }
        if (activeEvents.fadeStayDuration > 0f)
        {
            if (_useUnscaledTime)
                yield return new WaitForSecondsRealtime(activeEvents.fadeStayDuration);
            else
                yield return new WaitForSeconds(activeEvents.fadeStayDuration);
        }

        activeEvents.OnDurationFade?.Invoke();

        if (activeEvents.fadeOutDuration > 0f)
        {
            yield return StartCoroutine(FadeCanvasGroup(1f, 0f, activeEvents.fadeOutDuration));
        }
        else
        {
            _canvasGroup.alpha = 0f;
        }

        activeEvents.OnSequenceComplete?.Invoke();

        if (_disablePanelOnComplete)
        {
            _panelObject.SetActive(false);
        }

        _currentFadeRoutine = null;
    }
    private IEnumerator FadeCanvasGroup(float startAlpha, float targetAlpha, float duration)
    {
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