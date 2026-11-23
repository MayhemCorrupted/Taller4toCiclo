using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
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
    public int QualityIndex { get; private set; }      // 0: Baja, 1: Media, 2: Alta, 3: Ultra
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

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }
    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplySceneLightsShadowMode(ShadowIndex);
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
        bool isMobile = Application.isMobilePlatform;
        int defaultRes = FindCurrentResolutionIndex();
        int defaultQuality = isMobile ? 1 : 2; // Media en móvil, Alta en PC por defecto
        int defaultShadows = isMobile ? 1 : 2; // Bajas en móvil, Medias en PC por defecto
        int defaultFps = 1;                    // 60 FPS por defecto

        ResolutionIndex = Mathf.Clamp(PlayerPrefs.GetInt(RES_INDEX_KEY, defaultRes), 0, _filteredResolutions.Count - 1);
        ScreenModeIndex = Mathf.Clamp(PlayerPrefs.GetInt(SCREEN_MODE_KEY, 1), 0, 2);
        QualityIndex = Mathf.Clamp(PlayerPrefs.GetInt(QUALITY_KEY, defaultQuality), 0, 3);
        ShadowIndex = Mathf.Clamp(PlayerPrefs.GetInt(SHADOW_KEY, defaultShadows), 0, 3);
        FpsLimitIndex = Mathf.Clamp(PlayerPrefs.GetInt(FPS_LIMIT_KEY, defaultFps), 0, 3);
        AntiAliasingIndex = Mathf.Clamp(PlayerPrefs.GetInt(AA_KEY, isMobile ? 0 : 1), 0, 3);
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
        if (index < 0 || index > 3) return;
        ApplyQuality(index, save: true);
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
        bool isMobile = Application.isMobilePlatform;
        UniversalRenderPipelineAsset urpAsset = GetActiveURPAsset();

        switch (QualityIndex)
        {
            case 0: // Baja (Máxima reducción de polígonos, texturas y carga de renderizado)
                QualitySettings.lodBias = isMobile ? 0.35f : 0.40f;
                QualitySettings.maximumLODLevel = 1; // Omite LOD 0 en objetos con LODGroup
                QualitySettings.skinWeights = SkinWeights.TwoBones;
                QualitySettings.globalTextureMipmapLimit = 2; // 1/4 de resolución de texturas
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
                if (urpAsset != null)
                {
                    urpAsset.renderScale = isMobile ? 0.65f : 0.75f;
                    urpAsset.maxAdditionalLightsCount = isMobile ? 1 : 2;
                }
                break;

            case 1: // Media (Reducción moderada de polígonos y texturas)
                QualitySettings.lodBias = isMobile ? 0.60f : 0.70f;
                QualitySettings.maximumLODLevel = 0;
                QualitySettings.skinWeights = SkinWeights.FourBones;
                QualitySettings.globalTextureMipmapLimit = 1; // 1/2 de resolución de texturas
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
                if (urpAsset != null)
                {
                    urpAsset.renderScale = isMobile ? 0.80f : 0.90f;
                    urpAsset.maxAdditionalLightsCount = isMobile ? 2 : 4;
                }
                break;

            case 2: // Alta (Geometría y texturas estándar sin recortes)
                QualitySettings.lodBias = isMobile ? 0.85f : 1.00f;
                QualitySettings.maximumLODLevel = 0;
                QualitySettings.skinWeights = SkinWeights.FourBones;
                QualitySettings.globalTextureMipmapLimit = 0; // Resolución completa
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
                if (urpAsset != null)
                {
                    urpAsset.renderScale = isMobile ? 0.90f : 1.00f;
                    urpAsset.maxAdditionalLightsCount = isMobile ? 4 : 6;
                }
                break;

            case 3: // Ultra (Conserva mallas de máximos polígonos a mayor distancia)
                QualitySettings.lodBias = isMobile ? 1.15f : 1.50f;
                QualitySettings.maximumLODLevel = 0;
                QualitySettings.skinWeights = SkinWeights.FourBones;
                QualitySettings.globalTextureMipmapLimit = 0; // Resolución completa
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
                if (urpAsset != null)
                {
                    urpAsset.renderScale = 1.00f;
                    urpAsset.maxAdditionalLightsCount = isMobile ? 4 : 8;
                }
                break;
        }

        if (save)
            PlayerPrefs.SetInt(QUALITY_KEY, QualityIndex);
    }
    private void ApplyShadows(int index, bool save)
    {
        ShadowIndex = index;
        bool isMobile = Application.isMobilePlatform;
        UniversalRenderPipelineAsset urpAsset = GetActiveURPAsset();

        switch (ShadowIndex)
        {
            case 0: // Desactivadas
                QualitySettings.shadows = ShadowQuality.Disable;
                if (urpAsset != null)
                {
                    urpAsset.shadowDistance = 0f;
                    urpAsset.shadowCascadeCount = 1;
                }
                break;

            case 1: // Bajas (Sombras duras, menor resolución y 1 cascada)
                QualitySettings.shadows = ShadowQuality.HardOnly;
                QualitySettings.shadowResolution = ShadowResolution.Low;
                if (urpAsset != null)
                {
                    urpAsset.shadowDistance = isMobile ? 18f : 25f;
                    urpAsset.mainLightShadowmapResolution = isMobile ? 256 : 512;
                    urpAsset.additionalLightsShadowmapResolution = 256;
                    urpAsset.shadowCascadeCount = 1;
                }
                break;

            case 2: // Medias (Sombras suaves, resolución media y 2 cascadas)
                QualitySettings.shadows = ShadowQuality.All;
                QualitySettings.shadowResolution = ShadowResolution.Medium;
                if (urpAsset != null)
                {
                    urpAsset.shadowDistance = isMobile ? 35f : 50f;
                    urpAsset.mainLightShadowmapResolution = isMobile ? 512 : 1024;
                    urpAsset.additionalLightsShadowmapResolution = 512;
                    urpAsset.shadowCascadeCount = 2;
                }
                break;

            case 3: // Altas (Sombras suaves de alta definición y 4 cascadas en PC)
                QualitySettings.shadows = ShadowQuality.All;
                QualitySettings.shadowResolution = ShadowResolution.High;
                if (urpAsset != null)
                {
                    urpAsset.shadowDistance = isMobile ? 55f : 90f;
                    urpAsset.mainLightShadowmapResolution = isMobile ? 1024 : 2048;
                    urpAsset.additionalLightsShadowmapResolution = isMobile ? 512 : 1024;
                    urpAsset.shadowCascadeCount = isMobile ? 2 : 4;
                }
                break;
        }

        ApplySceneLightsShadowMode(ShadowIndex);

        if (save)
            PlayerPrefs.SetInt(SHADOW_KEY, ShadowIndex);
    }
    private static void ApplySceneLightsShadowMode(int shadowIndex)
    {
        LightShadows targetLightShadows = shadowIndex switch
        {
            0 => LightShadows.None,
            1 => LightShadows.Hard,
            _ => LightShadows.Soft
        };

        Light[] sceneLights = FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < sceneLights.Length; i++)
        {
            if (sceneLights[i] == null) continue;

            if (sceneLights[i].shadows != LightShadows.None || shadowIndex > 0)
            {
                if (sceneLights[i].type == LightType.Directional)
                {
                    sceneLights[i].shadows = targetLightShadows;
                }
                else if (sceneLights[i].shadows != LightShadows.None)
                {
                    sceneLights[i].shadows = targetLightShadows;
                }
            }
        }
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