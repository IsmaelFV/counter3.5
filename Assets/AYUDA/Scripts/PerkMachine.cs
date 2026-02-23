using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Máquina expendedora de perks — objeto interactuable del mundo.
/// El jugador se acerca, pulsa E y compra la perk asignada.
/// Cada máquina vende UNA perk específica.
///
/// SETUP:
/// 1. Crear un GameObject con el modelo de la máquina expendedora
/// 2. Añadir este script
/// 3. Asignar qué perk vende (perkType)
/// 4. Añadir un Collider sólido (isTrigger = false)
/// 5. (Opcional) AudioSource, luces, partículas
///
/// Usa detección por distancia (como WeaponUpgradeStation), no triggers.
/// </summary>
public class PerkMachine : MonoBehaviour
{
    [Header("=== CONFIGURACIÓN ===")]
    [Tooltip("Qué perk vende esta máquina")]
    [SerializeField] private PerkType perkType = PerkType.Juggernog;
    [Tooltip("Tecla para interactuar")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [Tooltip("Distancia máxima de interacción")]
    [SerializeField] private float interactionDistance = 3f;
    [Tooltip("¿Requiere electricidad para funcionar?")]
    [SerializeField] private bool requiresElectricity = false;

    [Header("=== COSTE (override) ===")]
    [Tooltip("Coste personalizado (0 = usar coste por defecto de PlayerPerkManager)")]
    [SerializeField] private int customCost = 0;

    [Header("=== VISUAL ===")]
    [Tooltip("Luz de la máquina (se tiñe del color de la perk)")]
    [SerializeField] private Light machineLight;
    [Tooltip("Intensidad de la luz cuando está activa")]
    [SerializeField] private float lightIntensity = 2f;
    [Tooltip("Renderer principal para el material glow")]
    [SerializeField] private Renderer machineRenderer;
    [Tooltip("Nombre de la propiedad de emisión del material")]
    [SerializeField] private string emissionProperty = "_EmissionColor";

    [Header("=== UI ===")]
    [Tooltip("Canvas/Panel propio de la estación (opcional)")]
    [SerializeField] private GameObject promptUI;

    [Header("=== SONIDOS ===")]
    [SerializeField] private AudioClip purchaseSound;
    [SerializeField] private AudioClip deniedSound;
    [SerializeField] private AudioClip alreadyOwnedSound;
    [SerializeField] private AudioClip ambientHum;
    [SerializeField, Range(0f, 1f)] private float ambientVolume = 0.2f;
    private AudioSource audioSource;
    private AudioSource ambientAudioSource;

    [Header("=== ANIMACIÓN ===")]
    [Tooltip("¿Animar la máquina al comprar?")]
    [SerializeField] private bool animateOnPurchase = true;

    // Estado
    private bool playerInRange = false;
    private Transform playerTransform;
    private PlayerPerkManager cachedPerkManager;
    private PlayerEconomy cachedEconomy;
    private bool isElectricityOn = true;

    // Animación
    private Vector3 originalScale;
    private float animTimer = 0f;
    private bool isAnimating = false;

    // Color de la perk
    private Color perkColor;
    private int perkCost;

    // =========================================================================
    // SISTEMA DE PRIORIDAD DE AUDIO AMBIENTAL
    // Sólo la máquina más cercana al jugador suena a volumen completo,
    // las demás se atenúan gradualmente según la distancia.
    // =========================================================================
    private static List<PerkMachine> allMachines = new List<PerkMachine>();
    [Tooltip("Distancia máxima a la que se escucha el hum (más allá = volumen 0)")]
    [SerializeField] private float ambientHearingRange = 15f;

    void Awake()
    {
        // Registrar esta máquina
        if (!allMachines.Contains(this))
            allMachines.Add(this);

        // AudioSource principal
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f;
        audioSource.playOnAwake = false;

        originalScale = transform.localScale;

        if (promptUI != null)
            promptUI.SetActive(false);
    }

    void Start()
    {
        // Color y coste de la perk
        perkColor = PlayerPerkManager.GetPerkColor(perkType);
        perkCost = customCost > 0 ? customCost : PlayerPerkManager.GetPerkCost(perkType);

        // Configurar visual según la perk
        SetupVisuals();

        // Configurar sonido ambiental
        SetupAmbientSound();

        // Buscar jugador
        FindPlayer();

        // Electricidad
        if (requiresElectricity)
        {
            isElectricityOn = false;
            SetMachineActive(false);
        }
    }

    void Update()
    {
        if (requiresElectricity && !isElectricityOn) return;

        // Detección por distancia
        CheckPlayerDistance();

        // Interacción
        if (playerInRange && Input.GetKeyDown(interactKey))
        {
            TryPurchasePerk();
        }

        // Actualizar prompt dinámico
        if (playerInRange)
        {
            string prompt = BuildPrompt();
            InteractionUI.Show(prompt);
        }

        // Ajustar volumen del hum según proximidad (solo la más cercana suena fuerte)
        UpdateAmbientVolume();

        // Animación procedural
        if (isAnimating)
        {
            animTimer += Time.deltaTime;
            float t = animTimer / 0.6f;

            if (t >= 1f)
            {
                transform.localScale = originalScale;
                isAnimating = false;
            }
            else
            {
                float spring = 1f + Mathf.Sin(t * Mathf.PI * 4f) * 0.1f * (1f - t);
                transform.localScale = originalScale * spring;
            }
        }
    }

    // =========================================================================
    // DETECCIÓN
    // =========================================================================

    private void FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            cachedPerkManager = player.GetComponent<PlayerPerkManager>();
            cachedEconomy = PlayerEconomy.Instance;
        }
        else
        {
            // Fallback
            var pm = FindObjectOfType<PlayerMovement>();
            if (pm != null)
            {
                playerTransform = pm.transform;
                cachedPerkManager = pm.GetComponent<PlayerPerkManager>();
            }
            cachedEconomy = PlayerEconomy.Instance;
        }
    }

    private void CheckPlayerDistance()
    {
        if (playerTransform == null)
        {
            if (cachedPerkManager == null) FindPlayer();
            return;
        }

        float dist = Vector3.Distance(transform.position, playerTransform.position);
        bool wasInRange = playerInRange;
        playerInRange = dist <= interactionDistance;

        // Entró en rango
        if (playerInRange && !wasInRange)
        {
            if (cachedEconomy == null)
                cachedEconomy = PlayerEconomy.Instance;
            if (cachedPerkManager == null)
                cachedPerkManager = playerTransform.GetComponent<PlayerPerkManager>();

            if (promptUI != null)
                promptUI.SetActive(true);
        }
        // Salió de rango
        else if (!playerInRange && wasInRange)
        {
            if (promptUI != null)
                promptUI.SetActive(false);
            InteractionUI.Hide();
        }
    }

    // =========================================================================
    // COMPRA DE PERK
    // =========================================================================

    private void TryPurchasePerk()
    {
        if (cachedPerkManager == null || cachedEconomy == null)
        {
            Debug.LogWarning("[PERK MACHINE] No se encontró PlayerPerkManager o PlayerEconomy");
            PlaySound(deniedSound);
            return;
        }

        // ¿Ya tiene esta perk?
        if (cachedPerkManager.HasPerk(perkType))
        {
            Debug.Log($"[PERK MACHINE] Ya tienes {perkType}");
            PlaySound(alreadyOwnedSound);
            return;
        }

        // ¿Puede adquirir más perks?
        if (!cachedPerkManager.CanAcquireMore)
        {
            Debug.Log($"[PERK MACHINE] Máximo de perks alcanzado");
            PlaySound(deniedSound);
            return;
        }

        // ¿Tiene suficientes monedas?
        if (!cachedEconomy.CanAfford(perkCost))
        {
            Debug.Log($"[PERK MACHINE] Monedas insuficientes. Necesitas {perkCost}, tienes {cachedEconomy.CurrentCoins}");
            PlaySound(deniedSound);
            return;
        }

        // ¡COMPRAR!
        cachedEconomy.SpendCoins(perkCost);
        bool success = cachedPerkManager.AcquirePerk(perkType);

        if (success)
        {
            PlaySound(purchaseSound);

            if (animateOnPurchase)
            {
                isAnimating = true;
                animTimer = 0f;
            }

            Debug.Log($"[PERK MACHINE] ¡{perkType} comprada por {perkCost} monedas! " +
                      $"Monedas restantes: {cachedEconomy.CurrentCoins}");
        }
        else
        {
            // Reembolsar si falló
            cachedEconomy.AddCoins(perkCost, "Reembolso");
            PlaySound(deniedSound);
        }
    }

    // =========================================================================
    // PROMPT
    // =========================================================================

    private string BuildPrompt()
    {
        if (cachedPerkManager == null)
            return $"[{interactKey}] Comprar perk";

        string perkName = perkType.ToString();
        string perkDesc = PlayerPerkManager.GetPerkDescription(perkType);

        // Ya la tiene
        if (cachedPerkManager.HasPerk(perkType))
            return $"{perkName} — YA ADQUIRIDA";

        // No puede más
        if (!cachedPerkManager.CanAcquireMore)
            return $"{perkName} — MÁXIMO DE PERKS ALCANZADO";

        int coins = cachedEconomy != null ? cachedEconomy.CurrentCoins : 0;

        if (cachedEconomy != null && cachedEconomy.CanAfford(perkCost))
            return $"[{interactKey}] {perkName} — {perkCost} monedas\n{perkDesc}";
        else
            return $"[{interactKey}] {perkName} — {perkCost} monedas (necesitas {perkCost - coins} más)\n{perkDesc}";
    }

    // =========================================================================
    // VISUAL
    // =========================================================================

    private void SetupVisuals()
    {
        // Luz de la máquina
        if (machineLight != null)
        {
            machineLight.color = perkColor;
            machineLight.intensity = lightIntensity;
        }

        // Emisión del material
        if (machineRenderer != null && machineRenderer.material.HasProperty(emissionProperty))
        {
            machineRenderer.material.EnableKeyword("_EMISSION");
            machineRenderer.material.SetColor(emissionProperty, perkColor * 2f);
        }
    }

    private void SetupAmbientSound()
    {
        if (ambientHum == null) return;

        // Crear AudioSource separado para el hum
        ambientAudioSource = gameObject.AddComponent<AudioSource>();
        ambientAudioSource.clip = ambientHum;
        ambientAudioSource.loop = true;
        ambientAudioSource.volume = 0f; // Empieza en 0, UpdateAmbientVolume lo ajustará
        ambientAudioSource.spatialBlend = 1f;
        ambientAudioSource.maxDistance = ambientHearingRange;
        ambientAudioSource.rolloffMode = AudioRolloffMode.Linear;
        ambientAudioSource.Play();
    }

    /// <summary>
    /// Ajusta el volumen del hum: la máquina más cercana al jugador suena a volumen completo,
    /// las demás se atenúan proporcionalmente. Si estás lejos de todas, no se oye ninguna.
    /// </summary>
    private void UpdateAmbientVolume()
    {
        if (ambientAudioSource == null || playerTransform == null) return;

        float myDist = Vector3.Distance(playerTransform.position, transform.position);

        // Si estoy fuera del rango de escucha, volumen 0
        if (myDist > ambientHearingRange)
        {
            ambientAudioSource.volume = 0f;
            return;
        }

        // Buscar la distancia de la máquina más cercana al jugador (que tenga hum)
        float closestDist = float.MaxValue;
        for (int i = 0; i < allMachines.Count; i++)
        {
            PerkMachine m = allMachines[i];
            if (m == null || m.ambientAudioSource == null) continue;
            if (!m.isElectricityOn) continue; // Ignorar máquinas apagadas

            float d = Vector3.Distance(playerTransform.position, m.transform.position);
            if (d < closestDist)
                closestDist = d;
        }

        // Soy la más cercana (o estoy empatada): volumen proporcional a la distancia
        // Cuanto más cerca, más fuerte (1.0 = encima, 0.0 = en el borde del rango)
        float distanceFade = 1f - Mathf.Clamp01(myDist / ambientHearingRange);

        // Si NO soy la más cercana, atenuar extra para que no se mezclen
        float priorityFactor = 1f;
        if (myDist > closestDist + 0.5f) // Margen de 0.5m para empate
        {
            // Atenuar según cuánto más lejos estoy respecto a la más cercana
            float diff = myDist - closestDist;
            priorityFactor = Mathf.Clamp01(1f - diff / (ambientHearingRange * 0.4f));
            priorityFactor *= 0.3f; // Las no-prioritarias suenan a máx 30% de su volumen
        }

        float targetVolume = ambientVolume * distanceFade * priorityFactor;
        ambientAudioSource.volume = Mathf.Lerp(ambientAudioSource.volume, targetVolume, Time.deltaTime * 5f);
    }

    private void OnDestroy()
    {
        allMachines.Remove(this);
    }

    /// <summary>
    /// Activa o desactiva la máquina (para sistema de electricidad).
    /// </summary>
    public void SetMachineActive(bool active)
    {
        isElectricityOn = active;

        if (machineLight != null)
            machineLight.enabled = active;

        if (ambientAudioSource != null)
        {
            if (active) ambientAudioSource.Play();
            else ambientAudioSource.Stop();
        }

        if (!active && playerInRange)
        {
            InteractionUI.Hide();
            playerInRange = false;
        }
    }

    /// <summary>
    /// Activa la electricidad de esta máquina.
    /// </summary>
    public void PowerOn()
    {
        SetMachineActive(true);
    }

    // =========================================================================
    // UTILIDADES
    // =========================================================================

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }

    /// <summary>
    /// Obtiene el tipo de perk de esta máquina.
    /// </summary>
    public PerkType GetPerkType() => perkType;

    /// <summary>
    /// Obtiene el coste de esta máquina.
    /// </summary>
    public int GetCost() => perkCost;

    // =========================================================================
    // GIZMOS
    // =========================================================================

    void OnDrawGizmosSelected()
    {
        Color gizmoColor = Application.isPlaying ? perkColor : PlayerPerkManager.GetPerkColor(perkType);
        gizmoColor.a = 0.3f;
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
        gizmoColor.a = 0.1f;
        Gizmos.color = gizmoColor;
        Gizmos.DrawSphere(transform.position, interactionDistance);
    }
}
