using UnityEngine;
[RequireComponent(typeof(Camera))]
public class CameraObstructionController : MonoBehaviour
{
    [Header("Objetivo")]
    [SerializeField] private Transform _playerTarget;
    [Tooltip("Altura desde los pies hacia el pecho del jugador donde apunta el Raycast.")]
    [SerializeField] private Vector3 _targetOffset = new(0f, 1.1f, 0f);

    [Header("Detección de Obstrucciones (Raycast Central)")]
    [Tooltip("Capas de paredes, techos o estructuras que pueden tapar al jugador.")]
    [SerializeField] private LayerMask _obstructionLayers;

    [Header("Configuración del Círculo Observable")]
    [Tooltip("Radio del círculo en proporción de pantalla (0.14 = 14% de la altura de pantalla).")]
    [SerializeField][Range(0.05f, 0.35f)] private float _circleRadius = 0.14f;
    [Tooltip("Suavizado del borde del círculo.")]
    [SerializeField][Range(0.01f, 0.15f)] private float _circleFalloff = 0.04f;
    [SerializeField] private float _transitionSpeed = 12f;

    private Camera _camera;
    private float _currentRadius = 0f;
    private bool _isPlayerObstructed = false;

    private static readonly int PlayerViewportPosID = Shader.PropertyToID("_PlayerViewportPos");
    private static readonly int CutoutRadiusID = Shader.PropertyToID("_CutoutRadius");
    private static readonly int CutoutFalloffID = Shader.PropertyToID("_CutoutFalloff");
    private static readonly int ScreenAspectID = Shader.PropertyToID("_ScreenAspect");
    private static readonly int IsObstructedID = Shader.PropertyToID("_IsObstructed");
    private void Awake()
    {
        _camera = GetComponent<Camera>();

        if (_playerTarget == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) _playerTarget = playerObj.transform;
        }
    }
    private void LateUpdate()
    {
        if (_playerTarget == null) return;

        Vector3 playerWorldPos = _playerTarget.position + _targetOffset;

        Ray centerRay = _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 rayOrigin = centerRay.origin;
        Vector3 toPlayer = playerWorldPos - rayOrigin;
        float distanceToPlayer = toPlayer.magnitude;
        Vector3 directionToPlayer = toPlayer.normalized;

        bool hitFromCamera = Physics.Raycast(rayOrigin, directionToPlayer, distanceToPlayer, _obstructionLayers, QueryTriggerInteraction.Ignore);
        bool hitFromPlayer = Physics.Raycast(playerWorldPos, -directionToPlayer, distanceToPlayer, _obstructionLayers, QueryTriggerInteraction.Ignore);

        _isPlayerObstructed = hitFromCamera || hitFromPlayer;

        float desiredRadius = _isPlayerObstructed ? _circleRadius : 0f;
        _currentRadius = Mathf.Lerp(_currentRadius, desiredRadius, Time.deltaTime * _transitionSpeed);

        Vector3 viewportPos = _camera.WorldToViewportPoint(playerWorldPos);

        Shader.SetGlobalVector(PlayerViewportPosID, new Vector4(viewportPos.x, viewportPos.y, 0f, 0f));
        Shader.SetGlobalFloat(CutoutRadiusID, _currentRadius);
        Shader.SetGlobalFloat(CutoutFalloffID, _circleFalloff);
        Shader.SetGlobalFloat(ScreenAspectID, (float)Screen.width / Mathf.Max(1, Screen.height));
        Shader.SetGlobalFloat(IsObstructedID, _isPlayerObstructed ? 1f : 0f);
    }
    private void OnDisable()
    {
        Shader.SetGlobalFloat(CutoutRadiusID, 0f);
        Shader.SetGlobalFloat(IsObstructedID, 0f);
    }
    private void OnDrawGizmosSelected()
    {
        if (_playerTarget == null) return;

        Camera cam = _camera != null ? _camera : GetComponent<Camera>();
        if (cam == null) return;

        Vector3 origin = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f)).origin;
        Vector3 target = _playerTarget.position + _targetOffset;

        Gizmos.color = _isPlayerObstructed ? Color.red : Color.green;
        Gizmos.DrawLine(origin, target);
        Gizmos.DrawSphere(target, 0.15f);
    }
}