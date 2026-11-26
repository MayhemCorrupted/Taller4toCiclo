using UnityEngine;
[RequireComponent(typeof(Collider))]
public class VolumetricFogArea : MonoBehaviour
{
    [Header("Densidad y Transición")]
    [Tooltip("Densidad de la niebla al salir de esta zona (0 = despejado).")]
    [SerializeField][Range(0.0f, 1.0f)] private float _defaultDensity = 0.0f;
    [Tooltip("Densidad objetivo de la niebla al entrar en esta zona (0.55 - 0.75 recomendado para bosque).")]
    [SerializeField][Range(0.0f, 1.0f)] private float _targetDensity = 0.65f;
    [Tooltip("Duración en segundos de la transición al entrar o salir.")]
    [SerializeField][Min(0.05f)] private float _transitionDuration = 1.0f;

    [Header("Personalización Opcional de esta Zona")]
    [SerializeField] private bool _overrideColor = false;
    [ColorUsage(true, true)]
    [SerializeField] private Color _areaFogColor = new(0.38f, 0.48f, 0.42f, 1f);

    [SerializeField] private bool _overrideHeight = false;
    [SerializeField] private float _areaFogHeight = 1.8f;
    public float DefaultDensity => _defaultDensity;
    public float TargetDensity => _targetDensity;
    public float TransitionDuration => _transitionDuration;
    public bool OverrideColor => _overrideColor;
    public Color AreaFogColor => _areaFogColor;
    public bool OverrideHeight => _overrideHeight;
    public float AreaFogHeight => _areaFogHeight;

    private void Awake()
    {
        if (!TryGetComponent<Collider>(out var col)) return;

        if (!col.isTrigger)
        {
            col.isTrigger = true;
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (ExteriorVolumetricFogController.Instance != null)
        {
            ExteriorVolumetricFogController.Instance.EnterFogArea(this);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (ExteriorVolumetricFogController.Instance != null)
        {
            ExteriorVolumetricFogController.Instance.ExitFogArea(this);
        }
    }
    private void OnDrawGizmosSelected()
    {
        if (!TryGetComponent<Collider>(out var col)) return;

        Gizmos.color = new Color(0.45f, 0.75f, 0.65f, 0.25f);
        Gizmos.DrawCube(col.bounds.center, col.bounds.size);
        Gizmos.color = new Color(0.45f, 0.75f, 0.65f, 0.8f);
        Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
    }
}
