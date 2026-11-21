using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
public interface IInteractable
{
    LocalizedString InteractionPrompt => null;
    void Interact();
}
[RequireComponent(typeof(PlayerInputs))]
[RequireComponent(typeof(PlayerFSM))]
public class PlayerInteraction : MonoBehaviour
{
    public event Action<bool> OnCanInteractChanged;
    public bool CanInteract { get; private set; }

    [Header("Detección")]
    [SerializeField] private LayerMask _interactLayer;
    [SerializeField] private float _interactRange = 3f;
    [Tooltip("Tag del Transform hijo donde aparecerá el texto sobre el objeto interactuable.")]
    [SerializeField] private string _promptAnchorTag = "InteractPrompt";

    [Header("Prompt UI (Canvas)")]
    [SerializeField] private RectTransform _promptContainer;
    [SerializeField] private CanvasGroup _promptCanvasGroup;
    [SerializeField] private TextMeshProUGUI _promptText;
    [Tooltip("Texto localizado de respaldo si un objeto interactuable deja vacío su propio LocalizedString.")]
    [SerializeField] private LocalizedString _defaultPrompt;

    [Header("Posicionamiento y Fade")]
    [SerializeField] private Vector2 _screenOffset = new(0f, 35f);
    [Min(0.01f)][SerializeField] private float _fadeDuration = 0.2f;

    private readonly Collider[] _interactBuffer = new Collider[8];
    private readonly Dictionary<Transform, Transform> _anchorCache = new();
    private PlayerFSM _playerFSM;
    private PlayerInputs _playerInputs;
    private Camera _mainCamera;
    private Canvas _parentCanvas;
    private IInteractable _currentClosestInteractable;
    private IInteractable _boundInteractable;
    private Transform _currentAnchorTransform;
    private string _cachedRawLocalizedText;
    private string _lastDeviceMode;
    private bool _isPromptBound;
    private void Awake()
    {
        _playerFSM = GetComponent<PlayerFSM>();
        _playerInputs = GetComponent<PlayerInputs>();
        _mainCamera = Camera.main;

        if (_promptContainer != null)
        {
            _parentCanvas = _promptContainer.GetComponentInParent<Canvas>();
            if (_promptCanvasGroup == null)
                _promptCanvasGroup = _promptContainer.GetComponent<CanvasGroup>();

            if (_promptCanvasGroup != null)
                _promptCanvasGroup.alpha = 0f;

            _promptContainer.gameObject.SetActive(false);
        }
    }
    private void OnEnable()
    {
        if (_playerInputs != null)
            _playerInputs.OnInteract += TryInteract;
    }
    private void OnDisable()
    {
        if (_playerInputs != null)
            _playerInputs.OnInteract -= TryInteract;

        _currentClosestInteractable = null;
        _currentAnchorTransform = null;
        SetCanInteractState(false, immediateHide: true);
    }
    private void OnDestroy()
    {
        if (_promptContainer != null)
            LeanTween.cancel(_promptContainer.gameObject);

        UnbindPromptText();
    }
    private void Update()
    {
        _currentClosestInteractable = FindClosestValidInteractable(out Transform detectedAnchor);
        _currentAnchorTransform = detectedAnchor;

        bool hasValidTarget = _currentClosestInteractable != null && _currentAnchorTransform != null;
        SetCanInteractState(hasValidTarget, immediateHide: false);

        if (CanInteract && _currentClosestInteractable != _boundInteractable)
        {
            BindTargetPrompt(_currentClosestInteractable);
        }

        if (CanInteract && _playerInputs != null && _playerInputs.CurrentDeviceMode != _lastDeviceMode)
        {
            RefreshPromptKeyDisplay();
        }
    }
    private void LateUpdate()
    {
        if (!CanInteract || _currentAnchorTransform == null || _promptContainer == null)
            return;

        UpdatePromptScreenPosition();
    }
    private void TryInteract()
    {
        IInteractable target = _currentClosestInteractable ?? FindClosestValidInteractable(out _);
        target?.Interact();
    }
    private IInteractable FindClosestValidInteractable(out Transform bestAnchor)
    {
        bestAnchor = null;
        if (_playerFSM == null) return null;

        if (UIManager.Instance != null && UIManager.Instance.CurrentState != GameState.Gameplay)
            return null;

        if (_playerFSM.CurrentState != PlayerState.Idle &&
            _playerFSM.CurrentState != PlayerState.Walking &&
            _playerFSM.CurrentState != PlayerState.Running)
        {
            return null;
        }

        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, _interactRange, _interactBuffer, _interactLayer);
        if (hitCount == 0) return null;

        float closestDistance = Mathf.Infinity;
        IInteractable closestInteractable = null;

        for (int i = 0; i < hitCount; i++)
        {
            Collider col = _interactBuffer[i];
            if (col == null) continue;

            IInteractable interactable = col.GetComponent<IInteractable>();
            interactable ??= col.GetComponentInParent<IInteractable>();

            if (interactable == null) continue;

            if (interactable is Behaviour behaviour && !behaviour.enabled)
                continue;

            Transform anchor = ResolveTaggedAnchor(col, interactable);
            float distance = Vector3.Distance(transform.position, anchor.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestInteractable = interactable;
                bestAnchor = anchor;
            }
        }

        return closestInteractable;
    }
    private Transform ResolveTaggedAnchor(Collider col, IInteractable interactable)
    {
        Component component = (interactable as Component);
        Transform root = component != null ? component.transform : null ?? col.transform;

        if (_anchorCache.TryGetValue(root, out Transform cachedAnchor) && cachedAnchor != null)
            return cachedAnchor;

        Transform foundAnchor = null;

        if (!string.IsNullOrWhiteSpace(_promptAnchorTag))
        {
            if (string.Equals(col.tag, _promptAnchorTag, StringComparison.Ordinal))
            {
                foundAnchor = col.transform;
            }
            else
            {
                Transform[] children = root.GetComponentsInChildren<Transform>(includeInactive: true);
                for (int i = 0; i < children.Length; i++)
                {
                    if (string.Equals(children[i].tag, _promptAnchorTag, StringComparison.Ordinal))
                    {
                        foundAnchor = children[i];
                        break;
                    }
                }
            }
        }

        foundAnchor =
        foundAnchor != null ? foundAnchor : root;
        _anchorCache[root] = foundAnchor;
        return foundAnchor;
    }
    private void SetCanInteractState(bool newState, bool immediateHide)
    {
        if (CanInteract == newState && !immediateHide) return;

        CanInteract = newState;
        OnCanInteractChanged?.Invoke(CanInteract);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetInteractableCursor(CanInteract);
        }

        if (CanInteract)
        {
            ShowPrompt();
        }
        else
        {
            HidePrompt(immediateHide);
        }
    }
    private void ShowPrompt()
    {
        if (_promptContainer == null) return;

        BindTargetPrompt(_currentClosestInteractable);
        UpdatePromptScreenPosition();

        LeanTween.cancel(_promptContainer.gameObject);
        _promptContainer.gameObject.SetActive(true);

        if (_promptCanvasGroup != null)
        {
            LeanTween.alphaCanvas(_promptCanvasGroup, 1f, _fadeDuration)
                     .setEaseOutCubic();
        }
    }
    private void HidePrompt(bool immediate)
    {
        if (_promptContainer == null) return;

        LeanTween.cancel(_promptContainer.gameObject);

        if (immediate || _promptCanvasGroup == null || !_promptContainer.gameObject.activeInHierarchy)
        {
            if (_promptCanvasGroup != null) _promptCanvasGroup.alpha = 0f;
            _promptContainer.gameObject.SetActive(false);
            UnbindPromptText();
            return;
        }

        LeanTween.alphaCanvas(_promptCanvasGroup, 0f, _fadeDuration)
                 .setEaseInCubic()
                 .setOnComplete(() =>
                 {
                     if (!CanInteract && _promptContainer != null)
                     {
                         _promptContainer.gameObject.SetActive(false);
                         UnbindPromptText();
                     }
                 });
    }
    private void BindTargetPrompt(IInteractable target)
    {
        UnbindPromptText();
        _boundInteractable = target;

        if (_promptText == null) return;

        LocalizedString objectPrompt = target?.InteractionPrompt;
        LocalizedString targetString = (objectPrompt != null && !objectPrompt.IsEmpty)
            ? objectPrompt
            : _defaultPrompt;

        if (targetString == null || targetString.IsEmpty)
        {
            _promptText.text = string.Empty;
            return;
        }

        if (LanguageManager.Instance != null)
        {
            _isPromptBound = true;
            LanguageManager.Instance.BindText(_promptText, targetString, rawText =>
            {
                _cachedRawLocalizedText = rawText;
                RefreshPromptKeyDisplay();
            });
        }
    }
    private void UnbindPromptText()
    {
        if (_isPromptBound && _promptText != null && LanguageManager.Instance != null)
        {
            LanguageManager.Instance.UnbindText(_promptText);
        }

        _isPromptBound = false;
        _boundInteractable = null;
        _cachedRawLocalizedText = null;
    }
    private void RefreshPromptKeyDisplay()
    {
        if (_promptText == null || string.IsNullOrEmpty(_cachedRawLocalizedText)) return;

        string keyName = GetCurrentInteractKeyName();
        _promptText.text = _cachedRawLocalizedText.Replace("{key}", keyName);
    }
    private string GetCurrentInteractKeyName()
    {
        if (_playerInputs == null || _playerInputs.InputSystemAsset == null)
            return "E";

        _lastDeviceMode = _playerInputs.CurrentDeviceMode;

        if (_lastDeviceMode == "Mobile_Touch")
            return "Touch";

        string controlScheme = _lastDeviceMode == "PC_Gamepad" ? "Gamepad" : "KeyboardMouse";
        InputAction interactAction = _playerInputs.InputSystemAsset.Gameplay.Interact;

        for (int i = 0; i < interactAction.bindings.Count; i++)
        {
            if (interactAction.bindings[i].groups.Contains(controlScheme))
            {
                return interactAction.GetBindingDisplayString(i);
            }
        }

        return "E";
    }
    private void UpdatePromptScreenPosition()
    {
        if (_currentAnchorTransform == null || _promptContainer == null) return;

        if (_mainCamera == null)
            _mainCamera = Camera.main;

        if (_mainCamera == null) return;

        Vector3 screenPos = _mainCamera.WorldToScreenPoint(_currentAnchorTransform.position);

        if (screenPos.z < 0f)
        {
            if (_promptContainer.gameObject.activeSelf)
                _promptContainer.gameObject.SetActive(false);
            return;
        }

        if (!_promptContainer.gameObject.activeSelf)
            _promptContainer.gameObject.SetActive(true);

        Vector2 finalScreenPoint = (Vector2)screenPos + _screenOffset;

        if (_parentCanvas == null || _parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            _promptContainer.position = finalScreenPoint;
        }
        else if (_promptContainer.parent is RectTransform parentRect)
        {
            Camera uiCam = _parentCanvas.renderMode == RenderMode.ScreenSpaceCamera ? _parentCanvas.worldCamera : null;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, finalScreenPoint, uiCam, out Vector2 localPoint))
            {
                _promptContainer.anchoredPosition = localPoint;
            }
        }
    }
}