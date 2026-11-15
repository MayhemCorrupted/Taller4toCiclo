using System;
using UnityEngine;
using UnityEngine.InputSystem;
public class PlayerInputs : MonoBehaviour
{
    public event Action OnLightAttack;
    public event Action OnHeavyAttack;
    public event Action OnSkill;
    public event Action OnInteract;
    public event Action OnOpenInventory;
    public event Action OnLockOn;
    public event Action<float> OnSwitchTarget;
    public event Action OnDodge;
    public event Action OnPause;
    public event Action OnSwap;
    public event Action OnUseItem;
    public Vector2 MoveInput { get; private set; }
    public bool InputRun { get; private set; }
    public bool InputBlock { get; private set; }
    public string CurrentDeviceMode { get; private set; } = "PC_KeyboardMouse";

    private PlayerInputActions _inputActions;
    private void Awake()
    {
        _inputActions = new();

        _inputActions.Gameplay.Move.performed += ctx => MoveInput = ctx.ReadValue<Vector2>();
        _inputActions.Gameplay.Move.canceled += ctx => MoveInput = Vector2.zero;

        _inputActions.Gameplay.Run.performed += ctx => InputRun = true;
        _inputActions.Gameplay.Run.canceled += ctx => InputRun = false;

        _inputActions.Gameplay.Block.performed += ctx => InputBlock = true;
        _inputActions.Gameplay.Block.canceled += ctx => InputBlock = false;

        _inputActions.Gameplay.LightAttack.performed += ctx => OnLightAttack?.Invoke();
        _inputActions.Gameplay.HeavyAttack.performed += ctx => OnHeavyAttack?.Invoke();
        _inputActions.Gameplay.Skill.performed += ctx => OnSkill?.Invoke();
        _inputActions.Gameplay.Dodge.performed += ctx => OnDodge?.Invoke();
        _inputActions.Gameplay.Interact.performed += ctx => OnInteract?.Invoke();
        _inputActions.Gameplay.LockOn.performed += ctx => OnLockOn?.Invoke();
        _inputActions.Gameplay.UseItem.performed += ctx => OnUseItem?.Invoke();
        _inputActions.Gameplay.Swap.performed += ctx => OnSwap?.Invoke();

        _inputActions.UI.OpenInventory.performed += ctx => OnOpenInventory?.Invoke();
        _inputActions.UI.Pause.performed += ctx => OnPause?.Invoke();
        InitializeDeviceMode();
    }
    private void OnEnable()
    {
        _inputActions.Enable();
    }
    private void OnDisable()
    {
        _inputActions.Disable();
    }
    private void OnDestroy()
    {
        _inputActions.Dispose();
    }
    private void Update()
    {
        DetectCurrentDevice();
        ReadMouseScroll();
    }
    private void InitializeDeviceMode()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        CurrentDeviceMode = "Mobile_Touch";
#else
        CurrentDeviceMode = "PC_KeyboardMouse";
#endif
    }
    private void ReadMouseScroll()
    {
        if (Mouse.current == null) return;

        float scrollY = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Abs(scrollY) > 0.01f)
        {
            OnSwitchTarget?.Invoke(Mathf.Sign(scrollY));
        }
    }
    private void DetectCurrentDevice()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        CurrentDeviceMode = "Mobile_Touch";
#elif UNITY_STANDALONE || UNITY_EDITOR
        bool usedMouseOrKeyboard =
            (Mouse.current != null && (Mouse.current.delta.ReadValue().sqrMagnitude > 0.1f ||
                                       Mouse.current.leftButton.wasPressedThisFrame ||
                                       Mouse.current.rightButton.wasPressedThisFrame)) ||
            (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame);

        if (usedMouseOrKeyboard)
        {
            CurrentDeviceMode = "PC_KeyboardMouse";
        }
        else if (Gamepad.current != null && Gamepad.current.wasUpdatedThisFrame)
        {
            CurrentDeviceMode = "PC_Gamepad";
        }
#endif
    }
    public void RebindAction(InputAction actionToRebind, int bindingIndex, Action onRebindComplete)
    {
        actionToRebind.Disable();

        actionToRebind.PerformInteractiveRebinding(bindingIndex)
            .OnComplete(operation =>
            {
                operation.Dispose();
                actionToRebind.Enable();
                onRebindComplete?.Invoke();
            })
            .Start();
    }
    public void ResetAllBindings()
    {
        _inputActions.RemoveAllBindingOverrides();
    }
    public void ResetSpecificBinding(InputAction actionToReset)
    {
        actionToReset.RemoveAllBindingOverrides();
    }
    public PlayerInputActions InputSystemAsset => _inputActions;
}