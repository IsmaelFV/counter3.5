using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// UI de combo/racha de kills. Muestra "x2 COMBO", "x3 COMBO", etc.
/// Se conecta automáticamente a PlayerEconomy (que ya tiene el sistema de rachas).
/// Incluye animación de punch-in, shake y colores progresivos.
/// 
/// Auto-crea toda la UI necesaria (plug & play).
/// SETUP: Añadir al mismo GameObject que PlayerEconomy (el jugador).
/// </summary>
public class ComboUI : MonoBehaviour
{
    // =========================================================================
    // CONFIGURACIÓN
    // =========================================================================

    [Header("=== POSICIÓN ===")]
    [Tooltip("Posición del texto de combo (normalizada 0-1)")]
    [SerializeField] private Vector2 screenPosition = new Vector2(0.5f, 0.72f);

    [Header("=== COLORES POR NIVEL ===")]
    [SerializeField] private Color comboColor2 = new Color(1f, 1f, 1f, 1f);       // x2: Blanco
    [SerializeField] private Color comboColor3 = new Color(1f, 0.9f, 0.3f, 1f);   // x3: Amarillo
    [SerializeField] private Color comboColor5 = new Color(1f, 0.5f, 0f, 1f);     // x5: Naranja
    [SerializeField] private Color comboColor8 = new Color(1f, 0.15f, 0.15f, 1f); // x8: Rojo
    [SerializeField] private Color comboColor12 = new Color(0.8f, 0f, 1f, 1f);    // x12+: Púrpura

    [Header("=== TAMAÑO ===")]
    [SerializeField] private float baseFontSize = 36f;
    [Tooltip("Cuánto crece la fuente por cada nivel de combo")]
    [SerializeField] private float fontSizePerLevel = 1.5f;
    [SerializeField] private float maxFontSize = 60f;

    [Header("=== ANIMACIÓN ===")]
    [Tooltip("Escala punch-in al subir el combo")]
    [SerializeField] private float punchScale = 1.6f;
    [Tooltip("Duración del punch")]
    [SerializeField] private float punchDuration = 0.15f;
    [Tooltip("Shake al perder el combo")]
    [SerializeField] private float lostShakeIntensity = 10f;
    [Tooltip("Duración del fade-out al perder combo")]
    [SerializeField] private float fadeOutDuration = 0.5f;

    [Header("=== TEXTO ===")]
    [Tooltip("Formato del texto. {0} = número de combo")]
    [SerializeField] private string comboFormat = "x{0} COMBO";
    [Tooltip("¿Mostrar bonus de monedas debajo?")]
    [SerializeField] private bool showBonusText = true;

    [Header("=== FUENTE (Opcional) ===")]
    [SerializeField] private TMP_FontAsset customFont;

    // =========================================================================
    // INTERNOS
    // =========================================================================

    private Canvas comboCanvas;
    private TextMeshProUGUI comboText;
    private TextMeshProUGUI bonusText;
    private CanvasGroup canvasGroup;
    private RectTransform comboRect;
    private PlayerEconomy economy;

    private int currentCombo = 0;
    private Coroutine punchCoroutine;
    private Coroutine fadeCoroutine;
    private bool isInitialized = false;

    // =========================================================================
    // LIFECYCLE
    // =========================================================================

    void Awake()
    {
        economy = GetComponent<PlayerEconomy>();
        Initialize();
    }

    void Start()
    {
        if (economy != null)
        {
            economy.OnStreakChanged += OnStreakChanged;
            economy.OnStreakLost += OnStreakLost;
        }
        else
        {
            Debug.LogWarning("[ComboUI] No se encontró PlayerEconomy en este GameObject.");
        }
    }

    void OnDestroy()
    {
        if (economy != null)
        {
            economy.OnStreakChanged -= OnStreakChanged;
            economy.OnStreakLost -= OnStreakLost;
        }
    }

    // =========================================================================
    // CALLBACKS
    // =========================================================================

    private void OnStreakChanged(int streak)
    {
        // Solo mostrar desde x2
        if (streak < 2)
        {
            currentCombo = streak;
            return;
        }

        currentCombo = streak;

        // Cancelar fade si está activo
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        // Actualizar texto
        comboText.text = string.Format(comboFormat, streak);

        // Texto de bonus
        if (showBonusText && bonusText != null)
        {
            int bonus = (streak - 1) * 10; // streakBonusPerKill default = 10
            bonusText.text = $"+{bonus} bonus";
            bonusText.color = new Color(1f, 1f, 1f, 0.6f);
        }

        // Color según nivel
        Color color = GetComboColor(streak);
        comboText.color = color;

        // Tamaño creciente
        float fontSize = Mathf.Min(baseFontSize + (streak - 2) * fontSizePerLevel, maxFontSize);
        comboText.fontSize = fontSize;

        // Outline
        comboText.outlineWidth = 0.25f;
        comboText.outlineColor = new Color32(0, 0, 0, 200);

        // Mostrar
        canvasGroup.alpha = 1f;

        // Animación punch
        if (punchCoroutine != null)
            StopCoroutine(punchCoroutine);
        punchCoroutine = StartCoroutine(PunchAnimation());
    }

    private void OnStreakLost(int lastStreak)
    {
        if (lastStreak < 2)
        {
            currentCombo = 0;
            return;
        }

        currentCombo = 0;

        // Fade out con shake
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);
        if (punchCoroutine != null)
            StopCoroutine(punchCoroutine);

        fadeCoroutine = StartCoroutine(LostComboAnimation());
    }

    // =========================================================================
    // ANIMACIONES
    // =========================================================================

    private IEnumerator PunchAnimation()
    {
        float punchForStreak = punchScale + (currentCombo - 2) * 0.05f; // Más punch con más combo
        punchForStreak = Mathf.Min(punchForStreak, 2f);

        comboRect.localScale = Vector3.one * punchForStreak;

        float elapsed = 0f;
        while (elapsed < punchDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / punchDuration;
            // Ease out back (ligero overshoot)
            float eased = 1f + (t - 1f) * (t - 1f) * (t - 1f);
            float scale = Mathf.Lerp(punchForStreak, 1f, eased);
            comboRect.localScale = Vector3.one * scale;
            yield return null;
        }

        comboRect.localScale = Vector3.one;
        punchCoroutine = null;
    }

    private IEnumerator LostComboAnimation()
    {
        // Shake breve
        float shakeTime = 0.2f;
        float shakeElapsed = 0f;
        Vector2 originalPos = comboRect.anchoredPosition;

        while (shakeElapsed < shakeTime)
        {
            shakeElapsed += Time.deltaTime;
            float intensity = lostShakeIntensity * (1f - shakeElapsed / shakeTime);
            Vector2 offset = new Vector2(
                Random.Range(-intensity, intensity),
                Random.Range(-intensity, intensity)
            );
            comboRect.anchoredPosition = originalPos + offset;
            yield return null;
        }
        comboRect.anchoredPosition = originalPos;

        // Cambiar texto a "COMBO PERDIDO"
        comboText.text = "COMBO LOST";
        comboText.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        if (bonusText != null)
            bonusText.text = "";

        // Fade out
        float fadeElapsed = 0f;
        while (fadeElapsed < fadeOutDuration)
        {
            fadeElapsed += Time.deltaTime;
            float t = fadeElapsed / fadeOutDuration;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        fadeCoroutine = null;
    }

    // =========================================================================
    // HELPERS
    // =========================================================================

    private Color GetComboColor(int streak)
    {
        if (streak >= 12) return comboColor12;
        if (streak >= 8) return comboColor8;
        if (streak >= 5) return comboColor5;
        if (streak >= 3) return comboColor3;
        return comboColor2;
    }

    // =========================================================================
    // SETUP
    // =========================================================================

    private void Initialize()
    {
        if (isInitialized) return;

        // Canvas
        GameObject canvasGO = new GameObject("ComboCanvas");
        canvasGO.transform.SetParent(transform);

        comboCanvas = canvasGO.AddComponent<Canvas>();
        comboCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        comboCanvas.sortingOrder = 105;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // Container
        GameObject container = new GameObject("ComboContainer");
        container.transform.SetParent(canvasGO.transform, false);

        comboRect = container.AddComponent<RectTransform>();
        comboRect.anchorMin = screenPosition;
        comboRect.anchorMax = screenPosition;
        comboRect.anchoredPosition = Vector2.zero;
        comboRect.sizeDelta = new Vector2(400f, 100f);

        canvasGroup = container.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        // Texto principal
        GameObject textGO = new GameObject("ComboText");
        textGO.transform.SetParent(container.transform, false);

        RectTransform textRT = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        comboText = textGO.AddComponent<TextMeshProUGUI>();
        comboText.alignment = TextAlignmentOptions.Center;
        comboText.fontSize = baseFontSize;
        comboText.fontStyle = FontStyles.Bold;
        comboText.enableWordWrapping = false;
        comboText.raycastTarget = false;
        if (customFont != null) comboText.font = customFont;

        // Texto de bonus (debajo)
        if (showBonusText)
        {
            GameObject bonusGO = new GameObject("BonusText");
            bonusGO.transform.SetParent(container.transform, false);

            RectTransform bonusRT = bonusGO.AddComponent<RectTransform>();
            bonusRT.anchorMin = new Vector2(0f, 0f);
            bonusRT.anchorMax = new Vector2(1f, 0f);
            bonusRT.anchoredPosition = new Vector2(0f, -25f);
            bonusRT.sizeDelta = new Vector2(400f, 30f);

            bonusText = bonusGO.AddComponent<TextMeshProUGUI>();
            bonusText.alignment = TextAlignmentOptions.Center;
            bonusText.fontSize = 18f;
            bonusText.fontStyle = FontStyles.Italic;
            bonusText.enableWordWrapping = false;
            bonusText.raycastTarget = false;
            if (customFont != null) bonusText.font = customFont;
        }

        isInitialized = true;
    }
}
