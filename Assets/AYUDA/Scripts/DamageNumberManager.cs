using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Manager singleton para números de daño flotantes.
/// Crea y gestiona un pool de objetos DamageNumber sobre un Canvas overlay.
/// Auto-crea toda la jerarquía necesaria si no existe (plug & play).
/// 
/// USO DESDE CUALQUIER SCRIPT:
///   DamageNumberManager.Instance.SpawnDamage(hitPoint, 50, DamageNumberType.Normal);
///   DamageNumberManager.Instance.SpawnDamage(hitPoint, 120, DamageNumberType.Headshot);
///   DamageNumberManager.Instance.SpawnDamage(hitPoint, 75, DamageNumberType.Critical);
/// </summary>
public class DamageNumberManager : MonoBehaviour
{
    // =========================================================================
    // SINGLETON
    // =========================================================================

    private static DamageNumberManager _instance;
    public static DamageNumberManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<DamageNumberManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("DamageNumberManager");
                    _instance = go.AddComponent<DamageNumberManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    // =========================================================================
    // CONFIGURACIÓN
    // =========================================================================

    [Header("=== COLORES POR TIPO DE DAÑO ===")]
    [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 1f);        // Blanco
    [SerializeField] private Color headshotColor = new Color(1f, 0.2f, 0.2f, 1f);  // Rojo
    [SerializeField] private Color criticalColor = new Color(1f, 0.85f, 0f, 1f);   // Amarillo/Dorado
    [SerializeField] private Color healColor = new Color(0.2f, 1f, 0.4f, 1f);      // Verde

    [Header("=== TAMAÑO DE FUENTE POR TIPO ===")]
    [SerializeField] private float normalFontSize = 28f;
    [SerializeField] private float headshotFontSize = 40f;
    [SerializeField] private float criticalFontSize = 34f;
    [SerializeField] private float healFontSize = 26f;

    [Header("=== ANIMACIÓN ===")]
    [Tooltip("Cuánto tiempo dura cada número en pantalla")]
    [SerializeField] private float numberLifetime = 1.2f;

    [Tooltip("Velocidad de ascenso (unidades mundo/seg)")]
    [SerializeField] private float floatSpeed = 1.5f;

    [Tooltip("Escala inicial (punch-in)")]
    [SerializeField] private float initialScale = 1.5f;

    [Tooltip("Escala final antes de desaparecer")]
    [SerializeField] private float finalScale = 0.5f;

    [Header("=== POOL ===")]
    [Tooltip("Cantidad inicial de números pre-instanciados")]
    [SerializeField] private int poolSize = 30;

    [Header("=== FUENTE (Opcional) ===")]
    [Tooltip("Font Asset de TMP para los números. Si está vacío usa LiberationSans SDF")]
    [SerializeField] private TMP_FontAsset customFont;

    // =========================================================================
    // CURVAS POR DEFECTO
    // =========================================================================

    // Curva de escala: punch in → hold → shrink
    private AnimationCurve defaultScaleCurve;
    // Curva de alpha: hold → fade out
    private AnimationCurve defaultAlphaCurve;

    // =========================================================================
    // INTERNOS
    // =========================================================================

    private Canvas damageCanvas;
    private List<DamageNumber> pool = new List<DamageNumber>();
    private bool isInitialized = false;

    // =========================================================================
    // LIFECYCLE
    // =========================================================================

    void Awake()
    {
        // Singleton enforcement
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        Initialize();
    }

    private void Initialize()
    {
        if (isInitialized) return;

        try
        {
            // Crear curvas de animación por defecto
            CreateDefaultCurves();

            // Crear Canvas overlay si no existe
            EnsureCanvas();

            // Pre-instanciar pool
            for (int i = 0; i < poolSize; i++)
            {
                CreatePooledNumber();
            }

            isInitialized = true;
            Debug.Log($"[DamageNumberManager] Inicializado con pool de {poolSize} números.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[DamageNumberManager] Error durante la inicialización: {ex.Message}");
            // Marcar como inicializado para no re-intentar fallidamente cada frame
            isInitialized = true; 
        }
    }

    // =========================================================================
    // MÉTODO PÚBLICO PRINCIPAL
    // =========================================================================

    /// <summary>
    /// Muestra un número de daño flotante en la posición del mundo indicada.
    /// </summary>
    /// <param name="worldPosition">Punto de impacto en el mundo</param>
    /// <param name="damage">Cantidad de daño a mostrar</param>
    /// <param name="type">Tipo de daño (afecta color y tamaño)</param>
    public void SpawnDamage(Vector3 worldPosition, int damage, DamageNumberType type = DamageNumberType.Normal)
    {
        if (!isInitialized) Initialize();

        DamageNumber number = GetFromPool();
        if (number == null) return;

        // Configurar según tipo
        DamageNumberConfig config = new DamageNumberConfig
        {
            worldPosition = worldPosition,
            damage = damage,
            color = GetColorForType(type),
            fontSize = GetFontSizeForType(type),
            lifetime = type == DamageNumberType.Headshot ? numberLifetime * 1.3f : numberLifetime,
            floatSpeed = floatSpeed,
            initialScale = type == DamageNumberType.Headshot ? initialScale * 1.4f : initialScale,
            finalScale = finalScale,
            scaleCurve = defaultScaleCurve,
            alphaCurve = defaultAlphaCurve
        };

        number.Initialize(config);
    }

    /// <summary>
    /// Versión simplificada: muestra un número con color personalizado.
    /// </summary>
    public void SpawnCustom(Vector3 worldPosition, int value, Color color, float fontSize = 28f)
    {
        if (!isInitialized) Initialize();

        DamageNumber number = GetFromPool();
        if (number == null) return;

        DamageNumberConfig config = new DamageNumberConfig
        {
            worldPosition = worldPosition,
            damage = value,
            color = color,
            fontSize = fontSize,
            lifetime = numberLifetime,
            floatSpeed = floatSpeed,
            initialScale = initialScale,
            finalScale = finalScale,
            scaleCurve = defaultScaleCurve,
            alphaCurve = defaultAlphaCurve
        };

        number.Initialize(config);
    }

    // =========================================================================
    // HELPERS
    // =========================================================================

    private Color GetColorForType(DamageNumberType type)
    {
        switch (type)
        {
            case DamageNumberType.Normal:    return normalColor;
            case DamageNumberType.Headshot:  return headshotColor;
            case DamageNumberType.Critical:  return criticalColor;
            case DamageNumberType.Heal:      return healColor;
            default:                         return normalColor;
        }
    }

    private float GetFontSizeForType(DamageNumberType type)
    {
        switch (type)
        {
            case DamageNumberType.Normal:    return normalFontSize;
            case DamageNumberType.Headshot:  return headshotFontSize;
            case DamageNumberType.Critical:  return criticalFontSize;
            case DamageNumberType.Heal:      return healFontSize;
            default:                         return normalFontSize;
        }
    }

    // =========================================================================
    // OBJECT POOL
    // =========================================================================

    private DamageNumber GetFromPool()
    {
        // Buscar uno inactivo
        for (int i = 0; i < pool.Count; i++)
        {
            if (!pool[i].IsActive())
                return pool[i];
        }

        // Pool agotado → expandir
        Debug.Log("[DamageNumberManager] Pool agotado, expandiendo...");
        return CreatePooledNumber();
    }

    private DamageNumber CreatePooledNumber()
    {
        GameObject go = new GameObject("DamageNumber");
        go.transform.SetParent(damageCanvas.transform, false);

        // RectTransform
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200f, 60f);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);

        // TextMeshPro
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.raycastTarget = false;
        tmp.fontSize = normalFontSize;
        tmp.fontStyle = FontStyles.Bold;
        if (customFont != null)
            tmp.font = customFont;

        // DamageNumber component
        DamageNumber dn = go.AddComponent<DamageNumber>();

        go.SetActive(false);
        pool.Add(dn);
        return dn;
    }

    // =========================================================================
    // CANVAS SETUP
    // =========================================================================

    private void EnsureCanvas()
    {
        try
        {
            if (damageCanvas != null) return;

            // Buscar Canvas existente con tag o nombre
            GameObject existing = GameObject.Find("DamageNumberCanvas");
            if (existing != null)
            {
                damageCanvas = existing.GetComponent<Canvas>();
                if (damageCanvas != null) return;
            }

            // Crear Canvas overlay dedicado
            GameObject canvasGO = new GameObject("DamageNumberCanvas");
            canvasGO.transform.SetParent(transform);

            damageCanvas = canvasGO.AddComponent<Canvas>();
            damageCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            damageCanvas.sortingOrder = 100; // Por encima de la mayoría de UI

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            Debug.Log("[DamageNumberManager] Canvas de números de daño creado automáticamente.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[DamageNumberManager] Error al crear el Canvas: {ex.Message}");
        }
    }

    // =========================================================================
    // CURVAS DE ANIMACIÓN
    // =========================================================================

    private void CreateDefaultCurves()
    {
        // Curva de escala: punch in rápido → hold → shrink suave
        // 0.0: 0.0 (aparece)
        // 0.1: 1.0 (tamaño completo rápido — punch)
        // 0.6: 1.0 (mantiene)
        // 1.0: 0.0 (desaparece)
        defaultScaleCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 12f),      // Aparece — tangente empinada
            new Keyframe(0.08f, 1.15f, 0f, 0f),  // Overshoot ligero
            new Keyframe(0.15f, 1f, 0f, 0f),     // Se estabiliza
            new Keyframe(0.65f, 1f, 0f, 0f),     // Mantiene
            new Keyframe(1f, 0f, -2f, 0f)         // Se encoge
        );

        // Curva de alpha: visible → fade out en el último 40%
        defaultAlphaCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.6f, 1f),
            new Keyframe(1f, 0f)
        );
    }
}

/// <summary>
/// Tipos de daño para determinar color y estilo del número flotante
/// </summary>
public enum DamageNumberType
{
    Normal,     // Daño normal — blanco
    Headshot,   // Headshot — rojo grande
    Critical,   // Crítico — amarillo/dorado
    Heal        // Curación — verde
}
