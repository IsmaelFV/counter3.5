using UnityEngine;
using TMPro;

/// <summary>
/// Número de daño flotante individual.
/// Se mueve hacia arriba, escala y se desvanece automáticamente.
/// Gestionado por DamageNumberManager (object pooling).
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class DamageNumber : MonoBehaviour
{
    // =========================================================================
    // CONFIGURACIÓN (se sobreescribe desde DamageNumberManager)
    // =========================================================================

    private TextMeshProUGUI textComponent;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    // Estado de animación
    private Vector3 worldPosition;
    private float lifetime;
    private float maxLifetime;
    private float floatSpeed;
    private float initialScale;
    private float finalScale;
    private Vector3 randomOffset;
    private bool isActive = false;

    // Curvas de animación
    private AnimationCurve scaleCurve;
    private AnimationCurve alphaCurve;

    // Referencia a cámara
    private Camera mainCamera;

    void Awake()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
        if (textComponent == null)
            textComponent = GetComponentInChildren<TextMeshProUGUI>();

        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    /// <summary>
    /// Inicializa y activa el número de daño con la configuración dada.
    /// Llamado por DamageNumberManager al sacar del pool.
    /// </summary>
    public void Initialize(DamageNumberConfig config)
    {
        mainCamera = Camera.main;
        if (mainCamera == null) return;

        // Posición en el mundo con offset aleatorio para que no se superpongan
        worldPosition = config.worldPosition;
        randomOffset = new Vector3(
            Random.Range(-0.3f, 0.3f),
            Random.Range(0f, 0.2f),
            Random.Range(-0.3f, 0.3f)
        );
        worldPosition += randomOffset;

        // Configurar texto
        textComponent.text = config.damage.ToString();
        textComponent.color = config.color;
        textComponent.fontSize = config.fontSize;

        // Outline para legibilidad
        textComponent.outlineWidth = 0.2f;
        textComponent.outlineColor = new Color32(0, 0, 0, 200);

        // Configurar animación
        lifetime = 0f;
        maxLifetime = config.lifetime;
        floatSpeed = config.floatSpeed;
        initialScale = config.initialScale;
        finalScale = config.finalScale;
        scaleCurve = config.scaleCurve;
        alphaCurve = config.alphaCurve;

        // Estado inicial
        canvasGroup.alpha = 1f;
        transform.localScale = Vector3.one * initialScale;

        isActive = true;
        gameObject.SetActive(true);

        // Posicionar inmediatamente
        UpdateScreenPosition();
    }

    void LateUpdate()
    {
        if (!isActive) return;

        lifetime += Time.deltaTime;

        // Flotar hacia arriba
        worldPosition += Vector3.up * floatSpeed * Time.deltaTime;

        // Progreso normalizado (0 → 1)
        float t = lifetime / maxLifetime;

        // Escala animada (punch in → shrink out)
        float scaleValue = scaleCurve != null
            ? Mathf.Lerp(initialScale, finalScale, scaleCurve.Evaluate(t))
            : Mathf.Lerp(initialScale, finalScale, t);
        transform.localScale = Vector3.one * scaleValue;

        // Alpha animada (mantener → fade out)
        float alphaValue = alphaCurve != null
            ? alphaCurve.Evaluate(t)
            : Mathf.Lerp(1f, 0f, Mathf.Clamp01((t - 0.5f) * 2f)); // Fade en la 2ª mitad
        canvasGroup.alpha = alphaValue;

        // Actualizar posición en pantalla
        UpdateScreenPosition();

        // ¿Terminó la vida?
        if (lifetime >= maxLifetime)
        {
            Deactivate();
        }
    }

    /// <summary>
    /// Convierte la posición del mundo a posición en pantalla y reposiciona el texto
    /// </summary>
    private void UpdateScreenPosition()
    {
        if (mainCamera == null) return;

        // Verificar que esté delante de la cámara
        Vector3 dirToNumber = worldPosition - mainCamera.transform.position;
        if (Vector3.Dot(dirToNumber, mainCamera.transform.forward) <= 0)
        {
            canvasGroup.alpha = 0f;
            return;
        }

        Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPosition);
        rectTransform.position = screenPos;
    }

    /// <summary>
    /// Desactiva y devuelve al pool
    /// </summary>
    public void Deactivate()
    {
        isActive = false;
        gameObject.SetActive(false);
    }

    public bool IsActive() => isActive;
}

/// <summary>
/// Datos de configuración para crear un número de daño
/// </summary>
public struct DamageNumberConfig
{
    public Vector3 worldPosition;
    public int damage;
    public Color color;
    public float fontSize;
    public float lifetime;
    public float floatSpeed;
    public float initialScale;
    public float finalScale;
    public AnimationCurve scaleCurve;
    public AnimationCurve alphaCurve;
}
