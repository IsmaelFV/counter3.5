using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Hitmarker estilo COD: muestra una cruz en el centro de pantalla
/// al impactar a un enemigo. Cruz roja para headshot/kill.
/// Auto-crea toda la UI necesaria (plug & play).
/// 
/// SETUP: Añadir al mismo GameObject que WeaponManager.
/// </summary>
public class HitmarkerUI : MonoBehaviour
{
    // =========================================================================
    // CONFIGURACIÓN
    // =========================================================================

    [Header("=== APARIENCIA ===")]
    [Tooltip("Color normal del hitmarker")]
    [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.9f);

    [Tooltip("Color de headshot/kill")]
    [SerializeField] private Color headshotColor = new Color(1f, 0.15f, 0.15f, 1f);

    [Tooltip("Tamaño del hitmarker en píxeles")]
    [SerializeField] private float markerSize = 24f;

    [Tooltip("Grosor de las líneas de la cruz")]
    [SerializeField] private float lineThickness = 3f;

    [Tooltip("Espacio central vacío (gap entre las 4 líneas)")]
    [SerializeField] private float centerGap = 6f;

    [Header("=== ANIMACIÓN ===")]
    [Tooltip("Duración de la aparición")]
    [SerializeField] private float displayDuration = 0.15f;

    [Tooltip("Duración del fade out")]
    [SerializeField] private float fadeOutDuration = 0.12f;

    [Tooltip("Escala inicial (punch-in)")]
    [SerializeField] private float punchScale = 1.3f;

    [Header("=== SONIDO (Opcional) ===")]
    [Tooltip("Sonido al impactar enemigo")]
    [SerializeField] private AudioClip hitmarkerSound;

    [Tooltip("Sonido al headshot")]
    [SerializeField] private AudioClip headshotSound;

    [SerializeField, Range(0f, 1f)] private float soundVolume = 0.3f;

    // =========================================================================
    // INTERNOS
    // =========================================================================

    private Canvas hitmarkerCanvas;
    private CanvasGroup markerGroup;
    private RectTransform markerRect;
    private Image[] lineImages = new Image[4]; // Top, Bottom, Left, Right
    private AudioSource audioSource;
    private Coroutine currentAnimation;
    private bool isInitialized = false;

    // =========================================================================
    // LIFECYCLE
    // =========================================================================

    void Awake()
    {
        Initialize();
    }

    void Start()
    {
        // Buscar AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = GetComponentInParent<AudioSource>();
    }

    // =========================================================================
    // API PÚBLICA
    // =========================================================================

    /// <summary>
    /// Muestra el hitmarker. Llamar al impactar un enemigo.
    /// </summary>
    /// <param name="isHeadshot">True = rojo grande, False = blanco normal</param>
    public void ShowHitmarker(bool isHeadshot = false)
    {
        if (!isInitialized) Initialize();

        if (currentAnimation != null)
            StopCoroutine(currentAnimation);

        currentAnimation = StartCoroutine(HitmarkerAnimation(isHeadshot));
    }

    // =========================================================================
    // ANIMACIÓN
    // =========================================================================

    private IEnumerator HitmarkerAnimation(bool isHeadshot)
    {
        // Configurar color
        Color color = isHeadshot ? headshotColor : normalColor;
        for (int i = 0; i < lineImages.Length; i++)
        {
            if (lineImages[i] != null)
                lineImages[i].color = color;
        }

        // Sonido
        if (audioSource != null)
        {
            AudioClip clip = isHeadshot ? (headshotSound ?? hitmarkerSound) : hitmarkerSound;
            if (clip != null)
                audioSource.PlayOneShot(clip, soundVolume);
        }

        // Punch-in
        float punchSize = isHeadshot ? punchScale * 1.2f : punchScale;
        markerRect.localScale = Vector3.one * punchSize;
        markerGroup.alpha = 1f;

        // Shrink back + hold
        float elapsed = 0f;
        while (elapsed < displayDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / displayDuration;
            float scale = Mathf.Lerp(punchSize, 1f, t * t); // Ease out
            markerRect.localScale = Vector3.one * scale;
            yield return null;
        }

        markerRect.localScale = Vector3.one;

        // Fade out
        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeOutDuration;
            markerGroup.alpha = Mathf.Lerp(1f, 0f, t);
            yield return null;
        }

        markerGroup.alpha = 0f;
        currentAnimation = null;
    }

    // =========================================================================
    // SETUP
    // =========================================================================

    private void Initialize()
    {
        if (isInitialized) return;

        // Canvas overlay
        GameObject canvasGO = new GameObject("HitmarkerCanvas");
        canvasGO.transform.SetParent(transform);

        hitmarkerCanvas = canvasGO.AddComponent<Canvas>();
        hitmarkerCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        hitmarkerCanvas.sortingOrder = 110; // Encima de TODO

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // Container centrado
        GameObject container = new GameObject("Hitmarker");
        container.transform.SetParent(canvasGO.transform, false);

        markerRect = container.AddComponent<RectTransform>();
        markerRect.anchorMin = new Vector2(0.5f, 0.5f);
        markerRect.anchorMax = new Vector2(0.5f, 0.5f);
        markerRect.sizeDelta = new Vector2(markerSize * 2f, markerSize * 2f);
        markerRect.anchoredPosition = Vector2.zero;

        markerGroup = container.AddComponent<CanvasGroup>();
        markerGroup.alpha = 0f;
        markerGroup.blocksRaycasts = false;
        markerGroup.interactable = false;

        // Crear 4 líneas de la cruz (Top, Bottom, Left, Right)
        // 45° rotadas → forman una X
        CreateLine(container.transform, 0, new Vector2(0, centerGap), new Vector2(lineThickness, markerSize - centerGap), 45f);   // Top-Right
        CreateLine(container.transform, 1, new Vector2(0, -centerGap), new Vector2(lineThickness, markerSize - centerGap), 45f);  // Bottom-Left
        CreateLine(container.transform, 2, new Vector2(centerGap, 0), new Vector2(lineThickness, markerSize - centerGap), -45f);  // Right
        CreateLine(container.transform, 3, new Vector2(-centerGap, 0), new Vector2(lineThickness, markerSize - centerGap), -45f); // Left

        isInitialized = true;
    }

    private void CreateLine(Transform parent, int index, Vector2 offset, Vector2 size, float angle)
    {
        GameObject lineGO = new GameObject($"Line_{index}");
        lineGO.transform.SetParent(parent, false);

        RectTransform rt = lineGO.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = offset;
        rt.sizeDelta = size;
        rt.localRotation = Quaternion.Euler(0, 0, angle);

        Image img = lineGO.AddComponent<Image>();
        img.color = normalColor;
        img.raycastTarget = false;

        lineImages[index] = img;
    }
}
