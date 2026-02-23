using UnityEngine;

/// <summary>
/// Destruye automáticamente efectos visuales después de un tiempo
/// </summary>
public class AutoDestroyEffect : MonoBehaviour
{
    [SerializeField] private float lifetime = 1f;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }
}
