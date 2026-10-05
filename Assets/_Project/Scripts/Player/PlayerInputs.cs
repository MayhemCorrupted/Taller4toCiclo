using System;
using UnityEngine;
using UnityEngine.InputSystem;
public class PlayerInputs : MonoBehaviour
{
    public event Action OnLightAttack;
    public event Action OnHeavyAttack;
    public event Action OnSkill;
    public event Action OnHeal;
    public event Action OnInteract;
    public event Action OnOpenInventory;
    public event Action OnLockOn;
    public event Action OnDodge;
    public event Action OnBlock;
    public Vector2 MoveInput { get; private set; }
    public bool InputRun { get; private set; }
    public bool InputBlock { get; private set; }
    public string CurrentDeviceMode { get; private set; }
    private PlayerInputActions _inputActions;
    void Awake()
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
        _inputActions.Gameplay.Heal.performed += ctx => OnHeal?.Invoke();
        _inputActions.Gameplay.LockOn.performed += ctx => OnLockOn?.Invoke();

        _inputActions.UI.OpenInventory.performed += ctx => OnOpenInventory?.Invoke();
    }
    private void OnEnable() => _inputActions.Enable();
    private void OnDisable() => _inputActions.Disable();
    private void Update()
    {
        DetectCurrentDevice();
    }
    private void DetectCurrentDevice()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        CurrentDeviceMode = "Mobile_Touch";
#elif UNITY_STANDALONE || UNITY_EDITOR
        if (Gamepad.current != null)
            CurrentDeviceMode = "PC_Gamepad";
        else
            CurrentDeviceMode = "PC_KeyboardMouse";
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
