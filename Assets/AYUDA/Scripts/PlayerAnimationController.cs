using UnityEngine;

/// <summary>
/// Controlador procedural del jugador - Sistema 100% procedural (sin Animator)
/// Lee el estado desde PlayerMovement y coordina los sistemas procedurales.
/// Trackea velocidad y aceleración del jugador para efectos dinámicos.
/// </summary>
public class PlayerAnimationController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Transform mainCamera;

    [Header("Sistemas Procedurales")]
    [SerializeField] private CameraHeadBob headBob;
    [SerializeField] private WeaponSway weaponSway;
    [SerializeField] private WeaponBob weaponBob;
    [SerializeField] private ProceduralFOVEffect fovEffect;
    [SerializeField] private SprintTilt sprintTilt;
    [SerializeField] private LandingImpact landingImpact;
    [SerializeField] private ProceduralDamageResponse damageResponse;
    [SerializeField] private ScreenShake screenShake;

    [Header("=== FOOTSTEP CAMERA SHAKE ===")]
    [Tooltip("Screen shake sutil sincronizado con pisadas al correr")]
    [SerializeField] private bool enableFootstepShake = true;
    [SerializeField] private float sprintFootstepShakePos = 0.004f;
    [SerializeField] private float sprintFootstepShakeRot = 0.3f;
    [SerializeField] private float sprintStepInterval = 0.3f;

    // Velocidad y aceleración trackeadas
    private Vector3 lastVelocity;
    private Vector3 currentAcceleration;
    private float footstepTimer;

    // Estado previo para detectar transiciones
    private bool wasSprinting;
    private bool wasSliding;
    private bool wasGrounded;

    void Start()
    {
        // Buscar PlayerMovement si no está asignado
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();

        if (mainCamera == null)
            mainCamera = Camera.main.transform;

        // Buscar componentes procedurales automáticamente
        if (headBob == null)
            headBob = GetComponentInChildren<CameraHeadBob>();

        if (weaponSway == null)
            weaponSway = GetComponentInChildren<WeaponSway>();

        if (weaponBob == null)
            weaponBob = GetComponentInChildren<WeaponBob>();

        if (fovEffect == null)
            fovEffect = GetComponentInChildren<ProceduralFOVEffect>();

        if (sprintTilt == null)
            sprintTilt = GetComponentInChildren<SprintTilt>();

        if (landingImpact == null)
            landingImpact = GetComponentInChildren<LandingImpact>();

        if (damageResponse == null)
            damageResponse = GetComponentInChildren<ProceduralDamageResponse>();

        if (screenShake == null)
            screenShake = GetComponentInChildren<ScreenShake>();
    }

    void Update()
    {
        if (playerMovement == null) return;

        bool isSprinting = playerMovement.IsSprinting;
        bool isSliding = playerMovement.IsSliding;
        bool isGrounded = playerMovement.IsGrounded;
        bool isMoving = playerMovement.IsMoving;

        // Actualizar FOV según sprint y slide
        if (fovEffect != null)
        {
            fovEffect.SetSprinting(isSprinting);
            fovEffect.SetSliding(isSliding, playerMovement.SlideProgress);
        }

        // Footstep camera shake durante sprint
        if (enableFootstepShake && isSprinting && isGrounded && isMoving && screenShake != null)
        {
            footstepTimer += Time.deltaTime;
            if (footstepTimer >= sprintStepInterval)
            {
                footstepTimer = 0f;
                screenShake.Shake(sprintFootstepShakePos, sprintFootstepShakeRot, 0.08f);
            }
        }
        else
        {
            footstepTimer = 0f;
        }

        // Detectar transición de slide a no-slide (FOV punch de salida)
        if (wasSliding && !isSliding && fovEffect != null)
        {
            fovEffect.ApplyFOVPulse(-3f, 0.2f);
        }

        // Actualizar estado previo
        wasSprinting = isSprinting;
        wasSliding = isSliding;
        wasGrounded = isGrounded;
    }

    /// <summary>
    /// Respuesta procedural al recibir daño (llamado desde PlayerHealth)
    /// </summary>
    public void TriggerTakeDamage()
    {
        if (damageResponse != null)
            damageResponse.OnDamageTaken();

        if (screenShake != null)
            screenShake.DamageShake();
    }

    /// <summary>
    /// Respuesta procedural a la muerte
    /// </summary>
    public void TriggerDie()
    {
        SetProceduralWeaponEnabled(false);
    }

    /// <summary>
    /// Activa o desactiva los sistemas procedurales
    /// </summary>
    public void SetProceduralWeaponEnabled(bool enabled)
    {
        if (headBob != null) headBob.SetEnabled(enabled);
        if (weaponSway != null) weaponSway.SetEnabled(enabled);
        if (weaponBob != null) weaponBob.SetEnabled(enabled);
    }

    // Getters delegados a PlayerMovement
    public bool IsMoving() => playerMovement != null && playerMovement.IsMoving;
    public bool IsRunning() => playerMovement != null && playerMovement.IsSprinting;
    public bool IsCrouching() => playerMovement != null && playerMovement.IsCrouching;
    public float GetNormalizedSpeed() => playerMovement != null ? playerMovement.NormalizedSpeed : 0f;
}
