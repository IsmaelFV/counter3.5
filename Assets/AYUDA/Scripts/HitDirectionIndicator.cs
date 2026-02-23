using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Sistema de indicadores direccionales de daño.
/// Muestra flechas/arcos rojos en los bordes de la pantalla indicando
/// la dirección desde la que el jugador recibió daño.
/// 
/// Auto-crea toda la UI necesaria si no existe (plug & play).
/// Se integra con PlayerHealth mediante el método ShowDamageDirection().
/// 
/// USO:
///   hitDirectionIndicator.ShowDamageDirection(damageSourcePosition);
/// </summary>
public class HitDirectionIndicator : MonoBehaviour
{
    // =========================================================================
    // CONFIGURACIÓN
    // =========================================================================

    [Header("=== APARIENCIA ===")]
    [Tooltip("Color del indicador de daño")]
    [SerializeField] private Color indicatorColor = new Color(0.8f, 0f, 0f, 0.9f);

    [Tooltip("Sprite para el indicador (si está vacío, se genera un arco procedural)")]
    [SerializeField] private Sprite indicatorSprite;

    [Tooltip("Distancia del centro de pantalla al indicador (px a 1080p)")]
    [SerializeField] private float indicatorDistance = 200f;

    [Tooltip("Tamaño del indicador en píxeles")]
    [SerializeField] private Vector2 indicatorSize = new Vector2(40f, 120f);

    [Header("=== TIMING ===")]
    [Tooltip("Duración total del indicador (segundos)")]
    [SerializeField] private float indicatorDuration = 1.0f;

    [Tooltip("Tiempo de fade-in rápido (segundos)")]
    [SerializeField] private float fadeInTime = 0.05f;

    [Tooltip("Tiempo antes de empezar el fade-out (segundos)")]
    [SerializeField] private float holdTime = 0.4f;

    [Header("=== COMPORTAMIENTO ===")]
    [Tooltip("Máximo de indicadores simultáneos")]
    [SerializeField] private int maxIndicators = 8;

    [Tooltip("¿Los indicadores siguen la rotación del jugador en tiempo real?")]
    [SerializeField] private bool trackPlayerRotation = true;

    [Tooltip("Ángulo mínimo entre indicadores para agruparlos (grados)")]
    [SerializeField] private float mergeAngleThreshold = 30f;

    // =========================================================================
    // INTERNOS
    // =========================================================================

    private Canvas indicatorCanvas;
    private RectTransform canvasRect;
    private List<DirectionIndicator> activeIndicators = new List<DirectionIndicator>();
    private List<DirectionIndicator> pool = new List<DirectionIndicator>();
    private Transform playerTransform;
    private bool isInitialized = false;

    // =========================================================================
    // CLASE INTERNA — datos de cada indicador activo
    // =========================================================================

    private class DirectionIndicator
    {
        public GameObject gameObject;
        public RectTransform rectTransform;
        public Image image;
        public CanvasGroup canvasGroup;

        public Vector3 damageSourceWorld;  // Posición mundo del atacante
        public float spawnTime;
        public float duration;
        public float fadeInTime;
        public float holdTime;
        public bool isActive;
    }

    // =========================================================================
    // LIFECYCLE
    // =========================================================================

    void Awake()
    {
        playerTransform = transform;
        Initialize();
    }

    void Start()
    {
        // Asegurar que tenemos la referencia al jugador
        if (playerTransform == null)
            playerTransform = transform;
    }

    void LateUpdate()
    {
        if (!isInitialized) return;

        for (int i = activeIndicators.Count - 1; i >= 0; i--)
        {
            DirectionIndicator ind = activeIndicators[i];
            if (!ind.isActive) continue;

            float elapsed = Time.time - ind.spawnTime;

            // ¿Expiró?
            if (elapsed >= ind.duration)
            {
                DeactivateIndicator(ind);
                activeIndicators.RemoveAt(i);
                continue;
            }

            // Calcular alpha
            float alpha = CalculateAlpha(elapsed, ind);
            ind.canvasGroup.alpha = alpha;

            // Actualizar rotación si trackPlayerRotation está activo
            if (trackPlayerRotation)
            {
                UpdateIndicatorRotation(ind);
            }
        }
    }

    // =========================================================================
    // API PÚBLICA
    // =========================================================================

    /// <summary>
    /// Muestra un indicador de daño apuntando hacia la fuente de daño.
    /// Llamar desde PlayerHealth.TakeDamage cuando se conoce la posición del atacante.
    /// </summary>
    /// <param name="damageSourcePosition">Posición en el mundo del objeto que causó daño</param>
    public void ShowDamageDirection(Vector3 damageSourcePosition)
    {
        if (!isInitialized) Initialize();

        // Verificar si ya hay un indicador cercano para fusionar
        for (int i = 0; i < activeIndicators.Count; i++)
        {
            DirectionIndicator existing = activeIndicators[i];
            if (!existing.isActive) continue;

            float angleBetween = Vector3.Angle(
                GetDirectionToSource(existing.damageSourceWorld),
                GetDirectionToSource(damageSourcePosition)
            );

            if (angleBetween < mergeAngleThreshold)
            {
                // Refrescar el indicador existente en vez de crear uno nuevo
                existing.spawnTime = Time.time;
                existing.damageSourceWorld = damageSourcePosition;

                // Flash: poner alpha al máximo brevemente
                existing.canvasGroup.alpha = 1f;
                return;
            }
        }

        // Crear nuevo indicador
        SpawnIndicator(damageSourcePosition);
    }

    /// <summary>
    /// Limpia todos los indicadores activos.
    /// </summary>
    public void ClearAll()
    {
        for (int i = activeIndicators.Count - 1; i >= 0; i--)
        {
            DeactivateIndicator(activeIndicators[i]);
        }
        activeIndicators.Clear();
    }

    // =========================================================================
    // SPAWNING
    // =========================================================================

    private void SpawnIndicator(Vector3 damageSourceWorld)
    {
        // Limitar indicadores simultáneos
        if (activeIndicators.Count >= maxIndicators)
        {
            // Reciclar el más antiguo
            DirectionIndicator oldest = activeIndicators[0];
            DeactivateIndicator(oldest);
            activeIndicators.RemoveAt(0);
        }

        DirectionIndicator ind = GetFromPool();
        ind.damageSourceWorld = damageSourceWorld;
        ind.spawnTime = Time.time;
        ind.duration = indicatorDuration;
        ind.fadeInTime = fadeInTime;
        ind.holdTime = holdTime;
        ind.isActive = true;

        // Color
        ind.image.color = indicatorColor;
        ind.canvasGroup.alpha = 0f;

        // Posicionar y rotar
        UpdateIndicatorRotation(ind);

        ind.gameObject.SetActive(true);
        activeIndicators.Add(ind);
    }

    // =========================================================================
    // ACTUALIZACIÓN DE POSICIÓN / ROTACIÓN
    // =========================================================================

    /// <summary>
    /// Calcula la dirección 2D al atacante y posiciona/rota el indicador en el borde.
    /// </summary>
    private void UpdateIndicatorRotation(DirectionIndicator ind)
    {
        if (playerTransform == null) return;

        // Dirección al atacante en espacio local del jugador (solo Y)
        Vector3 dirWorld = GetDirectionToSource(ind.damageSourceWorld);

        // Convertir a ángulo respecto al forward del jugador (plano horizontal)
        Vector3 playerForward = playerTransform.forward;
        playerForward.y = 0f;
        playerForward.Normalize();

        Vector3 dirFlat = dirWorld;
        dirFlat.y = 0f;
        dirFlat.Normalize();

        if (dirFlat.sqrMagnitude < 0.001f) return;

        // Ángulo con signo (positivo = derecha, negativo = izquierda)
        float angle = Vector3.SignedAngle(playerForward, dirFlat, Vector3.up);

        // Rotar el indicador en UI space
        // El indicador apunta "arriba" por defecto, así que -angle lo orienta correctamente
        ind.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -angle);

        // Posicionar en el borde circular
        float angleRad = angle * Mathf.Deg2Rad;
        Vector2 offset = new Vector2(Mathf.Sin(angleRad), -Mathf.Cos(angleRad)) * indicatorDistance;
        // Invertimos Cos porque en UI "arriba" es +Y pero nuestro ángulo 0 es "adelante"
        // Corrección: forward del jugador → arriba en pantalla
        offset = new Vector2(Mathf.Sin(angleRad), Mathf.Cos(angleRad)) * indicatorDistance;

        ind.rectTransform.anchoredPosition = offset;
    }

    private Vector3 GetDirectionToSource(Vector3 sourceWorld)
    {
        return (sourceWorld - playerTransform.position).normalized;
    }

    // =========================================================================
    // ALPHA CALCULATION
    // =========================================================================

    private float CalculateAlpha(float elapsed, DirectionIndicator ind)
    {
        // Fase 1: Fade in rápido
        if (elapsed < ind.fadeInTime)
        {
            return Mathf.Lerp(0f, 1f, elapsed / ind.fadeInTime);
        }

        // Fase 2: Hold al máximo
        float holdEnd = ind.fadeInTime + ind.holdTime;
        if (elapsed < holdEnd)
        {
            return 1f;
        }

        // Fase 3: Fade out
        float fadeOutDuration = ind.duration - holdEnd;
        if (fadeOutDuration <= 0f) return 0f;

        float fadeProgress = (elapsed - holdEnd) / fadeOutDuration;
        return Mathf.Lerp(1f, 0f, fadeProgress);
    }

    // =========================================================================
    // OBJECT POOL
    // =========================================================================

    private DirectionIndicator GetFromPool()
    {
        for (int i = 0; i < pool.Count; i++)
        {
            if (!pool[i].isActive)
                return pool[i];
        }

        // Expandir pool
        return CreatePooledIndicator();
    }

    private void DeactivateIndicator(DirectionIndicator ind)
    {
        ind.isActive = false;
        ind.gameObject.SetActive(false);
    }

    private DirectionIndicator CreatePooledIndicator()
    {
        DirectionIndicator ind = new DirectionIndicator();

        GameObject go = new GameObject("HitIndicator");
        go.transform.SetParent(canvasRect, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = indicatorSize;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0f); // Pivot abajo para que rote desde la base

        Image img = go.AddComponent<Image>();
        if (indicatorSprite != null)
        {
            img.sprite = indicatorSprite;
        }
        else
        {
            // Crear sprite procedural de arco/flecha
            img.sprite = CreateArrowSprite();
        }
        img.color = indicatorColor;
        img.raycastTarget = false;

        CanvasGroup cg = go.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.blocksRaycasts = false;
        cg.interactable = false;

        ind.gameObject = go;
        ind.rectTransform = rt;
        ind.image = img;
        ind.canvasGroup = cg;
        ind.isActive = false;

        go.SetActive(false);
        pool.Add(ind);
        return ind;
    }

    // =========================================================================
    // INICIALIZACIÓN
    // =========================================================================

    private void Initialize()
    {
        if (isInitialized) return;

        try
        {
            EnsureCanvas();

            // Pre-crear pool
            for (int i = 0; i < maxIndicators + 2; i++)
            {
                CreatePooledIndicator();
            }

            isInitialized = true;
            Debug.Log("[HitDirectionIndicator] Inicializado.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[HitDirectionIndicator] Error durante la inicialización: {ex.Message}");
            isInitialized = true; // Marcar como inicializado para evitar bucles de error
        }
    }

    private void EnsureCanvas()
    {
        try
        {
            if (indicatorCanvas != null)
            {
                canvasRect = indicatorCanvas.GetComponent<RectTransform>();
                return;
            }

            // Buscar Canvas existente
            GameObject existing = GameObject.Find("HitIndicatorCanvas");
            if (existing != null)
            {
                indicatorCanvas = existing.GetComponent<Canvas>();
                if (indicatorCanvas != null)
                {
                    canvasRect = indicatorCanvas.GetComponent<RectTransform>();
                    return;
                }
            }

            // Crear Canvas overlay dedicado
            GameObject canvasGO = new GameObject("HitIndicatorCanvas");
            canvasGO.transform.SetParent(transform);

            indicatorCanvas = canvasGO.AddComponent<Canvas>();
            indicatorCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            indicatorCanvas.sortingOrder = 99; // Debajo de damage numbers, encima de HUD normal

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();
            canvasRect = indicatorCanvas.GetComponent<RectTransform>();

            Debug.Log("[HitDirectionIndicator] Canvas de indicadores creado automáticamente.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[HitDirectionIndicator] Error al asegurar el Canvas: {ex.Message}");
        }
    }

    // =========================================================================
    // GENERACIÓN PROCEDURAL DE SPRITE
    // =========================================================================

    /// <summary>
    /// Crea un sprite de flecha/arco procedural si no se asigna uno personalizado.
    /// Es un triángulo elongado apuntando hacia arriba (como una flecha de daño).
    /// </summary>
    private Sprite CreateArrowSprite()
    {
        int width = 32;
        int height = 96;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        Color transparent = new Color(0, 0, 0, 0);
        Color white = Color.white;

        // Llenar con transparente
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = transparent;

        // Dibujar forma de arco/flecha (triángulo con gradiente)
        float centerX = width * 0.5f;

        for (int y = 0; y < height; y++)
        {
            // Progreso de abajo (0) a arriba (1)
            float t = (float)y / height;

            // Ancho del arco a esta altura (más ancho abajo, puntiagudo arriba)
            float halfWidth = (1f - t) * (width * 0.45f);

            // Alpha: más intenso arriba (punta), suave abajo
            float alpha = Mathf.Lerp(0.3f, 1f, t * t);

            // Suavizado extra en los extremos inferior
            if (t < 0.1f) alpha *= t / 0.1f;

            for (int x = 0; x < width; x++)
            {
                float distFromCenter = Mathf.Abs(x - centerX);
                if (distFromCenter <= halfWidth)
                {
                    // Suavizado en los bordes laterales
                    float edgeFade = 1f - Mathf.Clamp01((distFromCenter - halfWidth + 2f) / 2f);
                    float finalAlpha = alpha * edgeFade;
                    pixels[y * width + x] = new Color(1f, 1f, 1f, finalAlpha);
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0f), 100f);
    }

    // =========================================================================
    // API DE CONFIGURACIÓN EN RUNTIME
    // =========================================================================

    /// <summary>
    /// Cambia el color del indicador en runtime
    /// </summary>
    public void SetIndicatorColor(Color newColor)
    {
        indicatorColor = newColor;
    }

    /// <summary>
    /// Cambia la duración del indicador en runtime
    /// </summary>
    public void SetDuration(float newDuration)
    {
        indicatorDuration = Mathf.Max(0.1f, newDuration);
    }
}
