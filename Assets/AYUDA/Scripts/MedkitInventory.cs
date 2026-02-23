using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Gestiona el inventario de botiquines del jugador
/// </summary>
public class MedkitInventory : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private int maxMedkits = 3;
    [SerializeField] private int currentMedkits = 0;
    [SerializeField] private int healAmount = 50;

    [Header("Referencias")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private AudioClip useSound;
    [SerializeField] private AudioSource audioSource;

    [Header("Eventos")]
    public UnityEvent<int> OnMedkitCountChanged;

    void Start()
    {
        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // Notificar UI inicial
        OnMedkitCountChanged?.Invoke(currentMedkits);
    }

    void Update()
    {
        // Usar botiquín con Q
        if (Input.GetKeyDown(KeyCode.Q))
        {
            UseMedkit();
        }
    }

    /// <summary>
    /// Añade un botiquín al inventario
    /// </summary>
    public bool AddMedkit()
    {
        if (currentMedkits >= maxMedkits)
        {
            Debug.Log("Inventario de botiquines lleno");
            return false;
        }

        currentMedkits++;
        OnMedkitCountChanged?.Invoke(currentMedkits);
        Debug.Log($"Botiquín recogido. Total: {currentMedkits}/{maxMedkits}");
        return true;
    }

    /// <summary>
    /// Usa un botiquín para curarse
    /// </summary>
    public void UseMedkit()
    {
        // Verificar si tenemos botiquines
        if (currentMedkits <= 0)
        {
            Debug.Log("No tienes botiquines");
            return;
        }

        // Verificar si necesitamos curación
        if (playerHealth != null)
        {
            if (playerHealth.GetCurrentHealth() >= playerHealth.GetMaxHealth())
            {
                Debug.Log("Vida completa, no necesitas curarte");
                return;
            }

            // Usar botiquín
            currentMedkits--;
            playerHealth.Heal(healAmount);
            
            // Reproducir sonido
            if (audioSource != null && useSound != null)
            {
                audioSource.PlayOneShot(useSound);
            }

            OnMedkitCountChanged?.Invoke(currentMedkits);
            Debug.Log($"Botiquín usado. Restantes: {currentMedkits}/{maxMedkits}");
        }
    }

    /// <summary>
    /// Obtiene el número actual de botiquines
    /// </summary>
    public int GetCurrentMedkits()
    {
        return currentMedkits;
    }

    /// <summary>
    /// Obtiene el máximo de botiquines
    /// </summary>
    public int GetMaxMedkits()
    {
        return maxMedkits;
    }
}
