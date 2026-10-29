using UnityEngine;
public interface IPanel
{
    void OpenPanel();
    void ClosePanel();
}
public enum GameState { Gameplay, Dialogue, Inventory, PauseMenu }
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    public GameState CurrentState { get; private set; } = GameState.Gameplay;
    [SerializeField] private PauseMenuUI _pauseMenuUI;
    [SerializeField] private InventoryPanelUI _inventoryPanelUI;
    private PlayerInputs _playerInputs;
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        _playerInputs = FindAnyObjectByType<PlayerInputs>();
    }
    private void OnEnable()
    {
        if (_playerInputs != null)
        {
            _playerInputs.OnOpenInventory += HandleInventoryToggle;
            _playerInputs.OnPause += HandlePauseToggle;
        }
    }
    private void OnDisable()
    {
        if (_playerInputs != null)
        {
            _playerInputs.OnOpenInventory -= HandleInventoryToggle;
            _playerInputs.OnPause -= HandlePauseToggle;
        }
    }
    public void SetGameState(GameState newState)
    {
        CurrentState = newState;

        bool needsCursor = (CurrentState != GameState.Gameplay);
        if (_playerInputs.CurrentDeviceMode == "PC_KeyboardMouse")
        {
            Cursor.visible = needsCursor;
            Cursor.lockState = needsCursor ? CursorLockMode.None : CursorLockMode.Confined;
        }
    }
    private void HandleInventoryToggle()
    {
        if (_inventoryPanelUI == null) return;
        if (CurrentState == GameState.Gameplay)
        {
            SetGameState(GameState.Inventory);
            _inventoryPanelUI.OpenPanel();
        }
        else if (CurrentState == GameState.Inventory)
        {
            SetGameState(GameState.Gameplay);
            _inventoryPanelUI.ClosePanel();
        }
    }
    private void HandlePauseToggle()
    {
        if (_pauseMenuUI == null) return;

        if (CurrentState == GameState.Gameplay)
        {
            SetGameState(GameState.PauseMenu);
            _pauseMenuUI.OpenPanel();
        }
        else if (CurrentState == GameState.PauseMenu)
        {
            SetGameState(GameState.Gameplay);
            _pauseMenuUI.ClosePanel();
        }
    }
}