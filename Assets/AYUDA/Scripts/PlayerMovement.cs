using UnityEngine;

/// <summary>
/// Sistema completo de movimiento FPS para el jugador.
/// Incluye: WASD, mouse look, sprint, agacharse, salto y gravedad.
/// Requiere: CharacterController en el mismo GameObject.
/// Jerarquía: Player (este script + CharacterController) → CameraHolder → MainCamera
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    // =========================================================================
    // CONFIGURACIÓN
    // =========================================================================

    [Header("=== MOVIMIENTO ===")]
    [SerializeField] private float walkSpeed = 4f;
    [SerializeField] private float sprintSpeed = 7f;
    [SerializeField] private float crouchSpeed = 2f;
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float deceleration = 12f;

    [Header("=== SALTO Y GRAVEDAD ===")]
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private LayerMask groundMask = ~0; // Todas las capas por defecto
    [SerializeField] private float coyoteTime = 0.12f; // Tiempo extra para saltar tras dejar el suelo
    [SerializeField] private float jumpBufferTime = 0.15f; // Buffer de input de salto

    [Header("=== MOUSE LOOK ===")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float maxLookAngle = 85f;
    [SerializeField] private bool invertY = false;

    [Header("=== AGACHARSE (CROUCH) ===")]
    [SerializeField] private float standingHeight = 2f;
    [SerializeField] private float crouchHeight = 1.2f;
    [SerializeField] private float crouchTransitionSpeed = 8f;
    [SerializeField] private KeyCode crouchKey = KeyCode.LeftControl;

    [Header("=== SLIDE ===")]
    [Tooltip("Velocidad inicial del slide (se hereda del sprint)")]
    [SerializeField] private float slideSpeed = 10f;
    [Tooltip("Duración máxima del slide en segundos")]
    [SerializeField] private float slideDuration = 0.8f;
    [Tooltip("Velocidad mínima para que el slide no termine")]
    [SerializeField] private float slideMinSpeed = 3f;
    [Tooltip("Fricción inicial del slide")]
    [SerializeField] private float slideFriction = 8f;
    [Tooltip("Fricción máxima al final del slide (aumenta progresivamente)")]
    [SerializeField] private float slideMaxFriction = 18f;
    [Tooltip("Cooldown entre slides")]
    [SerializeField] private float slideCooldown = 0.5f;
    [Tooltip("Altura del collider durante el slide")]
    [SerializeField] private float slideHeight = 0.8f;
    [Tooltip("Boost de velocidad extra al iniciar el slide")]
    [SerializeField] private float slideBoost = 2f;
    [Tooltip("El slide se beneficia de pendientes hacia abajo")]
    [SerializeField] private float slopeSlideBonus = 5f;
    [SerializeField] private AudioClip slideSound;

    [Header("=== SPRINT ===")]
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
    [SerializeField] private bool canSprintWhileCrouching = false;
    [SerializeField] private bool canSprintBackwards = false;

    [Header("=== MANTLING (Escalar obstáculos) ===")]
    [Tooltip("¿Activar mantling automático?")]
    [SerializeField] private bool enableMantling = true;
    [Tooltip("Altura máxima de obstáculo que se puede escalar")]
    [SerializeField] private float mantleMaxHeight = 1.8f;
    [Tooltip("Altura mínima para activar mantling (no escalar escalones)")]
    [SerializeField] private float mantleMinHeight = 0.5f;
    [Tooltip("Distancia de detección frontal del obstáculo")]
    [SerializeField] private float mantleCheckDistance = 0.8f;
    [Tooltip("Velocidad del movimiento de mantling")]
    [SerializeField] private float mantleSpeed = 8f;
    [Tooltip("Capas que se pueden escalar")]
    [SerializeField] private LayerMask mantleLayerMask = ~0;

    [Header("=== REFERENCIAS ===")]
    [SerializeField] private Transform cameraHolder;
    [Tooltip("Si no se asigna, se busca Camera.main automáticamente")]
    [SerializeField] private Camera playerCamera;

    [Header("=== SONIDOS DE PASOS ===")]
    [SerializeField] private AudioSource footstepAudioSource;
    [SerializeField] private AudioClip[] footstepSounds;
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip landSound;
    [SerializeField] private float walkStepInterval = 0.5f;
    [SerializeField] private float sprintStepInterval = 0.3f;
    [SerializeField] private float crouchStepInterval = 0.7f;
    [SerializeField, Range(0f, 1f)] private float footstepVolume = 0.5f;

    // =========================================================================
    // COMPONENTES Y ESTADO INTERNO
    // =========================================================================

    private CharacterController controller;
    private Vector3 velocity;
    private float verticalVelocity;
    private float currentSpeed;
    private float targetSpeed;

    // Mouse Look
    private float xRotation = 0f;
    private float yRotation = 0f;
    private Vector2 currentMouseDelta;
    private Vector2 currentMouseDeltaVelocity;

    // Ground check
    private bool isGrounded;
    private bool wasGroundedLastFrame;
    private float lastGroundedTime;
    private float lastJumpPressTime;

    // Crouch
    private bool isCrouching;
    private float currentHeight;
    private Vector3 cameraStandPos;
    private Vector3 cameraCrouchPos;

    // Slide
    private bool isSliding;
    private float slideTimer;
    private float slideCooldownTimer;
    private Vector3 slideDirection;
    private float slideCurrentSpeed;
    private Vector3 cameraSlidePos;

    // Sprint
    private bool isSprinting;

    // Mantling
    private bool isMantling = false;
    private Vector3 mantleStartPos;
    private Vector3 mantleEndPos;
    private float mantleProgress = 0f;
    private float mantleTotalDistance;

    // Footsteps
    private float footstepTimer;
    private int lastFootstepIndex = -1;

    // Estado público para otros scripts
    private bool isMoving;
    private bool isAlive = true;

    // =========================================================================
    // PROPIEDADES PÚBLICAS (solo lectura)
    // =========================================================================

    /// <summary>¿Está en el suelo?</summary>
    public bool IsGrounded => isGrounded;
    /// <summary>¿Está moviéndose?</summary>
    public bool IsMoving => isMoving;
    /// <summary>¿Está esprintando?</summary>
    public bool IsSprinting => isSprinting;
    /// <summary>¿Está agachado?</summary>
    public bool IsCrouching => isCrouching;
    /// <summary>¿Está desliándose?</summary>
    public bool IsSliding => isSliding;
    /// <summary>¿Está escalando un obstáculo?</summary>
    public bool IsMantling => isMantling;
    /// <summary>Progreso del slide (0-1)</summary>
    public float SlideProgress => isSliding ? (slideTimer / slideDuration) : 0f;
    /// <summary>Velocidad actual normalizada (0-1)</summary>
    public float NormalizedSpeed => Mathf.InverseLerp(0f, sprintSpeed, currentSpeed);
    /// <summary>Velocidad actual en unidades/segundo</summary>
    public float CurrentSpeed => currentSpeed;
    /// <summary>Input horizontal del jugador</summary>
    public float HorizontalInput { get; private set; }
    /// <summary>Input vertical del jugador</summary>
    public float VerticalInput { get; private set; }

    // =========================================================================
    // INICIALIZACIÓN
    // =========================================================================

    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    void Start()
    {
        // Buscar cámara si no está asignada
        if (playerCamera == null)
            playerCamera = Camera.main;

        // Buscar CameraHolder si no está asignado
        // Si la cámara es hija directa del Player (sin CameraHolder intermedio),
        // usamos el transform de la cámara directamente
        if (cameraHolder == null && playerCamera != null)
        {
            Transform parent = playerCamera.transform.parent;
            if (parent != null && parent != transform)
                cameraHolder = parent; // Hay un CameraHolder intermedio
            else
                cameraHolder = playerCamera.transform; // Cámara directa bajo el Player
        }

        // Configurar cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Capturar rotación inicial del jugador
        yRotation = transform.eulerAngles.y;

        // Guardar altura original
        standingHeight = controller.height;
        currentHeight = standingHeight;

        // Calcular posiciones de cámara
        if (cameraHolder != null)
        {
            cameraStandPos = cameraHolder.localPosition;
            float heightDiff = standingHeight - crouchHeight;
            cameraCrouchPos = cameraStandPos - new Vector3(0f, heightDiff * 0.5f, 0f);
            float slideHeightDiff = standingHeight - slideHeight;
            cameraSlidePos = cameraStandPos - new Vector3(0f, slideHeightDiff * 0.5f, 0f);
        }

        // AudioSource
        if (footstepAudioSource == null)
            footstepAudioSource = GetComponent<AudioSource>();
    }

    // =========================================================================
    // LOOP PRINCIPAL
    // =========================================================================

    void Update()
    {
        if (!isAlive) return;

        // Si está haciendo mantling, solo procesar eso
        if (isMantling)
        {
            HandleMantlingMovement();
            return;
        }

        CheckGround();
        HandleMouseLook();
        HandleMovement();
        HandleJump();
        HandleCrouch();
        HandleSlide();
        HandleSprint();
        HandleMantling();
        ApplyGravity();
        ApplyMovement();
        HandleFootsteps();
        HandlePHDSlide();
    }

    // =========================================================================
    // GROUND CHECK
    // =========================================================================

    private void CheckGround()
    {
        wasGroundedLastFrame = isGrounded;

        // Usar el isGrounded del CharacterController + un raycast extra para mayor fiabilidad
        isGrounded = controller.isGrounded;

        // Fallback con SphereCast si CharacterController falla
        if (!isGrounded)
        {
            float radius = controller.radius * 0.9f;
            Vector3 origin = transform.position + Vector3.up * (radius + 0.01f);
            isGrounded = Physics.SphereCast(origin, radius, Vector3.down, 
                out _, groundCheckDistance + 0.01f, groundMask, QueryTriggerInteraction.Ignore);
        }

        // Actualizar coyote time
        if (isGrounded)
        {
            lastGroundedTime = Time.time;

            // Detectar aterrizaje (estaba en aire, ahora en suelo)
            if (!wasGroundedLastFrame)
            {
                OnLanded();
            }
        }
    }

    // =========================================================================
    // MOUSE LOOK
    // =========================================================================

    private void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        if (invertY) mouseY = -mouseY;

        // Rotación vertical — clamped para no girar 360
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -maxLookAngle, maxLookAngle);

        // Rotación horizontal — acumular en yRotation
        yRotation += mouseX;

        // Aplicar rotación horizontal al CUERPO del jugador
        transform.rotation = Quaternion.Euler(0f, yRotation, 0f);

        // Aplicar rotación vertical a la CÁMARA directamente (no al cameraHolder)
        // Así funciona sin importar la jerarquía
        if (playerCamera != null)
        {
            playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
    }

    // =========================================================================
    // MOVIMIENTO
    // =========================================================================

    private void HandleMovement()
    {
        // Leer input
        HorizontalInput = Input.GetAxisRaw("Horizontal");
        VerticalInput = Input.GetAxisRaw("Vertical");

        isMoving = Mathf.Abs(HorizontalInput) > 0.1f || Mathf.Abs(VerticalInput) > 0.1f;

        // Si está en slide, el movimiento lo controla HandleSlide
        if (isSliding) return;

        // Calcular velocidad objetivo
        if (isMoving)
        {
            if (isCrouching)
                targetSpeed = crouchSpeed;
            else if (isSprinting)
                targetSpeed = sprintSpeed;
            else
                targetSpeed = walkSpeed;
        }
        else
        {
            targetSpeed = 0f;
        }

        // Aceleración/deceleración suave
        float lerpRate = (targetSpeed > currentSpeed) ? acceleration : deceleration;
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, lerpRate * Time.deltaTime);

        // Calcular dirección de movimiento (relativa al jugador)
        Vector3 moveDirection = (transform.right * HorizontalInput + transform.forward * VerticalInput);
        
        // Normalizar para que diagonal no sea más rápida
        if (moveDirection.sqrMagnitude > 1f)
            moveDirection.Normalize();

        velocity = moveDirection * currentSpeed;
    }

    // =========================================================================
    // SALTO
    // =========================================================================

    private void HandleJump()
    {
        // Buffer de input de salto
        if (Input.GetKeyDown(KeyCode.Space))
        {
            lastJumpPressTime = Time.time;
        }

        // Intentar saltar: coyote time + jump buffer
        bool canJump = (Time.time - lastGroundedTime <= coyoteTime) && 
                       (Time.time - lastJumpPressTime <= jumpBufferTime);

        if (canJump && verticalVelocity <= 0f && !isCrouching)
        {
            verticalVelocity = jumpForce;
            lastJumpPressTime = -1f; // Consumir el buffer
            lastGroundedTime = -1f;  // Consumir el coyote time

            // Sonido de salto
            PlaySound(jumpSound);
        }
    }

    // =========================================================================
    // GRAVEDAD
    // =========================================================================

    private void ApplyGravity()
    {
        if (isGrounded && verticalVelocity < 0f)
        {
            // Fuerza mínima hacia abajo para mantener el controller pegado al suelo
            verticalVelocity = -2f;
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }
    }

    // =========================================================================
    // APLICAR MOVIMIENTO FINAL
    // =========================================================================

    private void ApplyMovement()
    {
        Vector3 finalMove = velocity + Vector3.up * verticalVelocity;
        controller.Move(finalMove * Time.deltaTime);
    }

    // =========================================================================
    // AGACHARSE
    // =========================================================================

    private void HandleCrouch()
    {
        bool wantsCrouch = Input.GetKey(crouchKey);

        // Si está en slide, no procesar crouch normal
        if (isSliding)
        {
            // El slide termina cuando se suelta crouch (si hay espacio) o se acaba el timer
            if (!wantsCrouch && !IsHeadBlocked())
            {
                EndSlide();
                return;
            }

            // Transición de altura durante slide
            controller.height = Mathf.Lerp(controller.height, slideHeight,
                crouchTransitionSpeed * 2f * Time.deltaTime);
            controller.center = Vector3.up * (controller.height * 0.5f);

            if (cameraHolder != null)
            {
                cameraHolder.localPosition = Vector3.Lerp(
                    cameraHolder.localPosition, cameraSlidePos,
                    crouchTransitionSpeed * 2f * Time.deltaTime);
            }
            return;
        }

        // Iniciar slide: si se pulsa crouch mientras se sprinta
        if (wantsCrouch && !isCrouching && isSprinting && isGrounded && slideCooldownTimer <= 0f)
        {
            StartSlide();
            return;
        }

        if (wantsCrouch && !isCrouching)
        {
            isCrouching = true;
        }
        else if (!wantsCrouch && isCrouching)
        {
            if (!IsHeadBlocked())
            {
                isCrouching = false;
            }
        }

        // Transición suave de altura
        float targetHeight = isCrouching ? crouchHeight : standingHeight;
        float previousHeight = controller.height;

        controller.height = Mathf.Lerp(controller.height, targetHeight, 
            crouchTransitionSpeed * Time.deltaTime);

        controller.center = Vector3.up * (controller.height * 0.5f);

        if (cameraHolder != null)
        {
            Vector3 targetCamPos = isCrouching ? cameraCrouchPos : cameraStandPos;
            cameraHolder.localPosition = Vector3.Lerp(
                cameraHolder.localPosition, targetCamPos, 
                crouchTransitionSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// Verifica si hay algo arriba que impida levantarse
    /// </summary>
    private bool IsHeadBlocked()
    {
        float checkHeight = standingHeight - crouchHeight;
        Vector3 origin = transform.position + Vector3.up * crouchHeight;
        return Physics.Raycast(origin, Vector3.up, checkHeight + 0.1f, groundMask, 
            QueryTriggerInteraction.Ignore);
    }

    // =========================================================================
    // SLIDE
    // =========================================================================

    /// <summary>
    /// Inicia el slide: bloquea dirección del sprint y lanza al jugador hacia adelante
    /// </summary>
    private void StartSlide()
    {
        isSliding = true;
        isCrouching = true;
        isSprinting = false;
        slideTimer = 0f;

        // Dirección del slide = dirección actual de movimiento
        slideDirection = (transform.right * HorizontalInput + transform.forward * VerticalInput).normalized;
        if (slideDirection.sqrMagnitude < 0.1f)
            slideDirection = transform.forward;

        // Velocidad inicial = máximo entre slideSpeed y sprint + boost
        slideCurrentSpeed = Mathf.Max(slideSpeed, sprintSpeed + slideBoost);

        // Sonido de slide
        PlaySound(slideSound);
    }

    /// <summary>
    /// Finaliza el slide y transiciona a crouch o standing
    /// </summary>
    private void EndSlide()
    {
        isSliding = false;
        slideCooldownTimer = slideCooldown;

        // Si sigue pulsando crouch, quedarse agachado
        if (Input.GetKey(crouchKey))
        {
            isCrouching = true;
        }
        else if (!IsHeadBlocked())
        {
            isCrouching = false;
        }

        // Transicionar la velocidad actual al movimiento normal
        currentSpeed = Mathf.Min(slideCurrentSpeed, walkSpeed);
    }

    /// <summary>
    /// Gestiona la física del slide cada frame
    /// </summary>
    private void HandleSlide()
    {
        // Cooldown
        if (slideCooldownTimer > 0f)
            slideCooldownTimer -= Time.deltaTime;

        if (!isSliding) return;

        slideTimer += Time.deltaTime;

        // Bonus por pendiente descendente
        float slopeBonus = 0f;
        if (isGrounded)
        {
            RaycastHit slopeHit;
            if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out slopeHit, 1.5f, groundMask))
            {
                float slopeAngle = Vector3.Angle(slopeHit.normal, Vector3.up);
                float slopeDot = Vector3.Dot(slideDirection, Vector3.down);
                if (slopeDot > 0f && slopeAngle > 5f)
                {
                    slopeBonus = slopeSlideBonus * slopeDot * (slopeAngle / 45f);
                }
            }
        }

        // Fricción creciente con el tiempo (más difícil mantener velocidad al final)
        float slideProgress = slideTimer / slideDuration;
        float currentFriction = Mathf.Lerp(slideFriction, slideMaxFriction, slideProgress * slideProgress);
        
        // Aplicar fricción (desacelerar) o bonus de pendiente
        slideCurrentSpeed -= (currentFriction - slopeBonus) * Time.deltaTime;

        // Terminar slide si se acabó el tiempo, la velocidad es muy baja, o dejó el suelo
        if (slideTimer >= slideDuration || slideCurrentSpeed <= slideMinSpeed || !isGrounded)
        {
            EndSlide();
            return;
        }

        // Permitir pequeño control direccional durante el slide (strafe sutil)
        float strafeInfluence = 0.15f;
        Vector3 strafe = transform.right * HorizontalInput * strafeInfluence;
        Vector3 finalDirection = (slideDirection + strafe).normalized;

        // Aplicar movimiento del slide
        velocity = finalDirection * slideCurrentSpeed;
        currentSpeed = slideCurrentSpeed;

        // Saltar cancela el slide
        if (Input.GetKeyDown(KeyCode.Space))
        {
            EndSlide();
            isCrouching = false;
            verticalVelocity = jumpForce;
            PlaySound(jumpSound);
        }
    }

    // =========================================================================
    // SPRINT
    // =========================================================================

    private void HandleSprint()
    {
        // No esprintar durante slide
        if (isSliding) return;

        bool wantsSprint = Input.GetKey(sprintKey);

        // Condiciones para esprintar
        isSprinting = wantsSprint 
            && isMoving 
            && isGrounded
            && (!isCrouching || canSprintWhileCrouching)
            && (canSprintBackwards || VerticalInput > 0.1f);

        // Quitar agachado si se empieza a esprintar
        if (isSprinting && isCrouching && !canSprintWhileCrouching)
        {
            if (!IsHeadBlocked())
                isCrouching = false;
        }
    }

    // =========================================================================
    // SONIDOS DE PASOS
    // =========================================================================

    private void HandleFootsteps()
    {
        if (!isMoving || !isGrounded || footstepSounds == null || footstepSounds.Length == 0)
        {
            footstepTimer = 0f;
            return;
        }

        // Intervalo según velocidad
        float stepInterval = isSprinting ? sprintStepInterval 
            : (isCrouching ? crouchStepInterval : walkStepInterval);

        footstepTimer += Time.deltaTime;

        if (footstepTimer >= stepInterval)
        {
            footstepTimer = 0f;
            PlayFootstep();
        }
    }

    private void PlayFootstep()
    {
        if (footstepSounds.Length == 0 || footstepAudioSource == null) return;

        // Elegir sonido aleatorio sin repetir el anterior
        int index;
        if (footstepSounds.Length > 1)
        {
            do { index = Random.Range(0, footstepSounds.Length); }
            while (index == lastFootstepIndex);
        }
        else
        {
            index = 0;
        }

        lastFootstepIndex = index;
        footstepAudioSource.PlayOneShot(footstepSounds[index], footstepVolume);
    }

    // =========================================================================
    // EVENTOS
    // =========================================================================

    private void OnLanded()
    {
        PlaySound(landSound, footstepVolume * 0.8f);
    }

    // =========================================================================
    // UTILIDADES
    // =========================================================================

    private void PlaySound(AudioClip clip, float volume = -1f)
    {
        if (clip == null || footstepAudioSource == null) return;
        footstepAudioSource.PlayOneShot(clip, volume < 0 ? footstepVolume : volume);
    }

    /// <summary>
    /// Desactiva el movimiento del jugador (ej: al morir)
    /// </summary>
    public void DisableMovement()
    {
        isAlive = false;
        velocity = Vector3.zero;
        verticalVelocity = 0f;
    }

    /// <summary>
    /// Reactiva el movimiento del jugador
    /// </summary>
    public void EnableMovement()
    {
        isAlive = true;
    }

    /// <summary>
    /// Cambia la sensibilidad del ratón en runtime
    /// </summary>
    public void SetMouseSensitivity(float sensitivity)
    {
        mouseSensitivity = Mathf.Max(0.1f, sensitivity);
    }

    /// <summary>
    /// Obtiene la sensibilidad actual del ratón
    /// </summary>
    public float GetMouseSensitivity()
    {
        return mouseSensitivity;
    }

    /// <summary>
    /// Aplica un impulso vertical (ej: trampolines, knockback)
    /// </summary>
    public void AddVerticalImpulse(float force)
    {
        verticalVelocity = force;
    }

    /// <summary>
    /// Dibuja gizmos de debug en el editor
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if (controller == null) controller = GetComponent<CharacterController>();
        if (controller == null) return;

        // Dibujar esfera de ground check
        Gizmos.color = isGrounded ? Color.green : Color.red;
        float radius = controller.radius * 0.9f;
        Vector3 origin = transform.position + Vector3.up * radius;
        Gizmos.DrawWireSphere(origin + Vector3.down * groundCheckDistance, radius);

        // Dibujar rango de mantling
        if (enableMantling)
        {
            Gizmos.color = Color.cyan;
            Vector3 mantleLow = transform.position + Vector3.up * mantleMinHeight + transform.forward * mantleCheckDistance;
            Vector3 mantleHigh = transform.position + Vector3.up * mantleMaxHeight + transform.forward * mantleCheckDistance;
            Gizmos.DrawLine(mantleLow, mantleHigh);
            Gizmos.DrawWireSphere(mantleLow, 0.1f);
            Gizmos.DrawWireSphere(mantleHigh, 0.1f);
        }
    }

    // =========================================================================
    // MANTLING (Escalar obstáculos)
    // =========================================================================

    /// <summary>
    /// Detecta si hay un obstáculo escalable delante y lo escala
    /// </summary>
    private void HandleMantling()
    {
        if (!enableMantling) return;
        if (isCrouching || isSliding || isMantling) return;
        if (!Input.GetKeyDown(KeyCode.Space)) return;
        if (isGrounded) return; // Solo en aire (ya saltó)

        // Raycast frontal a la altura del pecho para detectar obstáculo
        Vector3 chestOrigin = transform.position + Vector3.up * (mantleMinHeight + 0.1f);
        
        if (!Physics.Raycast(chestOrigin, transform.forward, out RaycastHit wallHit, 
            mantleCheckDistance, mantleLayerMask, QueryTriggerInteraction.Ignore))
        {
            return; // No hay obstáculo delante
        }

        // Raycast hacia abajo desde arriba del obstáculo para encontrar la superficie superior
        Vector3 topCheckOrigin = wallHit.point + transform.forward * 0.1f + Vector3.up * (mantleMaxHeight - mantleMinHeight + 0.5f);
        
        if (!Physics.Raycast(topCheckOrigin, Vector3.down, out RaycastHit topHit, 
            mantleMaxHeight + 0.5f, mantleLayerMask, QueryTriggerInteraction.Ignore))
        {
            return; // No se encontró superficie superior
        }

        // Verificar que la altura está en rango
        float surfaceHeight = topHit.point.y - transform.position.y;
        if (surfaceHeight < mantleMinHeight || surfaceHeight > mantleMaxHeight)
        {
            return; // Muy bajo (escalón) o muy alto
        }

        // Verificar que hay espacio arriba para el jugador
        Vector3 standPos = topHit.point + Vector3.up * (standingHeight * 0.5f + 0.1f);
        if (Physics.CheckCapsule(topHit.point + Vector3.up * 0.1f, 
            standPos, controller.radius * 0.8f, mantleLayerMask, QueryTriggerInteraction.Ignore))
        {
            return; // No hay espacio arriba
        }

        // ¡Iniciar mantling!
        StartMantling(topHit.point);
    }

    /// <summary>
    /// Inicia el movimiento de mantling
    /// </summary>
    private void StartMantling(Vector3 targetSurface)
    {
        isMantling = true;
        mantleProgress = 0f;
        mantleStartPos = transform.position;
        // Posición final: sobre la superficie + un poco hacia delante
        mantleEndPos = targetSurface + Vector3.up * 0.1f + transform.forward * 0.3f;
        mantleTotalDistance = Vector3.Distance(mantleStartPos, mantleEndPos);

        // Resetear velocity
        velocity = Vector3.zero;
        verticalVelocity = 0f;
    }

    /// <summary>
    /// Ejecuta el movimiento suave de mantling (curva bezier)
    /// </summary>
    private void HandleMantlingMovement()
    {
        mantleProgress += mantleSpeed * Time.deltaTime / mantleTotalDistance;

        if (mantleProgress >= 1f)
        {
            // Finalizar mantling
            mantleProgress = 1f;
            transform.position = mantleEndPos;
            isMantling = false;
            verticalVelocity = 0f;
            return;
        }

        // Curva bezier para movimiento suave: sube, pasa por encima, aterriza
        float t = Mathf.SmoothStep(0f, 1f, mantleProgress);
        
        // Punto de control (arco por encima del destino)
        Vector3 midPoint = Vector3.Lerp(mantleStartPos, mantleEndPos, 0.5f);
        midPoint.y = Mathf.Max(mantleStartPos.y, mantleEndPos.y) + 0.5f;

        // Bezier cuadrático
        float u = 1f - t;
        Vector3 bezierPos = u * u * mantleStartPos + 2f * u * t * midPoint + t * t * mantleEndPos;

        // Mover directamente (sin CharacterController para evitar colisiones)
        transform.position = bezierPos;

        // Permitir mouse look durante mantling
        HandleMouseLook();
    }

    // =========================================================================
    // PHD FLOPPER — SLIDE DAMAGE
    // =========================================================================

    /// <summary>
    /// Si PhD Flopper está activa, envía damage check durante el slide
    /// </summary>
    private void HandlePHDSlide()
    {
        if (!isSliding) return;
        if (PlayerPerkManager.Instance == null) return;
        if (!PlayerPerkManager.Instance.SlideDealsDamage) return;

        PlayerPerkManager.Instance.HandlePHDSlide(transform.position, slideDirection);
    }

    // =========================================================================
    // STAMIN-UP INTEGRATION
    // =========================================================================

    /// <summary>
    /// Cambia la velocidad de sprint en runtime (usado por Stamin-Up).
    /// </summary>
    public void SetSprintSpeed(float newSpeed)
    {
        sprintSpeed = Mathf.Max(walkSpeed, newSpeed);
    }

    /// <summary>
    /// Obtiene la velocidad de sprint actual.
    /// </summary>
    public float GetSprintSpeed()
    {
        return sprintSpeed;
    }
}
