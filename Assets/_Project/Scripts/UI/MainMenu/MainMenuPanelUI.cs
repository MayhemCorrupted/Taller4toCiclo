using UnityEngine;
public class MainMenuPanelUI : MonoBehaviour
{
    [Header("Contenedores Principales")]
    [SerializeField] private GameObject _mainMenuContainer;
    [SerializeField] private GameObject _playRootContainer;
    [SerializeField] private GameObject _optionsRootContainer;

    [Header("Sub-contenedores de Opciones")]
    [Tooltip("Opcional: Si tienes un panel intermedio solo con los botones 'Audio' y 'Gráficos'.")]
    [SerializeField] private GameObject _optionsCategorySelectorContainer;
    [SerializeField] private GameObject _audioPanelContainer;
    [SerializeField] private GameObject _graphicsPanelContainer;

    [Header("Comportamiento de Apertura")]
    [Tooltip("True: Al entrar a Opciones abre Audio por defecto. False: Muestra primero el selector de categorías.")]
    [SerializeField] private bool _openAudioByDefault = true;
    private void Start()
    {
        ShowMainMenu();
    }
    /// <summary>
    /// Muestra el menú principal y oculta los contenedores de partida y opciones.
    /// </summary>
    public void ShowMainMenu()
    {
        SetActiveSafe(_mainMenuContainer, true);
        SetActiveSafe(_playRootContainer, false);
        SetActiveSafe(_optionsRootContainer, false);
        SetActiveSafe(_optionsCategorySelectorContainer, false);
        SetActiveSafe(_audioPanelContainer, false);
        SetActiveSafe(_graphicsPanelContainer, false);
    }
    /// <summary>
    /// Cambia del menú principal al panel raíz de Slots y Nombre.
    /// </summary>
    public void OpenPlayPanel()
    {
        SetActiveSafe(_mainMenuContainer, false);
        SetActiveSafe(_optionsRootContainer, false);
        SetActiveSafe(_playRootContainer, true);
    }
    /// <summary>
    /// Cierra el panel de Slots y regresa al menú principal.
    /// </summary>
    public void ClosePlayPanelToMainMenu()
    {
        ShowMainMenu();
    }
    /// <summary>
    /// Vincula este método al botón 'Opciones' del menú principal.
    /// </summary>
    public void OpenOptions()
    {
        SetActiveSafe(_mainMenuContainer, false);
        SetActiveSafe(_playRootContainer, false);
        SetActiveSafe(_optionsRootContainer, true);

        if (_openAudioByDefault)
        {
            OpenAudioOptions();
        }
        else
        {
            ShowOptionsCategorySelector();
        }
    }
    /// <summary>
    /// Muestra el selector de categorías (Audio / Gráficos) ocultando los sub-paneles.
    /// </summary>
    public void ShowOptionsCategorySelector()
    {
        SetActiveSafe(_optionsRootContainer, true);
        SetActiveSafe(_optionsCategorySelectorContainer, true);
        SetActiveSafe(_audioPanelContainer, false);
        SetActiveSafe(_graphicsPanelContainer, false);
    }
    /// <summary>
    /// Vincula este método al botón 'Audio' dentro de Opciones.
    /// </summary>
    public void OpenAudioOptions()
    {
        SetActiveSafe(_optionsRootContainer, true);
        if (!_openAudioByDefault)
            SetActiveSafe(_optionsCategorySelectorContainer, false);

        SetActiveSafe(_audioPanelContainer, true);
        SetActiveSafe(_graphicsPanelContainer, false);
    }
    /// <summary>
    /// Vincula este método al botón 'Gráficos' dentro de Opciones.
    /// </summary>
    public void OpenGraphicsOptions()
    {
        SetActiveSafe(_optionsRootContainer, true);
        if (!_openAudioByDefault)
            SetActiveSafe(_optionsCategorySelectorContainer, false);

        SetActiveSafe(_audioPanelContainer, false);
        SetActiveSafe(_graphicsPanelContainer, true);
    }
    /// <summary>
    /// Vincula este método al botón 'Regresar' dentro de los sub-paneles de Audio o Gráficos.
    /// </summary>
    public void BackFromSubPanel()
    {
        if (!_openAudioByDefault && _optionsCategorySelectorContainer != null)
        {
            ShowOptionsCategorySelector();
        }
        else
        {
            CloseOptionsToMainMenu();
        }
    }
    /// <summary>
    /// Vincula este método al botón 'Regresar' general de Opciones hacia el Menú Principal.
    /// </summary>
    public void CloseOptionsToMainMenu()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SaveAudioPreferences();

        if (GraphicsManager.Instance != null)
            GraphicsManager.Instance.SaveGraphicsPreferences();

        ShowMainMenu();
    }
    private static void SetActiveSafe(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active)
            target.SetActive(active);
    }
}