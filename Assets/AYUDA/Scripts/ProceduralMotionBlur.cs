using UnityEngine;
#if UNITY_POST_PROCESSING_STACK_V2
using UnityEngine.Rendering.PostProcessing;
#endif

/// <summary>
/// Motion Blur procedural que se activa durante sprint y slide.
/// Requiere Post Processing Stack v2 instalado.
/// Instalación: Window > Package Manager > Post Processing
/// </summary>
public class ProceduralMotionBlur : MonoBehaviour
{
#if UNITY_POST_PROCESSING_STACK_V2
    [Header("=== INTENSIDAD ===")]
    [SerializeField, Range(0f, 1f)] private float normalBlur = 0f;
    [SerializeField, Range(0f, 1f)] private float sprintBlur = 0.25f;
    [SerializeField, Range(0f, 1f)] private float slideBlur = 0.45f;

    [Header("=== TRANSICIÓN ===")]
    [SerializeField] private float blurTransitionSpeed = 5f;

    [Header("Referencias")]
    [SerializeField] private PostProcessVolume postProcessVolume;
    [SerializeField] private PlayerMovement playerMovement;

    private MotionBlur motionBlurEffect;
    private float targetBlurAmount = 0f;
    private float currentBlurAmount = 0f;

    void Start()
    {
        if (postProcessVolume == null)
            postProcessVolume = GetComponent<PostProcessVolume>();

        if (playerMovement == null)
            playerMovement = GetComponentInParent<PlayerMovement>();

        // Buscar el efecto MotionBlur en el profile
        if (postProcessVolume != null && postProcessVolume.profile != null)
        {
            postProcessVolume.profile.TryGetSettings(out motionBlurEffect);
            if (motionBlurEffect == null)
            {
                Debug.LogWarning("ProceduralMotionBlur: No se encontró MotionBlur en el PostProcessProfile. Añade Motion Blur en el perfil.");
            }
        }
        else
        {
            Debug.LogWarning("ProceduralMotionBlur: PostProcessVolume o Profile no asignado.");
        }
    }

    void Update()
    {
        if (motionBlurEffect == null || playerMovement == null) return;

        // Determinar blur objetivo según estado
        if (playerMovement.IsSliding)
            targetBlurAmount = slideBlur;
        else if (playerMovement.IsSprinting)
            targetBlurAmount = sprintBlur;
        else
            targetBlurAmount = normalBlur;

        // Transición suave
        currentBlurAmount = Mathf.Lerp(currentBlurAmount, targetBlurAmount, Time.deltaTime * blurTransitionSpeed);

        // Aplicar al efecto (shutterAngle controla la intensidad)
        motionBlurEffect.shutterAngle.value = currentBlurAmount * 360f; // 0-360 grados
    }

    /// <summary>
    /// Establece blur manualmente (útil para eventos especiales)
    /// </summary>
    public void SetBlur(float amount, float duration = 0f)
    {
        if (motionBlurEffect == null) return;
        
        if (duration > 0f)
            StartCoroutine(BlurPulse(amount, duration));
        else
            currentBlurAmount = amount;
    }

    private System.Collections.IEnumerator BlurPulse(float peakAmount, float duration)
    {
        float elapsed = 0f;
        float startAmount = currentBlurAmount;

        // Subir al pico
        while (elapsed < duration * 0.3f)
        {
            elapsed += Time.deltaTime;
            currentBlurAmount = Mathf.Lerp(startAmount, peakAmount, elapsed / (duration * 0.3f));
            motionBlurEffect.shutterAngle.value = currentBlurAmount * 360f;
            yield return null;
        }

        // Bajar
        float decayStart = elapsed;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = (elapsed - decayStart) / (duration * 0.7f);
            currentBlurAmount = Mathf.Lerp(peakAmount, targetBlurAmount, t);
            motionBlurEffect.shutterAngle.value = currentBlurAmount * 360f;
            yield return null;
        }
    }
#else
    void Start()
    {
        Debug.LogWarning("ProceduralMotionBlur requiere Post Processing Stack v2. Instálalo desde Package Manager.");
    }
#endif
}
