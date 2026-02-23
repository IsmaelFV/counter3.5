using UnityEngine;

/// <summary>
/// Trazador visual de bala usando LineRenderer.
/// Se crea desde WeaponManager al disparar.
/// Viaja desde el muzzle del arma hasta el punto de impacto y se desvanece.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class BulletTracer : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private float speed = 300f;
    [SerializeField] private float lifetime = 0.3f;
    [SerializeField] private float startWidth = 0.02f;
    [SerializeField] private float endWidth = 0.005f;
    [SerializeField] private float fadeSpeed = 5f;

    private LineRenderer lineRenderer;
    private Vector3 startPoint;
    private Vector3 endPoint;
    private float progress = 0f;
    private float totalDistance;
    private float fadeTimer;
    private bool reachedTarget = false;
    private Color tracerColor;

    /// <summary>
    /// Inicializa el trazador con puntos de inicio y fin
    /// </summary>
    public void Initialize(Vector3 start, Vector3 end, Color color, float tracerSpeed = 300f)
    {
        startPoint = start;
        endPoint = end;
        tracerColor = color;
        speed = tracerSpeed > 0f ? tracerSpeed : 300f;

        totalDistance = Vector3.Distance(start, end);

        // Configurar LineRenderer
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
            lineRenderer = gameObject.AddComponent<LineRenderer>();

        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = startWidth;
        lineRenderer.endWidth = endWidth;
        lineRenderer.useWorldSpace = true;

        // Material emisivo (funciona sin asignar material externo)
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = tracerColor;
        lineRenderer.endColor = tracerColor;

        // Posición inicial
        lineRenderer.SetPosition(0, startPoint);
        lineRenderer.SetPosition(1, startPoint);

        // Si velocidad es muy alta, hacerlo instantáneo
        if (speed >= 1000f || totalDistance < 1f)
        {
            lineRenderer.SetPosition(0, startPoint);
            lineRenderer.SetPosition(1, endPoint);
            reachedTarget = true;
            fadeTimer = 0f;
        }

        // Auto-destruir
        Destroy(gameObject, lifetime + 1f);
    }

    void Update()
    {
        if (lineRenderer == null) return;

        if (!reachedTarget)
        {
            // Avanzar el trazador hacia el objetivo
            progress += (speed / totalDistance) * Time.deltaTime;
            progress = Mathf.Clamp01(progress);

            Vector3 currentEnd = Vector3.Lerp(startPoint, endPoint, progress);
            
            // El inicio del tracer se retrasa un poco para crear efecto de línea corta
            float trailProgress = Mathf.Max(0f, progress - 0.3f);
            Vector3 currentStart = Vector3.Lerp(startPoint, endPoint, trailProgress);

            lineRenderer.SetPosition(0, currentStart);
            lineRenderer.SetPosition(1, currentEnd);

            if (progress >= 1f)
            {
                reachedTarget = true;
                fadeTimer = 0f;
            }
        }
        else
        {
            // Desvanecer tras llegar al destino
            fadeTimer += Time.deltaTime * fadeSpeed;
            float alpha = Mathf.Lerp(tracerColor.a, 0f, fadeTimer);

            Color fadedColor = new Color(tracerColor.r, tracerColor.g, tracerColor.b, alpha);
            lineRenderer.startColor = fadedColor;
            lineRenderer.endColor = fadedColor;

            // También reducir el ancho
            float widthMult = Mathf.Lerp(1f, 0f, fadeTimer);
            lineRenderer.startWidth = startWidth * widthMult;
            lineRenderer.endWidth = endWidth * widthMult;

            if (alpha <= 0.01f)
            {
                Destroy(gameObject);
            }
        }
    }

    /// <summary>
    /// Configura el ancho del trazador
    /// </summary>
    public void SetWidth(float start, float end)
    {
        startWidth = start;
        endWidth = end;
        if (lineRenderer != null)
        {
            lineRenderer.startWidth = startWidth;
            lineRenderer.endWidth = endWidth;
        }
    }
}
