using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PressurePlate : MonoBehaviour
{
   // [SerializeField] private Transform plateMesh;
   // [SerializeField] private float pressDepth = 0.08f;
   // [SerializeField] private float moveSpeed = 10f;

   // [Header("Configuración")]
   // [SerializeField] private bool stayPressedOnceTriggered = false; 
   // [SerializeField] private AudioSource audioSource;
   // [SerializeField] private AudioClip clickDownSound;
   // [SerializeField] private AudioClip clickUpSound;
   // public UnityEvent onPlatePressed;
   // public UnityEvent onPlateReleased;


   // private Vector3 initialLocalPos;
   // private Vector3 pressedLocalPos;
   // private bool isPressed = false;
    
   // private List<Collider> objectsOnPlate = new List<Collider>();

   // private void Start()
   // {
   //     if (plateMesh != null)
   //     {
   //         initialLocalPos = plateMesh.localPosition;
   //         pressedLocalPos = initialLocalPos + Vector3.down * pressDepth;
   //     }
   // }

   // private void Update()
   // {
   //     if (plateMesh == null) return;

   //     Vector3 targetPos = isPressed ? pressedLocalPos : initialLocalPos;
   //     plateMesh.localPosition = Vector3.Lerp(plateMesh.localPosition, targetPos, Time.deltaTime * moveSpeed);
   // }

   // private void OnTriggerEnter(Collider other)
   // {
   //     if (!other.CompareTag("Player")) return;

   //     if (!objectsOnPlate.Contains(other))
   //         objectsOnPlate.Add(other);

   //     if (!isPressed)
   //     {
   //         isPressed = true;
   //         if (audioSource && clickDownSound) audioSource.PlayOneShot(clickDownSound);
   //         onPlatePressed?.Invoke();
   //     }
   // }

   // private void OnTriggerExit(Collider other)
   // {
   //     if (objectsOnPlate.Contains(other))
   //         objectsOnPlate.Remove(other);

   //     if (objectsOnPlate.Count == 0 && isPressed && !stayPressedOnceTriggered)
   //     {
   //         isPressed = false;
   //         if (audioSource && clickUpSound) audioSource.PlayOneShot(clickUpSound);
   //         onPlateReleased?.Invoke();
   //     }
   // }
}