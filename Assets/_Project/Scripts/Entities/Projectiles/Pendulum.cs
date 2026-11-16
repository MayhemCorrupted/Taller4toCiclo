using UnityEngine;

public class Pendulum : MonoBehaviour
{
    [SerializeField] private float speed = 1.5f;
    [SerializeField] private float limit = 75f;

    [Tooltip("Sincroniza todos")]
    [SerializeField] private bool randomStart = false;
    [Tooltip("1.54 es un cuarto de del recorrido, 3.14 es la mitad y 4.71 es tres cuartos")]
    [SerializeField] private float startOffset = 0f;

    void Awake()
    {
        if (randomStart)
        {
            startOffset = Random.Range(0f, Mathf.PI * 2f);
        }
    }

    void Update()
    {
        float angle = limit * Mathf.Sin((Time.time * speed) + startOffset);
        transform.localRotation = Quaternion.Euler(0f, 0f, angle);
    }
}