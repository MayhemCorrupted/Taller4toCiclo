using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

using ShadowQuality = UnityEngine.ShadowQuality;
using ShadowResolution = UnityEngine.ShadowResolution;
public class GraphicsManager : MonoBehaviour
{
    public static GraphicsManager Instance { get; private set; }

    private const string RES_INDEX_KEY = "Gfx_ResolutionIndex";
    private const string SCREEN_MODE_KEY = "Gfx_ScreenModeIndex";
    private const string QUALITY_KEY = "Gfx_QualityIndex";
    private const string SHADOW_KEY = "Gfx_ShadowIndex";
    private const string FPS_LIMIT_KEY = "Gfx_FpsLimitIndex";
    private const string AA_KEY = "Gfx_AntiAliasingIndex";
    private const string VSYNC_KEY = "Gfx_VSyncEnabled";

    private readonly List<Resolution> _filteredResolutions = new();

    public IReadOnlyList<Resolution> AvailableResolutions => _filteredResolutions;
    public int ResolutionIndex { get; private set; }
    public int ScreenModeIndex { get; private set; }   // 0: Pantalla completa, 1: Sin bordes, 2: Ventana
    public int QualityIndex { get; private set; }
    public int ShadowIndex { get; private set; }       // 0: Desactivadas, 1: Bajas, 2: Medias, 3: Altas
    public int FpsLimitIndex { get; private set; }     // 0: 30 FPS, 1: 60 FPS, 2: 120 FPS, 3: Ilimitado
    public int AntiAliasingIndex { get; private set; } // 0: Desactivado, 1: 2x, 2: 4x, 3: 8x
    public bool VSyncEnabled { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildFilteredResolutions();
        LoadAndApplyAllSettings();
    }
    private void BuildFilteredResolutions()
    {
        _filteredResolutions.Clear();
        Resolution[] allRes = Screen.resolutions;
        var seenDimensions = new HashSet<(int w, int h)>();

        for (int i = allRes.Length - 1; i >= 0; i--)
        {
            var key = (allRes[i].width, allRes[i].height);
            if (seenDimensions.Add(key))
            {
                _filteredResolutions.Add(allRes[i]);
            }
        }

        _filteredResolutions.Reverse();

        if (_filteredResolutions.Count == 0)
        {
            _filteredResolutions.Add(Screen.currentResolution);
        }
    }
    private int FindCurrentResolutionIndex()
    {
        for (int i = 0; i < _filteredResolutions.Count; i++)
        {
            if (_filteredResolutions[i].width == Screen.width &&
                _filteredResolutions[i].height == Screen.height)
            {
                return i;
            }
        }
        return _filteredResolutions.Count - 1;
    }
    private void LoadAndApplyAllSettings()
    {
        int defaultRes = FindCurrentResolutionIndex();
        int defaultQuality = QualitySettings.GetQualityLevel();
        int defaultFps = 1; // 60 FPS por defecto

        ResolutionIndex = Mathf.Clamp(PlayerPrefs.GetInt(RES_INDEX_KEY, defaultRes), 0, _filteredResolutions.Count - 1);
        ScreenModeIndex = Mathf.Clamp(PlayerPrefs.GetInt(SCREEN_MODE_KEY, 1), 0, 2); // Sin bordes por defecto
        QualityIndex = Mathf.Clamp(PlayerPrefs.GetInt(QUALITY_KEY, defaultQuality), 0, QualitySettings.names.Length - 1);
        ShadowIndex = Mathf.Clamp(PlayerPrefs.GetInt(SHADOW_KEY, 2), 0, 3); // Medias por defecto
        FpsLimitIndex = Mathf.Clamp(PlayerPrefs.GetInt(FPS_LIMIT_KEY, defaultFps), 0, 3);
        AntiAliasingIndex = Mathf.Clamp(PlayerPrefs.GetInt(AA_KEY, 1), 0, 3); // 2x MSAA por defecto
        VSyncEnabled = PlayerPrefs.GetInt(VSYNC_KEY, 0) == 1;

        ApplyQuality(QualityIndex, save: false);
        ApplyDisplaySettings(ResolutionIndex, ScreenModeIndex, save: false);
        ApplyShadows(ShadowIndex, save: false);
        ApplyAntiAliasing(AntiAliasingIndex, save: false);
        ApplyVSyncAndFrameLimit(VSyncEnabled, FpsLimitIndex, save: false);
    }
    public void SetResolution(int index)
    {
        if (index < 0 || index >= _filteredResolutions.Count) return;
        ApplyDisplaySettings(index, ScreenModeIndex, save: true);
    }
    public void SetScreenMode(int index)
    {
        if (index < 0 || index > 2) return;
        ApplyDisplaySettings(ResolutionIndex, index, save: true);
    }
    public void SetQuality(int index)
    {
        if (index < 0 || index >= QualitySettings.names.Length) return;
        ApplyQuality(index, save: true);
        ApplyShadows(ShadowIndex, save: false);
        ApplyAntiAliasing(AntiAliasingIndex, save: false);
        ApplyVSyncAndFrameLimit(VSyncEnabled, FpsLimitIndex, save: false);
    }
    public void SetShadows(int index)
    {
        if (index < 0 || index > 3) return;
        ApplyShadows(index, save: true);
    }
    public void SetFrameLimit(int index)
    {
        if (index < 0 || index > 3) return;
        ApplyVSyncAndFrameLimit(VSyncEnabled, index, save: true);
    }
    public void SetAntiAliasing(int index)
    {
        if (index < 0 || index > 3) return;
        ApplyAntiAliasing(index, save: true);
    }
    public void SetVSync(bool enabled)
    {
        ApplyVSyncAndFrameLimit(enabled, FpsLimitIndex, save: true);
    }
    public void SaveGraphicsPreferences()
    {
        PlayerPrefs.Save();
        GameLogger.LogInfo("Preferencias gráficas guardadas en disco.");
    }
    private void ApplyDisplaySettings(int resIndex, int modeIndex, bool save)
    {
        ResolutionIndex = resIndex;
        ScreenModeIndex = modeIndex;

        if (!Application.isMobilePlatform && _filteredResolutions.Count > 0)
        {
            Resolution targetRes = _filteredResolutions[ResolutionIndex];
            FullScreenMode mode = modeIndex switch
            {
                0 => FullScreenMode.ExclusiveFullScreen,
                1 => FullScreenMode.FullScreenWindow,
                2 => FullScreenMode.Windowed,
                _ => FullScreenMode.FullScreenWindow
            };

            Screen.SetResolution(targetRes.width, targetRes.height, mode);
        }

        if (save)
        {
            PlayerPrefs.SetInt(RES_INDEX_KEY, ResolutionIndex);
            PlayerPrefs.SetInt(SCREEN_MODE_KEY, ScreenModeIndex);
        }
    }
    private void ApplyQuality(int index, bool save)
    {
        QualityIndex = index;
        QualitySettings.SetQualityLevel(QualityIndex, applyExpensiveChanges: true);

        if (save)
            PlayerPrefs.SetInt(QUALITY_KEY, QualityIndex);
    }
    private void ApplyShadows(int index, bool save)
    {
        ShadowIndex = index;
        UniversalRenderPipelineAsset urpAsset = GetActiveURPAsset();

        switch (ShadowIndex)
        {
            case 0: // Desactivadas
                QualitySettings.shadows = ShadowQuality.Disable;
                if (urpAsset != null)
                {
                    urpAsset.shadowDistance = 0f;
                }
                break;

            case 1: // Bajas
                QualitySettings.shadows = ShadowQuality.HardOnly;
                QualitySettings.shadowResolution = ShadowResolution.Low;
                if (urpAsset != null)
                {
                    urpAsset.shadowDistance = 25f;
                    urpAsset.mainLightShadowmapResolution = 512;
                }
                break;

            case 2: // Medias
                QualitySettings.shadows = ShadowQuality.All;
                QualitySettings.shadowResolution = ShadowResolution.Medium;
                if (urpAsset != null)
                {
                    urpAsset.shadowDistance = 50f;
                    urpAsset.mainLightShadowmapResolution = 1024;
                }
                break;

            case 3: // Altas
                QualitySettings.shadows = ShadowQuality.All;
                QualitySettings.shadowResolution = ShadowResolution.High;
                if (urpAsset != null)
                {
                    urpAsset.shadowDistance = 90f;
                    urpAsset.mainLightShadowmapResolution = 2048;
                }
                break;
        }

        if (save)
            PlayerPrefs.SetInt(SHADOW_KEY, ShadowIndex);
    }
    private void ApplyAntiAliasing(int index, bool save)
    {
        AntiAliasingIndex = index;
        int msaaSamples = index switch
        {
            0 => 1, // Desactivado
            1 => 2, // 2x MSAA
            2 => 4, // 4x MSAA
            3 => 8, // 8x MSAA
            _ => 1
        };

        UniversalRenderPipelineAsset urpAsset = GetActiveURPAsset();
        if (urpAsset != null)
        {
            urpAsset.msaaSampleCount = msaaSamples;
        }
        else
        {
            QualitySettings.antiAliasing = msaaSamples == 1 ? 0 : msaaSamples;
        }

        if (save)
            PlayerPrefs.SetInt(AA_KEY, AntiAliasingIndex);
    }
    private void ApplyVSyncAndFrameLimit(bool vsync, int fpsIndex, bool save)
    {
        VSyncEnabled = vsync;
        FpsLimitIndex = fpsIndex;

        QualitySettings.vSyncCount = (!Application.isMobilePlatform && VSyncEnabled) ? 1 : 0;

        Application.targetFrameRate = FpsLimitIndex switch
        {
            0 => 30,
            1 => 60,
            2 => 120,
            3 => -1, // Ilimitado
            _ => 60
        };

        if (save)
        {
            PlayerPrefs.SetInt(VSYNC_KEY, VSyncEnabled ? 1 : 0);
            PlayerPrefs.SetInt(FPS_LIMIT_KEY, FpsLimitIndex);
        }
    }
    private static UniversalRenderPipelineAsset GetActiveURPAsset()
    {
        if (QualitySettings.renderPipeline is UniversalRenderPipelineAsset qualityUrp)
            return qualityUrp;

        return GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
    }
}