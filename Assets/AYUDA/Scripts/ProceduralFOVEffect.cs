using UnityEngine;

/// <summary>
/// FOV procedural estilo Robobeat.
/// Transiciones spring-based para sprint/aim, FOV punch al disparar,
/// y breathing sutil del FOV en idle.
/// </summary>
public class ProceduralFOVEffect : MonoBehaviour
{
    [Header("=== FOV BASE ===")]
    [SerializeField] private float normalFOV = 60f;
    [SerializeField] private float sprintFOV = 72f;
    [SerializeField] private float slideFOV = 78f;
    [Tooltip("FOV máximo al final del slide (progresivo)")]
    [SerializeField] private float slideMaxFOV = 85f;
    [SerializeField] private float aimFOV = 45f;

    [Header("=== TRANSICI\u00d3N ===")]
    [Tooltip("Velocidad de transición — spring-like")]
    [SerializeField] private float fovTransitionSpeed = 10f;
    [Tooltip("Overshoot al cambiar de estado (0 = ninguno)")]
    [SerializeField] private float fovOvershoot = 2f;

    [Header("=== FOV PUNCH AL DISPARAR ===")]
    [Tooltip("Cambio de FOV instant\u00e1neo al disparar (positivo = ampliar, negativo = reducir)")]
    [SerializeField] private float shootPunchFOV = 2f;
    [SerializeField] private float shootPunchDuration = 0.12f;

    [Header("=== BREATHING FOV ===")]
    [Tooltip("Micro variaci\u00f3n de FOV constante para sensaci\u00f3n org\u00e1nica")]
    [SerializeField] private float breathingFOVAmount = 0.3f;
    [SerializeField] private float breathingFOVSpeed = 0.8f;

    [Header("Referencias")]
    [SerializeField] private Camera targetCamera;

    // Estado
    private float targetFOV;
    private float currentFOVVelocity; // Para SmoothDamp
    private bool isSprinting = false;
    private bool isSliding = false;
    private bool isAiming = false;
    private float slideProgress = 0f; // 0-1 para FOV progresivo
    private float breathTimer = 0f;

    // Pulse
    private float pulseOffset = 0f;
    private Coroutine pulseCoroutine;

    void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera != null)
        {
            normalFOV = targetCamera.fieldOfView;
            targetFOV = normalFOV;
        }
    }

    void Update()
    {
        if (targetCamera == null) return;

        // Determinar FOV objetivo
        if (isAiming)
            targetFOV = aimFOV;
        else if (isSliding)
        {
            // FOV progresivo durante slide (aumenta con el tiempo)
            targetFOV = Mathf.Lerp(slideFOV, slideMaxFOV, slideProgress);
        }
        else if (isSprinting)
            targetFOV = sprintFOV;
        else
            targetFOV = normalFOV;

        // Breathing sutil
        breathTimer += Time.deltaTime * breathingFOVSpeed;
        float breathOffset = Mathf.Sin(breathTimer) * breathingFOVAmount;

        // Transición con SmoothDamp (overshoot configurable)
        float smoothTime = 1f / fovTransitionSpeed;
        float overshootTarget = targetFOV + breathOffset + pulseOffset;
        // Aplicar overshoot como boost temporal cuando hay cambio de estado
        float fovDiff = overshootTarget - targetCamera.fieldOfView;
        if (Mathf.Abs(fovDiff) > 1f)
            overshootTarget += Mathf.Sign(fovDiff) * fovOvershoot;

        float dampedFOV = Mathf.SmoothDamp(
            targetCamera.fieldOfView,
            overshootTarget,
            ref currentFOVVelocity,
            smoothTime,
            Mathf.Infinity,
            Time.deltaTime
        );

        targetCamera.fieldOfView = dampedFOV;
    }

    /// <summary>
    /// Establece si el jugador está esprintando
    /// </summary>
    public void SetSprinting(bool sprinting)
    {
        isSprinting = sprinting;
    }

    /// <summary>
    /// Establece si el jugador está desliándose (slide) con progreso
    /// </summary>
    public void SetSliding(bool sliding, float progress = 0f)
    {
        isSliding = sliding;
        slideProgress = progress;
    }

    /// <summary>
    /// Establece si el jugador está apuntando (ADS)
    /// </summary>
    public void SetAiming(bool aiming)
    {
        isAiming = aiming;
    }

    /// <summary>
    /// Cambia la velocidad de transición del FOV dinámicamente (para ADS más suave, etc.)
    /// </summary>
    public void SetTransitionSpeed(float speed)
    {
        fovTransitionSpeed = speed;
    }

    /// <summary>
    /// Aplica un pulso de FOV temporal (al disparar, aterrizar, etc.)
    /// </summary>
    public void ApplyFOVPulse(float extraFOV, float duration)
    {
        if (pulseCoroutine != null)
            StopCoroutine(pulseCoroutine);
        pulseCoroutine = StartCoroutine(FOVPulseCoroutine(extraFOV, duration));
    }

    /// <summary>
    /// FOV punch rápido al disparar (llamar desde WeaponManager)
    /// </summary>
    public void ShootPunch()
    {
        ApplyFOVPulse(shootPunchFOV, shootPunchDuration);
    }

    private System.Collections.IEnumerator FOVPulseCoroutine(float extraFOV, float duration)
    {
        float elapsed = 0f;

        // Fase 1: Apply punch instantáneo
        pulseOffset = extraFOV;

        // Fase 2: Decay gradual con curva cuadrática
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // Curva cuadrática para decay rápido al principio, suave al final
            pulseOffset = extraFOV * (1f - t * t);
            yield return null;
        }

        pulseOffset = 0f;
        pulseCoroutine = null;
    }

    /// <summary>
    /// Establece un nuevo FOV base
    /// </summary>
    public void SetNormalFOV(float fov)
    {
        normalFOV = fov;
    }
}
