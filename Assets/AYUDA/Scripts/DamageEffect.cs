using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using System.Collections;

/// <summary>
/// Maneja los efectos visuales cuando el jugador recibe daño
/// </summary>
public class DamageEffect : MonoBehaviour
{
    [Header("Configuración de Efecto")]
    [SerializeField] private float effectDuration = 0.5f;
    [SerializeField] private float maxVignetteIntensity = 0.6f;
    [SerializeField] private Color damageColor = new Color(1f, 0f, 0f, 1f); // Rojo

    [Header("Referencias")]
    [SerializeField] private PostProcessVolume postProcessVolume;
    
    private Vignette vignette;
    private ColorGrading colorGrading;
    private Coroutine currentEffect;

    void Start()
    {
        // Buscar PostProcessVolume si no está asignado
        if (postProcessVolume == null)
        {
            postProcessVolume = GetComponent<PostProcessVolume>();
            
            // Si aún no existe, buscar en la cámara
            if (postProcessVolume == null)
            {
                Camera mainCamera = Camera.main;
                if (mainCamera != null)
                    postProcessVolume = mainCamera.GetComponent<PostProcessVolume>();
            }
        }

        // Intentar obtener los efectos
        if (postProcessVolume != null && postProcessVolume.profile != null)
        {
            postProcessVolume.profile.TryGetSettings(out vignette);
            postProcessVolume.profile.TryGetSettings(out colorGrading);
        }
    }

    /// <summary>
    /// Muestra el efecto de daño
    /// </summary>
    public void ShowDamageEffect()
    {
        // Detener efecto anterior si existe
        if (currentEffect != null)
            StopCoroutine(currentEffect);

        currentEffect = StartCoroutine(DamageEffectCoroutine());
    }

    private IEnumerator DamageEffectCoroutine()
    {
        float elapsed = 0f;

        // Aplicar efecto de viñeta si está disponible
        if (vignette != null)
        {
            vignette.enabled.value = true;
            vignette.color.value = damageColor;
        }

        // Fade in rápido
        while (elapsed < effectDuration * 0.3f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (effectDuration * 0.3f);

            if (vignette != null)
                vignette.intensity.value = Mathf.Lerp(0f, maxVignetteIntensity, t);

            yield return null;
        }

        // Fade out más lento
        elapsed = 0f;
        while (elapsed < effectDuration * 0.7f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (effectDuration * 0.7f);

            if (vignette != null)
                vignette.intensity.value = Mathf.Lerp(maxVignetteIntensity, 0f, t);

            yield return null;
        }

        // Asegurar que el efecto esté completamente apagado
        if (vignette != null)
        {
            vignette.intensity.value = 0f;
            vignette.enabled.value = false;
        }

        currentEffect = null;
    }

    /// <summary>
    /// Limpia los efectos inmediatamente
    /// </summary>
    public void ClearEffects()
    {
        if (currentEffect != null)
            StopCoroutine(currentEffect);

        if (vignette != null)
        {
            vignette.intensity.value = 0f;
            vignette.enabled.value = false;
        }
    }

    void OnDisable()
    {
        ClearEffects();
    }
}
