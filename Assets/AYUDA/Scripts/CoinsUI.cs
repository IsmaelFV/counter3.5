using UnityEngine;
using TMPro;

/// <summary>
/// HUD que muestra las monedas del jugador en la UI.
/// Se suscribe automáticamente a PlayerEconomy y se actualiza cuando cambian las monedas.
/// Incluye: conteo animado, punch scale, color feedback, indicador de racha y multiplicador.
///
/// SETUP:
/// 1. Crear un TextMeshPro en tu Canvas de UI
/// 2. Añadir este script al texto (o a un objeto padre)
/// 3. Asignar la referencia al TextMeshPro en el Inspector
/// 4. ¡Listo! Se actualiza automáticamente
/// </summary>
public class CoinsUI : MonoBehaviour
{
    [Header("=== REFERENCIAS ===")]
    [Tooltip("Texto donde se mostrará la cantidad de monedas")]
    [SerializeField] private TextMeshProUGUI coinsText;
    [Tooltip("(Opcional) Texto para mostrar la fuente del ingreso (+50 Headshot!)")]
    [SerializeField] private TextMeshProUGUI earningPopupText;
    [Tooltip("(Opcional) Texto para mostrar racha de kills")]
    [SerializeField] private TextMeshProUGUI streakText;
    [Tooltip("(Opcional) Texto para multiplicador activo")]
    [SerializeField] private TextMeshProUGUI multiplierText;

    [Header("=== FORMATO ===")]
    [Tooltip("Prefijo antes del número (ej: 'Monedas: ', '$', '💰 ')")]
    [SerializeField] private string prefix = "$ ";
    [Tooltip("Sufijo después del número (ej: ' pts', 'Z', '')")]
    [SerializeField] private string suffix = "";
    [Tooltip("Formato del número (N0 = separador miles, F0 = sin decimales)")]
    [SerializeField] private string numberFormat = "N0";

    [Header("=== CONTEO ANIMADO ===")]
    [Tooltip("¿Animar el conteo numérico? (sube/baja gradualmente)")]
    [SerializeField] private bool animateCount = true;
    [Tooltip("Velocidad del conteo animado (más alto = más rápido)")]
    [SerializeField] private float countSpeed = 8f;

    [Header("=== ANIMACIÓN VISUAL ===")]
    [Tooltip("¿Animar el texto al ganar/perder monedas?")]
    [SerializeField] private bool animateOnChange = true;
    [Tooltip("Escala máxima de la animación (1.2 = 20% más grande)")]
    [SerializeField] private float punchScale = 1.25f;
    [Tooltip("Duración de la animación en segundos")]
    [SerializeField] private float animDuration = 0.35f;
    [Tooltip("Color cuando ganas monedas")]
    [SerializeField] private Color gainColor = new Color(0.3f, 1f, 0.3f, 1f); // Verde
    [Tooltip("Color cuando pierdes monedas")]
    [SerializeField] private Color loseColor = new Color(1f, 0.3f, 0.3f, 1f); // Rojo
    [Tooltip("Color normal del texto")]
    [SerializeField] private Color normalColor = Color.white;

    [Header("=== POPUP DE GANANCIAS ===")]
    [Tooltip("Duración del popup de ganancias")]
    [SerializeField] private float popupDuration = 1.5f;
    [Tooltip("Color del popup de ganancias")]
    [SerializeField] private Color popupGainColor = new Color(1f, 0.85f, 0f, 1f); // Dorado
    [Tooltip("Color del popup de headshot")]
    [SerializeField] private Color popupHeadshotColor = new Color(1f, 0.2f, 0.2f, 1f); // Rojo

    [Header("=== RACHA ===")]
    [Tooltip("Color de la racha")]
    [SerializeField] private Color streakColor = new Color(1f, 0.6f, 0f, 1f); // Naranja

    // Estado interno
    private int targetCoins = 0;
    private float displayedCoinsFloat = 0f;
    private int lastDisplayedCoins = 0;
    private Vector3 originalScale;
    private bool isScaleAnimating = false;
    private float scaleAnimTimer = 0f;
    private bool isGain = true;

    // Popup state
    private float popupTimer = 0f;
    private bool isPopupActive = false;

    // Streak state
    private float streakFadeTimer = 0f;

    void Start()
    {
        // Auto-buscar el TextMeshPro si no está asignado
        if (coinsText == null)
            coinsText = GetComponent<TextMeshProUGUI>();

        if (coinsText == null)
            coinsText = GetComponentInChildren<TextMeshProUGUI>();

        if (coinsText == null)
        {
            Debug.LogError("[CoinsUI] No se encontró TextMeshProUGUI. Asigna la referencia en el Inspector.");
            enabled = false;
            return;
        }

        // Guardar escala original
        originalScale = coinsText.transform.localScale;

        // Ocultar textos opcionales al inicio
        if (earningPopupText != null)
        {
            earningPopupText.gameObject.SetActive(false);
        }
        if (streakText != null)
        {
            streakText.gameObject.SetActive(false);
        }
        if (multiplierText != null)
        {
            multiplierText.gameObject.SetActive(false);
        }

        // Suscribirse al evento de cambio de monedas
        if (PlayerEconomy.Instance != null)
        {
            ConnectToEconomy();
        }
        else
        {
            // Si aún no existe, intentar encontrarlo en el siguiente frame
            Invoke(nameof(TryConnectLater), 0.1f);
        }
    }

    /// <summary>
    /// Conectar a todos los eventos de PlayerEconomy.
    /// </summary>
    private void ConnectToEconomy()
    {
        PlayerEconomy eco = PlayerEconomy.Instance;

        eco.OnCoinsChanged += OnCoinsChanged;
        eco.OnCoinsEarned += OnCoinsEarned;
        eco.OnStreakChanged += OnStreakChanged;
        eco.OnStreakLost += OnStreakLost;
        eco.OnMultiplierChanged += OnMultiplierChanged;

        // Inicializar display
        targetCoins = eco.CurrentCoins;
        displayedCoinsFloat = targetCoins;
        lastDisplayedCoins = targetCoins;
        RefreshCoinsText(targetCoins);
    }

    /// <summary>
    /// Intento tardío de conectar (por si PlayerEconomy se carga después)
    /// </summary>
    private void TryConnectLater()
    {
        if (PlayerEconomy.Instance != null)
        {
            ConnectToEconomy();
        }
        else
        {
            Debug.LogWarning("[CoinsUI] No se encontró PlayerEconomy.Instance. Asegúrate de que el jugador tiene el componente PlayerEconomy.");
        }
    }

    void Update()
    {
        // === Conteo animado (smooth number transition) ===
        if (animateCount && Mathf.Abs(displayedCoinsFloat - targetCoins) > 0.5f)
        {
            displayedCoinsFloat = Mathf.Lerp(displayedCoinsFloat, targetCoins, Time.deltaTime * countSpeed);

            int displayInt = Mathf.RoundToInt(displayedCoinsFloat);
            if (displayInt != lastDisplayedCoins)
            {
                lastDisplayedCoins = displayInt;
                RefreshCoinsText(displayInt);
            }
        }
        else if (animateCount && lastDisplayedCoins != targetCoins)
        {
            // Snap final
            displayedCoinsFloat = targetCoins;
            lastDisplayedCoins = targetCoins;
            RefreshCoinsText(targetCoins);
        }

        // === Animación de punch scale ===
        if (isScaleAnimating)
        {
            scaleAnimTimer += Time.deltaTime;
            float t = scaleAnimTimer / animDuration;

            if (t >= 1f)
            {
                coinsText.transform.localScale = originalScale;
                coinsText.color = normalColor;
                isScaleAnimating = false;
            }
            else
            {
                // Curva spring: scale up rápido → oscillación → settle
                float spring = 1f + (punchScale - 1f) * Mathf.Exp(-3f * t) * Mathf.Cos(t * Mathf.PI * 4f);
                coinsText.transform.localScale = originalScale * Mathf.Max(spring, 0.9f);

                // Fade de color
                Color targetColor = isGain ? gainColor : loseColor;
                coinsText.color = Color.Lerp(targetColor, normalColor, t * t); // ease-in
            }
        }

        // === Popup fade out ===
        if (isPopupActive && earningPopupText != null)
        {
            popupTimer += Time.deltaTime;
            float t = popupTimer / popupDuration;

            if (t >= 1f)
            {
                earningPopupText.gameObject.SetActive(false);
                isPopupActive = false;
            }
            else
            {
                // Subir ligeramente + fade out
                Color c = earningPopupText.color;
                c.a = 1f - t * t; // ease-in fade
                earningPopupText.color = c;

                // Mover hacia arriba
                Vector3 pos = earningPopupText.rectTransform.anchoredPosition;
                pos.y += Time.deltaTime * 20f;
                earningPopupText.rectTransform.anchoredPosition = pos;
            }
        }

        // === Streak fade ===
        if (streakText != null && streakText.gameObject.activeSelf)
        {
            streakFadeTimer += Time.deltaTime;
            // La racha se oculta automáticamente cuando PlayerEconomy resetea la streak
        }
    }

    // =========================================================================
    // CALLBACKS DE PlayerEconomy
    // =========================================================================

    /// <summary>Cuando cambian las monedas (valor total).</summary>
    private void OnCoinsChanged(int newAmount)
    {
        bool gained = newAmount > targetCoins;
        bool shouldAnimate = animateOnChange && targetCoins > 0;

        targetCoins = newAmount;

        if (!animateCount)
        {
            displayedCoinsFloat = newAmount;
            lastDisplayedCoins = newAmount;
            RefreshCoinsText(newAmount);
        }

        // Trigger punch animation
        if (shouldAnimate)
        {
            isScaleAnimating = true;
            scaleAnimTimer = 0f;
            isGain = gained;
        }
    }

    /// <summary>Cuando se ganan monedas con detalle (para popup).</summary>
    private void OnCoinsEarned(int amount, int total, string source)
    {
        if (earningPopupText == null) return;

        // Mostrar popup "+50 Headshot!"
        earningPopupText.gameObject.SetActive(true);
        earningPopupText.text = $"+{amount}";
        if (!string.IsNullOrEmpty(source) && source != "Generic")
            earningPopupText.text += $" {source}";

        // Color según la fuente
        Color popupColor = popupGainColor;
        if (source.Contains("Headshot"))
            popupColor = popupHeadshotColor;
        else if (source.Contains("Melee"))
            popupColor = new Color(0.6f, 0.4f, 1f, 1f); // Morado

        earningPopupText.color = popupColor;

        // Reset posición y timer
        earningPopupText.rectTransform.anchoredPosition = Vector2.zero;
        popupTimer = 0f;
        isPopupActive = true;
    }

    /// <summary>Cuando la racha de kills cambia.</summary>
    private void OnStreakChanged(int streak)
    {
        if (streakText == null) return;

        if (streak >= 2)
        {
            streakText.gameObject.SetActive(true);
            streakText.text = $"x{streak} RACHA!";
            streakText.color = streakColor;
            streakFadeTimer = 0f;

            // Escalar cuando sube
            streakText.transform.localScale = Vector3.one * 1.3f;
        }
    }

    /// <summary>Cuando se pierde la racha.</summary>
    private void OnStreakLost(int lastStreak)
    {
        if (streakText == null) return;
        streakText.gameObject.SetActive(false);
    }

    /// <summary>Cuando el multiplicador cambia.</summary>
    private void OnMultiplierChanged(float multiplier)
    {
        if (multiplierText == null) return;

        if (multiplier > 1.01f)
        {
            multiplierText.gameObject.SetActive(true);
            multiplierText.text = $"x{multiplier:F1} MONEDAS";
            multiplierText.color = new Color(1f, 0.85f, 0f, 1f); // Dorado
        }
        else
        {
            multiplierText.gameObject.SetActive(false);
        }
    }

    // =========================================================================
    // UTILIDADES
    // =========================================================================

    /// <summary>Actualiza solo el texto formateado.</summary>
    private void RefreshCoinsText(int amount)
    {
        if (coinsText == null) return;
        string formattedNumber = amount.ToString(numberFormat);
        coinsText.text = $"{prefix}{formattedNumber}{suffix}";
    }

    /// <summary>
    /// Desuscribirse al destruir
    /// </summary>
    void OnDestroy()
    {
        if (PlayerEconomy.Instance != null)
        {
            PlayerEconomy.Instance.OnCoinsChanged -= OnCoinsChanged;
            PlayerEconomy.Instance.OnCoinsEarned -= OnCoinsEarned;
            PlayerEconomy.Instance.OnStreakChanged -= OnStreakChanged;
            PlayerEconomy.Instance.OnStreakLost -= OnStreakLost;
            PlayerEconomy.Instance.OnMultiplierChanged -= OnMultiplierChanged;
        }
    }

    // =========================================================================
    // API PÚBLICA
    // =========================================================================

    /// <summary>Cambia el prefijo del texto.</summary>
    public void SetPrefix(string newPrefix)
    {
        prefix = newPrefix;
        RefreshCoinsText(lastDisplayedCoins);
    }

    /// <summary>Fuerza una actualización manual.</summary>
    public void ForceUpdate()
    {
        if (PlayerEconomy.Instance != null)
        {
            targetCoins = PlayerEconomy.Instance.CurrentCoins;
            displayedCoinsFloat = targetCoins;
            lastDisplayedCoins = targetCoins;
            RefreshCoinsText(targetCoins);
        }
    }
}
