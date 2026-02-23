using UnityEngine;

/// <summary>
/// Sway procedural avanzado del arma.
/// Sistema basado en velocidad del ratón con inercia, momentum, overshoot spring,
/// tilt en Z al girar, breathing orgánico en idle, PLUS:
/// - Sway por velocidad de movimiento (el arma reacciona al WASD)
/// - Inercia de aceleración (arma se retrasa al cambiar de dirección)
/// - Comportamiento aéreo (arma flota y se mueve con menos fricción)
/// - Reducción contextual (menos sway corriendo, más al agacharse/apuntar)
/// </summary>
public class WeaponSway : MonoBehaviour
{
    [Header("=== SWAY DE POSICIÓN (Ratón) ===")]
    [SerializeField] private bool enableSway = true;
    [SerializeField] private float swayAmount = 0.035f;
    [SerializeField] private float maxSwayAmount = 0.08f;
    [SerializeField] private float swaySmoothSpeed = 8f;

    [Header("=== SWAY DE ROTACIÓN (Ratón) ===")]
    [SerializeField] private float rotationSwayAmount = 5f;
    [SerializeField] private float maxRotationSway = 8f;
    [SerializeField] private float rotationSmoothSpeed = 10f;

    [Header("=== TILT (Roll al girar) ===")]
    [Tooltip("El arma rota en Z al girar horizontalmente")]
    [SerializeField] private float tiltAmount = 4f;
    [SerializeField] private float maxTilt = 6f;
    [SerializeField] private float tiltSmoothSpeed = 6f;

    [Header("=== INERCIA / MOMENTUM ===")]
    [Tooltip("El arma sobrecompensa ligeramente antes de volver (overshoot)")]
    [SerializeField] private float inertiaDamping = 0.85f;
    [SerializeField] private float inertiaResponse = 15f;

    [Header("=== SWAY POR MOVIMIENTO (WASD) ===")]
    [Tooltip("El arma se mueve ligeramente en dirección opuesta al strafe")]
    [SerializeField] private float movementSwayAmount = 0.012f;
    [Tooltip("El arma se inclina al strafear")]
    [SerializeField] private float movementTiltAmount = 2f;
    [Tooltip("Suavizado del sway de movimiento")]
    [SerializeField] private float movementSwaySmoothSpeed = 6f;

    [Header("=== INERCIA DE ACELERACIÓN ===")]
    [Tooltip("El arma se retrasa al acelerar/frenar (lag de peso)")]
    [SerializeField] private float accelerationLagAmount = 0.008f;
    [Tooltip("Respuesta del lag de aceleración")]
    [SerializeField] private float accelerationLagSpeed = 5f;
    [SerializeField] private float maxAccelerationLag = 0.02f;

    [Header("=== COMPORTAMIENTO AÉREO ===")]
    [Tooltip("En el aire, el arma flota con menos restricción")]
    [SerializeField] private float airborneSwayMultiplier = 0.4f;
    [Tooltip("Velocidad vertical del arma en el aire (sube al saltar, baja al caer)")]
    [SerializeField] private float airborneVerticalResponse = 0.008f;
    [SerializeField] private float airborneVerticalMax = 0.025f;

    [Header("=== MODIFICADORES CONTEXTUALES ===")]
    [Tooltip("Multiplicador de sway al correr (menor = arma más estable)")]
    [SerializeField] private float sprintSwayMultiplier = 0.6f;
    [Tooltip("Multiplicador de sway al agacharse (mayor = arma más suelta)")]
    [SerializeField] private float crouchSwayMultiplier = 1.3f;

    [Header("=== BREATHING (Idle Sway) ===")]
    [SerializeField] private bool enableBreathing = true;
    [SerializeField] private float breathingSpeed = 1.2f;
    [SerializeField] private float breathingAmountX = 0.003f;
    [SerializeField] private float breathingAmountY = 0.005f;
    [SerializeField] private float breathingRotAmount = 0.6f;

    [Header("=== IDLE MICRO SWAY ===")]
    [Tooltip("Micro-movimiento con ruido Perlin en idle para que el arma nunca esté quieta")]
    [SerializeField] private float microSwayAmount = 0.001f;
    [SerializeField] private float microSwaySpeed = 2f;

    // Estado interno
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private float breathingTimer = 0f;

    // Velocidades suavizadas para inercia (ratón)
    private Vector2 smoothedMouseDelta;
    private Vector2 mouseVelocity;
    private float smoothedTilt;

    // Sway de movimiento
    private Vector3 smoothedMovementSway;
    private Vector3 targetMovementSway;
    private float smoothedMovementTilt;

    // Inercia de aceleración
    private Vector3 lastPlayerVelocity;
    private Vector3 accelerationLag;
    private Vector3 smoothedAccelerationLag;

    // Comportamiento aéreo
    private float airborneVerticalOffset;
    private float smoothedAirborneOffset;

    // Perlin noise seeds
    private float noiseOffsetX;
    private float noiseOffsetY;

    // Referencias
    private CharacterController characterController;
    private PlayerMovement playerMovement;

    void Start()
    {
        initialPosition = transform.localPosition;
        initialRotation = transform.localRotation;

        characterController = GetComponentInParent<CharacterController>();
        playerMovement = GetComponentInParent<PlayerMovement>();

        // Seeds aleatorios para que cada arma tenga micro-sway diferente
        noiseOffsetX = Random.Range(0f, 1000f);
        noiseOffsetY = Random.Range(0f, 1000f);

        lastPlayerVelocity = Vector3.zero;
    }

    void Update()
    {
        if (!enableSway) return;

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        // Estado del jugador
        bool isGrounded = characterController != null && characterController.isGrounded;
        bool isSprinting = playerMovement != null && playerMovement.IsSprinting;
        bool isCrouching = playerMovement != null && playerMovement.IsCrouching;
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // Multiplicador contextual
        float contextMultiplier = 1f;
        if (isSprinting) contextMultiplier = sprintSwayMultiplier;
        else if (isCrouching) contextMultiplier = crouchSwayMultiplier;
        if (!isGrounded) contextMultiplier *= airborneSwayMultiplier;

        // --- INERCIA / MOMENTUM (Ratón) ---
        Vector2 targetDelta = new Vector2(mouseX, mouseY);
        mouseVelocity = Vector2.Lerp(mouseVelocity, targetDelta, Time.deltaTime * inertiaResponse);
        smoothedMouseDelta = Vector2.Lerp(smoothedMouseDelta, mouseVelocity, Time.deltaTime * inertiaResponse * inertiaDamping);

        // --- POSICIÓN POR RATÓN ---
        float swayX = -smoothedMouseDelta.x * swayAmount * contextMultiplier;
        float swayY = -smoothedMouseDelta.y * swayAmount * contextMultiplier;
        swayX = Mathf.Clamp(swayX, -maxSwayAmount, maxSwayAmount);
        swayY = Mathf.Clamp(swayY, -maxSwayAmount, maxSwayAmount);

        Vector3 targetPosition = initialPosition + new Vector3(swayX, swayY, 0f);

        // --- SWAY POR MOVIMIENTO (WASD) ---
        targetMovementSway = new Vector3(
            -horizontal * movementSwayAmount,
            0f,
            -vertical * movementSwayAmount * 0.5f
        );

        smoothedMovementSway = Vector3.Lerp(smoothedMovementSway, targetMovementSway,
            Time.deltaTime * movementSwaySmoothSpeed);

        targetPosition += smoothedMovementSway;

        // --- INERCIA DE ACELERACIÓN ---
        if (playerMovement != null)
        {
            Vector3 currentVel = new Vector3(
                playerMovement.HorizontalInput,
                0f,
                playerMovement.VerticalInput
            );

            Vector3 accel = (currentVel - lastPlayerVelocity) / Mathf.Max(Time.deltaTime, 0.001f);
            lastPlayerVelocity = currentVel;

            // El lag va en dirección opuesta a la aceleración
            Vector3 targetLag = new Vector3(
                -accel.x * accelerationLagAmount,
                0f,
                -accel.z * accelerationLagAmount
            );

            // Clamp
            targetLag.x = Mathf.Clamp(targetLag.x, -maxAccelerationLag, maxAccelerationLag);
            targetLag.z = Mathf.Clamp(targetLag.z, -maxAccelerationLag, maxAccelerationLag);

            smoothedAccelerationLag = Vector3.Lerp(smoothedAccelerationLag, targetLag,
                Time.deltaTime * accelerationLagSpeed);

            targetPosition += smoothedAccelerationLag;
        }

        // --- COMPORTAMIENTO AÉREO ---
        if (!isGrounded && characterController != null)
        {
            // Usar velocidad vertical del CharacterController via PlayerMovement
            float verticalVel = characterController.velocity.y;
            float targetAirOffset = Mathf.Clamp(
                verticalVel * airborneVerticalResponse,
                -airborneVerticalMax,
                airborneVerticalMax
            );
            smoothedAirborneOffset = Mathf.Lerp(smoothedAirborneOffset, targetAirOffset,
                Time.deltaTime * 5f);
        }
        else
        {
            smoothedAirborneOffset = Mathf.Lerp(smoothedAirborneOffset, 0f, Time.deltaTime * 8f);
        }
        targetPosition.y += smoothedAirborneOffset;

        // --- BREATHING ---
        if (enableBreathing)
        {
            breathingTimer += Time.deltaTime * breathingSpeed;
            float breathX = Mathf.Sin(breathingTimer * 0.7f) * breathingAmountX +
                           Mathf.Sin(breathingTimer * 1.3f) * breathingAmountX * 0.5f;
            float breathY = Mathf.Sin(breathingTimer) * breathingAmountY +
                           Mathf.Cos(breathingTimer * 0.6f) * breathingAmountY * 0.3f;
            targetPosition.x += breathX;
            targetPosition.y += breathY;
        }

        // --- MICRO SWAY (Perlin noise) ---
        float microX = (Mathf.PerlinNoise(Time.time * microSwaySpeed + noiseOffsetX, 0f) - 0.5f) * 2f * microSwayAmount;
        float microY = (Mathf.PerlinNoise(0f, Time.time * microSwaySpeed + noiseOffsetY) - 0.5f) * 2f * microSwayAmount;
        targetPosition.x += microX;
        targetPosition.y += microY;

        // Aplicar posición con suavizado
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, Time.deltaTime * swaySmoothSpeed);

        // --- ROTACIÓN POR RATÓN ---
        float rotX = -smoothedMouseDelta.y * rotationSwayAmount * contextMultiplier;
        float rotY = smoothedMouseDelta.x * rotationSwayAmount * contextMultiplier;
        rotX = Mathf.Clamp(rotX, -maxRotationSway, maxRotationSway);
        rotY = Mathf.Clamp(rotY, -maxRotationSway, maxRotationSway);

        // --- TILT (Roll) por ratón ---
        float targetTilt = -smoothedMouseDelta.x * tiltAmount * contextMultiplier;
        targetTilt = Mathf.Clamp(targetTilt, -maxTilt, maxTilt);
        smoothedTilt = Mathf.Lerp(smoothedTilt, targetTilt, Time.deltaTime * tiltSmoothSpeed);

        // --- TILT por movimiento lateral ---
        float moveTiltTarget = -horizontal * movementTiltAmount;
        smoothedMovementTilt = Mathf.Lerp(smoothedMovementTilt, moveTiltTarget,
            Time.deltaTime * movementSwaySmoothSpeed);

        // Breathing rotation
        float breathRot = 0f;
        if (enableBreathing)
        {
            breathRot = Mathf.Sin(breathingTimer * 0.8f) * breathingRotAmount;
        }

        Quaternion targetRotation = initialRotation * Quaternion.Euler(
            rotX + breathRot,
            rotY,
            smoothedTilt + smoothedMovementTilt
        );

        // Aplicar rotación con suavizado
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime * rotationSmoothSpeed);
    }

    /// <summary>
    /// Permite ajustar la intensidad del sway en runtime
    /// </summary>
    public void SetSwayAmount(float amount)
    {
        swayAmount = amount;
    }

    /// <summary>
    /// Activa o desactiva el weapon sway
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        enableSway = enabled;

        if (!enabled)
        {
            transform.localPosition = initialPosition;
            transform.localRotation = initialRotation;
            smoothedMouseDelta = Vector2.zero;
            mouseVelocity = Vector2.zero;
            smoothedTilt = 0f;
            smoothedMovementSway = Vector3.zero;
            smoothedAccelerationLag = Vector3.zero;
            smoothedAirborneOffset = 0f;
            smoothedMovementTilt = 0f;
        }
    }

    /// <summary>
    /// Resetea la posición del arma (útil al cambiar de arma)
    /// </summary>
    public void ResetPosition()
    {
        initialPosition = transform.localPosition;
        initialRotation = transform.localRotation;
        breathingTimer = 0f;
        smoothedMouseDelta = Vector2.zero;
        mouseVelocity = Vector2.zero;
        smoothedTilt = 0f;
        smoothedMovementSway = Vector3.zero;
        smoothedAccelerationLag = Vector3.zero;
        smoothedAirborneOffset = 0f;
        smoothedMovementTilt = 0f;
        lastPlayerVelocity = Vector3.zero;

        // Nuevas seeds
        noiseOffsetX = Random.Range(0f, 1000f);
        noiseOffsetY = Random.Range(0f, 1000f);
    }
}
