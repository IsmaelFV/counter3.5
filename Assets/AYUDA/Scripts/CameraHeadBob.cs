using UnityEngine;

/// <summary>
/// Head Bobbing procedural avanzado de la cámara.
/// Patrón Lissajous con rotación procedural, impacto de pisada,
/// bobbing en Z (profundidad), escalado por velocidad real,
/// y micro-variaciones orgánicas para que nunca se sienta mecánico.
/// Sistema ADITIVO: aplica offsets sobre la posición base sin interferir
/// con otros sistemas (crouch, slide, etc.)
/// </summary>
public class CameraHeadBob : MonoBehaviour
{
    [Header("=== CONFIGURACION GENERAL ===")]
    [SerializeField] private bool enableHeadBob = true;

    [Header("Caminar")]
    [SerializeField] private float walkBobSpeed = 10f;
    [SerializeField] private float walkBobX = 0.025f;
    [SerializeField] private float walkBobY = 0.035f;

    [Header("Sprint")]
    [SerializeField] private float sprintBobSpeed = 15f;
    [SerializeField] private float sprintBobX = 0.04f;
    [SerializeField] private float sprintBobY = 0.06f;

    [Header("Agachado")]
    [SerializeField] private float crouchBobSpeed = 6f;
    [SerializeField] private float crouchBobX = 0.01f;
    [SerializeField] private float crouchBobY = 0.015f;

    [Header("=== ROTACIÓN PROCEDURAL ===")]
    [Tooltip("Roll (inclinación lateral) sincronizado con el bob horizontal")]
    [SerializeField] private float walkRollAmount = 0.6f;
    [SerializeField] private float sprintRollAmount = 1.8f;
    [Tooltip("Pitch (cabeceo) sincronizado con el bob vertical")]
    [SerializeField] private float walkPitchAmount = 0.3f;
    [SerializeField] private float sprintPitchAmount = 0.9f;

    [Header("=== IMPACTO DE PISADA ===")]
    [Tooltip("Énfasis extra en el punto más bajo del ciclo (simula peso de pisada)")]
    [SerializeField] private float stepImpactMultiplier = 1.4f;
    [Tooltip("Qué tan agudo es el impacto (mayor = pico más marcado)")]
    [SerializeField] private float stepImpactSharpness = 3f;

    [Header("=== BOB EN PROFUNDIDAD (Z) ===")]
    [Tooltip("Micro-movimiento adelante/atrás sincronizado con pisadas")]
    [SerializeField] private float walkBobZ = 0.005f;
    [SerializeField] private float sprintBobZ = 0.012f;

    [Header("=== ESCALADO POR VELOCIDAD ===")]
    [Tooltip("Intensidad del bob se escala con la velocidad real del jugador")]
    [SerializeField] private bool useVelocityScaling = true;
    [SerializeField] private float velocityScaleMin = 0.3f;
    [SerializeField] private float velocityScaleMax = 1.2f;

    [Header("=== MICRO-VARIACIÓN ORGÁNICA ===")]
    [Tooltip("Variación sutil de amplitud por ciclo para romper la repetición")]
    [SerializeField] private float organicVariation = 0.15f;

    [Header("=== SUAVIZADO ===")]
    [SerializeField] private float bobSmooth = 12f;
    [SerializeField] private float returnSmooth = 8f;
    [SerializeField] private float intensityLerpSpeed = 5f;
    [SerializeField] private float rotationSmooth = 10f;

    [Header("Referencias")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private CharacterController characterController;

    // Offset aditivo del bob - se aplica y remueve limpiamente cada frame
    private Vector3 currentBobOffset;
    private Vector3 targetBobOffset;
    private Vector3 lastAppliedOffset;
    private float timer = 0f;
    private float currentIntensity = 0f;

    // Rotación procedural
    private Vector3 currentBobRotation;
    private Vector3 targetBobRotation;

    // Velocidad real del jugador
    private Vector3 lastPosition;
    private float realSpeed;

    // Perlin seeds para variación orgánica
    private float noiseSeedAmp;
    private float noiseSeedSpeed;

    // Referencia a PlayerMovement
    private PlayerMovement playerMovement;

    void Start()
    {
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;

        if (characterController == null)
            characterController = GetComponentInParent<CharacterController>();

        playerMovement = GetComponentInParent<PlayerMovement>();

        currentBobOffset = Vector3.zero;
        targetBobOffset = Vector3.zero;
        lastAppliedOffset = Vector3.zero;
        currentBobRotation = Vector3.zero;
        targetBobRotation = Vector3.zero;
        lastPosition = transform.position;

        noiseSeedAmp = Random.Range(0f, 1000f);
        noiseSeedSpeed = Random.Range(0f, 1000f);
    }

    void LateUpdate()
    {
        if (!enableHeadBob || cameraTransform == null || characterController == null)
            return;

        // 1. Remover el offset del frame anterior para obtener la posicion base limpia
        cameraTransform.localPosition -= lastAppliedOffset;

        // Calcular velocidad real del jugador (horizontal solamente)
        Vector3 horizontalVel = transform.position - lastPosition;
        horizontalVel.y = 0f;
        float rawSpeed = horizontalVel.magnitude / Mathf.Max(Time.deltaTime, 0.001f);
        realSpeed = Mathf.Lerp(realSpeed, rawSpeed, Time.deltaTime * 8f);
        lastPosition = transform.position;

        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");
        bool isMoving = (Mathf.Abs(horizontalInput) > 0.1f || Mathf.Abs(verticalInput) > 0.1f);
        bool isGrounded = characterController.isGrounded;
        bool isSprinting = Input.GetKey(KeyCode.LeftShift) && isMoving && verticalInput > 0.1f;
        bool isCrouching = Input.GetKey(KeyCode.LeftControl);

        // Intensidad objetivo
        float targetIntensity = 0f;
        if (isMoving && isGrounded)
            targetIntensity = isSprinting ? 1.5f : (isCrouching ? 0.4f : 1f);

        currentIntensity = Mathf.Lerp(currentIntensity, targetIntensity, Time.deltaTime * intensityLerpSpeed);

        // Escalado por velocidad real
        float velocityScale = 1f;
        if (useVelocityScaling && playerMovement != null)
        {
            float normalizedVel = playerMovement.NormalizedSpeed;
            velocityScale = Mathf.Lerp(velocityScaleMin, velocityScaleMax, normalizedVel);
        }

        if (currentIntensity > 0.05f)
        {
            // Parámetros interpolados según estado
            float speed, bobX, bobY, bobZ, rollAmt, pitchAmt;

            if (currentIntensity > 1.2f)
            {
                float t = (currentIntensity - 1f) / 0.5f;
                speed = Mathf.Lerp(walkBobSpeed, sprintBobSpeed, t);
                bobX = Mathf.Lerp(walkBobX, sprintBobX, t);
                bobY = Mathf.Lerp(walkBobY, sprintBobY, t);
                bobZ = Mathf.Lerp(walkBobZ, sprintBobZ, t);
                rollAmt = Mathf.Lerp(walkRollAmount, sprintRollAmount, t);
                pitchAmt = Mathf.Lerp(walkPitchAmount, sprintPitchAmount, t);
            }
            else if (currentIntensity < 0.7f)
            {
                speed = crouchBobSpeed;
                bobX = crouchBobX;
                bobY = crouchBobY;
                bobZ = 0f;
                rollAmt = walkRollAmount * 0.3f;
                pitchAmt = walkPitchAmount * 0.2f;
            }
            else
            {
                speed = walkBobSpeed;
                bobX = walkBobX;
                bobY = walkBobY;
                bobZ = walkBobZ;
                rollAmt = walkRollAmount;
                pitchAmt = walkPitchAmount;
            }

            // Micro-variación orgánica por ciclo (Perlin noise modula amplitud Y velocidad)
            float noiseAmp = 1f + (Mathf.PerlinNoise(timer * 0.3f + noiseSeedAmp, 0f) - 0.5f) * 2f * organicVariation;
            float noiseSpd = 1f + (Mathf.PerlinNoise(timer * 0.2f + noiseSeedSpeed, 0f) - 0.5f) * 2f * organicVariation * 0.5f;

            timer += Time.deltaTime * speed * noiseSpd;

            // Aplicar escalado de velocidad
            bobX *= velocityScale * noiseAmp;
            bobY *= velocityScale * noiseAmp;
            bobZ *= velocityScale;

            // --- PATRÓN LISSAJOUS con impacto de pisada ---
            float hBob = Mathf.Cos(timer) * bobX;

            // Bob vertical con énfasis de impacto: sin(2t) + pico extra en el punto más bajo
            float rawVBob = Mathf.Sin(timer * 2f);
            // Impacto de pisada: acentuar los valles del seno con potencia
            float stepImpact = 0f;
            if (rawVBob < 0f)
            {
                // Solo en la fase descendente, crear un pico de impacto
                float normalizedDip = -rawVBob; // 0 a 1
                stepImpact = -Mathf.Pow(normalizedDip, stepImpactSharpness) * (stepImpactMultiplier - 1f);
            }
            float vBob = (rawVBob + stepImpact) * bobY;

            // Bob en Z sincronizado con pisadas (empuje sutil adelante/atrás)
            float zBob = Mathf.Sin(timer * 2f + 0.5f) * bobZ;

            targetBobOffset = new Vector3(hBob, vBob, zBob);

            // --- ROTACIÓN PROCEDURAL ---
            // Roll sincronizado con movimiento horizontal (cabeza se inclina con paso)
            float roll = Mathf.Cos(timer) * rollAmt * velocityScale;
            // Pitch sincronizado con vertical (cabeceo de pisada)
            float pitch = Mathf.Sin(timer * 2f) * pitchAmt * velocityScale;
            // Yaw mínimo para romper simetría
            float yaw = Mathf.Sin(timer * 0.5f) * rollAmt * 0.1f;

            targetBobRotation = new Vector3(pitch, yaw, roll);
        }
        else
        {
            timer = Mathf.Lerp(timer, 0f, Time.deltaTime * 3f);
            targetBobOffset = Vector3.zero;
            targetBobRotation = Vector3.zero;
        }

        // 2. Suavizar offset de posición
        float lerpRate = (targetBobOffset.sqrMagnitude > 0.001f) ? bobSmooth : returnSmooth;
        currentBobOffset = Vector3.Lerp(currentBobOffset, targetBobOffset, Time.deltaTime * lerpRate);

        // 2b. Suavizar rotación
        currentBobRotation = Vector3.Lerp(currentBobRotation, targetBobRotation, Time.deltaTime * rotationSmooth);

        // 3. Aplicar offset de posición
        lastAppliedOffset = currentBobOffset;
        cameraTransform.localPosition += lastAppliedOffset;

        // 4. Aplicar rotación aditivamente (sobre la rotación actual de la cámara)
        if (currentBobRotation.sqrMagnitude > 0.0001f)
        {
            Vector3 euler = cameraTransform.localEulerAngles;
            cameraTransform.localRotation = Quaternion.Euler(
                euler.x + currentBobRotation.x,
                euler.y + currentBobRotation.y,
                euler.z + currentBobRotation.z
            );
        }
    }

    /// <summary>
    /// Permite ajustar la intensidad del bobbing en runtime
    /// </summary>
    public void SetBobAmount(float amount)
    {
        walkBobY = amount;
    }

    /// <summary>
    /// Activa o desactiva el head bobbing
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        enableHeadBob = enabled;

        if (!enabled && cameraTransform != null)
        {
            // Remover offset residual
            cameraTransform.localPosition -= lastAppliedOffset;
            currentBobOffset = Vector3.zero;
            targetBobOffset = Vector3.zero;
            lastAppliedOffset = Vector3.zero;
            currentBobRotation = Vector3.zero;
            targetBobRotation = Vector3.zero;
            timer = 0f;
            currentIntensity = 0f;
        }
    }
}
