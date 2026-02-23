using UnityEngine;

/// <summary>
/// Efecto procedural cuando el jugador recibe daño
/// Combina: flinch del arma + rotación de cámara + efecto visual
/// Reemplaza los triggers de Animator para daño
/// </summary>
public class ProceduralDamageResponse : MonoBehaviour
{
    [Header("Flinch del Arma")]
    [SerializeField] private float weaponFlinchAmount = 0.05f;
    [SerializeField] private float weaponFlinchRotation = 8f;
    [SerializeField] private float flinchDuration = 0.25f;
    [SerializeField] private float flinchRecoverySpeed = 10f;

    [Header("Rotación de Cámara por Daño")]
    [SerializeField] private float cameraFlinchRotation = 3f;
    [SerializeField] private float cameraFlinchDuration = 0.2f;

    [Header("Efecto de Velocidad Reducida (Hit Stun)")]
    [SerializeField] private bool enableHitStun = true;
    [SerializeField] private float hitStunDuration = 0.1f;
    [SerializeField] private float hitStunTimeScale = 0.9f;

    [Header("Referencias")]
    [SerializeField] private Transform weaponHolder;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private ScreenShake screenShake;

    // Estado interno
    private bool isFlinching = false;
    private float flinchTimer = 0f;
    private Vector3 weaponOriginalPos;
    private Quaternion weaponOriginalRot;
    private Vector3 flinchDirection;

    void Start()
    {
        if (cameraTransform == null)
        {
            Camera cam = Camera.main;
            if (cam != null) cameraTransform = cam.transform;
        }

        if (screenShake == null && cameraTransform != null)
            screenShake = cameraTransform.GetComponent<ScreenShake>();

        if (weaponHolder != null)
        {
            weaponOriginalPos = weaponHolder.localPosition;
            weaponOriginalRot = weaponHolder.localRotation;
        }
    }

    void Update()
    {
        if (!isFlinching || weaponHolder == null) return;

        flinchTimer += Time.deltaTime;

        if (flinchTimer >= flinchDuration)
        {
            // Recuperar posición del arma
            weaponHolder.localPosition = Vector3.Lerp(
                weaponHolder.localPosition,
                weaponOriginalPos,
                Time.deltaTime * flinchRecoverySpeed
            );

            weaponHolder.localRotation = Quaternion.Slerp(
                weaponHolder.localRotation,
                weaponOriginalRot,
                Time.deltaTime * flinchRecoverySpeed
            );

            // Verificar si ya volvió
            if (Vector3.Distance(weaponHolder.localPosition, weaponOriginalPos) < 0.001f)
            {
                weaponHolder.localPosition = weaponOriginalPos;
                weaponHolder.localRotation = weaponOriginalRot;
                isFlinching = false;
            }
        }
    }

    /// <summary>
    /// Activa la respuesta procedural al recibir daño
    /// Llamar desde PlayerHealth.TakeDamage()
    /// </summary>
    public void OnDamageTaken()
    {
        OnDamageTaken(Vector3.zero);
    }

    /// <summary>
    /// Activa la respuesta procedural con dirección del daño
    /// </summary>
    public void OnDamageTaken(Vector3 damageDirection)
    {
        // 1. Flinch del arma
        ApplyWeaponFlinch(damageDirection);

        // 2. Screen shake
        if (screenShake != null)
        {
            screenShake.DamageShake();
        }

        // 3. Camera flinch rotation
        ApplyCameraFlinch(damageDirection);

        // 4. Hit stun temporal
        if (enableHitStun)
        {
            StartCoroutine(HitStunCoroutine());
        }
    }

    /// <summary>
    /// Aplica flinch al arma proceduralmente
    /// </summary>
    private void ApplyWeaponFlinch(Vector3 direction)
    {
        if (weaponHolder == null) return;

        weaponOriginalPos = weaponHolder.localPosition;
        weaponOriginalRot = weaponHolder.localRotation;

        // Dirección aleatoria si no se especifica
        if (direction == Vector3.zero)
        {
            direction = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-0.5f, 0.5f),
                Random.Range(-0.3f, 0f)
            ).normalized;
        }

        // Aplicar desplazamiento
        weaponHolder.localPosition += direction * weaponFlinchAmount;

        // Aplicar rotación de flinch
        float rotX = Random.Range(-weaponFlinchRotation, weaponFlinchRotation);
        float rotY = Random.Range(-weaponFlinchRotation * 0.5f, weaponFlinchRotation * 0.5f);
        weaponHolder.localRotation *= Quaternion.Euler(rotX, rotY, 0f);

        isFlinching = true;
        flinchTimer = 0f;
    }

    /// <summary>
    /// Aplica rotación de flinch a la cámara
    /// </summary>
    private void ApplyCameraFlinch(Vector3 direction)
    {
        if (cameraTransform == null) return;

        StartCoroutine(CameraFlinchCoroutine());
    }

    private System.Collections.IEnumerator CameraFlinchCoroutine()
    {
        float elapsed = 0f;
        float rotX = Random.Range(-cameraFlinchRotation, cameraFlinchRotation);
        float rotY = Random.Range(-cameraFlinchRotation * 0.5f, cameraFlinchRotation * 0.5f);

        Quaternion startRot = cameraTransform.localRotation;
        Quaternion flinchRot = startRot * Quaternion.Euler(rotX, rotY, 0f);

        // Aplicar flinch rápido
        while (elapsed < cameraFlinchDuration * 0.3f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (cameraFlinchDuration * 0.3f);
            cameraTransform.localRotation = Quaternion.Slerp(startRot, flinchRot, t);
            yield return null;
        }

        // Recuperar suavemente
        elapsed = 0f;
        while (elapsed < cameraFlinchDuration * 0.7f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (cameraFlinchDuration * 0.7f);
            cameraTransform.localRotation = Quaternion.Slerp(flinchRot, startRot, t);
            yield return null;
        }

        cameraTransform.localRotation = startRot;
    }

    private System.Collections.IEnumerator HitStunCoroutine()
    {
        Time.timeScale = hitStunTimeScale;
        yield return new WaitForSecondsRealtime(hitStunDuration);
        Time.timeScale = 1f;
    }

    /// <summary>
    /// Actualiza las posiciones originales del arma
    /// </summary>
    public void UpdateWeaponOriginalTransform()
    {
        if (weaponHolder != null)
        {
            weaponOriginalPos = weaponHolder.localPosition;
            weaponOriginalRot = weaponHolder.localRotation;
        }
    }
}
