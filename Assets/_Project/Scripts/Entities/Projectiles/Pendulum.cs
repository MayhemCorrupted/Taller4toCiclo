using UnityEngine;

public class Pendulum : MonoBehaviour
{
    public enum SwingAxis { X, Y, Z }

    [SerializeField] private float speed = 1.5f;
    [Tooltip("90 = llega totalmente horizontal a ambos lados (-90° a +90°)")]
    [SerializeField] private float limit = 90f;
    [SerializeField] private SwingAxis axis = SwingAxis.Z;
    [SerializeField] private bool invertDirection = false;

    [Tooltip("Sincroniza todos")]
    [SerializeField] private bool randomStart = false;
    [Tooltip("1.57 = empieza arriba en horizontal, 0 = empieza abajo en vertical")]
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
        float direction = invertDirection ? -1f : 1f;
        float angle = limit * direction * Mathf.Sin((Time.time * speed) + startOffset);

        Vector3 euler = Vector3.zero;
        switch (axis)
        {
            case SwingAxis.X: euler.x = angle; break;
            case SwingAxis.Y: euler.y = angle; break;
            case SwingAxis.Z: euler.z = angle; break;
        }

        transform.localRotation = Quaternion.Euler(euler);
    }
}