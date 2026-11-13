using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public class PauseMenuUI : MonoBehaviour, IPanel
{
    [Header("Contenedores")]
    [SerializeField] private GameObject _mainPauseContainer;
    [Tooltip("Contenedor lateral izquierdo con los botones principales de pausa (único que se desliza aquí).")]
    [SerializeField] private GameObject _optionsContainer;
    [SerializeField] private GameObject _quitContainer;
    [SerializeField] private GameObject _configContainer;
    [Header("Botones")]
    [SerializeField] private Button _resumeButton;
    [Header("LeanTween Slide Settings")]
    [SerializeField] private float _transitionDuration = 0.35f;
    [SerializeField] private LeanTweenType _easeIn = LeanTweenType.easeOutQuint;
    [SerializeField] private LeanTweenType _easeOut = LeanTweenType.easeInOutQuint;
    public Action<bool> IsOpen;
    private bool _isTransitioning = false;
    private RectTransform _optionsRect;
    private Vector2 _optionsTargetPos;
    private void Awake()
    {
        if (_resumeButton != null) _resumeButton.onClick.AddListener(ResumeGame);

        if (_optionsContainer != null)
        {
            _optionsRect = _optionsContainer.GetComponent<RectTransform>();
            _optionsTargetPos = _optionsRect.anchoredPosition;
            _optionsContainer.SetActive(false);
        }

        if (_quitContainer != null) _quitContainer.SetActive(false);
        if (_configContainer != null) _configContainer.SetActive(false);
        if (_mainPauseContainer != null) _mainPauseContainer.SetActive(false);

        IsOpen?.Invoke(false);
    }
    private void OnDestroy()
    {
        if (_resumeButton != null) _resumeButton.onClick.RemoveAllListeners();
    }
    public void OpenPanel()
    {
        if (_isTransitioning) return;
        StartCoroutine(OpeningPanel());
    }
    public void ClosePanel()
    {
        if (_isTransitioning) return;
        StartCoroutine(ClosingPanel());
    }
    private void ResumeGame()
    {
        if (_isTransitioning) return;
        UIManager.Instance.SetGameState(GameState.Gameplay);
        ClosePanel();
    }
    public void EnableResumeButton(bool state)
    {
        if (_resumeButton != null) _resumeButton.interactable = state;
    }
    private void FirstSelectButton(Button btn)
    {
        if (btn != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(btn.gameObject);
        }
    }
    private float GetHiddenLeftX(RectTransform rect)
    {
        if (rect == null) return -1000f;
        Canvas canvas = rect.GetComponentInParent<Canvas>();
        float canvasWidth = canvas != null ? canvas.GetComponent<RectTransform>().rect.width : 2560f;
        float anchorXInCanvas = canvasWidth * rect.anchorMin.x;
        float rightEdgeOffset = rect.rect.width * (1f - rect.pivot.x);
        return -anchorXInCanvas - rightEdgeOffset - 20f;
    }
    private IEnumerator OpeningPanel()
    {
        _isTransitioning = true;
        IsOpen?.Invoke(true);

        if (_quitContainer != null) _quitContainer.SetActive(false);
        if (_configContainer != null) _configContainer.SetActive(false);
        if (_mainPauseContainer != null) _mainPauseContainer.SetActive(true);
        if (_optionsContainer != null) _optionsContainer.SetActive(false);

        EnableResumeButton(true);
        FirstSelectButton(_resumeButton);

        if (_optionsRect != null)
        {
            LeanTween.cancel(_optionsRect.gameObject);
            float hiddenX = GetHiddenLeftX(_optionsRect);
            _optionsRect.anchoredPosition = new Vector2(hiddenX, _optionsTargetPos.y);

            LeanTween.moveX(_optionsRect, _optionsTargetPos.x, _transitionDuration)
                .setEase(_easeIn)
                .setIgnoreTimeScale(true);
        }

        yield return new WaitForSecondsRealtime(_transitionDuration);
        _isTransitioning = false;
    }
    private IEnumerator ClosingPanel()
    {
        _isTransitioning = true;
        EnableResumeButton(false);

        IsOpen?.Invoke(false);

        if (_optionsRect != null)
        {
            LeanTween.cancel(_optionsRect.gameObject);
            float hiddenX = GetHiddenLeftX(_optionsRect);

            LeanTween.moveX(_optionsRect, hiddenX, _transitionDuration)
                .setEase(_easeOut)
                .setIgnoreTimeScale(true);
        }

        yield return new WaitForSecondsRealtime(_transitionDuration);

        if (_optionsContainer != null) _optionsContainer.SetActive(false);
        if (_mainPauseContainer != null) _mainPauseContainer.SetActive(false);
        _isTransitioning = false;
    }
}