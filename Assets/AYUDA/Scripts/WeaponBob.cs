using UnityEngine;

/// <summary>
/// Bobbing procedural avanzado del arma.
/// Patrón Lissajous (figura-8) con rotación, impulso vertical exagerado,
/// kickback al correr, transiciones suaves con inercia, PLUS:
/// - Respuesta vertical al salto/caída (arma sube al saltar, baja al caer)
/// - Inercia de cambio de dirección (arma se retrasa al cambiar de sentido)
/// - Micro-variaciones orgánicas por ciclo (Perlin noise en amplitud)
/// - Step impact (énfasis en el punto más bajo del bob, como pisada)
/// </summary>
public class WeaponBob : MonoBehaviour
{
    [Header("=== BOBBING GENERAL ===")]
    [SerializeField] private bool enableBob = true;

    [Header("Caminar")]
    [SerializeField] private float walkBobSpeed = 10f;
    [SerializeField] private float walkBobAmountX = 0.018f;
    [SerializeField] private float walkBobAmountY = 0.025f;
    [SerializeField] private float walkBobAmountZ = 0.008f;

    [Header("Correr")]
    [SerializeField] private float sprintBobSpeed = 15f;
    [SerializeField] private float sprintBobAmountX = 0.04f;
    [SerializeField] private float sprintBobAmountY = 0.055f;
    [SerializeField] private float sprintBobAmountZ = 0.02f;

    [Header("Agachado")]
    [SerializeField] private float crouchBobSpeed = 6f;
    [SerializeField] private float crouchBobAmountX = 0.006f;
    [SerializeField] private float crouchBobAmountY = 0.009f;

    [Header("=== ROTACIÓN AL BOBBING ===")]
    [SerializeField] private float walkRollAmount = 1.5f;
    [SerializeField] private float sprintRollAmount = 4f;
    [SerializeField] private float walkPitchAmount = 0.8f;
    [SerializeField] private float sprintPitchAmount = 2.5f;

    [Header("=== STRAFE TILT ===")]
    [Tooltip("Inclinación del arma al moverse lateralmente")]
    [SerializeField] private float strafeTiltAmount = 3.5f;
    [SerializeField] private float strafeTiltSpeed = 8f;

    [Header("=== SPRINT PULL BACK ===")]
    [Tooltip("Al esprintar, el arma se retrae ligeramente y rota hacia arriba")]
    [SerializeField] private float sprintPullBackZ = 0.03f;
    [SerializeField] private float sprintPullDownY = -0.015f;
    [SerializeField] private float sprintPitchUp = 5f;

    [Header("=== RESPUESTA VERTICAL (Salto/Caída) ===")]
    [Tooltip("El arma sube al saltar y baja al caer")]
    [SerializeField] private float jumpVerticalResponse = 0.015f;
    [Tooltip("Velocidad de respuesta al estado aéreo")]
    [SerializeField] private float airborneResponseSpeed = 4f;
    [SerializeField] private float maxAirborneOffset = 0.04f;
    [Tooltip("Pitch del arma en el aire (apunta ligeramente arriba al subir, abajo al caer)")]
    [SerializeField] private float airbornePitchAmount = 3f;

    [Header("=== INERCIA DE DIRECCIÓN ===")]
    [Tooltip("El arma se retrasa al cambiar bruscamente de dirección")]
    [SerializeField] private float directionInertiaAmount = 0.006f;
    [SerializeField] private float directionInertiaSpeed = 5f;
    [SerializeField] private float maxDirectionInertia = 0.015f;

    [Header("=== MICRO-VARIACIÓN ORGÁNICA ===")]
    [Tooltip("Variación de amplitud por ciclo para romper el patrón mecánico")]
    [SerializeField] private float organicVariation = 0.12f;

    [Header("=== STEP IMPACT ===")]
    [Tooltip("Énfasis en el punto más bajo del bob (pisada con peso)")]
    [SerializeField] private float stepImpactMultiplier = 1.3f;
    [SerializeField] private float stepImpactSharpness = 2.5f;

    [Header("=== SUAVIZADO ===")]
    [SerializeField] private float smoothSpeed = 12f;
    [SerializeField] private float returnSpeed = 10f;
    [SerializeField] private float bobIntensityLerpSpeed = 6f;

    [Header("Referencias")]
    [SerializeField] private CharacterController characterController;

    // Estado interno
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private float bobTimer = 0f;
    private float currentIntensity = 0f;
    private float currentStrafeTilt = 0f;
    private Vector3 targetBobPos;
    private Quaternion targetBobRot;

    // Respuesta vertical
    private float smoothedAirborneOffset = 0f;
    private float airbornePitch = 0f;

    // Inercia de dirección
    private Vector2 lastMoveInput;
    private Vector3 directionInertiaOffset;
    private Vector3 smoothedDirectionInertia;

    // Perlin noise seeds
    private float noiseSeedAmp;
    private float noiseSeedSpd;

    // Referencia
    private PlayerMovement playerMovement;

    void Start()
    {
        originalPosition = transform.localPosition;
        originalRotation = transform.localRotation;

        if (characterController == null)
            characterController = GetComponentInParent<CharacterController>();

        playerMovement = GetComponentInParent<PlayerMovement>();

        noiseSeedAmp = Random.Range(0f, 1000f);
        noiseSeedSpd = Random.Range(0f, 1000f);
    }

    void Update()
    {
        if (!enableBob) return;

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        bool isMoving = (Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f);
        bool isGrounded = characterController != null && characterController.isGrounded;
        bool isSprinting = Input.GetKey(KeyCode.LeftShift) && isMoving && vertical > 0.1f;
        bool isCrouching = Input.GetKey(KeyCode.LeftControl);

        // Intensidad objetivo basada en estado
        float targetIntensity = 0f;
        if (isMoving && isGrounded)
            targetIntensity = isSprinting ? 1.5f : (isCrouching ? 0.5f : 1f);

        currentIntensity = Mathf.Lerp(currentIntensity, targetIntensity, Time.deltaTime * bobIntensityLerpSpeed);

        // --- STRAFE TILT ---
        float targetTilt = -horizontal * strafeTiltAmount;
        currentStrafeTilt = Mathf.Lerp(currentStrafeTilt, targetTilt, Time.deltaTime * strafeTiltSpeed);

        // --- INERCIA DE DIRECCIÓN ---
        Vector2 currentMoveInput = new Vector2(horizontal, vertical);
        Vector2 inputDelta = currentMoveInput - lastMoveInput;
        lastMoveInput = currentMoveInput;

        Vector3 targetDirInertia = new Vector3(
            -inputDelta.x * directionInertiaAmount / Mathf.Max(Time.deltaTime, 0.001f),
            0f,
            -inputDelta.y * directionInertiaAmount / Mathf.Max(Time.deltaTime, 0.001f)
        );

        targetDirInertia.x = Mathf.Clamp(targetDirInertia.x, -maxDirectionInertia, maxDirectionInertia);
        targetDirInertia.z = Mathf.Clamp(targetDirInertia.z, -maxDirectionInertia, maxDirectionInertia);

        smoothedDirectionInertia = Vector3.Lerp(smoothedDirectionInertia, targetDirInertia,
            Time.deltaTime * directionInertiaSpeed);

        // Decaer hacia cero
        smoothedDirectionInertia = Vector3.Lerp(smoothedDirectionInertia, Vector3.zero,
            Time.deltaTime * directionInertiaSpeed * 0.5f);

        // --- RESPUESTA VERTICAL (Aéreo) ---
        if (!isGrounded && characterController != null)
        {
            float verticalVel = characterController.velocity.y;
            float targetOffset = verticalVel * jumpVerticalResponse;
            targetOffset = Mathf.Clamp(targetOffset, -maxAirborneOffset, maxAirborneOffset);
            smoothedAirborneOffset = Mathf.Lerp(smoothedAirborneOffset, targetOffset,
                Time.deltaTime * airborneResponseSpeed);

            // Pitch aéreo
            float targetAirbornePitch = -verticalVel * airbornePitchAmount * 0.1f;
            targetAirbornePitch = Mathf.Clamp(targetAirbornePitch, -airbornePitchAmount, airbornePitchAmount);
            airbornePitch = Mathf.Lerp(airbornePitch, targetAirbornePitch, Time.deltaTime * airborneResponseSpeed);
        }
        else
        {
            smoothedAirborneOffset = Mathf.Lerp(smoothedAirborneOffset, 0f, Time.deltaTime * 10f);
            airbornePitch = Mathf.Lerp(airbornePitch, 0f, Time.deltaTime * 10f);
        }

        if (currentIntensity > 0.05f)
        {
            // Parámetros interpolados según intensidad
            float bobSpeed, bobX, bobY, bobZ, rollAmt, pitchAmt;

            if (currentIntensity > 1.2f) // Sprint
            {
                float t = (currentIntensity - 1f) / 0.5f;
                bobSpeed = Mathf.Lerp(walkBobSpeed, sprintBobSpeed, t);
                bobX = Mathf.Lerp(walkBobAmountX, sprintBobAmountX, t);
                bobY = Mathf.Lerp(walkBobAmountY, sprintBobAmountY, t);
                bobZ = Mathf.Lerp(walkBobAmountZ, sprintBobAmountZ, t);
                rollAmt = Mathf.Lerp(walkRollAmount, sprintRollAmount, t);
                pitchAmt = Mathf.Lerp(walkPitchAmount, sprintPitchAmount, t);
            }
            else if (currentIntensity < 0.7f) // Agachado
            {
                bobSpeed = crouchBobSpeed;
                bobX = crouchBobAmountX;
                bobY = crouchBobAmountY;
                bobZ = 0f;
                rollAmt = walkRollAmount * 0.4f;
                pitchAmt = walkPitchAmount * 0.3f;
            }
            else // Caminar
            {
                bobSpeed = walkBobSpeed;
                bobX = walkBobAmountX;
                bobY = walkBobAmountY;
                bobZ = walkBobAmountZ;
                rollAmt = walkRollAmount;
                pitchAmt = walkPitchAmount;
            }

            // Micro-variación orgánica (Perlin modula amplitud y velocidad por ciclo)
            float noiseAmp = 1f + (Mathf.PerlinNoise(bobTimer * 0.25f + noiseSeedAmp, 0f) - 0.5f) * 2f * organicVariation;
            float noiseSpd = 1f + (Mathf.PerlinNoise(bobTimer * 0.15f + noiseSeedSpd, 0f) - 0.5f) * 2f * organicVariation * 0.4f;

            // Increment timer con variación
            bobTimer += Time.deltaTime * bobSpeed * noiseSpd;

            // Aplicar variación orgánica a amplitudes
            bobX *= noiseAmp;
            bobY *= noiseAmp;

            // --- PATRÓN LISSAJOUS (figura-8) con step impact ---
            float lissX = Mathf.Cos(bobTimer) * bobX;

            // Bob vertical con impacto de pisada
            float rawVBob = Mathf.Sin(bobTimer * 2f);
            float stepImpact = 0f;
            if (rawVBob < 0f)
            {
                float normalizedDip = -rawVBob;
                stepImpact = -Mathf.Pow(normalizedDip, stepImpactSharpness) * (stepImpactMultiplier - 1f);
            }
            float lissY = (rawVBob + stepImpact) * bobY;

            // Empujón sutil en Z sincronizado con pisadas
            float lissZ = Mathf.Sin(bobTimer * 2f) * bobZ;

            // Posición base del bob + inercia de dirección + offset aéreo
            targetBobPos = originalPosition + new Vector3(lissX, lissY, lissZ);
            targetBobPos += smoothedDirectionInertia;
            targetBobPos.y += smoothedAirborneOffset;

            // Sprint pull-back: retraer arma al correr
            if (currentIntensity > 1.2f)
            {
                float sprintT = (currentIntensity - 1f) / 0.5f;
                targetBobPos.z += sprintPullBackZ * sprintT;
                targetBobPos.y += sprintPullDownY * sprintT;
            }

            // --- ROTACIÓN DEL BOB ---
            float rollBob = Mathf.Cos(bobTimer) * rollAmt * noiseAmp;
            float pitchBob = Mathf.Sin(bobTimer * 2f) * pitchAmt * noiseAmp;
            float sprintPitch = 0f;

            if (currentIntensity > 1.2f)
            {
                float sprintT = (currentIntensity - 1f) / 0.5f;
                sprintPitch = sprintPitchUp * sprintT;
            }

            // Combinar roll del bob + strafe tilt + sprint pitch + airborne pitch
            targetBobRot = originalRotation * Quaternion.Euler(
                pitchBob + sprintPitch + airbornePitch,
                0f,
                rollBob + currentStrafeTilt
            );

            // Aplicar con suavizado
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetBobPos, Time.deltaTime * smoothSpeed);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, targetBobRot, Time.deltaTime * smoothSpeed);
        }
        else
        {
            // Volver a posición original suavemente
            bobTimer = Mathf.Lerp(bobTimer, 0f, Time.deltaTime * 3f);

            // Mantener strafe tilt + respuesta aérea incluso parado
            Vector3 restPos = originalPosition;
            restPos.y += smoothedAirborneOffset;
            restPos += smoothedDirectionInertia;

            Quaternion restRot = originalRotation * Quaternion.Euler(airbornePitch, 0f, currentStrafeTilt);

            transform.localPosition = Vector3.Lerp(transform.localPosition, restPos, Time.deltaTime * returnSpeed);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, restRot, Time.deltaTime * returnSpeed);
        }
    }

    /// <summary>
    /// Activa o desactiva el weapon bob
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        enableBob = enabled;
        if (!enabled)
        {
            transform.localPosition = originalPosition;
            transform.localRotation = originalRotation;
            bobTimer = 0f;
            currentIntensity = 0f;
            smoothedAirborneOffset = 0f;
            airbornePitch = 0f;
            smoothedDirectionInertia = Vector3.zero;
        }
    }

    /// <summary>
    /// Resetea la posición original (útil al cambiar de arma)
    /// </summary>
    public void ResetOriginalPosition()
    {
        originalPosition = transform.localPosition;
        originalRotation = transform.localRotation;
        bobTimer = 0f;
        currentIntensity = 0f;
        smoothedAirborneOffset = 0f;
        airbornePitch = 0f;
        smoothedDirectionInertia = Vector3.zero;
        lastMoveInput = Vector2.zero;
    }
}
