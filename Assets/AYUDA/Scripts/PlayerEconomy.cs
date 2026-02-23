using UnityEngine;

/// <summary>
/// Sistema de economía del jugador — versión mejorada.
/// Gestiona monedas con: multiplicadores, rachas de kills, estadísticas,
/// persistencia opcional y recompensas configurables.
/// Añadir al objeto Player.
/// </summary>
public class PlayerEconomy : MonoBehaviour
{
    [Header("=== ECONOMÍA ===")]
    [Tooltip("Monedas iniciales")]
    [SerializeField] private int startingCoins = 0;
    [Tooltip("Monedas máximas que puede tener el jugador (0 = sin límite)")]
    [SerializeField] private int maxCoins = 0;

    [Header("=== RECOMPENSAS BASE ===")]
    [Tooltip("Monedas base por matar un enemigo normal")]
    [SerializeField] private int baseKillReward = 50;
    [Tooltip("Monedas extra por headshot")]
    [SerializeField] private int headshotBonus = 25;
    [Tooltip("Monedas extra por kill con melee")]
    [SerializeField] private int meleeKillBonus = 30;

    [Header("=== RACHA DE KILLS (Combo) ===")]
    [Tooltip("¿Activar sistema de rachas?")]
    [SerializeField] private bool enableKillStreak = true;
    [Tooltip("Tiempo máximo entre kills para mantener la racha (segundos)")]
    [SerializeField] private float streakTimeWindow = 4f;
    [Tooltip("Bonus de monedas por cada kill en racha (acumulativo)")]
    [SerializeField] private int streakBonusPerKill = 10;
    [Tooltip("Monedas máximas de bonus por racha")]
    [SerializeField] private int maxStreakBonus = 100;

    [Header("=== MULTIPLICADOR GLOBAL ===")]
    [Tooltip("Multiplicador global de monedas ganadas (1 = normal, 2 = doble)")]
    [SerializeField] private float coinMultiplier = 1f;
    [Tooltip("Duración del multiplicador temporal (0 = permanente)")]
    [SerializeField] private float multiplierDuration = 0f;

    [Header("=== PERSISTENCIA ===")]
    [Tooltip("¿Guardar monedas entre sesiones con PlayerPrefs?")]
    [SerializeField] private bool saveCoins = false;
    [Tooltip("Clave de PlayerPrefs para guardar")]
    [SerializeField] private string saveKey = "PlayerCoins";

    [Header("=== DEBUG ===")]
    [SerializeField] private bool debugMode = false;
    [Tooltip("Tecla para añadir monedas de prueba")]
    [SerializeField] private KeyCode debugAddCoinsKey = KeyCode.F9;
    [SerializeField] private int debugCoinsAmount = 500;
    [Tooltip("Tecla para resetear monedas")]
    [SerializeField] private KeyCode debugResetKey = KeyCode.F10;

    // Estado interno
    private int currentCoins;
    private int currentStreak = 0;
    private float lastKillTime = -999f;
    private float multiplierEndTime = 0f;
    private float baseMultiplier = 1f;

    // Estadísticas de sesión
    private int totalCoinsEarned = 0;
    private int totalCoinsSpent = 0;
    private int totalKills = 0;
    private int bestStreak = 0;

    // =========================================================================
    // EVENTOS
    // =========================================================================

    /// <summary>Evento: monedas cambiaron (cantidad actual)</summary>
    public System.Action<int> OnCoinsChanged;

    /// <summary>Evento: monedas ganadas con detalle (cantidad ganada, nuevo total, fuente)</summary>
    public System.Action<int, int, string> OnCoinsEarned;

    /// <summary>Evento: monedas gastadas con detalle (cantidad, nuevo total)</summary>
    public System.Action<int, int> OnCoinsSpent;

    /// <summary>Evento: racha actualizada (kills en racha)</summary>
    public System.Action<int> OnStreakChanged;

    /// <summary>Evento: racha perdida (última racha)</summary>
    public System.Action<int> OnStreakLost;

    /// <summary>Evento: multiplicador cambiado (nuevo multiplicador)</summary>
    public System.Action<float> OnMultiplierChanged;

    // =========================================================================
    // PROPIEDADES PÚBLICAS
    // =========================================================================

    /// <summary>Monedas actuales del jugador</summary>
    public int CurrentCoins => currentCoins;

    /// <summary>Racha actual de kills</summary>
    public int CurrentStreak => currentStreak;

    /// <summary>Mejor racha de la sesión</summary>
    public int BestStreak => bestStreak;

    /// <summary>Total de monedas ganadas esta sesión</summary>
    public int TotalCoinsEarned => totalCoinsEarned;

    /// <summary>Total de monedas gastadas esta sesión</summary>
    public int TotalCoinsSpent => totalCoinsSpent;

    /// <summary>Multiplicador de monedas actual (incluye temporales)</summary>
    public float ActiveMultiplier => GetActiveMultiplier();

    /// <summary>Recompensa base por kill</summary>
    public int BaseKillReward => baseKillReward;

    // Singleton para acceso fácil desde otros scripts
    public static PlayerEconomy Instance { get; private set; }

    // =========================================================================
    // LIFECYCLE
    // =========================================================================

    void Awake()
    {
        // Singleton simple
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        // Cargar monedas guardadas o usar valor inicial
        if (saveCoins && PlayerPrefs.HasKey(saveKey))
        {
            currentCoins = PlayerPrefs.GetInt(saveKey, startingCoins);
        }
        else
        {
            currentCoins = startingCoins;
        }

        baseMultiplier = coinMultiplier;
    }

    void Start()
    {
        OnCoinsChanged?.Invoke(currentCoins);
    }

    void Update()
    {
        // Verificar expiración del multiplicador temporal
        if (multiplierDuration > 0f && multiplierEndTime > 0f && Time.time >= multiplierEndTime)
        {
            coinMultiplier = baseMultiplier;
            multiplierEndTime = 0f;
            OnMultiplierChanged?.Invoke(GetActiveMultiplier());
            Debug.Log("[ECONOMY] Multiplicador temporal expirado.");
        }

        // Verificar expiración de racha
        if (enableKillStreak && currentStreak > 0)
        {
            if (Time.time - lastKillTime > streakTimeWindow)
            {
                int lostStreak = currentStreak;
                currentStreak = 0;
                OnStreakLost?.Invoke(lostStreak);

                if (debugMode)
                    Debug.Log($"[ECONOMY] Racha de {lostStreak} kills perdida.");
            }
        }

        // Debug controls
        if (debugMode)
        {
            if (Input.GetKeyDown(debugAddCoinsKey))
            {
                AddCoins(debugCoinsAmount, "Debug");
                Debug.Log($"[ECONOMY] DEBUG: +{debugCoinsAmount} monedas. Total: {currentCoins}");
            }
            if (Input.GetKeyDown(debugResetKey))
            {
                ResetCoins();
                Debug.Log("[ECONOMY] DEBUG: Monedas reseteadas a 0.");
            }
        }
    }

    void OnApplicationQuit()
    {
        if (saveCoins)
            SaveCoins();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            if (saveCoins)
                SaveCoins();
            Instance = null;
        }
    }

    // =========================================================================
    // GESTIÓN DE MONEDAS
    // =========================================================================

    /// <summary>
    /// Añade monedas al jugador con fuente opcional para tracking.
    /// </summary>
    public void AddCoins(int amount, string source = "Generic")
    {
        if (amount <= 0) return;

        // Aplicar multiplicador
        int finalAmount = Mathf.RoundToInt(amount * GetActiveMultiplier());

        currentCoins += finalAmount;

        // Aplicar límite máximo si está configurado
        if (maxCoins > 0)
            currentCoins = Mathf.Min(currentCoins, maxCoins);

        // Estadísticas
        totalCoinsEarned += finalAmount;

        // Eventos
        OnCoinsChanged?.Invoke(currentCoins);
        OnCoinsEarned?.Invoke(finalAmount, currentCoins, source);

        if (debugMode)
            Debug.Log($"[ECONOMY] +{finalAmount} monedas ({source}). Total: {currentCoins}" +
                      (GetActiveMultiplier() > 1f ? $" [x{GetActiveMultiplier():F1}]" : ""));

        // Auto-guardar
        if (saveCoins)
            SaveCoins();
    }

    /// <summary>
    /// Intenta gastar monedas. Retorna true si tenía suficientes.
    /// </summary>
    public bool SpendCoins(int amount)
    {
        if (amount <= 0) return false;
        if (currentCoins < amount) return false;

        currentCoins -= amount;

        // Estadísticas
        totalCoinsSpent += amount;

        // Eventos
        OnCoinsChanged?.Invoke(currentCoins);
        OnCoinsSpent?.Invoke(amount, currentCoins);

        if (debugMode)
            Debug.Log($"[ECONOMY] -{amount} monedas. Total: {currentCoins}");

        // Auto-guardar
        if (saveCoins)
            SaveCoins();

        return true;
    }

    /// <summary>
    /// ¿Tiene suficientes monedas?
    /// </summary>
    public bool CanAfford(int amount)
    {
        return currentCoins >= amount;
    }

    /// <summary>
    /// Resetea las monedas a 0 (o al valor inicial).
    /// </summary>
    public void ResetCoins()
    {
        currentCoins = startingCoins;
        OnCoinsChanged?.Invoke(currentCoins);

        if (saveCoins)
            SaveCoins();
    }

    /// <summary>
    /// Establece las monedas directamente (para cargar partidas, etc.)
    /// </summary>
    public void SetCoins(int amount)
    {
        currentCoins = Mathf.Max(0, amount);
        if (maxCoins > 0)
            currentCoins = Mathf.Min(currentCoins, maxCoins);

        OnCoinsChanged?.Invoke(currentCoins);

        if (saveCoins)
            SaveCoins();
    }

    // =========================================================================
    // RECOMPENSA POR KILL
    // =========================================================================

    /// <summary>
    /// Registra un kill y da la recompensa correspondiente.
    /// Llamar desde EnemyHealth.Die().
    /// </summary>
    /// <param name="customReward">Recompensa personalizada (0 = usar baseKillReward)</param>
    /// <param name="isHeadshot">¿Fue un headshot?</param>
    /// <param name="isMeleeKill">¿Fue con melee?</param>
    public void RegisterKill(int customReward = 0, bool isHeadshot = false, bool isMeleeKill = false)
    {
        totalKills++;

        // Calcular recompensa base
        int reward = customReward > 0 ? customReward : baseKillReward;

        // Bonus por headshot
        if (isHeadshot)
            reward += headshotBonus;

        // Bonus por melee
        if (isMeleeKill)
            reward += meleeKillBonus;

        // Sistema de rachas
        if (enableKillStreak)
        {
            if (Time.time - lastKillTime <= streakTimeWindow)
            {
                currentStreak++;
            }
            else
            {
                currentStreak = 1;
            }

            lastKillTime = Time.time;

            // Bonus por racha
            int streakBonus = Mathf.Min((currentStreak - 1) * streakBonusPerKill, maxStreakBonus);
            reward += streakBonus;

            // Actualizar mejor racha
            if (currentStreak > bestStreak)
                bestStreak = currentStreak;

            OnStreakChanged?.Invoke(currentStreak);

            if (debugMode && currentStreak > 1)
                Debug.Log($"[ECONOMY] ¡Racha x{currentStreak}! Bonus: +{streakBonus}");
        }

        // Construir la fuente del texto
        string source = "Kill";
        if (isHeadshot) source = "Headshot";
        if (isMeleeKill) source = "Melee Kill";
        if (currentStreak > 2) source += $" (x{currentStreak})";

        // Añadir monedas (el multiplicador se aplica dentro de AddCoins)
        AddCoins(reward, source);
    }

    // =========================================================================
    // MULTIPLICADOR
    // =========================================================================

    /// <summary>
    /// Aplica un multiplicador temporal de monedas.
    /// </summary>
    /// <param name="multiplier">Multiplicador (2 = doble, 3 = triple)</param>
    /// <param name="duration">Duración en segundos (0 = permanente hasta resetear)</param>
    public void SetTemporaryMultiplier(float multiplier, float duration = 0f)
    {
        coinMultiplier = Mathf.Max(0.1f, multiplier);

        if (duration > 0f)
        {
            multiplierDuration = duration;
            multiplierEndTime = Time.time + duration;
        }
        else
        {
            multiplierDuration = 0f;
            multiplierEndTime = 0f;
        }

        OnMultiplierChanged?.Invoke(GetActiveMultiplier());

        if (debugMode)
            Debug.Log($"[ECONOMY] Multiplicador x{multiplier:F1}" +
                      (duration > 0 ? $" por {duration}s" : " permanente"));
    }

    /// <summary>
    /// Resetea el multiplicador al valor base.
    /// </summary>
    public void ResetMultiplier()
    {
        coinMultiplier = baseMultiplier;
        multiplierEndTime = 0f;
        multiplierDuration = 0f;
        OnMultiplierChanged?.Invoke(GetActiveMultiplier());
    }

    /// <summary>
    /// Obtiene el multiplicador activo actual (con temporales).
    /// </summary>
    private float GetActiveMultiplier()
    {
        return coinMultiplier;
    }

    // =========================================================================
    // PERSISTENCIA
    // =========================================================================

    /// <summary>
    /// Guarda las monedas en PlayerPrefs.
    /// </summary>
    public void SaveCoins()
    {
        PlayerPrefs.SetInt(saveKey, currentCoins);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Carga las monedas desde PlayerPrefs.
    /// </summary>
    public void LoadCoins()
    {
        if (PlayerPrefs.HasKey(saveKey))
        {
            currentCoins = PlayerPrefs.GetInt(saveKey, startingCoins);
            OnCoinsChanged?.Invoke(currentCoins);
        }
    }

    /// <summary>
    /// Borra los datos guardados.
    /// </summary>
    public void ClearSavedData()
    {
        PlayerPrefs.DeleteKey(saveKey);
        PlayerPrefs.Save();
    }

    // =========================================================================
    // ESTADÍSTICAS
    // =========================================================================

    /// <summary>
    /// Obtiene un resumen de estadísticas de la sesión.
    /// </summary>
    public string GetSessionStats()
    {
        return $"Monedas: {currentCoins}\n" +
               $"Total ganado: {totalCoinsEarned}\n" +
               $"Total gastado: {totalCoinsSpent}\n" +
               $"Kills totales: {totalKills}\n" +
               $"Mejor racha: {bestStreak}";
    }
}
