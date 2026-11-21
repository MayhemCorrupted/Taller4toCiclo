using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;
public class CathedralPortal : MonoBehaviour, IInteractable
{
    [Header("Prompt de Interacción")]
    [Tooltip("Texto localizado con '{key}' (Ej: '[{key}] Cruzar umbral').")]
    [SerializeField] private LocalizedString _interactionPrompt;

    [Header("Configuración del Portal")]
    [Tooltip("Desactívalo en el Portal A y déjalo activo en el Portal B")]
    [SerializeField] private bool isUnlocked = false;
    [SerializeField] private CathedralPortal linkedPortal;

    [Header("Visuales y Destino")]
    [Tooltip("Objeto interior del portal")]
    [SerializeField] private GameObject portalInteriorVisual;
    [Tooltip("Punto frente al portal donde aparecerá el Player")]
    [SerializeField] private Transform spawnPoint;

    [Header("Configuración del Viaje")]
    [SerializeField] private float teleportDelay = 1.5f;
    [SerializeField] private string playerTag = "Player";

    [Header("Eventos")]
    public UnityEvent onPortalUnlocked;
    public UnityEvent onTeleportStarted;
    public UnityEvent onTeleportCompleted;

    public LocalizedString InteractionPrompt => _interactionPrompt;

    private bool isTeleporting = false;
    private GameObject cachedPlayer;
    private CharacterController playerCC;
    private PlayerInputs playerInputs;
    private void Awake()
    {
        if (portalInteriorVisual != null)
        {
            portalInteriorVisual.SetActive(isUnlocked);
        }
    }
    private void Start()
    {
        cachedPlayer = GameObject.FindGameObjectWithTag(playerTag);
        if (cachedPlayer != null)
        {
            playerCC = cachedPlayer.GetComponent<CharacterController>();
            playerInputs = cachedPlayer.GetComponent<PlayerInputs>();
        }
    }
    public void Interact()
    {
        if (isTeleporting) return;
        if (!isUnlocked) return;

        if (linkedPortal != null && !linkedPortal.isUnlocked)
        {
            linkedPortal.UnlockPortal();
        }

        if (linkedPortal != null && cachedPlayer != null)
        {
            StartCoroutine(TeleportRoutine());
        }
    }
    public void UnlockPortal()
    {
        if (isUnlocked) return;

        isUnlocked = true;

        if (portalInteriorVisual != null)
        {
            portalInteriorVisual.SetActive(true);
        }

        onPortalUnlocked?.Invoke();
    }
    private IEnumerator TeleportRoutine()
    {
        isTeleporting = true;
        linkedPortal.isTeleporting = true;

        if (playerInputs != null)
        {
            playerInputs.enabled = false;
        }

        onTeleportStarted?.Invoke();

        yield return new WaitForSeconds(teleportDelay);

        Transform destination = linkedPortal.spawnPoint != null ? linkedPortal.spawnPoint : linkedPortal.transform;

        if (playerCC != null)
        {
            playerCC.enabled = false;
            cachedPlayer.transform.SetPositionAndRotation(destination.position, destination.rotation);
            playerCC.enabled = true;
        }
        else
        {
            cachedPlayer.transform.SetPositionAndRotation(destination.position, destination.rotation);
        }

        onTeleportCompleted?.Invoke();

        if (playerInputs != null)
        {
            playerInputs.enabled = true;
        }

        WaitForSeconds waitForSeconds = new(0.2f);
        yield return waitForSeconds;
        isTeleporting = false;
        linkedPortal.isTeleporting = false;
    }
}