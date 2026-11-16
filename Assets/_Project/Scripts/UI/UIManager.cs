using UnityEngine;
public interface IPanel
{
    void OpenPanel();
    void ClosePanel();
}

public enum GameState { Gameplay, Dialogue, Inventory, PauseMenu, Stadistics, GameOver }
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    public GameState CurrentState { get; private set; } = GameState.Gameplay;
    [Header("Paneles")]
    [SerializeField] private PauseMenuUI _pauseMenuUI;
    [SerializeField] private InventoryPanelUI _inventoryPanelUI;
    [SerializeField] private GameOverUI _gameOverUI;

    [Header("Configuración de Cambio de Pantalla")]
    [Tooltip("Tiempo mínimo en segundos entre pulsaciones para abrir/cerrar pantallas.")]
    [SerializeField] private float _panelToggleCooldown = 0.1f;

    [Header("Sprites / Texturas del Cursor")]
    [Tooltip("Textura del cursor durante el combate/exploración (importar como Texture Type: Cursor).")]
    [SerializeField] private Texture2D _gameplayCursorTexture;
    [Tooltip("Punto activo de clic del cursor de gameplay (ej: centro de la mira).")]
    [SerializeField] private Vector2 _gameplayCursorHotspot = new(16f, 16f);

    [Tooltip("Textura del cursor cuando el jugador está en rango para interactuar con un objeto o NPC.")]
    [SerializeField] private Texture2D _interactCursorTexture;
    [Tooltip("Punto activo del cursor de interacción.")]
    [SerializeField] private Vector2 _interactCursorHotspot = new(16f, 16f);

    [Tooltip("Textura del cursor al abrir paneles como Estadísticas, Inventario, Pausa, Game Over u Opciones de Diálogo.")]
    [SerializeField] private Texture2D _uiCursorTexture;
    [Tooltip("Punto activo de clic del cursor de menú (ej: punta de la flecha en 0,0).")]
    [SerializeField] private Vector2 _uiCursorHotspot = Vector2.zero;

    private PlayerInputs _playerInputs;
    private float _lastToggleTime = -1f;
    private bool _isInteractableAvailable = false;
    private bool _isDialogueChoiceActive = false;
    private string _lastDeviceMode;
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
        if (_playerInputs == null) _playerInputs = FindAnyObjectByType<PlayerInputs>();

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
        SetGameState(CurrentState);
    }
    private void Update()
    {
        if (_playerInputs != null && _playerInputs.CurrentDeviceMode != _lastDeviceMode)
        {
            _lastDeviceMode = _playerInputs.CurrentDeviceMode;
            UpdateCursorState();
        }
    }
    public void SetGameState(GameState newState)
    {
        CurrentState = newState;

        if (newState != GameState.Dialogue)
        {
            _isDialogueChoiceActive = false;
        }

        if (_playerInputs == null) _playerInputs = FindAnyObjectByType<PlayerInputs>();
        if (_playerInputs != null)
        {
            _playerInputs.ApplyGameStateInputs(newState);
        }

        UpdateCursorState();
    }
    public void OpenGameOver()
    {
        if (CurrentState == GameState.GameOver) return;

        SetGameState(GameState.GameOver);
        if (_gameOverUI != null)
        {
            _gameOverUI.OpenPanel();
        }
    }
    public void SetInteractableCursor(bool canInteract)
    {
        if (_isInteractableAvailable == canInteract) return;

        _isInteractableAvailable = canInteract;
        if (CurrentState == GameState.Gameplay)
        {
            UpdateCursorState();
        }
    }
    public void SetDialogueOptionsVisible(bool isVisible)
    {
        if (_isDialogueChoiceActive == isVisible) return;

        _isDialogueChoiceActive = isVisible;
        if (CurrentState == GameState.Dialogue)
        {
            UpdateCursorState();
        }
    }
    public void UpdateCursorState()
    {
        bool isTouchDevice = _playerInputs != null && _playerInputs.CurrentDeviceMode == "Mobile_Touch";

        if (isTouchDevice)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            return;
        }

        if (CurrentState == GameState.Dialogue)
        {
            if (_isDialogueChoiceActive)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                Cursor.SetCursor(_uiCursorTexture, _uiCursorHotspot, CursorMode.Auto);
            }
            else
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.None;
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            }
            return;
        }

        bool isMenuOpen = (CurrentState != GameState.Gameplay);
        Cursor.visible = true;
        Cursor.lockState = isMenuOpen ? CursorLockMode.None : CursorLockMode.Confined;

        if (isMenuOpen)
        {
            Cursor.SetCursor(_uiCursorTexture, _uiCursorHotspot, CursorMode.Auto);
        }
        else if (_isInteractableAvailable && _interactCursorTexture != null)
        {
            Cursor.SetCursor(_interactCursorTexture, _interactCursorHotspot, CursorMode.Auto);
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