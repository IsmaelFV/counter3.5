using UnityEngine;

/// <summary>
/// Inclinación procedural avanzada de la cámara.
/// Strafe tilt con overshoot, sprint lean, slide effects, landing tilt,
/// breathing orgánico, PLUS:
/// - Tilt de aceleración (lean hacia la dirección de aceleración/frenado)
/// - Lean proporcional a velocidad real (no solo estado binario)
/// - Slide tilt progresivo (aumenta durante el slide)
/// - Breathing orgánico con variación de Perlin
/// Se ejecuta en LateUpdate para no interferir con el mouse look.
/// </summary>
public class SprintTilt : MonoBehaviour
{
    [Header("=== STRAFE TILT ===")]
    [Tooltip("Grados de roll al strafear")]
    [SerializeField] private float strafeMaxTilt = 3f;
    [SerializeField] private float strafeTiltSpeed = 8f;
    [SerializeField] private float strafeReturnSpeed = 10f;
    [SerializeField] private bool enableStrafeTilt = true;

    [Header("=== SPRINT TILT ===")]
    [Tooltip("Multiplicador de tilt al esprintar")]
    [SerializeField] private float sprintTiltMultiplier = 1.6f;
    [Tooltip("Lean hacia adelante al esprintar (pitch sutil)")]
    [SerializeField] private float sprintLeanAngle = 1.2f;
    [SerializeField] private float sprintLeanSpeed = 5f;

    [Header("=== SLIDE TILT ===")]
    [Tooltip("Lean hacia adelante durante el slide (más agresivo que sprint)")]
    [SerializeField] private float slideLeanAngle = 3f;
    [Tooltip("Lean máximo al final del slide (progresivo)")]
    [SerializeField] private float slideMaxLeanAngle = 5f;
    [Tooltip("Tilt lateral durante slide para sensación dinámica")]
    [SerializeField] private float slideTiltZ = 4f;
    [SerializeField] private float slideTiltSpeed = 12f;

    [Header("=== LANDING TILT ===")]
    [Tooltip("Inclinación al aterrizar")]
    [SerializeField] private float landingTiltAmount = 2.5f;
    [SerializeField] private float landingTiltRecovery = 6f;
    [SerializeField] private float minFallForTilt = 0.8f;

    [Header("=== TILT DE ACELERACIÓN ===")]
    [Tooltip("La cámara se inclina en la dirección de aceleración")]
    [SerializeField] private float accelerationTiltAmount = 1.5f;
    [Tooltip("Lean hacia atrás al frenar, hacia adelante al acelerar")]
    [SerializeField] private float accelerationLeanAmount = 0.8f;
    [SerializeField] private float accelerationTiltSpeed = 4f;
    [SerializeField] private float maxAccelerationTilt = 3f;

    [Header("=== LEAN POR VELOCIDAD ===")]
    [Tooltip("Lean proporcional a la velocidad real del jugador")]
    [SerializeField] private float velocityLeanScale = 0.3f;

    [Header("=== BREATHING MICRO TILT ===")]
    [Tooltip("Balanceo constante sutil para que la cámara nunca esté perfectamente quieta")]
    [SerializeField] private float breathingAmount = 0.2f;
    [SerializeField] private float breathingSpeed = 0.9f;
    [Tooltip("Variación orgánica del breathing con Perlin noise")]
    [SerializeField] private float breathingOrganicVariation = 0.3f;

    [Header("=== INERTIA ===")]
    [Tooltip("La cámara sobrecompensa ligeramente al parar de strafear")]
    [SerializeField] private float overshootAmount = 0.3f;

    [Header("Referencias")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private CharacterController characterController;

    // Estado
    private float currentTiltZ = 0f;
    private float currentLeanX = 0f;
    private float landingTilt = 0f;
    private float tiltVelocity = 0f;
    private float breathTimer = 0f;

    // Aceleración
    private float lastHorizontalSpeed = 0f;
    private float lastForwardSpeed = 0f;
    private float smoothedAccelTiltZ = 0f;
    private float smoothedAccelLeanX = 0f;

    // Landing detection
    private bool wasGrounded = true;
    private float highestY = 0f;

    // Perlin seeds
    private float breathNoiseSeed;

    // Cached reference
    private PlayerMovement playerMovement;

    void Start()
    {
        if (cameraTransform == null)
        {
            Camera cam = Camera.main;
            if (cam != null) cameraTransform = cam.transform;
        }

        if (characterController == null)
            characterController = GetComponent<CharacterController>();

        playerMovement = GetComponent<PlayerMovement>();

        if (characterController != null)
            highestY = transform.position.y;

        breathNoiseSeed = Random.Range(0f, 1000f);
    }

    void LateUpdate()
    {
        if (cameraTransform == null) return;

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        bool isMoving = Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f;
        bool isSprinting = Input.GetKey(KeyCode.LeftShift) && isMoving && vertical > 0.1f;
        bool isGrounded = characterController != null && characterController.isGrounded;

        // Detectar slide a través de PlayerMovement
        bool isSliding = playerMovement != null && playerMovement.IsSliding;
        float slideProgress = playerMovement != null ? playerMovement.SlideProgress : 0f;

        // Velocidad normalizada para escalado
        float normalizedSpeed = playerMovement != null ? playerMovement.NormalizedSpeed : 0f;

        // --- STRAFE TILT con overshoot ---
        float targetTiltZ = 0f;
        if (isSliding)
        {
            // Tilt durante slide + strafe sutil
            targetTiltZ = slideTiltZ + (-horizontal * strafeMaxTilt * 0.5f);
        }
        else if (enableStrafeTilt && isMoving && isGrounded)
        {
            float mult = isSprinting ? sprintTiltMultiplier : 1f;
            // Escalar con velocidad real para que sea proporcional
            float velScale = Mathf.Lerp(0.5f, 1f, normalizedSpeed);
            targetTiltZ = -horizontal * strafeMaxTilt * mult * velScale;
        }

        // SmoothDamp con overshoot natural
        float smoothTime = (Mathf.Abs(targetTiltZ) > Mathf.Abs(currentTiltZ))
            ? 1f / strafeTiltSpeed
            : 1f / strafeReturnSpeed;

        currentTiltZ = Mathf.SmoothDamp(currentTiltZ, targetTiltZ, ref tiltVelocity,
            smoothTime, Mathf.Infinity, Time.deltaTime);

        // Overshoot: cuando el target es 0, inyectar inercia extra
        if (Mathf.Abs(targetTiltZ) < 0.01f && Mathf.Abs(tiltVelocity) > 0.1f)
        {
            currentTiltZ += tiltVelocity * overshootAmount * Time.deltaTime;
        }

        // --- TILT DE ACELERACIÓN ---
        float currentHSpeed = horizontal;
        float currentFSpeed = vertical;

        float hAccel = (currentHSpeed - lastHorizontalSpeed) / Mathf.Max(Time.deltaTime, 0.001f);
        float fAccel = (currentFSpeed - lastForwardSpeed) / Mathf.Max(Time.deltaTime, 0.001f);

        lastHorizontalSpeed = currentHSpeed;
        lastForwardSpeed = currentFSpeed;

        // Tilt lateral por aceleración horizontal (lean hacia donde se acelera)
        float accelTiltTarget = Mathf.Clamp(-hAccel * accelerationTiltAmount * 0.1f,
            -maxAccelerationTilt, maxAccelerationTilt);
        smoothedAccelTiltZ = Mathf.Lerp(smoothedAccelTiltZ, accelTiltTarget,
            Time.deltaTime * accelerationTiltSpeed);

        // Lean por aceleración frontal (lean atrás al frenar, adelante al acelerar)
        float accelLeanTarget = Mathf.Clamp(fAccel * accelerationLeanAmount * 0.1f,
            -maxAccelerationTilt, maxAccelerationTilt);
        smoothedAccelLeanX = Mathf.Lerp(smoothedAccelLeanX, accelLeanTarget,
            Time.deltaTime * accelerationTiltSpeed);

        // Decaer aceleración a cero
        smoothedAccelTiltZ = Mathf.Lerp(smoothedAccelTiltZ, 0f, Time.deltaTime * accelerationTiltSpeed * 0.3f);
        smoothedAccelLeanX = Mathf.Lerp(smoothedAccelLeanX, 0f, Time.deltaTime * accelerationTiltSpeed * 0.3f);

        // --- SPRINT/SLIDE LEAN ---
        float targetLean;
        float leanSpeed;
        if (isSliding)
        {
            // Lean progresivo durante el slide (aumenta con el tiempo)
            targetLean = Mathf.Lerp(slideLeanAngle, slideMaxLeanAngle, slideProgress);
            leanSpeed = slideTiltSpeed;
        }
        else if (isSprinting)
        {
            // Lean proporcional a velocidad real
            targetLean = sprintLeanAngle * Mathf.Lerp(0.6f, 1f, normalizedSpeed);
            leanSpeed = sprintLeanSpeed;
        }
        else
        {
            // Lean muy sutil por velocidad (incluso caminando)
            targetLean = normalizedSpeed * velocityLeanScale;
            leanSpeed = sprintLeanSpeed;
        }
        currentLeanX = Mathf.Lerp(currentLeanX, targetLean, Time.deltaTime * leanSpeed);

        // --- LANDING TILT ---
        if (!isGrounded)
        {
            if (transform.position.y > highestY)
                highestY = transform.position.y;
        }

        if (!wasGrounded && isGrounded)
        {
            float fallDist = highestY - transform.position.y;
            if (fallDist > minFallForTilt)
            {
                landingTilt = Mathf.Clamp(fallDist * 0.8f, 0f, landingTiltAmount);
            }
            highestY = transform.position.y;
        }

        if (isGrounded && wasGrounded)
            highestY = transform.position.y;

        landingTilt = Mathf.Lerp(landingTilt, 0f, Time.deltaTime * landingTiltRecovery);

        // --- BREATHING con variación orgánica ---
        breathTimer += Time.deltaTime * breathingSpeed;

        // Perlin noise modula la amplitud del breathing para que sea orgánico
        float breathNoiseAmp = 1f + (Mathf.PerlinNoise(breathTimer * 0.3f + breathNoiseSeed, 0f) - 0.5f)
            * 2f * breathingOrganicVariation;

        float breathRoll = (Mathf.Sin(breathTimer * 1.1f) * breathingAmount +
                          Mathf.Sin(breathTimer * 0.6f) * breathingAmount * 0.4f) * breathNoiseAmp;

        // Breathing en pitch también (muy sutil)
        float breathPitch = Mathf.Sin(breathTimer * 0.7f) * breathingAmount * 0.15f * breathNoiseAmp;

        // --- COMBINAR ---
        float totalRoll = currentTiltZ + breathRoll + smoothedAccelTiltZ;
        float totalPitch = currentLeanX + landingTilt + breathPitch + smoothedAccelLeanX;

        // Aplicar SOLO roll + lean, preservando el pitch del mouse look
        Vector3 currentEuler = cameraTransform.localEulerAngles;
        cameraTransform.localRotation = Quaternion.Euler(
            currentEuler.x + totalPitch,
            currentEuler.y,
            totalRoll
        );

        wasGrounded = isGrounded;
    }

    /// <summary>
    /// Obtiene la inclinación lateral actual
    /// </summary>
    public float GetCurrentTiltZ()
    {
        return currentTiltZ;
    }
}
