using System.Collections.Generic;
using UnityEngine;
public class ExteriorVolumetricFogController : MonoBehaviour
{
    public static ExteriorVolumetricFogController Instance { get; private set; }

    [Header("Referencias del Jugador (para la reactividad)")]
    [Tooltip("Si se deja vacío, busca automáticamente el objeto con Tag 'Player'.")]
    [SerializeField] private Transform _playerTransform;

    [Header("Burbuja de Despeje y Estela del Jugador")]
    [Tooltip("Radio en metros alrededor del jugador donde la niebla se despeja por completo.")]
    [Min(0f)][SerializeField] private float _fogClearRadius = 2.4f;
    [Tooltip("Distancia de transición suave desde despejado hasta niebla densa.")]
    [Min(0.1f)][SerializeField] private float _fogClearFalloff = 3.6f;
    [Tooltip("Fuerza con la que el movimiento del jugador aparta las volutas de niebla.")]
    [Range(0f, 1.5f)][SerializeField] private float _fogPushStrength = 0.35f;
    [Min(0.01f)][SerializeField] private float _velocitySmoothTime = 0.15f;

    [Header("Configuración Visual e Integración con Iluminación")]
    [ColorUsage(true, true)]
    [SerializeField] private Color _baseFogColor = new(0.22f, 0.26f, 0.32f, 1f);
    [Tooltip("Multiplica el color de la niebla por la luz ambiental de la escena para que no se vea plana ni sobrepuesta.")]
    [SerializeField] private bool _tintWithAmbientLight = true;
    [Tooltip("Cuánto influye la luz ambiental de la escena en el color final de la niebla (0 = color puro, 1 = totalmente teñida por el ambiente).")]
    [Range(0f, 1f)][SerializeField] private float _ambientLightInfluence = 0.7f;
    [Tooltip("Tope máximo de opacidad real en el shader para no tapar las texturas del suelo.")]
    [Range(0.05f, 1f)][SerializeField] private float _maxVisualOpacity = 0.45f;

    [Header("Densidad y Altura Base")]
    [Tooltip("Densidad base fuera de cualquier VolumetricFogArea (0 = sin niebla hasta entrar en un trigger).")]
    [Range(0f, 1f)][SerializeField] private float _baseExteriorDensity = 0f;
    [Tooltip("Altura máxima en unidades de mundo (Y) donde se concentra la niebla.")]
    [SerializeField] private float _baseFogMaxHeight = 1.6f;
    [Tooltip("Suavizado vertical de la niebla.")]
    [Min(0.1f)][SerializeField] private float _fogHeightFalloff = 2.2f;
    [SerializeField] private Vector2 _windSpeed = new(0.15f, 0.08f);
    [Min(0.05f)][SerializeField] private float _defaultTransitionDuration = 1.0f;

    [Header("Optimización de Capas (GPU)")]
    [Tooltip("Renderers de los planos horizontales de niebla que se apagan automáticamente cuando la densidad es 0.")]
    [SerializeField] private List<Renderer> _exteriorFogRenderers = new();

    [Header("Sincronización con Niebla Ambiental URP (Desactivar en Isométrica)")]
    [Tooltip("En cámaras ortográficas isométricas se recomienda dejarlo desactivado para no lavar las texturas.")]
    [SerializeField] private bool _syncUnityEnvironmentFog = false;
    [Range(0f, 0.2f)][SerializeField] private float _maxEnvironmentFogDensity = 0.015f;
    private readonly List<VolumetricFogArea> _activeFogAreas = new();
    private float _currentDensity;
    private float _targetDensity;
    private float _currentTransitionDuration;
    private Color _currentFogColor;
    private Color _targetFogColor;
    private float _currentFogHeight;
    private float _targetFogHeight;
    private Vector3 _lastPlayerPos;
    private Vector3 _smoothedPlayerVelocity;
    private Vector3 _velocityRef;
    private bool _hasLastPlayerPos;
    private static readonly int FogColorID = Shader.PropertyToID("_ExteriorFogColor");
    private static readonly int FogDensityID = Shader.PropertyToID("_ExteriorFogDensity");
    private static readonly int FogHeightID = Shader.PropertyToID("_ExteriorFogHeight");
    private static readonly int FogFalloffID = Shader.PropertyToID("_ExteriorFogFalloff");
    private static readonly int WindSpeedID = Shader.PropertyToID("_ExteriorFogWindSpeed");

    private static readonly int PlayerWorldPosID = Shader.PropertyToID("_PlayerWorldPos");
    private static readonly int FogClearRadiusID = Shader.PropertyToID("_FogClearRadius");
    private static readonly int FogClearFalloffID = Shader.PropertyToID("_FogClearFalloff");
    private static readonly int PlayerVelocityID = Shader.PropertyToID("_PlayerVelocity");
    private static readonly int FogPushStrengthID = Shader.PropertyToID("_FogPushStrength");
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        _currentDensity = _baseExteriorDensity;
        _targetDensity = _baseExteriorDensity;
        _currentTransitionDuration = _defaultTransitionDuration;

        _currentFogColor = _baseFogColor;
        _targetFogColor = _baseFogColor;
        _currentFogHeight = _baseFogMaxHeight;
        _targetFogHeight = _baseFogMaxHeight;

        ApplyInitialShaderProperties();
    }
    private void Start()
    {
        ResolvePlayerTarget();
        EvaluateTargetFogState();
        UpdateRenderersVisibility(_currentDensity > 0.005f);
    }
    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
    private void Update()
    {
        UpdatePlayerReactivity();
        UpdateFogTransitions();
    }
    private void ResolvePlayerTarget()
    {
        if (_playerTransform != null) return;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            _playerTransform = playerObj.transform;
            _lastPlayerPos = _playerTransform.position;
            _hasLastPlayerPos = true;
        }
    }
    private void UpdatePlayerReactivity()
    {
        if (_playerTransform == null)
        {
            ResolvePlayerTarget();
            if (_playerTransform == null) return;
        }

        Vector3 currentPos = _playerTransform.position;
        Vector3 rawVelocity = Vector3.zero;

        if (_hasLastPlayerPos && Time.deltaTime > 0.0001f)
        {
            rawVelocity = (currentPos - _lastPlayerPos) / Time.deltaTime;
            rawVelocity.y = 0f;
        }

        _lastPlayerPos = currentPos;
        _hasLastPlayerPos = true;

        _smoothedPlayerVelocity = Vector3.SmoothDamp(
            _smoothedPlayerVelocity,
            rawVelocity,
            ref _velocityRef,
            _velocitySmoothTime
        );

        Shader.SetGlobalVector(PlayerWorldPosID, currentPos);
        Shader.SetGlobalVector(PlayerVelocityID, _smoothedPlayerVelocity);
    }
    private void UpdateFogTransitions()
    {
        float stepSpeed = _currentTransitionDuration > 0.001f ? (Time.deltaTime / _currentTransitionDuration) : 1f;

        if (!Mathf.Approximately(_currentDensity, _targetDensity))
        {
            _currentDensity = Mathf.MoveTowards(_currentDensity, _targetDensity, stepSpeed);
            Shader.SetGlobalFloat(FogDensityID, _currentDensity * _maxVisualOpacity);

            if (_syncUnityEnvironmentFog)
            {
                RenderSettings.fogDensity = _maxEnvironmentFogDensity * _currentDensity;
            }
        }

        if (_currentFogColor != _targetFogColor)
        {
            _currentFogColor = Color.Lerp(_currentFogColor, _targetFogColor, stepSpeed * 3f);
        }

        Color finalLitColor = ComputeLitFogColor(_currentFogColor);
        Shader.SetGlobalColor(FogColorID, finalLitColor);

        if (_syncUnityEnvironmentFog)
        {
            RenderSettings.fogColor = finalLitColor;
        }

        if (!Mathf.Approximately(_currentFogHeight, _targetFogHeight))
        {
            _currentFogHeight = Mathf.Lerp(_currentFogHeight, _targetFogHeight, stepSpeed * 3f);
            Shader.SetGlobalFloat(FogHeightID, _currentFogHeight);
        }

        UpdateRenderersVisibility(_currentDensity > 0.005f);
    }
    private Color ComputeLitFogColor(Color rawColor)
    {
        if (!_tintWithAmbientLight)
            return rawColor;

        Color ambient = RenderSettings.ambientLight;
        Color litColor = new(
            rawColor.r * ambient.r * 1.4f,
            rawColor.g * ambient.g * 1.4f,
            rawColor.b * ambient.b * 1.4f,
            1f
        );

        Color blended = Color.Lerp(rawColor, litColor, _ambientLightInfluence);
        blended.a = 1f;
        return blended;
    }
    #region Control de Zonas (VolumetricFogArea)
    public void EnterFogArea(VolumetricFogArea area)
    {
        if (area == null) return;

        if (!_activeFogAreas.Contains(area))
        {
            _activeFogAreas.Add(area);
        }

        _currentTransitionDuration = area.TransitionDuration;
        EvaluateTargetFogState();
    }
    public void ExitFogArea(VolumetricFogArea area)
    {
        if (area == null) return;

        _activeFogAreas.Remove(area);
        _currentTransitionDuration = area.TransitionDuration;
        EvaluateTargetFogState();
    }
    private void EvaluateTargetFogState()
    {
        if (_activeFogAreas.Count > 0)
        {
            VolumetricFogArea activeArea = _activeFogAreas[^1];
            _targetDensity = activeArea.TargetDensity;
            _targetFogColor = activeArea.OverrideColor ? activeArea.AreaFogColor : _baseFogColor;
            _targetFogHeight = activeArea.OverrideHeight ? activeArea.AreaFogHeight : _baseFogMaxHeight;
        }
        else
        {
            _targetDensity = _baseExteriorDensity;
            _targetFogColor = _baseFogColor;
            _targetFogHeight = _baseFogMaxHeight;
        }
    }
    #endregion
    private void ApplyInitialShaderProperties()
    {
        Color finalLitColor = ComputeLitFogColor(_currentFogColor);

        Shader.SetGlobalColor(FogColorID, finalLitColor);
        Shader.SetGlobalFloat(FogDensityID, _currentDensity * _maxVisualOpacity);
        Shader.SetGlobalFloat(FogHeightID, _currentFogHeight);
        Shader.SetGlobalFloat(FogFalloffID, _fogHeightFalloff);
        Shader.SetGlobalVector(WindSpeedID, _windSpeed);

        Shader.SetGlobalFloat(FogClearRadiusID, _fogClearRadius);
        Shader.SetGlobalFloat(FogClearFalloffID, _fogClearFalloff);
        Shader.SetGlobalFloat(FogPushStrengthID, _fogPushStrength);

        RenderSettings.fog = _syncUnityEnvironmentFog;
        if (_syncUnityEnvironmentFog)
        {
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = finalLitColor;
            RenderSettings.fogDensity = _maxEnvironmentFogDensity * _currentDensity;
        }
    }
    private void UpdateRenderersVisibility(bool isVisible)
    {
        for (int i = 0; i < _exteriorFogRenderers.Count; i++)
        {
            Renderer fogRenderer = _exteriorFogRenderers[i];
            if (fogRenderer != null && fogRenderer.enabled != isVisible)
            {
                fogRenderer.enabled = isVisible;
            }
        }
    }
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            _currentDensity = _baseExteriorDensity;
            _currentFogColor = _baseFogColor;
            _currentFogHeight = _baseFogMaxHeight;
            ApplyInitialShaderProperties();
        }
        else
        {
            Shader.SetGlobalFloat(FogDensityID, _currentDensity * _maxVisualOpacity);
            Shader.SetGlobalFloat(FogFalloffID, _fogHeightFalloff);
            Shader.SetGlobalVector(WindSpeedID, _windSpeed);
            Shader.SetGlobalFloat(FogClearRadiusID, _fogClearRadius);
            Shader.SetGlobalFloat(FogClearFalloffID, _fogClearFalloff);
            Shader.SetGlobalFloat(FogPushStrengthID, _fogPushStrength);
            EvaluateTargetFogState();
        }
    }
#endif
}