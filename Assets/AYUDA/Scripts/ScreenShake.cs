using UnityEngine;

/// <summary>
/// Screen Shake procedural estilo Robobeat.
/// Basado en Perlin noise para movimiento suave (no random jittery).
/// Incluye shake posicional + rotacional, con perfiles para disparo, dano, landing, explosion.
/// Sistema ADITIVO: no sobreescribe la posicion base de la camara.
/// </summary>
public class ScreenShake : MonoBehaviour
{
    [Header("=== LIMITES ===")]
    [SerializeField] private float maxShakePosition = 0.12f;
    [SerializeField] private float maxShakeRotation = 3f;

    [Header("=== SHAKE POR DISPARO ===")]
    [SerializeField] private float shootShakePos = 0.015f;
    [SerializeField] private float shootShakeRot = 0.8f;
    [SerializeField] private float shootShakeDuration = 0.1f;

    [Header("=== SHAKE POR DANO ===")]
    [SerializeField] private float damageShakePos = 0.06f;
    [SerializeField] private float damageShakeRot = 2.5f;
    [SerializeField] private float damageShakeDuration = 0.35f;

    [Header("=== SHAKE POR ATERRIZAJE ===")]
    [SerializeField] private float landShakePos = 0.04f;
    [SerializeField] private float landShakeRot = 1.5f;
    [SerializeField] private float landShakeDuration = 0.25f;

    [Header("=== PERLIN NOISE ===")]
    [Tooltip("Frecuencia del Perlin noise - mas alto = mas rapido")]
    [SerializeField] private float noiseFrequency = 25f;

    [Header("Referencias")]
    [SerializeField] private Transform cameraTransform;

    // Estado interno
    private float currentShakePos = 0f;
    private float currentShakeRot = 0f;
    private float currentShakeDuration = 0f;
    private float shakeTimer = 0f;
    private bool isShaking = false;

    // Offset aditivo - se remueve y reaplica limpiamente cada frame
    private Vector3 lastAppliedPosOffset;

    // Perlin noise seeds (diferentes para cada eje)
    private float seedX;
    private float seedY;
    private float seedRotX;
    private float seedRotY;
    private float seedRotZ;

    void Start()
    {
        if (cameraTransform == null)
        {
            Camera cam = Camera.main;
            if (cam != null)
                cameraTransform = cam.transform;
        }

        lastAppliedPosOffset = Vector3.zero;

        // Seeds aleatorios
        seedX = Random.Range(0f, 1000f);
        seedY = Random.Range(0f, 1000f);
        seedRotX = Random.Range(0f, 1000f);
        seedRotY = Random.Range(0f, 1000f);
        seedRotZ = Random.Range(0f, 1000f);
    }

    void LateUpdate()
    {
        if (cameraTransform == null) return;

        // 1. Remover el offset del frame anterior para obtener posicion limpia
        cameraTransform.localPosition -= lastAppliedPosOffset;

        Vector3 newPosOffset = Vector3.zero;

        if (!isShaking)
        {
            // Sin shake activo: offset = zero (la camara queda donde otros la pongan)
            lastAppliedPosOffset = Vector3.zero;
            return;
        }

        shakeTimer += Time.deltaTime;

        if (shakeTimer >= currentShakeDuration)
        {
            isShaking = false;
            lastAppliedPosOffset = Vector3.zero;
            return;
        }

        // Intensidad decreciente con curva cuadratica (mas punch al inicio)
        float progress = shakeTimer / currentShakeDuration;
        float envelope = (1f - progress) * (1f - progress); // Cuadratica

        // --- POSICION con Perlin noise ---
        float posAmount = currentShakePos * envelope;
        posAmount = Mathf.Min(posAmount, maxShakePosition);

        float time = shakeTimer * noiseFrequency;
        float offsetX = (Mathf.PerlinNoise(seedX + time, 0f) - 0.5f) * 2f * posAmount;
        float offsetY = (Mathf.PerlinNoise(seedY + time, 0f) - 0.5f) * 2f * posAmount;

        newPosOffset = new Vector3(offsetX, offsetY, 0f);

        // --- ROTACION con Perlin noise ---
        float rotAmount = currentShakeRot * envelope;
        rotAmount = Mathf.Min(rotAmount, maxShakeRotation);

        float rotX = (Mathf.PerlinNoise(seedRotX + time, 0f) - 0.5f) * 2f * rotAmount;
        float rotY = (Mathf.PerlinNoise(seedRotY + time, 0f) - 0.5f) * 2f * rotAmount * 0.5f;
        float rotZ = (Mathf.PerlinNoise(seedRotZ + time, 0f) - 0.5f) * 2f * rotAmount * 0.7f;

        // 2. Aplicar nuevo offset ADITIVAMENTE
        lastAppliedPosOffset = newPosOffset;
        cameraTransform.localPosition += lastAppliedPosOffset;

        // Aplicar rotacion como offset (preservar rotaciones existentes)
        Vector3 currentEuler = cameraTransform.localEulerAngles;
        cameraTransform.localRotation = Quaternion.Euler(
            currentEuler.x + rotX,
            currentEuler.y + rotY,
            currentEuler.z + rotZ
        );
    }

    /// <summary>
    /// Aplica shake con posicion + rotacion
    /// </summary>
    public void Shake(float posAmount, float rotAmount, float duration)
    {
        // Acumular si ya hay shake activo (no reemplazar)
        if (isShaking)
        {
            currentShakePos = Mathf.Max(currentShakePos, posAmount);
            currentShakeRot = Mathf.Max(currentShakeRot, rotAmount);
            currentShakeDuration = Mathf.Max(currentShakeDuration - shakeTimer, duration);
        }
        else
        {
            currentShakePos = posAmount;
            currentShakeRot = rotAmount;
            currentShakeDuration = duration;
        }

        shakeTimer = 0f;
        isShaking = true;

        // Nuevas seeds para variedad
        seedX = Random.Range(0f, 1000f);
        seedY = Random.Range(0f, 1000f);
        seedRotX = Random.Range(0f, 1000f);
        seedRotY = Random.Range(0f, 1000f);
        seedRotZ = Random.Range(0f, 1000f);
    }

    /// <summary>
    /// Backward-compatible: shake solo con posicion
    /// </summary>
    public void Shake(float amount, float duration)
    {
        Shake(amount, amount * 15f, duration);
    }

    /// <summary>
    /// Shake al disparar
    /// </summary>
    public void ShootShake()
    {
        Shake(shootShakePos, shootShakeRot, shootShakeDuration);
    }

    /// <summary>
    /// Shake al recibir dano
    /// </summary>
    public void DamageShake()
    {
        Shake(damageShakePos, damageShakeRot, damageShakeDuration);
    }

    /// <summary>
    /// Shake al aterrizar
    /// </summary>
    public void LandingShake(float fallIntensity = 1f)
    {
        float intensity = Mathf.Clamp01(fallIntensity);
        Shake(landShakePos * intensity, landShakeRot * intensity, landShakeDuration);
    }

    /// <summary>
    /// Shake de explosion
    /// </summary>
    public void ExplosionShake(float distance, float maxDistance = 20f)
    {
        float normalizedDistance = 1f - Mathf.Clamp01(distance / maxDistance);
        Shake(maxShakePosition * normalizedDistance, maxShakeRotation * normalizedDistance, 0.5f);
    }

    /// <summary>
    /// Ya no es necesario llamar a esto - el sistema es aditivo automaticamente
    /// Se mantiene por compatibilidad
    /// </summary>
    public void UpdateOriginalPosition()
    {
        // No-op: el sistema aditivo no necesita posicion original
    }
}
