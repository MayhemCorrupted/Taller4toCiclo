using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }
    public enum TutorialPhase
    {
        None,
        Movement,
        Combat,
        Inventory
    }
    [Serializable] public class TutorialPromptEntry
    {
        [Tooltip("Opcional: Si se asigna, usa LanguageManager; si está vacío, usa FallbackText.")]
        public LocalizedString LocalizedText;
        [TextArea(1, 3)]
        public string FallbackText;
    }

    [Header("Referencias de UI General")]
    [SerializeField] private GameObject _tutorialPanel;
    [SerializeField] private CanvasGroup _canvasGroup;

    [Header("Estilo Visual y Animación")]
    [SerializeField] private Color _pendingTextColor = Color.white;
    [SerializeField] private Color _completedTextColor = new(0.65f, 0.65f, 0.65f, 0.85f);
    [SerializeField] private float _fadeDuration = 0.25f;
    [SerializeField] private float _hideDelayAfterComplete = 1.1f;
    [SerializeField] private float _armorReminderDuration = 4.5f;

    [Header("Parte 1: Movimiento (3 Líneas TMP)")]
    [SerializeField] private GameObject _movementContainer;
    [SerializeField] private TextMeshProUGUI[] _movementLinesTMP;
    [SerializeField]
    private TutorialPromptEntry _movePrompt = new()
    {
        FallbackText = "Muévete usando [{move}]"
    };
    [SerializeField]
    private TutorialPromptEntry _runPrompt = new()
    {
        FallbackText = "Mantén [{run}] mientras te mueves para correr"
    };
    [SerializeField]
    private TutorialPromptEntry _dodgePrompt = new()
    {
        FallbackText = "Esquiva usando [{dodge}]"
    };

    [Header("Parte 2: Combate en Enemigos (4 Líneas TMP)")]
    [SerializeField] private GameObject _combatContainer;
    [SerializeField] private TextMeshProUGUI[] _combatLinesTMP;
    [Tooltip("Opcional: Lista de enemigos válidos para el tutorial. Si está vacío, contará golpes contra cualquier enemigo.")]
    [SerializeField] private Health[] _tutorialEnemies;
    [Tooltip("Si está activo, exige acertar ambas habilidades especiales (ID 0 y ID 1) en los enemigos.")]
    [SerializeField] private bool _requireBothSpecials = true;
    [SerializeField]
    private TutorialPromptEntry _lightAttackPrompt = new()
    {
        FallbackText = "Golpea al enemigo con un ataque ligero [{light}]"
    };
    [SerializeField]
    private TutorialPromptEntry _heavyAttackPrompt = new()
    {
        FallbackText = "Golpea al enemigo con un ataque fuerte [{heavy}]"
    };
    [SerializeField]
    private TutorialPromptEntry _swapSkillPrompt = new()
    {
        FallbackText = "Cambia de habilidad especial con [{swap}]"
    };
    [SerializeField]
    private TutorialPromptEntry _specialSkillPrompt = new()
    {
        FallbackText = "Acierta tus habilidades especiales [{skill}] en el enemigo"
    };

    [Header("Parte 3: Inventario - Consumible Equipable (2 Líneas TMP)")]
    [SerializeField] private GameObject _inventoryContainer;
    [SerializeField] private TextMeshProUGUI[] _inventoryLinesTMP;
    [Tooltip("Activar si el objeto de prueba no se puede equipar en acceso rápido (ej: Stat Boost) y solo se mueve entre slots generales.")]
    [SerializeField] private bool _useGeneralSlotsOnlyMode = false;
    [SerializeField]
    private TutorialPromptEntry _openInventoryPrompt = new()
    {
        FallbackText = "Abre el inventario con [{inventory}]"
    };
    [SerializeField]
    private TutorialPromptEntry _dragToQuickSlotPrompt = new()
    {
        FallbackText = "Arrastra el consumible al slot de acceso rápido"
    };
    [SerializeField]
    private TutorialPromptEntry _clickToInventorySlotPrompt = new()
    {
        FallbackText = "Haz clic en el consumible equipado y luego clic en un slot de inventario para guardarlo"
    };

    [Header("Parte 3B: Inventario - Solo Slots Generales (Stat Boost)")]
    [SerializeField]
    private TutorialPromptEntry _dragBetweenSlotsPrompt = new()
    {
        FallbackText = "Arrastra el objeto hacia otro slot de tu inventario"
    };
    [SerializeField]
    private TutorialPromptEntry _clickBetweenSlotsPrompt = new()
    {
        FallbackText = "Haz clic en el objeto y luego clic en otro slot de inventario para moverlo"
    };
    [SerializeField]
    private TutorialPromptEntry _armorReminderPrompt = new()
    {
        FallbackText = "Recuerda: Los slots de equipamiento funcionan igual (arrastrar o hacer clic para equipar y desequipar)."
    };
    public TutorialPhase CurrentPhase { get; private set; } = TutorialPhase.None;

    private PlayerInputs _playerInputs;
    private PlayerAnimator _playerAnimator;
    private PlayerCombat _playerCombat;
    private WeaponHitbox _weaponHitbox;
    private InventoryManager _inventoryManager;
    private SlotsUI _slotsUI;

    private string _lastDeviceMode;
    private Coroutine _sequenceCoroutine;
    private TutorialPhase _pendingPhaseAfterStandUp = TutorialPhase.None;

    private bool _hasMoved;
    private bool _hasRan;
    private bool _hasDodged;

    private bool _hasHitLight;
    private bool _hasHitHeavy;
    private bool _hasSwappedSkill;
    private bool _hasHitSkill0;
    private bool _hasHitSkill1;

    private int _inventoryStep;
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        _playerInputs = FindAnyObjectByType<PlayerInputs>();
        _playerAnimator = FindAnyObjectByType<PlayerAnimator>(FindObjectsInactive.Include);
        _playerCombat = FindAnyObjectByType<PlayerCombat>();
        _weaponHitbox = FindAnyObjectByType<WeaponHitbox>(FindObjectsInactive.Include);
        _inventoryManager = FindAnyObjectByType<InventoryManager>();
        _slotsUI = FindAnyObjectByType<SlotsUI>(FindObjectsInactive.Include);

        if (_canvasGroup == null && _tutorialPanel != null)
            _canvasGroup = _tutorialPanel.GetComponent<CanvasGroup>();

        SetActivePhaseContainer(TutorialPhase.None);

        if (_tutorialPanel != null)
            _tutorialPanel.SetActive(false);
    }
    private void OnEnable()
    {
        if (_playerInputs != null)
        {
            _playerInputs.OnSwap += HandleSwapSkill;
            _playerInputs.OnDodge += HandleDodge;
        }

        if (_playerAnimator != null)
            _playerAnimator.OnStandUpCompleted += HandlePlayerStandUpCompleted;

        if (_weaponHitbox != null)
            _weaponHitbox.OnHitTarget += HandleMeleeHitEnemy;

        if (_playerCombat != null)
            _playerCombat.OnAreaSkillHitEnemy += HandleAreaSkillHitEnemy;

        PlayerProjectile.OnAnyProjectileHitEnemy += HandleProjectileHitEnemy;

        if (_slotsUI != null)
            _slotsUI.OnSlotActionProcessed += HandleInventorySlotAction;
    }
    private void OnDisable()
    {
        if (_playerInputs != null)
        {
            _playerInputs.OnSwap -= HandleSwapSkill;
            _playerInputs.OnDodge -= HandleDodge;
        }

        if (_playerAnimator != null)
            _playerAnimator.OnStandUpCompleted -= HandlePlayerStandUpCompleted;

        if (_weaponHitbox != null)
            _weaponHitbox.OnHitTarget -= HandleMeleeHitEnemy;

        if (_playerCombat != null)
            _playerCombat.OnAreaSkillHitEnemy -= HandleAreaSkillHitEnemy;

        PlayerProjectile.OnAnyProjectileHitEnemy -= HandleProjectileHitEnemy;

        if (_slotsUI != null)
            _slotsUI.OnSlotActionProcessed -= HandleInventorySlotAction;

        UnbindAllTexts();
        if (_canvasGroup != null) LeanTween.cancel(_canvasGroup.gameObject);
    }
    private void Update()
    {
        if (_pendingPhaseAfterStandUp != TutorialPhase.None && !ShouldWaitForStandUp())
        {
            HandlePlayerStandUpCompleted();
            return;
        }

        if (CurrentPhase == TutorialPhase.None) return;

        if (_playerInputs != null && _playerInputs.CurrentDeviceMode != _lastDeviceMode)
        {
            _lastDeviceMode = _playerInputs.CurrentDeviceMode;
            RefreshCurrentPhaseUI();
        }

        switch (CurrentPhase)
        {
            case TutorialPhase.Movement:
                EvaluateMovementTutorial();
                break;

            case TutorialPhase.Inventory:
                EvaluateInventoryStep0();
                break;
        }
    }
    public void StartMovementTutorial()
    {
        ResetRoutineAndState();

        if (ShouldWaitForStandUp())
        {
            CurrentPhase = TutorialPhase.None;
            _pendingPhaseAfterStandUp = TutorialPhase.Movement;
            return;
        }

        BeginMovementPhase();
    }
    public void StartCombatTutorial()
    {
        ResetRoutineAndState();

        if (ShouldWaitForStandUp())
        {
            CurrentPhase = TutorialPhase.None;
            _pendingPhaseAfterStandUp = TutorialPhase.Combat;
            return;
        }

        BeginCombatPhase();
    }
    public void SetTutorialEnemies(Health[] enemies)
    {
        _tutorialEnemies = enemies;
    }
    public void SetGeneralSlotsOnlyMode(bool generalOnly)
    {
        _useGeneralSlotsOnlyMode = generalOnly;

        if (CurrentPhase == TutorialPhase.Inventory)
        {
            RefreshCurrentPhaseUI();
        }
    }
    public void StartInventoryTutorial()
    {
        ResetRoutineAndState();

        if (ShouldWaitForStandUp())
        {
            CurrentPhase = TutorialPhase.None;
            _pendingPhaseAfterStandUp = TutorialPhase.Inventory;
            return;
        }

        BeginInventoryPhase();
    }
    public void StartGeneralSlotsInventoryTutorial()
    {
        _useGeneralSlotsOnlyMode = true;
        StartInventoryTutorial();
    }
    public void HideTutorialImmediate()
    {
        ResetRoutineAndState();
        CurrentPhase = TutorialPhase.None;
        _useGeneralSlotsOnlyMode = false;
        UnbindAllTexts();
        SetActivePhaseContainer(TutorialPhase.None);

        if (_tutorialPanel != null)
            _tutorialPanel.SetActive(false);
    }
    private bool ShouldWaitForStandUp()
    {
        if (_playerAnimator == null)
            _playerAnimator = FindAnyObjectByType<PlayerAnimator>(FindObjectsInactive.Include);

        return _playerAnimator != null && (_playerAnimator.IsLaying || _playerAnimator.IsStandingUp);
    }
    private void HandlePlayerStandUpCompleted()
    {
        if (_pendingPhaseAfterStandUp == TutorialPhase.None || ShouldWaitForStandUp()) return;

        TutorialPhase phaseToStart = _pendingPhaseAfterStandUp;
        _pendingPhaseAfterStandUp = TutorialPhase.None;

        switch (phaseToStart)
        {
            case TutorialPhase.Movement:
                BeginMovementPhase();
                break;

            case TutorialPhase.Combat:
                BeginCombatPhase();
                break;

            case TutorialPhase.Inventory:
                BeginInventoryPhase();
                break;
        }
    }
    private void BeginMovementPhase()
    {
        CurrentPhase = TutorialPhase.Movement;
        _hasMoved = false;
        _hasRan = false;
        _hasDodged = false;

        ShowPanel();
        RefreshCurrentPhaseUI();
    }
    private void BeginCombatPhase()
    {
        CurrentPhase = TutorialPhase.Combat;
        _hasHitLight = false;
        _hasHitHeavy = false;
        _hasSwappedSkill = false;
        _hasHitSkill0 = false;
        _hasHitSkill1 = false;

        ShowPanel();
        RefreshCurrentPhaseUI();
    }
    private void BeginInventoryPhase()
    {
        CurrentPhase = TutorialPhase.Inventory;

        bool isInventoryOpen = UIManager.Instance != null && UIManager.Instance.CurrentState == GameState.Inventory;
        _inventoryStep = isInventoryOpen ? 1 : 0;

        ShowPanel();
        RefreshCurrentPhaseUI();
    }
    private void EvaluateMovementTutorial()
    {
        if (_playerInputs == null || !_playerInputs.AreGameplayInputsEnabled) return;
        if (ShouldWaitForStandUp()) return;

        bool changed = false;
        bool isMoving = _playerInputs.MoveInput.sqrMagnitude > 0.05f;

        if (!_hasMoved && isMoving)
        {
            _hasMoved = true;
            changed = true;
        }

        if (!_hasRan && isMoving && _playerInputs.InputRun)
        {
            _hasRan = true;
            changed = true;
        }

        if (changed)
        {
            RefreshCurrentPhaseUI();
            CheckMovementCompletion();
        }
    }
    private void HandleDodge()
    {
        if (CurrentPhase != TutorialPhase.Movement) return;
        if (_playerInputs == null || !_playerInputs.AreGameplayInputsEnabled) return;
        if (ShouldWaitForStandUp()) return;

        if (!_hasDodged)
        {
            _hasDodged = true;
            RefreshCurrentPhaseUI();
            CheckMovementCompletion();
        }
    }
    private void CheckMovementCompletion()
    {
        if (_hasMoved && _hasRan && _hasDodged)
        {
            ScheduleHidePanel(_hideDelayAfterComplete);
        }
    }
    private void HandleMeleeHitEnemy(Health targetHealth, bool isHeavy)
    {
        if (CurrentPhase != TutorialPhase.Combat) return;
        if (!IsValidTutorialEnemy(targetHealth)) return;

        bool changed = false;
        if (!isHeavy && !_hasHitLight)
        {
            _hasHitLight = true;
            changed = true;
        }
        else if (isHeavy && !_hasHitHeavy)
        {
            _hasHitHeavy = true;
            changed = true;
        }

        if (changed) CheckCombatCompletion();
    }
    private void HandleProjectileHitEnemy(Health targetHealth)
    {
        if (CurrentPhase != TutorialPhase.Combat) return;
        if (!IsValidTutorialEnemy(targetHealth)) return;

        if (!_hasHitSkill0)
        {
            _hasHitSkill0 = true;
            CheckCombatCompletion();
        }
    }
    private void HandleAreaSkillHitEnemy(Health targetHealth)
    {
        if (CurrentPhase != TutorialPhase.Combat) return;
        if (!IsValidTutorialEnemy(targetHealth)) return;

        if (!_hasHitSkill1)
        {
            _hasHitSkill1 = true;
            CheckCombatCompletion();
        }
    }
    private void HandleSwapSkill()
    {
        if (CurrentPhase != TutorialPhase.Combat) return;

        if (!_hasSwappedSkill)
        {
            _hasSwappedSkill = true;
            CheckCombatCompletion();
        }
    }
    private bool IsValidTutorialEnemy(Health targetHealth)
    {
        if (targetHealth == null || targetHealth is PlayerHealth) return false;

        if (_tutorialEnemies != null && _tutorialEnemies.Length > 0)
        {
            bool hasAssignedEnemies = false;
            for (int i = 0; i < _tutorialEnemies.Length; i++)
            {
                if (_tutorialEnemies[i] == null) continue;
                hasAssignedEnemies = true;
                if (_tutorialEnemies[i] == targetHealth) return true;
            }

            if (hasAssignedEnemies) return false;
        }

        return true;
    }
    private bool IsSpecialObjectiveCompleted()
    {
        return _requireBothSpecials ? (_hasHitSkill0 && _hasHitSkill1) : (_hasHitSkill0 || _hasHitSkill1);
    }
    private void CheckCombatCompletion()
    {
        RefreshCurrentPhaseUI();

        if (_hasHitLight && _hasHitHeavy && _hasSwappedSkill && IsSpecialObjectiveCompleted())
        {
            ScheduleHidePanel(_hideDelayAfterComplete);
        }
    }
    private void EvaluateInventoryStep0()
    {
        if (_inventoryStep != 0) return;

        if (UIManager.Instance != null && UIManager.Instance.CurrentState == GameState.Inventory)
        {
            _inventoryStep = 1;
            RefreshCurrentPhaseUI();
        }
    }
    private void HandleInventorySlotAction(SelectedSlotType source, SelectedSlotType target, bool isDrag)
    {
        if (CurrentPhase != TutorialPhase.Inventory) return;

        if (_useGeneralSlotsOnlyMode)
        {
            EvaluateGeneralSlotsOnlyAction(source, target, isDrag);
            return;
        }

        if (_inventoryStep == 1 && isDrag)
        {
            if (source == SelectedSlotType.General && target == SelectedSlotType.Consumable)
            {
                if (_inventoryManager != null && _inventoryManager.EquippedConsumable != null)
                {
                    _inventoryStep = 2;
                    RefreshCurrentPhaseUI();
                }
                else
                {
                    _useGeneralSlotsOnlyMode = true;
                    RefreshCurrentPhaseUI();
                }
            }
            else if (source == SelectedSlotType.General && target == SelectedSlotType.General)
            {
                _useGeneralSlotsOnlyMode = true;
                _inventoryStep = 2;
                RefreshCurrentPhaseUI();
            }
        }
        else if (_inventoryStep == 2 && !isDrag &&
                 source == SelectedSlotType.Consumable && target == SelectedSlotType.General)
        {
            _inventoryStep = 3;
            RefreshCurrentPhaseUI();
            ScheduleHidePanel(_armorReminderDuration);
        }
    }
    private void EvaluateGeneralSlotsOnlyAction(SelectedSlotType source, SelectedSlotType target, bool isDrag)
    {
        if (_inventoryStep == 1 && isDrag &&
            source == SelectedSlotType.General && target == SelectedSlotType.General)
        {
            _inventoryStep = 2;
            RefreshCurrentPhaseUI();
        }
        else if (_inventoryStep == 2 && !isDrag &&
                 source == SelectedSlotType.General && target == SelectedSlotType.General)
        {
            _inventoryStep = 3;
            RefreshCurrentPhaseUI();
            ScheduleHidePanel(_armorReminderDuration);
        }
    }
    private void RefreshCurrentPhaseUI()
    {
        UnbindAllTexts();
        SetActivePhaseContainer(CurrentPhase);

        TextMeshProUGUI[] activeLines = GetLinesArrayForPhase(CurrentPhase);
        if (activeLines == null || activeLines.Length == 0) return;

        for (int i = 0; i < activeLines.Length; i++)
        {
            if (activeLines[i] != null)
            {
                activeLines[i].gameObject.SetActive(false);
                activeLines[i].text = string.Empty;
            }
        }

        switch (CurrentPhase)
        {
            case TutorialPhase.Movement:
                SetLine(activeLines, 0, _movePrompt, _hasMoved);
                SetLine(activeLines, 1, _runPrompt, _hasRan);
                SetLine(activeLines, 2, _dodgePrompt, _hasDodged);
                break;

            case TutorialPhase.Combat:
                SetLine(activeLines, 0, _lightAttackPrompt, _hasHitLight);
                SetLine(activeLines, 1, _heavyAttackPrompt, _hasHitHeavy);
                SetLine(activeLines, 2, _swapSkillPrompt, _hasSwappedSkill);
                SetLine(activeLines, 3, _specialSkillPrompt, IsSpecialObjectiveCompleted());
                break;

            case TutorialPhase.Inventory:
                TutorialPromptEntry step1Prompt = _useGeneralSlotsOnlyMode
                    ? _dragBetweenSlotsPrompt
                    : _dragToQuickSlotPrompt;

                TutorialPromptEntry step2Prompt = _useGeneralSlotsOnlyMode
                    ? _clickBetweenSlotsPrompt
                    : _clickToInventorySlotPrompt;

                if (_inventoryStep == 0)
                {
                    SetLine(activeLines, 0, _openInventoryPrompt, false);
                }
                else if (_inventoryStep == 1)
                {
                    SetLine(activeLines, 0, _openInventoryPrompt, true);
                    SetLine(activeLines, 1, step1Prompt, false);
                }
                else if (_inventoryStep == 2)
                {
                    SetLine(activeLines, 0, step1Prompt, true);
                    SetLine(activeLines, 1, step2Prompt, false);
                }
                else if (_inventoryStep == 3)
                {
                    SetLine(activeLines, 0, step2Prompt, true);
                    SetLine(activeLines, 1, _armorReminderPrompt, false);
                }
                break;
        }
    }
    private void SetActivePhaseContainer(TutorialPhase phase)
    {
        if (_movementContainer != null)
            _movementContainer.SetActive(phase == TutorialPhase.Movement);

        if (_combatContainer != null)
            _combatContainer.SetActive(phase == TutorialPhase.Combat);

        if (_inventoryContainer != null)
            _inventoryContainer.SetActive(phase == TutorialPhase.Inventory);
    }
    private TextMeshProUGUI[] GetLinesArrayForPhase(TutorialPhase phase)
    {
        return phase switch
        {
            TutorialPhase.Movement => _movementLinesTMP,
            TutorialPhase.Combat => _combatLinesTMP,
            TutorialPhase.Inventory => _inventoryLinesTMP,
            _ => null
        };
    }
    private void SetLine(TextMeshProUGUI[] targetArray, int lineIndex, TutorialPromptEntry entry, bool isCompleted)
    {
        if (targetArray == null || lineIndex < 0 || lineIndex >= targetArray.Length) return;

        TextMeshProUGUI tmp = targetArray[lineIndex];
        if (tmp == null) return;

        tmp.gameObject.SetActive(true);
        tmp.color = isCompleted ? _completedTextColor : _pendingTextColor;

        if (LanguageManager.Instance != null && entry.LocalizedText != null && !entry.LocalizedText.IsEmpty)
        {
            LanguageManager.Instance.BindText(tmp, entry.LocalizedText, (localizedStr) =>
            {
                if (tmp != null)
                {
                    string formatted = ReplaceKeyPlaceholders(localizedStr);
                    tmp.text = isCompleted ? $"<s>{formatted}</s>" : formatted;
                }
            });
        }
        else
        {
            string formatted = ReplaceKeyPlaceholders(entry.FallbackText);
            tmp.text = isCompleted ? $"<s>{formatted}</s>" : formatted;
        }
    }
    private void UnbindAllTexts()
    {
        if (LanguageManager.Instance == null) return;

        UnbindTextArray(_movementLinesTMP);
        UnbindTextArray(_combatLinesTMP);
        UnbindTextArray(_inventoryLinesTMP);
    }
    private static void UnbindTextArray(TextMeshProUGUI[] lines)
    {
        if (lines == null) return;

        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i] != null)
                LanguageManager.Instance.UnbindText(lines[i]);
        }
    }
    private void ShowPanel()
    {
        if (_tutorialPanel == null) return;

        _lastDeviceMode = _playerInputs != null ? _playerInputs.CurrentDeviceMode : "PC_KeyboardMouse";
        _tutorialPanel.SetActive(true);

        if (_canvasGroup != null)
        {
            LeanTween.cancel(_canvasGroup.gameObject);
            _canvasGroup.alpha = 0f;
            LeanTween.alphaCanvas(_canvasGroup, 1f, _fadeDuration).setEaseOutCubic();
        }
    }
    private void ScheduleHidePanel(float delay)
    {
        if (_sequenceCoroutine != null) StopCoroutine(_sequenceCoroutine);
        _sequenceCoroutine = StartCoroutine(HidePanelRoutine(delay));
    }
    private IEnumerator HidePanelRoutine(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);

        if (_canvasGroup != null)
        {
            LeanTween.cancel(_canvasGroup.gameObject);
            LeanTween.alphaCanvas(_canvasGroup, 0f, _fadeDuration)
                     .setEaseInCubic()
                     .setOnComplete(FinalizeHidePanel);
        }
        else
        {
            FinalizeHidePanel();
        }
    }
    private void FinalizeHidePanel()
    {
        CurrentPhase = TutorialPhase.None;
        _useGeneralSlotsOnlyMode = false;
        UnbindAllTexts();
        SetActivePhaseContainer(TutorialPhase.None);

        if (_tutorialPanel != null)
            _tutorialPanel.SetActive(false);
    }
    private void ResetRoutineAndState()
    {
        _pendingPhaseAfterStandUp = TutorialPhase.None;

        if (_sequenceCoroutine != null)
        {
            StopCoroutine(_sequenceCoroutine);
            _sequenceCoroutine = null;
        }
    }
    private string ReplaceKeyPlaceholders(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        if (_playerInputs == null || _playerInputs.InputSystemAsset == null) return text;

        PlayerInputActions actions = _playerInputs.InputSystemAsset;

        if (text.Contains("{move}"))
            text = text.Replace("{move}", GetBindingDisplay(actions.Gameplay.Move, "WASD"));
        if (text.Contains("{run}"))
            text = text.Replace("{run}", GetBindingDisplay(actions.Gameplay.Run, "Shift"));
        if (text.Contains("{light}"))
            text = text.Replace("{light}", GetBindingDisplay(actions.Gameplay.LightAttack, "Clic Izq."));
        if (text.Contains("{heavy}"))
            text = text.Replace("{heavy}", GetBindingDisplay(actions.Gameplay.HeavyAttack, "Clic Der."));
        if (text.Contains("{skill}"))
            text = text.Replace("{skill}", GetBindingDisplay(actions.Gameplay.Skill, "F"));
        if (text.Contains("{swap}"))
            text = text.Replace("{swap}", GetBindingDisplay(actions.Gameplay.Swap, "Q"));
        if (text.Contains("{dodge}"))
            text = text.Replace("{dodge}", GetBindingDisplay(actions.Gameplay.Dodge, "Espacio"));
        if (text.Contains("{block}"))
            text = text.Replace("{block}", GetBindingDisplay(actions.Gameplay.Block, "Ctrl"));
        if (text.Contains("{interact}"))
            text = text.Replace("{interact}", GetBindingDisplay(actions.Gameplay.Interact, "E"));
        if (text.Contains("{lockon}"))
            text = text.Replace("{lockon}", GetBindingDisplay(actions.Gameplay.LockOn, "Tab"));
        if (text.Contains("{inventory}"))
            text = text.Replace("{inventory}", GetBindingDisplay(actions.UI.OpenInventory, "I"));

        return text;
    }
    private string GetBindingDisplay(InputAction action, string fallback)
    {
        if (action == null) return fallback;

        string controlScheme = _playerInputs.CurrentDeviceMode switch
        {
            "Mobile_Touch" => "Touch",
            "PC_Gamepad" => "Gamepad",
            _ => "KeyboardMouse",
        };

        for (int i = 0; i < action.bindings.Count; i++)
        {
            InputBinding binding = action.bindings[i];

            if (binding.isComposite)
            {
                if (i + 1 < action.bindings.Count &&
                    action.bindings[i + 1].isPartOfComposite &&
                    action.bindings[i + 1].groups.Contains(controlScheme))
                {
                    return action.GetBindingDisplayString(i);
                }
            }
            else if (!binding.isPartOfComposite && binding.groups.Contains(controlScheme))
            {
                return action.GetBindingDisplayString(i);
            }
        }

        return fallback;
    }
}