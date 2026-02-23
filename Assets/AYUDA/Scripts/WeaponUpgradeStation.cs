using UnityEngine;

/// <summary>
/// Estación de mejora de armas — objeto interactuable del mundo.
/// El jugador se acerca, pulsa la tecla de interacción y mejora su arma actual.
/// Máximo 2 mejoras por arma. El coste depende del arma (rifle > pistola).
/// Al mejorar: aumenta daño, velocidad de disparo, cargador y reserva, y recarga TODA la munición.
///
/// SETUP:
/// 1. Crear un GameObject vacío (o con mesh visual)
/// 2. Añadir este script
/// 3. Añadir un Collider sólido (isTrigger = false) para que no se atraviese
/// 4. NO necesita trigger ni tag "Player" — detección por distancia automática
/// 5. (Opcional) AudioSource para sonidos, UI text para info
/// </summary>
public class WeaponUpgradeStation : MonoBehaviour
{
    [Header("=== CONFIGURACIÓN ===")]
    [Tooltip("Tecla para interactuar")]
    [SerializeField] private KeyCode interactKey = KeyCode.F;
    [Tooltip("Distancia máxima de interacción")]
    [SerializeField] private float interactionDistance = 3.5f;

    [Header("=== UI ===")]
    [Tooltip("Canvas/Panel propio de la estación (opcional, se muestra al estar cerca)")]
    [SerializeField] private GameObject promptUI;
    [Tooltip("TMPro text para mostrar info detallada de mejora (opcional)")]
    [SerializeField] private TMPro.TextMeshProUGUI upgradeInfoText;

    [Header("=== EFECTOS ===")]
    [SerializeField] private AudioClip upgradeSound;
    [SerializeField] private AudioClip failSound;
    [SerializeField] private AudioClip maxLevelSound;
    [SerializeField] private GameObject upgradeVFXPrefab;
    [Tooltip("Punto donde aparece el VFX (si null, usa transform.position)")]
    [SerializeField] private Transform vfxPoint;
    private AudioSource audioSource;

    [Header("=== ANIMACIÓN PROCEDURAL ===")]
    [Tooltip("¿Animar la estación al mejorar? (pulse de escala)")]
    [SerializeField] private bool animateOnUpgrade = true;

    // Estado
    private bool playerInRange = false;
    private Transform playerTransform;
    private WeaponManager cachedWeaponManager;
    private PlayerEconomy cachedEconomy;

    // Animación
    private Vector3 originalScale;
    private float animTimer = 0f;
    private bool isAnimating = false;

    void Awake()
    {
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
        // Buscar al Player — por tag o por componente
        FindPlayer();
    }

    private void FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            cachedWeaponManager = player.GetComponent<WeaponManager>();
            if (cachedWeaponManager == null)
                cachedWeaponManager = player.GetComponentInChildren<WeaponManager>();
        }
        else
        {
            // Fallback: buscar por componente
            var pm = FindObjectOfType<PlayerMovement>();
            if (pm != null)
            {
                playerTransform = pm.transform;
                cachedWeaponManager = pm.GetComponent<WeaponManager>();
                if (cachedWeaponManager == null)
                    cachedWeaponManager = pm.GetComponentInChildren<WeaponManager>();
            }
        }

        cachedEconomy = PlayerEconomy.Instance;
    }

    void Update()
    {
        // ===== DETECCIÓN POR DISTANCIA (no depende de triggers) =====
        CheckPlayerDistance();

        // Detectar interacción
        if (playerInRange && Input.GetKeyDown(interactKey))
        {
            TryUpgrade();
        }

        // Actualizar info text si está visible
        if (playerInRange && upgradeInfoText != null)
        {
            UpdateInfoText();
        }

        // Actualizar prompt con info dinámica
        if (playerInRange)
        {
            string prompt = BuildInteractionPrompt();
            InteractionUI.Show(prompt);
        }

        // Animación procedural
        if (isAnimating)
        {
            animTimer += Time.deltaTime;
            float t = animTimer / 0.5f;

            if (t >= 1f)
            {
                transform.localScale = originalScale;
                isAnimating = false;
            }
            else
            {
                float spring = 1f + Mathf.Sin(t * Mathf.PI * 3f) * 0.15f * (1f - t);
                transform.localScale = originalScale * spring;
            }
        }
    }

    /// <summary>
    /// Comprueba distancia al jugador cada frame. No depende de triggers.
    /// </summary>
    private void CheckPlayerDistance()
    {
        if (playerTransform == null)
        {
            // Intentar buscar de nuevo (por si se spawneó tarde)
            if (cachedWeaponManager == null)
                FindPlayer();
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

            if (promptUI != null)
                promptUI.SetActive(true);

            Debug.Log("[UPGRADE STATION] Jugador en rango de mejora");
        }
        // Salió de rango
        else if (!playerInRange && wasInRange)
        {
            if (promptUI != null)
                promptUI.SetActive(false);

            InteractionUI.Hide();
        }
    }

    /// <summary>
    /// Construye el texto del prompt de interacción con info del coste.
    /// </summary>
    /// <summary>
    /// Obtiene el nombre real del arma (por tipo de clase, igual que WeaponManager).
    /// </summary>
    private string GetWeaponDisplayName(WeaponBase weapon)
    {
        if (weapon is Weapon_Rifle) return "Rifle de Asalto";
        if (weapon is Weapon_Pistol) return "Pistola";
        if (weapon is Weapon_Shotgun) return "Escopeta";
        if (weapon is Weapon_ShotgunAuto) return "Escopeta Automática";
        if (weapon is Weapon_BurstRifle) return "Rifle de Ráfagas";
        if (weapon is Weapon_SemiAutoRifle) return "Rifle Semi-Auto";
        // Fallback al nombre del WeaponData
        return weapon.GetWeaponData().weaponName;
    }

    private string BuildInteractionPrompt()
    {
        if (cachedWeaponManager == null)
            return $"[{interactKey}] Mejorar arma";

        WeaponBase weapon = cachedWeaponManager.GetCurrentWeapon();
        if (weapon == null)
            return "Sin arma equipada";

        string weaponName = GetWeaponDisplayName(weapon);

        if (!weapon.CanUpgrade)
            return $"{weaponName} — NIVEL MÁXIMO";

        int cost = weapon.GetUpgradeCost();
        int coins = cachedEconomy != null ? cachedEconomy.CurrentCoins : 0;
        int lvl = weapon.UpgradeLevel + 1;

        if (cachedEconomy != null && cachedEconomy.CanAfford(cost))
            return $"[{interactKey}] Mejorar {weaponName} (Nv.{lvl}) — {cost} monedas";
        else
            return $"[{interactKey}] Mejorar {weaponName} (Nv.{lvl}) — {cost} monedas (necesitas {cost - coins} más)";
    }

    /// <summary>
    /// Intenta mejorar el arma actual del jugador.
    /// </summary>
    private void TryUpgrade()
    {
        if (cachedWeaponManager == null || cachedEconomy == null)
        {
            Debug.LogWarning("[UPGRADE STATION] No se encontró WeaponManager o PlayerEconomy");
            PlaySound(failSound);
            return;
        }

        WeaponBase currentWeapon = cachedWeaponManager.GetCurrentWeapon();
        if (currentWeapon == null)
        {
            Debug.LogWarning("[UPGRADE STATION] No hay arma equipada");
            PlaySound(failSound);
            return;
        }

        // ¿Ya al máximo? (nivel 2)
        if (!currentWeapon.CanUpgrade)
        {
            Debug.Log($"[UPGRADE STATION] {currentWeapon.GetWeaponData().weaponName} ya está al nivel máximo (2/2)");
            PlaySound(maxLevelSound);
            return;
        }

        // Obtener coste (depende del arma y nivel)
        int cost = currentWeapon.GetUpgradeCost();

        // ¿Tiene monedas?
        if (!cachedEconomy.CanAfford(cost))
        {
            Debug.Log($"[UPGRADE STATION] Monedas insuficientes. Necesitas {cost}, tienes {cachedEconomy.CurrentCoins}");
            PlaySound(failSound);
            return;
        }

        // ¡MEJORAR!
        cachedEconomy.SpendCoins(cost);
        currentWeapon.ApplyUpgrade();

        // Feedback
        PlaySound(upgradeSound);
        SpawnVFX();

        if (animateOnUpgrade)
        {
            isAnimating = true;
            animTimer = 0f;
        }

        Debug.Log($"[UPGRADE STATION] ¡{currentWeapon.GetWeaponData().weaponName} mejorada! " +
                  $"Nivel {currentWeapon.UpgradeLevel}/{currentWeapon.MaxUpgradeLevel}. " +
                  $"Monedas restantes: {cachedEconomy.CurrentCoins}");
    }

    /// <summary>
    /// Actualiza el texto detallado de info (panel propio opcional).
    /// </summary>
    private void UpdateInfoText()
    {
        if (cachedWeaponManager == null) return;

        WeaponBase weapon = cachedWeaponManager.GetCurrentWeapon();
        if (weapon == null)
        {
            upgradeInfoText.text = "Sin arma equipada";
            return;
        }

        WeaponData data = weapon.GetWeaponData();
        if (!weapon.CanUpgrade)
        {
            upgradeInfoText.text = $"<b>{data.weaponName}</b>\n<color=#FFD700>★ NIVEL MÁXIMO ★</color>";
            return;
        }

        int cost = weapon.GetUpgradeCost();
        bool canAfford = cachedEconomy != null && cachedEconomy.CanAfford(cost);
        string costColor = canAfford ? "#4CAF50" : "#FF5252";

        upgradeInfoText.text = $"<b>{data.weaponName}</b>\n" +
                               $"Coste: <color={costColor}>{cost} monedas</color>\n\n" +
                               weapon.GetUpgradeDescription() +
                               $"\n\n<i>[{interactKey}] Mejorar</i>";
    }

    // =========================================================================
    // DETECCIÓN — ya no usa triggers, usa distancia en Update()
    // =========================================================================

    // OnTriggerEnter/Exit eliminados — la detección por distancia es más fiable
    // No depende de tags, triggers, ni colliders especiales

    // =========================================================================
    // UTILIDADES
    // =========================================================================

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }

    private void SpawnVFX()
    {
        if (upgradeVFXPrefab == null) return;

        Vector3 pos = vfxPoint != null ? vfxPoint.position : transform.position + Vector3.up;
        GameObject vfx = Instantiate(upgradeVFXPrefab, pos, Quaternion.identity);
        Destroy(vfx, 3f);
    }

    // =========================================================================
    // GIZMOS
    // =========================================================================

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, 3f);
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.1f);
        Gizmos.DrawSphere(transform.position, 3f);
    }
}
