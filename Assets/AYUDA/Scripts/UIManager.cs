using UnityEngine;

/// <summary>
/// Controlador central del HUD completo.
/// Conecta TODOS los sistemas del juego con sus respectivos elementos de UI.
/// Colocar en el Canvas raíz.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("=== REFERENCIAS DE SISTEMAS ===")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private WeaponManager weaponManager;
    [SerializeField] private MedkitInventory medkitInventory;

    [Header("=== PANELES DE UI ===")]
    [SerializeField] private HealthBarUI healthBarUI;
    [SerializeField] private WeaponUI weaponUI;
    [SerializeField] private MedkitUI medkitUI;
    [SerializeField] private InteractionUI interactionUI;
    [SerializeField] private CoinsUI coinsUI;

    [Header("=== PANEL GAME OVER ===")]
    [SerializeField] private GameObject gameOverPanel;

    private void Awake()
    {
        // Singleton
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    // =========================================================================
    // AUTO-BOOTSTRAP: Si no hay UIManager en la escena, crearlo automáticamente
    // =========================================================================
    // Esto resuelve el problema de prefab variants donde el UIManager no fue
    // añadido como componente al Canvas del PlayerUI.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoBootstrap()
    {
        // Si ya existe uno, no hacer nada
        if (FindObjectOfType<UIManager>() != null) return;

        // Buscar un Canvas que tenga HealthBarUI o WeaponUI (ese es el Canvas correcto)
        Canvas[] canvases = FindObjectsOfType<Canvas>(true);
        Canvas targetCanvas = null;

        foreach (Canvas canvas in canvases)
        {
            if (canvas.GetComponentInChildren<HealthBarUI>(true) != null ||
                canvas.GetComponentInChildren<WeaponUI>(true) != null)
            {
                targetCanvas = canvas;
                break;
            }
        }

        // Si no se encontró un Canvas con scripts de UI, buscar cualquier Canvas
        if (targetCanvas == null && canvases.Length > 0)
        {
            targetCanvas = canvases[0];
        }

        if (targetCanvas != null)
        {
            UIManager manager = targetCanvas.gameObject.AddComponent<UIManager>();
            Debug.Log("[UIManager] Auto-creado en Canvas '" + targetCanvas.name + "' (no existía en la escena).");
        }
        else
        {
            Debug.LogWarning("[UIManager] No se encontró ningún Canvas. El HUD no funcionará.");
        }
    }

    private void Start()
    {
        // ===== AUTO-BUSCAR REFERENCIAS SI NO ESTÁN ASIGNADAS =====
        FindReferences();

        // ===== CONECTAR EVENTOS =====
        ConnectHealthEvents();
        ConnectWeaponEvents();
        ConnectMedkitEvents();
        ConnectEconomyEvents();

        // Ocultar Game Over al inicio
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        Debug.Log("[UIManager] HUD inicializado correctamente.");
    }

    // =========================================================================
    // BUSCAR REFERENCIAS AUTOMÁTICAMENTE
    // =========================================================================

    private void FindReferences()
    {
        // Buscar sistemas del jugador
        if (playerHealth == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerHealth = player.GetComponent<PlayerHealth>();
                if (weaponManager == null)
                    weaponManager = player.GetComponent<WeaponManager>();
                if (medkitInventory == null)
                    medkitInventory = player.GetComponent<MedkitInventory>();
            }
        }

        // Buscar UI components en hijos del Canvas
        if (healthBarUI == null)
            healthBarUI = GetComponentInChildren<HealthBarUI>(true);
        if (weaponUI == null)
            weaponUI = GetComponentInChildren<WeaponUI>(true);
        if (medkitUI == null)
            medkitUI = GetComponentInChildren<MedkitUI>(true);
        if (interactionUI == null)
            interactionUI = GetComponentInChildren<InteractionUI>(true);
        if (interactionUI == null)
            interactionUI = FindObjectOfType<InteractionUI>();
        // No crear si no existe — el script se pondrá en el Player o donde el usuario quiera
        if (coinsUI == null)
            coinsUI = GetComponentInChildren<CoinsUI>(true);
    }

    // =========================================================================
    // CONECTAR EVENTOS DE SALUD
    // =========================================================================

    private void ConnectHealthEvents()
    {
        if (playerHealth == null)
        {
            Debug.LogWarning("[UIManager] PlayerHealth no encontrado. La barra de vida no funcionará.");
            return;
        }

        // Salud cambia → actualizar barra
        playerHealth.OnHealthChanged.AddListener(OnHealthChanged);

        // Jugador recibe daño → notificación visual
        playerHealth.OnPlayerDamaged.AddListener(OnPlayerDamaged);

        // Jugador se cura → notificación "+50 Vida"
        playerHealth.OnPlayerHealed.AddListener(OnPlayerHealed);

        // Jugador muere → game over
        playerHealth.OnPlayerDeath.AddListener(OnPlayerDeath);

        // Actualizar UI inicial
        OnHealthChanged(playerHealth.GetCurrentHealth(), playerHealth.GetMaxHealth());
    }

    // =========================================================================
    // CONECTAR EVENTOS DE ARMAS
    // =========================================================================

    private void ConnectWeaponEvents()
    {
        if (weaponManager == null)
        {
            Debug.LogWarning("[UIManager] WeaponManager no encontrado. La UI de armas no funcionará.");
            return;
        }

        // Suscribirse a eventos del WeaponManager
        weaponManager.OnWeaponSwitched += OnWeaponSwitched;
        weaponManager.OnAmmoUpdated += OnAmmoUpdated;
        weaponManager.OnReloadStart += OnReloadStart;
        weaponManager.OnReloadEnd += OnReloadEnd;
        weaponManager.OnOutOfAmmo += OnOutOfAmmo;
    }

    // =========================================================================
    // CONECTAR EVENTOS DE BOTIQUINES
    // =========================================================================

    private void ConnectMedkitEvents()
    {
        if (medkitInventory == null)
        {
            Debug.LogWarning("[UIManager] MedkitInventory no encontrado.");
            return;
        }

        medkitInventory.OnMedkitCountChanged.AddListener(OnMedkitCountChanged);

        // Actualizar UI inicial
        OnMedkitCountChanged(medkitInventory.GetCurrentMedkits());
    }

    // =========================================================================
    // CALLBACKS DE SALUD
    // =========================================================================

    private void OnHealthChanged(int current, int max)
    {
        if (healthBarUI != null)
            healthBarUI.UpdateHealthBar(current, max);
    }

    private void OnPlayerDamaged()
    {
        // El efecto visual de daño ya lo maneja DamageEffect/ScreenShake
        // Aquí podemos añadir un flash rojo en el borde del HUD si queremos
    }

    private void OnPlayerHealed()
    {
        // Si quieres notificaciones, añade NotificationSystem de nuevo
        Debug.Log("[UIManager] +50 Vida");
    }

    private void OnPlayerDeath()
    {
        Debug.Log("[UIManager] HAS MUERTO");

        // Game Over se maneja por GameManager, pero podemos ocultar HUD
        // después de un delay
        Invoke(nameof(HideHUDElements), 1.5f);
    }

    private void HideHUDElements()
    {
        if (healthBarUI != null) healthBarUI.gameObject.SetActive(false);
        if (weaponUI != null) weaponUI.gameObject.SetActive(false);
        if (medkitUI != null) medkitUI.gameObject.SetActive(false);
        if (coinsUI != null) coinsUI.gameObject.SetActive(false);
    }

    // =========================================================================
    // CONECTAR EVENTOS DE ECONOMÍA
    // =========================================================================

    private void ConnectEconomyEvents()
    {
        if (PlayerEconomy.Instance == null)
        {
            Debug.LogWarning("[UIManager] PlayerEconomy no encontrado. La UI de monedas no funcionará.");
            return;
        }

        // CoinsUI se auto-conecta a PlayerEconomy, pero verificamos que exista
        if (coinsUI != null)
        {
            Debug.Log("[UIManager] CoinsUI conectada al sistema de economía.");
        }
        else
        {
            Debug.LogWarning("[UIManager] CoinsUI no encontrada en el Canvas. Crea un TextMeshPro y añade CoinsUI.");
        }
    }

    // =========================================================================
    // CALLBACKS DE ARMAS
    // =========================================================================

    private void OnWeaponSwitched(string weaponName, Sprite icon, bool isAutomatic)
    {
        if (weaponUI != null)
        {
            weaponUI.UpdateWeaponInfo(weaponName, icon);
            weaponUI.UpdateFireMode(isAutomatic);
        }

        Debug.Log($"[UIManager] Arma cambiada: {weaponName}");
    }

    private void OnAmmoUpdated(int inMagazine, int inReserve)
    {
        if (weaponUI != null)
            weaponUI.UpdateAmmo(inMagazine, inReserve);
    }

    private void OnReloadStart()
    {
        if (weaponUI != null)
            weaponUI.ShowReloadIndicator(true);

        Debug.Log("[UIManager] Recargando...");
    }

    private void OnReloadEnd()
    {
        if (weaponUI != null)
            weaponUI.ShowReloadIndicator(false);
    }

    private void OnOutOfAmmo()
    {
        Debug.Log("[UIManager] ¡Sin munición!");
    }

    // =========================================================================
    // CALLBACKS DE BOTIQUINES
    // =========================================================================

    private void OnMedkitCountChanged(int count)
    {
        if (medkitUI != null)
            medkitUI.UpdateMedkitCount(count);
    }

    // =========================================================================
    // MÉTODOS PÚBLICOS PARA OTROS SISTEMAS
    // =========================================================================

    /// <summary>
    /// Muestra el prompt de interacción
    /// </summary>
    public void ShowInteraction(string message)
    {
        if (interactionUI != null)
            interactionUI.ShowInteractionPrompt(message);
    }

    /// <summary>
    /// Oculta el prompt de interacción
    /// </summary>
    public void HideInteraction()
    {
        if (interactionUI != null)
            interactionUI.HideInteractionPrompt();
    }

    private void OnDestroy()
    {
        // Desuscribirse de eventos
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged.RemoveListener(OnHealthChanged);
            playerHealth.OnPlayerDamaged.RemoveListener(OnPlayerDamaged);
            playerHealth.OnPlayerHealed.RemoveListener(OnPlayerHealed);
            playerHealth.OnPlayerDeath.RemoveListener(OnPlayerDeath);
        }

        if (weaponManager != null)
        {
            weaponManager.OnWeaponSwitched -= OnWeaponSwitched;
            weaponManager.OnAmmoUpdated -= OnAmmoUpdated;
            weaponManager.OnReloadStart -= OnReloadStart;
            weaponManager.OnReloadEnd -= OnReloadEnd;
            weaponManager.OnOutOfAmmo -= OnOutOfAmmo;
        }

        if (medkitInventory != null)
        {
            medkitInventory.OnMedkitCountChanged.RemoveListener(OnMedkitCountChanged);
        }
    }
}
