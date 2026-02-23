using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Overlay de sangre en pantalla cuando el jugador tiene poca vida.
/// Muestra manchas estáticas que se intensifican cuanto menos vida queda.
/// También flashea brevemente al recibir daño.
/// 
/// Auto-crea toda la UI necesaria (plug & play).
/// SETUP: Añadir al mismo GameObject que PlayerHealth.
/// </summary>
public class ScreenBloodOverlay : MonoBehaviour
{
    // =========================================================================
    // CONFIGURACIÓN
    // =========================================================================

    [Header("=== UMBRAL DE ACTIVACIÓN ===")]
    [Tooltip("Porcentaje de vida debajo del cual aparece la sangre (0-1)")]
    [SerializeField, Range(0f, 1f)] private float activationThreshold = 0.4f;

    [Tooltip("Porcentaje de vida para máxima intensidad")]
    [SerializeField, Range(0f, 1f)] private float criticalThreshold = 0.15f;

    [Header("=== OVERLAY PERSISTENTE (vida baja) ===")]
    [Tooltip("Color del overlay de sangre")]
    [SerializeField] private Color bloodColor = new Color(0.5f, 0f, 0f, 0.4f);

    [Tooltip("Alpha máxima del overlay en estado crítico")]
    [SerializeField, Range(0f, 1f)] private float maxOverlayAlpha = 0.5f;

    [Tooltip("Velocidad del pulso cuando está en estado crítico")]
    [SerializeField] private float criticalPulseSpeed = 2f;

    [Tooltip("Intensidad del pulso (variación de alpha)")]
    [SerializeField, Range(0f, 0.3f)] private float pulseIntensity = 0.15f;

    [Header("=== FLASH DE DAÑO ===")]
    [Tooltip("Alpha del flash al recibir un golpe")]
    [SerializeField, Range(0f, 1f)] private float damageFlashAlpha = 0.35f;

    [Tooltip("Duración del flash de daño")]
    [SerializeField] private float damageFlashDuration = 0.3f;

    [Header("=== VIGNETTE DE SANGRE ===")]
    [Tooltip("¿Usar vignette (bordes oscuros) en vez de overlay plano?")]
    [SerializeField] private bool useVignette = true;

    // =========================================================================
    // INTERNOS
    // =========================================================================

    private Canvas bloodCanvas;
    private Image overlayImage;          // Overlay persistente
    private Image flashImage;            // Flash de impacto
    private CanvasGroup overlayGroup;
    private CanvasGroup flashGroup;

    private PlayerHealth playerHealth;
    private float targetOverlayAlpha = 0f;
    private float currentOverlayAlpha = 0f;
    private bool isInitialized = false;
    private Coroutine flashCoroutine;

    // =========================================================================
    // LIFECYCLE
    // =========================================================================

    void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        Initialize();
    }

    void Start()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged.AddListener(OnHealthChanged);
            playerHealth.OnPlayerDamaged.AddListener(OnDamaged);
        }
        else
        {
            Debug.LogWarning("[ScreenBloodOverlay] No se encontró PlayerHealth en este GameObject.");
        }
    }

    void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged.RemoveListener(OnHealthChanged);
            playerHealth.OnPlayerDamaged.RemoveListener(OnDamaged);
        }
    }

    void Update()
    {
        if (!isInitialized || playerHealth == null) return;

        // Suavizar transición del overlay
        currentOverlayAlpha = Mathf.Lerp(currentOverlayAlpha, targetOverlayAlpha, Time.deltaTime * 5f);

        // Pulso en estado crítico
        float pulseOffset = 0f;
        if (playerHealth.GetHealthPercentage() <= criticalThreshold && playerHealth.IsAlive())
        {
            pulseOffset = Mathf.Sin(Time.time * criticalPulseSpeed * Mathf.PI * 2f) * pulseIntensity;
        }

        float finalAlpha = Mathf.Clamp01(currentOverlayAlpha + pulseOffset);
        overlayGroup.alpha = finalAlpha;
    }

    // =========================================================================
    // CALLBACKS
    // =========================================================================

    private void OnHealthChanged(int current, int max)
    {
        if (max <= 0) return;

        float healthPct = (float)current / max;

        if (healthPct >= activationThreshold || !playerHealth.IsAlive())
        {
            // Vida suficiente → sin overlay
            targetOverlayAlpha = 0f;
        }
        else
        {
            // Mapear: activationThreshold → 0 alpha, criticalThreshold → maxOverlayAlpha
            float range = activationThreshold - criticalThreshold;
            float normalized = Mathf.Clamp01((activationThreshold - healthPct) / range);
            targetOverlayAlpha = normalized * maxOverlayAlpha;
        }
    }

    private void OnDamaged()
    {
        if (!isInitialized) return;

        // Flash rápido de sangre al recibir daño
        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        flashCoroutine = StartCoroutine(DamageFlashCoroutine());
    }

    // =========================================================================
    // ANIMACIONES
    // =========================================================================

    private IEnumerator DamageFlashCoroutine()
    {
        flashGroup.alpha = damageFlashAlpha;

        float elapsed = 0f;
        while (elapsed < damageFlashDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / damageFlashDuration;
            // Ease out cuadrático
            flashGroup.alpha = Mathf.Lerp(damageFlashAlpha, 0f, t * t);
            yield return null;
        }

        flashGroup.alpha = 0f;
        flashCoroutine = null;
    }

    // =========================================================================
    // SETUP
    // =========================================================================

    private void Initialize()
    {
        if (isInitialized) return;

        // Canvas overlay
        GameObject canvasGO = new GameObject("ScreenBloodCanvas");
        canvasGO.transform.SetParent(transform);

        bloodCanvas = canvasGO.AddComponent<Canvas>();
        bloodCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        bloodCanvas.sortingOrder = 90; // Debajo del HUD pero encima del juego

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        // === OVERLAY PERSISTENTE (vida baja) ===
        overlayImage = CreateFullScreenImage(canvasGO.transform, "BloodOverlay");
        overlayGroup = overlayImage.gameObject.AddComponent<CanvasGroup>();
        overlayGroup.alpha = 0f;
        overlayGroup.blocksRaycasts = false;
        overlayGroup.interactable = false;

        if (useVignette)
        {
            overlayImage.sprite = CreateVignetteSprite();
            overlayImage.type = Image.Type.Simple;
            overlayImage.color = bloodColor;
        }
        else
        {
            overlayImage.color = bloodColor;
        }

        // === FLASH DE DAÑO ===
        flashImage = CreateFullScreenImage(canvasGO.transform, "BloodFlash");
        flashGroup = flashImage.gameObject.AddComponent<CanvasGroup>();
        flashGroup.alpha = 0f;
        flashGroup.blocksRaycasts = false;
        flashGroup.interactable = false;

        if (useVignette)
        {
            flashImage.sprite = CreateVignetteSprite();
            flashImage.type = Image.Type.Simple;
            flashImage.color = new Color(0.6f, 0f, 0f, 1f);
        }
        else
        {
            flashImage.color = new Color(0.6f, 0f, 0f, 0.5f);
        }

        isInitialized = true;
    }

    private Image CreateFullScreenImage(Transform parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = go.AddComponent<Image>();
        img.raycastTarget = false;
        return img;
    }

    /// <summary>
    /// Crea un sprite de vignette procedural (bordes oscuros, centro transparente).
    /// Similar al efecto de sangre en COD cuando tienes poca vida.
    /// </summary>
    private Sprite CreateVignetteSprite()
    {
        int size = 256;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[size * size];
        float center = size * 0.5f;
        float maxDist = center * 1.1f; // Ligeramente mayor para que los bordes sean sólidos

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - center) / center;
                float dy = (y - center) / center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                // Vignette: transparente en el centro, opaco en los bordes
                // Curva suave con più intensidad en las esquinas
                float alpha = Mathf.Clamp01(Mathf.Pow(dist, 2.2f));

                // Añadir irregularidad (más orgánico, como sangre)
                float noise = Mathf.PerlinNoise(x * 0.05f, y * 0.05f);
                alpha *= Mathf.Lerp(0.7f, 1.3f, noise);
                alpha = Mathf.Clamp01(alpha);

                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    // =========================================================================
    // API PÚBLICA
    // =========================================================================

    /// <summary>
    /// Fuerza un flash de sangre (útil para efectos especiales).
    /// </summary>
    public void ForceFlash(float alpha = -1f)
    {
        if (!isInitialized) Initialize();

        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        if (alpha > 0f)
        {
            float savedAlpha = damageFlashAlpha;
            damageFlashAlpha = alpha;
            flashCoroutine = StartCoroutine(DamageFlashCoroutine());
            damageFlashAlpha = savedAlpha;
        }
        else
        {
            flashCoroutine = StartCoroutine(DamageFlashCoroutine());
        }
    }
}
