using UnityEngine;
public interface IPanel
{
    void OpenPanel();
    void ClosePanel();
}
public enum GameState { Gameplay, Dialogue, Inventory, PauseMenu, Stadistics }
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    public GameState CurrentState { get; private set; } = GameState.Gameplay;

    [Header("Paneles")]
    [SerializeField] private PauseMenuUI _pauseMenuUI;
    [SerializeField] private InventoryPanelUI _inventoryPanelUI;

    [Header("Configuración de Cambio de Pantalla")]
    [Tooltip("Tiempo mínimo en segundos entre pulsaciones para abrir/cerrar pantallas.")]
    [SerializeField] private float _panelToggleCooldown = 0.1f;

    [Header("Sprites / Texturas del Cursor")]
    [Tooltip("Textura del cursor durante el combate/exploración (importar como Texture Type: Cursor).")]
    [SerializeField] private Texture2D _gameplayCursorTexture;
    [Tooltip("Punto activo de clic del cursor de gameplay (ej: centro de la mira).")]
    [SerializeField] private Vector2 _gameplayCursorHotspot = new(16f, 16f);

    [Tooltip("Textura del cursor al abrir paneles como Estadísticas, Inventario o Pausa.")]
    [SerializeField] private Texture2D _uiCursorTexture;
    [Tooltip("Punto activo de clic del cursor de menú (ej: punta de la flecha en 0,0).")]
    [SerializeField] private Vector2 _uiCursorHotspot = Vector2.zero;

    private PlayerInputs _playerInputs;
    private float _lastToggleTime = -1f;
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

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
    private void Start()
    {
        UpdateCursorState();
    }
    public void SetGameState(GameState newState)
    {
        CurrentState = newState;
        UpdateCursorState();
    }
    public void UpdateCursorState()
    {
        if (_playerInputs != null && _playerInputs.CurrentDeviceMode == "Mobile_Touch")
        {
            Cursor.visible = false;
            return;
        }

        bool isPanelOpen = (CurrentState != GameState.Gameplay);
        Cursor.visible = true;
        Cursor.lockState = isPanelOpen ? CursorLockMode.None : CursorLockMode.Confined;

        if (isPanelOpen)
        {
            Cursor.SetCursor(_uiCursorTexture, _uiCursorHotspot, CursorMode.Auto);
        }
        else
        {
            Cursor.SetCursor(_gameplayCursorTexture, _gameplayCursorHotspot, CursorMode.Auto);
        }
    }
    private bool CanTogglePanel()
    {
        if (Time.unscaledTime - _lastToggleTime < _panelToggleCooldown)
            return false;

        _lastToggleTime = Time.unscaledTime;
        return true;
    }
    private void HandleInventoryToggle()
    {
        if (_inventoryPanelUI == null || !CanTogglePanel()) return;

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
        if (_pauseMenuUI == null || !CanTogglePanel()) return;

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