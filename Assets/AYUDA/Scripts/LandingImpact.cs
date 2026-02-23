using UnityEngine;

/// <summary>
/// Efecto procedural de impacto al aterrizar estilo Robobeat.
/// Drop exagerado con bounce-back (overshoot spring), FOV punch,
/// y arma que cae dramaticamente y rebota.
/// Sistema ADITIVO para camara: no sobreescribe la posicion base.
/// </summary>
public class LandingImpact : MonoBehaviour
{
    [Header("=== DETECCION ===")]
    [SerializeField] private float minFallDistance = 1f;

    [Header("=== IMPACTO EN CAMARA ===")]
    [SerializeField] private float cameraDropAmount = 0.15f;
    [SerializeField] private float cameraRotationDrop = 4f;

    [Header("=== IMPACTO EN ARMA ===")]
    [SerializeField] private float weaponDropAmount = 0.12f;
    [SerializeField] private float weaponRotationDrop = 8f;
    [SerializeField] private float weaponForwardPush = 0.03f;

    [Header("=== BOUNCE BACK (Spring) ===")]
    [Tooltip("Stiffness del spring de rebote - mas alto = rebote mas rapido")]
    [SerializeField] private float springStiffness = 80f;
    [Tooltip("Damping - mas bajo = mas rebotes")]
    [SerializeField] private float springDamping = 8f;

    [Header("=== FOV PUNCH ===")]
    [SerializeField] private float fovPunchAmount = 4f;
    [SerializeField] private float fovPunchDuration = 0.3f;

    [Header("=== LIMITES ===")]
    [SerializeField] private float maxImpactIntensity = 1f;
    [SerializeField] private float fallDistanceForMax = 8f;

    [Header("Referencias")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform weaponHolder;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private ScreenShake screenShake;
    [SerializeField] private ProceduralFOVEffect fovEffect;

    // Estado interno
    private bool wasGrounded = true;
    private float highestY = 0f;
    private bool isFalling = false;

    // Spring state - camara
    private Vector3 cameraSpringPos;
    private Vector3 cameraSpringVel;
    private Vector3 cameraSpringRot;
    private Vector3 cameraSpringRotVel;

    // Spring state - arma
    private Vector3 weaponSpringPos;
    private Vector3 weaponSpringVel;
    private Vector3 weaponSpringRot;
    private Vector3 weaponSpringRotVel;

    private bool springActive = false;

    // Offset aditivo de camara - rastreado para limpiar cada frame
    private Vector3 lastAppliedCameraOffset;

    // Arma: posiciones originales (el arma SI se puede resetear, no tiene conflicto de crouch)
    private Vector3 weaponOriginalPos;
    private Quaternion weaponOriginalRot;

    void Start()
    {
        if (characterController == null)
            characterController = GetComponent<CharacterController>();

        if (cameraTransform == null)
        {
            Camera cam = Camera.main;
            if (cam != null) cameraTransform = cam.transform;
        }

        if (screenShake == null && cameraTransform != null)
            screenShake = cameraTransform.GetComponent<ScreenShake>();

        if (fovEffect == null)
            fovEffect = GetComponentInChildren<ProceduralFOVEffect>();

        lastAppliedCameraOffset = Vector3.zero;

        if (weaponHolder != null)
        {
            weaponOriginalPos = weaponHolder.localPosition;
            weaponOriginalRot = weaponHolder.localRotation;
        }
    }

    void Update()
    {
        if (characterController == null) return;

        bool isGrounded = characterController.isGrounded;

        // Detectar inicio de caida
        if (wasGrounded && !isGrounded)
        {
            highestY = transform.position.y;
            isFalling = true;
        }

        // Rastrear punto mas alto durante la caida
        if (!isGrounded && isFalling)
        {
            if (transform.position.y > highestY)
                highestY = transform.position.y;
        }

        // Detectar aterrizaje
        if (!wasGrounded && isGrounded && isFalling)
        {
            float fallDistance = highestY - transform.position.y;

            if (fallDistance > minFallDistance)
            {
                float intensity = Mathf.Clamp01(fallDistance / fallDistanceForMax) * maxImpactIntensity;
                ApplyLandingImpact(intensity);
            }

            isFalling = false;
        }

        // Spring physics para bounce-back
        if (springActive)
        {
            UpdateSpring();
        }

        wasGrounded = isGrounded;
    }

    /// <summary>
    /// Aplica el impacto de aterrizaje con spring impulse
    /// </summary>
    private void ApplyLandingImpact(float intensity)
    {
        // Actualizar posicion original del arma
        if (weaponHolder != null)
        {
            weaponOriginalPos = weaponHolder.localPosition;
            weaponOriginalRot = weaponHolder.localRotation;
        }

        // Impulso del spring de CAMARA - posicion
        cameraSpringPos = Vector3.zero;
        cameraSpringVel = new Vector3(0f, -cameraDropAmount * intensity * springStiffness * 0.5f, 0f);

        // Impulso del spring de CAMARA - rotacion
        cameraSpringRot = Vector3.zero;
        cameraSpringRotVel = new Vector3(-cameraRotationDrop * intensity * springStiffness * 0.3f, 0f, 0f);

        // Impulso del spring de ARMA
        weaponSpringPos = Vector3.zero;
        weaponSpringVel = new Vector3(
            0f,
            -weaponDropAmount * intensity * springStiffness * 0.5f,
            weaponForwardPush * intensity * springStiffness * 0.3f
        );

        weaponSpringRot = Vector3.zero;
        weaponSpringRotVel = new Vector3(
            -weaponRotationDrop * intensity * springStiffness * 0.3f,
            0f,
            Random.Range(-2f, 2f) * intensity * springStiffness * 0.1f
        );

        springActive = true;

        // Screen shake
        if (screenShake != null)
            screenShake.LandingShake(intensity);

        // FOV punch
        if (fovEffect != null)
            fovEffect.ApplyFOVPulse(-fovPunchAmount * intensity, fovPunchDuration);
    }

    /// <summary>
    /// Actualiza la fisica del spring (bounce-back natural)
    /// Sistema aditivo para camara, absoluto para arma
    /// </summary>
    private void UpdateSpring()
    {
        float dt = Time.deltaTime;

        // --- CAMARA (ADITIVO) ---
        if (cameraTransform != null)
        {
            // 1. Remover offset anterior
            cameraTransform.localPosition -= lastAppliedCameraOffset;

            // 2. Spring physics
            Vector3 camPosForce = -springStiffness * cameraSpringPos - springDamping * cameraSpringVel;
            cameraSpringVel += camPosForce * dt;
            cameraSpringPos += cameraSpringVel * dt;

            // Rotacion spring
            Vector3 camRotForce = -springStiffness * cameraSpringRot - springDamping * cameraSpringRotVel;
            cameraSpringRotVel += camRotForce * dt;
            cameraSpringRot += cameraSpringRotVel * dt;

            // 3. Aplicar nuevo offset aditivamente
            lastAppliedCameraOffset = cameraSpringPos;
            cameraTransform.localPosition += lastAppliedCameraOffset;
        }

        // --- ARMA (puede ser absoluto, no tiene conflicto con crouch) ---
        if (weaponHolder != null)
        {
            Vector3 wpnPosForce = -springStiffness * weaponSpringPos - springDamping * weaponSpringVel;
            weaponSpringVel += wpnPosForce * dt;
            weaponSpringPos += weaponSpringVel * dt;

            Vector3 wpnRotForce = -springStiffness * weaponSpringRot - springDamping * weaponSpringRotVel;
            weaponSpringRotVel += wpnRotForce * dt;
            weaponSpringRot += weaponSpringRotVel * dt;

            weaponHolder.localPosition = weaponOriginalPos + weaponSpringPos;
            weaponHolder.localRotation = weaponOriginalRot * Quaternion.Euler(weaponSpringRot);
        }

        // Verificar si el spring se calmo
        bool camCalm = cameraSpringPos.sqrMagnitude < 0.00001f && cameraSpringVel.sqrMagnitude < 0.00001f;
        bool wpnCalm = weaponSpringPos.sqrMagnitude < 0.00001f && weaponSpringVel.sqrMagnitude < 0.00001f;

        if (camCalm && wpnCalm)
        {
            springActive = false;

            // Limpiar offset residual de camara
            if (cameraTransform != null)
            {
                cameraTransform.localPosition -= lastAppliedCameraOffset;
                lastAppliedCameraOffset = Vector3.zero;
            }

            if (weaponHolder != null)
            {
                weaponHolder.localPosition = weaponOriginalPos;
                weaponHolder.localRotation = weaponOriginalRot;
            }
        }
    }

    /// <summary>
    /// Actualiza posiciones originales del arma (no de camara, esa es aditiva)
    /// </summary>
    public void UpdateOriginalPositions()
    {
        if (weaponHolder != null)
        {
            weaponOriginalPos = weaponHolder.localPosition;
            weaponOriginalRot = weaponHolder.localRotation;
        }
    }
}
