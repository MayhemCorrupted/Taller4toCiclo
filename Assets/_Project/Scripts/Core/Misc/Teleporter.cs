using UnityEngine;
using UnityEngine.Events;
public class Teleporter : MonoBehaviour
{
    [Header("Referencia del Jugador")]
    [Tooltip("Transform raíz del jugador (con CharacterController). Si se deja vacío, se buscará por Tag 'Player'.")]
    [SerializeField] private Transform _player;

    [Header("Puntos de Teletransporte")]
    [SerializeField] private Transform _pointA;
    [SerializeField] private Transform _pointB;

    [Header("Opciones de Teletransporte")]
    [Tooltip("Si está activo, el jugador adoptará la rotación Y del punto de destino.")]
    [SerializeField] private bool _applyDestinationRotation = true;
    [Tooltip("Si está activo, cancela estados de dash o ataque en PlayerFSM al teletransportarse.")]
    [SerializeField] private bool _resetActionStateOnTeleport = true;

    [Header("Opcional: Activación automática por Trigger")]
    [Tooltip("Si este objeto tiene un Collider en modo IsTrigger, teletransportará automáticamente al Punto B al entrar.")]
    [SerializeField] private bool _teleportToBOnTriggerEnter = false;

    [Header("Eventos")]
    [Tooltip("Se dispara justo después de mover al jugador al destino.")]
    public UnityEvent OnTeleportCompleted;

    private CharacterController _characterController;
    private PlayerFSM _playerFSM;
    private void Awake()
    {
        ResolvePlayerReferences();
    }
    private void ResolvePlayerReferences()
    {
        if (_player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                _player = playerObj.transform;
            }
            else
            {
                Debug.LogWarning($"[PointTeleport] No se encontró ningún objeto con Tag 'Player' desde {gameObject.name}.");
                return;
            }
        }

        if (_characterController == null)
            _characterController = _player.GetComponent<CharacterController>();

        if (_playerFSM == null)
            _playerFSM = _player.GetComponent<PlayerFSM>();
    }
    public void TeleportToPointB()
    {
        ExecuteTeleport(_pointB);
    }
    public void TeleportToPointA()
    {
        ExecuteTeleport(_pointA);
    }
    public void TeleportBetweenPoints()
    {
        if (_pointA == null || _pointB == null)
        {
            Debug.LogWarning($"[PointTeleport] Faltan asignar Punto A o Punto B en {gameObject.name}.");
            return;
        }

        if (_player == null) ResolvePlayerReferences();
        if (_player == null) return;

        float distToA = (_player.position - _pointA.position).sqrMagnitude;
        float distToB = (_player.position - _pointB.position).sqrMagnitude;

        Transform target = distToA <= distToB ? _pointB : _pointA;
        ExecuteTeleport(target);
    }
    public void TeleportTo(Transform customDestination)
    {
        ExecuteTeleport(customDestination);
    }
    private void ExecuteTeleport(Transform destination)
    {
        if (destination == null)
        {
            Debug.LogWarning($"[PointTeleport] El punto de destino es nulo en {gameObject.name}.");
            return;
        }

        if (_player == null) ResolvePlayerReferences();
        if (_player == null) return;

        if (_characterController != null)
            _characterController.enabled = false;

        if (_applyDestinationRotation)
        {
            Quaternion targetRotation = Quaternion.Euler(0f, destination.eulerAngles.y, 0f);
            _player.SetPositionAndRotation(destination.position, targetRotation);
        }
        else
        {
            _player.position = destination.position;
        }

        Physics.SyncTransforms();

        if (_characterController != null)
            _characterController.enabled = true;

        if (_resetActionStateOnTeleport && _playerFSM != null)
        {
            if (_playerFSM.CurrentState == PlayerState.Dodging ||
                _playerFSM.CurrentState == PlayerState.Attacking ||
                _playerFSM.CurrentState == PlayerState.Skill)
            {
                _playerFSM.ChangeState(PlayerState.Idle);
            }
        }
        OnTeleportCompleted?.Invoke();
    }
}