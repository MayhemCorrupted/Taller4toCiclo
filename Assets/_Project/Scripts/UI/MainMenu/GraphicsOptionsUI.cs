using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class GraphicsOptionsUI : MonoBehaviour
{
    [Header("Dropdowns Gráficos (TMP_Dropdown)")]
    [SerializeField] private TMP_Dropdown _resolutionDropdown;
    [SerializeField] private TMP_Dropdown _screenModeDropdown;
    [SerializeField] private TMP_Dropdown _qualityDropdown;
    [SerializeField] private TMP_Dropdown _shadowsDropdown;
    [SerializeField] private TMP_Dropdown _fpsLimitDropdown;
    [SerializeField] private TMP_Dropdown _antiAliasingDropdown;

    [Header("Toggle V-Sync")]
    [SerializeField] private Toggle _vSyncToggle;

    [Header("Contenedores de Fila Exclusivos de PC (Opcional)")]
    [Tooltip("Se ocultarán automáticamente si el juego corre en Android.")]
    [SerializeField] private GameObject _resolutionRowObject;
    [SerializeField] private GameObject _screenModeRowObject;
    [SerializeField] private GameObject _vSyncRowObject;
    private bool _isInitialized;
    private void Awake()
    {
        ConfigurePlatformVisibility();
    }
    private void OnEnable()
    {
        if (GraphicsManager.Instance == null) return;

        if (!_isInitialized)
        {
            PopulateAllDropdowns();
            _isInitialized = true;
        }

        SyncUIFromManager();
        RegisterListeners();
    }
    private void OnDisable()
    {
        UnregisterListeners();

        if (GraphicsManager.Instance != null)
            GraphicsManager.Instance.SaveGraphicsPreferences();
    }
    private void ConfigurePlatformVisibility()
    {
        bool isDesktop = !Application.isMobilePlatform;
        if (_resolutionRowObject != null) _resolutionRowObject.SetActive(isDesktop);
        if (_screenModeRowObject != null) _screenModeRowObject.SetActive(isDesktop);
        if (_vSyncRowObject != null) _vSyncRowObject.SetActive(isDesktop);
    }
    private void PopulateAllDropdowns()
    {
        if (_resolutionDropdown != null)
        {
            _resolutionDropdown.ClearOptions();
            var resOptions = new List<string>();
            var resolutions = GraphicsManager.Instance.AvailableResolutions;
            for (int i = 0; i < resolutions.Count; i++)
            {
                resOptions.Add($"{resolutions[i].width} x {resolutions[i].height}");
            }
            _resolutionDropdown.AddOptions(resOptions);
        }

        if (_screenModeDropdown != null)
        {
            _screenModeDropdown.ClearOptions();
            _screenModeDropdown.AddOptions(new List<string>
            {
                "Pantalla completa",
                "Sin bordes",
                "Ventana"
            });
        }

        if (_qualityDropdown != null)
        {
            _qualityDropdown.ClearOptions();
            _qualityDropdown.AddOptions(new List<string>
            {
                "Baja",
                "Media",
                "Alta",
                "Ultra"
            });
        }

        if (_shadowsDropdown != null)
        {
            _shadowsDropdown.ClearOptions();
            _shadowsDropdown.AddOptions(new List<string>
            {
                "Desactivadas",
                "Bajas",
                "Medias",
                "Altas"
            });
        }

        if (_fpsLimitDropdown != null)
        {
            _fpsLimitDropdown.ClearOptions();
            _fpsLimitDropdown.AddOptions(new List<string>
            {
                "30 FPS",
                "60 FPS",
                "120 FPS",
                "Ilimitado"
            });
        }

        if (_antiAliasingDropdown != null)
        {
            _antiAliasingDropdown.ClearOptions();
            _antiAliasingDropdown.AddOptions(new List<string>
            {
                "Desactivado",
                "2x MSAA",
                "4x MSAA",
                "8x MSAA"
            });
        }
    }
    private void SyncUIFromManager()
    {
        var gfx = GraphicsManager.Instance;
        if (gfx == null) return;

        if (_resolutionDropdown != null) _resolutionDropdown.SetValueWithoutNotify(gfx.ResolutionIndex);
        if (_screenModeDropdown != null) _screenModeDropdown.SetValueWithoutNotify(gfx.ScreenModeIndex);
        if (_qualityDropdown != null) _qualityDropdown.SetValueWithoutNotify(gfx.QualityIndex);
        if (_shadowsDropdown != null) _shadowsDropdown.SetValueWithoutNotify(gfx.ShadowIndex);
        if (_fpsLimitDropdown != null) _fpsLimitDropdown.SetValueWithoutNotify(gfx.FpsLimitIndex);
        if (_antiAliasingDropdown != null) _antiAliasingDropdown.SetValueWithoutNotify(gfx.AntiAliasingIndex);
        if (_vSyncToggle != null) _vSyncToggle.SetIsOnWithoutNotify(gfx.VSyncEnabled);
    }
    private void RegisterListeners()
    {
        if (_resolutionDropdown != null) _resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        if (_screenModeDropdown != null) _screenModeDropdown.onValueChanged.AddListener(OnScreenModeChanged);
        if (_qualityDropdown != null) _qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
        if (_shadowsDropdown != null) _shadowsDropdown.onValueChanged.AddListener(OnShadowsChanged);
        if (_fpsLimitDropdown != null) _fpsLimitDropdown.onValueChanged.AddListener(OnFpsLimitChanged);
        if (_antiAliasingDropdown != null) _antiAliasingDropdown.onValueChanged.AddListener(OnAntiAliasingChanged);
        if (_vSyncToggle != null) _vSyncToggle.onValueChanged.AddListener(OnVSyncChanged);
    }
    private void UnregisterListeners()
    {
        if (_resolutionDropdown != null) _resolutionDropdown.onValueChanged.RemoveListener(OnResolutionChanged);
        if (_screenModeDropdown != null) _screenModeDropdown.onValueChanged.RemoveListener(OnScreenModeChanged);
        if (_qualityDropdown != null) _qualityDropdown.onValueChanged.RemoveListener(OnQualityChanged);
        if (_shadowsDropdown != null) _shadowsDropdown.onValueChanged.RemoveListener(OnShadowsChanged);
        if (_fpsLimitDropdown != null) _fpsLimitDropdown.onValueChanged.RemoveListener(OnFpsLimitChanged);
        if (_antiAliasingDropdown != null) _antiAliasingDropdown.onValueChanged.RemoveListener(OnAntiAliasingChanged);
        if (_vSyncToggle != null) _vSyncToggle.onValueChanged.RemoveListener(OnVSyncChanged);
    }
    private void OnResolutionChanged(int index)
    {
        if (GraphicsManager.Instance != null)
            GraphicsManager.Instance.SetResolution(index);
    }
    private void OnScreenModeChanged(int index)
    {
        if (GraphicsManager.Instance != null)
            GraphicsManager.Instance.SetScreenMode(index);
    }
    private void OnQualityChanged(int index)
    {
        if (GraphicsManager.Instance != null)
            GraphicsManager.Instance.SetQuality(index);
    }
    private void OnShadowsChanged(int index)
    {
        if (GraphicsManager.Instance != null)
            GraphicsManager.Instance.SetShadows(index);
    }
    private void OnFpsLimitChanged(int index)
    {
        if (GraphicsManager.Instance != null)
            GraphicsManager.Instance.SetFrameLimit(index);
    }
    private void OnAntiAliasingChanged(int index)
    {
        if (GraphicsManager.Instance != null)
            GraphicsManager.Instance.SetAntiAliasing(index);
    }
    private void OnVSyncChanged(bool isOn)
    {
        if (GraphicsManager.Instance != null)
            GraphicsManager.Instance.SetVSync(isOn);
    }
}