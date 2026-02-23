using UnityEngine;

/// <summary>
/// Botiquín que el jugador puede recoger con E
/// </summary>
public class MedkitPickup : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private KeyCode pickupKey = KeyCode.E;
    [SerializeField] private AudioClip pickupSound;

    [Header("Efectos Visuales")]
    [SerializeField] private float rotationSpeed = 50f;
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobHeight = 0.5f;

    private Transform playerTransform;
    private MedkitInventory playerInventory;
    private bool isPlayerNearby = false;
    private Vector3 startPosition;

    void Start()
    {
        // Buscar jugador
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            playerInventory = player.GetComponent<MedkitInventory>();
        }

        startPosition = transform.position;
    }

    void Update()
    {
        // Efecto de rotación
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

        // Efecto de flotación (bobbing)
        float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(startPosition.x, newY, startPosition.z);

        // Verificar distancia con el jugador
        if (playerTransform != null)
        {
            float distance = Vector3.Distance(transform.position, playerTransform.position);
            isPlayerNearby = distance <= interactionDistance;

            // Recoger con E
            if (isPlayerNearby && Input.GetKeyDown(pickupKey))
            {
                TryPickup();
            }
        }
    }

    /// <summary>
    /// Intenta recoger el botiquín
    /// </summary>
    private void TryPickup()
    {
        if (playerInventory != null)
        {
            bool success = playerInventory.AddMedkit();
            
            if (success)
            {
                // Reproducir sonido
                if (pickupSound != null)
                {
                    AudioSource.PlayClipAtPoint(pickupSound, transform.position);
                }

                // Destruir el objeto
                Destroy(gameObject);
            }
        }
    }

    /// <summary>
    /// Mostrar prompt de interacción cuando el jugador está cerca
    /// </summary>
    void OnDrawGizmosSelected()
    {
        // Visualizar radio de interacción en el editor
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }

    /// <summary>
    /// Para que otros scripts sepan si el jugador puede interactuar
    /// </summary>
    public bool CanPickup()
    {
        return isPlayerNearby;
    }
}
