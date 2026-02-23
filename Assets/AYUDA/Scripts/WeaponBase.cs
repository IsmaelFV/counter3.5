using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Clase base abstracta para todas las armas
/// Sistema de disparo real con raycast desde la cámara, dispersión, penetración,
/// caída de daño, trazadores e impactos.
/// Sistema 100% procedural - sin Animator
/// </summary>
public abstract class WeaponBase : MonoBehaviour
{
    [Header("Datos del Arma")]
    [SerializeField] protected WeaponData weaponData;

    [Header("Referencias")]
    [SerializeField] protected Transform muzzlePoint;
    [SerializeField] protected AudioSource audioSource;

    [Header("=== RETROCESO PROCEDURAL (Estilo Robobeat) ===")]
    [SerializeField] protected float recoilAmount = 0.06f;
    [SerializeField] protected float recoilSpeed = 18f;
    [SerializeField] protected float recoilRotation = 6f;
    [Tooltip("Kick vertical de cámara en grados (snap rápido hacia arriba)")]
    [SerializeField] protected float recoilKickUp = 1.5f;
    [Tooltip("Variación horizontal aleatoria del kick")]
    [SerializeField] protected float recoilKickSideRandom = 0.4f;
    [Tooltip("Fuerza del spring de retorno (más alto = más snappy)")]
    [SerializeField] protected float recoilSpringStiffness = 120f;
    [Tooltip("Damping del spring (más bajo = más rebote)")]
    [SerializeField] protected float recoilSpringDamping = 12f;

    // Referencias para retroceso
    protected Camera mainCamera;
    protected Vector3 originalWeaponPosition;
    protected Quaternion originalWeaponRotation;

    // Spring state para retroceso snappy
    private Vector3 recoilPositionSpring;
    private Vector3 recoilPositionVelocity;
    private Vector3 recoilRotationSpring;
    private Vector3 recoilRotationVelocity;

    // Camera recoil offset aditivo (no sobreescribe posición base)
    private Vector3 cameraRecoilOffset;
    private Vector3 lastAppliedCameraOffset;

    // Estado de munición
    protected int currentAmmoInMagazine;
    protected int currentReserveAmmo;
    
    // Control de disparo y recarga
    protected float nextFireTime = 0f;
    protected bool isReloading = false;

    // Cooldown para sonido de cargador vacío (evita spam en armas automáticas)
    private float nextEmptySoundTime = 0f;
    private const float EMPTY_SOUND_COOLDOWN = 0.25f;

    // Estado de melee
    private bool isMeleeing = false;
    private Coroutine meleeCoroutine;

    // Referencia al transform raíz del jugador para ignorar en raycasts
    private Transform playerRoot;

    // Referencia cacheada al PlayerMovement
    private PlayerMovement cachedPlayerMovement;

    // Sistema de dispersión
    protected float currentSpread = 0f;

    // =========================================================================
    // SISTEMA DE MEJORAS
    // =========================================================================
    
    /// <summary>Nivel actual de mejora (0 = sin mejorar, max = 2)</summary>
    private int upgradeLevel = 0;
    
    /// <summary>Nivel máximo de mejora — solo 2 mejoras por arma</summary>
    private const int MAX_UPGRADE_LEVEL = 2;

    /// <summary>Multiplicadores por nivel (índice 0=base, 1=mejora1, 2=mejora2)</summary>
    // Nivel 1: +25% daño, -12% cadencia, +30% cargador/reserva, -10% dispersión, -8% recarga
    // Nivel 2: +55% daño, -25% cadencia, +60% cargador/reserva, -20% dispersión, -15% recarga
    private static readonly float[] damageMult      = { 1f, 1.25f, 1.55f };
    private static readonly float[] fireRateMult    = { 1f, 0.88f, 0.75f };
    private static readonly float[] magazineMult    = { 1f, 1.3f,  1.6f  };
    private static readonly float[] spreadMult      = { 1f, 0.90f, 0.80f };
    private static readonly float[] reloadTimeMult  = { 1f, 0.92f, 0.85f };

    /// <summary>Evento: arma mejorada (nivel nuevo)</summary>
    public System.Action<int> OnWeaponUpgraded;

    // Resultados de último disparo (para que WeaponManager use)
    private List<HitInfo> lastHitResults = new List<HitInfo>();

    /// <summary>
    /// Información de un impacto de bala
    /// </summary>
    public struct HitInfo
    {
        public Vector3 point;
        public Vector3 normal;
        public Collider collider;
        public float distance;
        public bool isDamageable;
        public int damageApplied;

        public HitInfo(RaycastHit hit, bool damageable, int damage)
        {
            point = hit.point;
            normal = hit.normal;
            collider = hit.collider;
            distance = hit.distance;
            isDamageable = damageable;
            damageApplied = damage;
        }
    }

    // Eventos para UI
    public System.Action<int, int> OnAmmoChanged; // (ammo en cargador, ammo reserva)
    public System.Action OnReloadStarted;
    public System.Action OnReloadFinished;
    public System.Action OnShoot;

    /// <summary>Evento: disparo con información de origen y destino para trazador</summary>
    public System.Action<Vector3, Vector3, bool> OnShootWithTracer; // (muzzlePos, endPoint, didHit)

    /// <summary>Evento: impacto registrado (punto, normal, fueEnemigo)</summary>
    public System.Action<Vector3, Vector3, bool> OnBulletImpact; // (hitPoint, hitNormal, wasEnemy)

    /// <summary>
    /// Awake se ejecuta antes que todos los Start(), garantizando que la munición
    /// esté inicializada cuando WeaponManager lea los datos del arma.
    /// </summary>
    protected virtual void Awake()
    {
        InitializeAmmo();
    }

    /// <summary>
    /// Inicializa la munición desde WeaponData. Se puede llamar varias veces sin problema.
    /// </summary>
    private void InitializeAmmo()
    {
        if (weaponData != null && currentAmmoInMagazine == 0 && currentReserveAmmo == 0)
        {
            currentAmmoInMagazine = weaponData.maxAmmoInMagazine;
            currentReserveAmmo = weaponData.maxReserveAmmo;
        }
    }

    protected virtual void Start()
    {
        if (weaponData == null)
        {
            Debug.LogError($"WeaponData no asignado en {gameObject.name}");
            return;
        }

        // Buscar componentes si no están asignados
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = GetComponentInChildren<AudioSource>();
        if (audioSource == null)
            audioSource = GetComponentInParent<AudioSource>();
        // Si no hay ningún AudioSource en la jerarquía, crear uno automáticamente
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f; // 3D sound
            Debug.LogWarning($"[{gameObject.name}] No se encontró AudioSource. Se creó uno automáticamente.");
        }

        // Configurar retroceso procedural
        mainCamera = Camera.main;
        cameraRecoilOffset = Vector3.zero;
        lastAppliedCameraOffset = Vector3.zero;

        // Guardar referencia al jugador para ignorar en raycasts
        cachedPlayerMovement = GetComponentInParent<PlayerMovement>();
        playerRoot = GetComponentInParent<PlayerHealth>()?.transform;
        if (playerRoot == null && cachedPlayerMovement != null)
            playerRoot = cachedPlayerMovement.transform;

        originalWeaponPosition = transform.localPosition;
        originalWeaponRotation = transform.localRotation;

        // Inicializar dispersión
        currentSpread = weaponData.baseSpread;

        // Notificar estado inicial
        OnAmmoChanged?.Invoke(currentAmmoInMagazine, currentReserveAmmo);
    }

    /// <summary>
    /// Intenta disparar el arma (debe ser implementado por cada arma)
    /// </summary>
    public abstract bool TryShoot();

    /// <summary>
    /// Verifica si se puede disparar
    /// </summary>
    protected bool CanShoot()
    {
        return !isReloading && !isMeleeing &&
               currentAmmoInMagazine > 0 && 
               Time.time >= nextFireTime;
    }

    /// <summary>
    /// Ejecuta la lógica de disparo completa
    /// </summary>
    protected virtual void PerformShoot()
    {
        // Reducir munición
        currentAmmoInMagazine--;
        OnAmmoChanged?.Invoke(currentAmmoInMagazine, currentReserveAmmo);

        // Establecer próximo tiempo de disparo
        nextFireTime = Time.time + GetUpgradedFireRate();

        // Reproducir sonido
        PlaySound(weaponData.shootSound);

        // Retroceso procedural
        ApplyRecoil();

        // Notificar disparo (para WeaponManager: muzzle flash, shake, etc.)
        OnShoot?.Invoke();

        // ===== SISTEMA DE DISPARO REAL =====
        PerformRealShot();

        // Añadir dispersión por disparo
        currentSpread = Mathf.Min(currentSpread + weaponData.spreadPerShot, GetUpgradedMaxSpread());
    }

    /// <summary>
    /// Update para retroceso spring-based y recuperación de dispersión
    /// </summary>
    protected virtual void Update()
    {
        // --- SPRING PHYSICS para retroceso snappy con overshoot ---
        // Posición: F = -kx - bv (damped spring)
        Vector3 posForce = -recoilSpringStiffness * recoilPositionSpring - recoilSpringDamping * recoilPositionVelocity;
        recoilPositionVelocity += posForce * Time.deltaTime;
        recoilPositionSpring += recoilPositionVelocity * Time.deltaTime;

        // Rotación: misma física de spring
        Vector3 rotForce = -recoilSpringStiffness * recoilRotationSpring - recoilSpringDamping * recoilRotationVelocity;
        recoilRotationVelocity += rotForce * Time.deltaTime;
        recoilRotationSpring += recoilRotationVelocity * Time.deltaTime;

        // Limpiar valores muy pequeños
        if (recoilPositionSpring.sqrMagnitude < 0.000001f && recoilPositionVelocity.sqrMagnitude < 0.000001f)
        {
            recoilPositionSpring = Vector3.zero;
            recoilPositionVelocity = Vector3.zero;
        }
        if (recoilRotationSpring.sqrMagnitude < 0.0001f && recoilRotationVelocity.sqrMagnitude < 0.0001f)
        {
            recoilRotationSpring = Vector3.zero;
            recoilRotationVelocity = Vector3.zero;
        }

        // Aplicar spring al arma
        transform.localPosition = originalWeaponPosition + recoilPositionSpring;
        transform.localRotation = originalWeaponRotation * Quaternion.Euler(recoilRotationSpring);

        // Aplicar kick de cámara ADITIVAMENTE
        if (mainCamera != null)
        {
            // Remover offset anterior
            mainCamera.transform.localPosition -= lastAppliedCameraOffset;

            // Decaer el offset de recoil hacia zero
            cameraRecoilOffset = Vector3.Lerp(cameraRecoilOffset, Vector3.zero, Time.deltaTime * recoilSpeed);
            if (cameraRecoilOffset.sqrMagnitude < 0.000001f)
                cameraRecoilOffset = Vector3.zero;

            // Aplicar nuevo offset
            lastAppliedCameraOffset = cameraRecoilOffset;
            mainCamera.transform.localPosition += lastAppliedCameraOffset;
        }

        // Recuperar dispersión con el tiempo
        if (currentSpread > GetUpgradedBaseSpread())
        {
            currentSpread = Mathf.Lerp(currentSpread, GetUpgradedBaseSpread(), 
                weaponData.spreadRecoverySpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// Aplica retroceso procedural estilo Robobeat (spring-based, snappy)
    /// </summary>
    protected virtual void ApplyRecoil()
    {
        // --- KICK DE CÁMARA (push instantáneo, aditivo) ---
        if (mainCamera != null)
        {
            cameraRecoilOffset -= mainCamera.transform.InverseTransformDirection(mainCamera.transform.forward) * recoilAmount;
        }

        // --- SPRING IMPULSE al arma ---
        // Empujón hacia atrás + arriba + aleatorio lateral
        float sideKick = Random.Range(-recoilKickSideRandom, recoilKickSideRandom);
        Vector3 posImpulse = new Vector3(sideKick * 0.02f, recoilAmount * 0.15f, -recoilAmount * 0.6f);
        recoilPositionVelocity += posImpulse * recoilSpringStiffness * 0.5f;
        recoilPositionSpring += posImpulse;

        // Rotación: kick up + random side
        Vector3 rotImpulse = new Vector3(
            -recoilRotation,       // Pitch up
            sideKick * 2f,         // Yaw aleatorio
            sideKick * 1.5f        // Roll aleatorio
        );
        recoilRotationVelocity += rotImpulse * recoilSpringStiffness * 0.3f;
        recoilRotationSpring += rotImpulse;
    }

    // =========================================================================
    // SISTEMA DE DISPARO REAL
    // =========================================================================

    /// <summary>
    /// Ejecuta el disparo real: raycast desde el centro de la cámara con dispersión,
    /// detección de impactos, daño con caída por distancia y penetración
    /// </summary>
    protected virtual void PerformRealShot()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return;
        }

        lastHitResults.Clear();

        // Si es escopeta, disparar múltiples perdigones
        if (weaponData.fireMode == FireMode.Shotgun)
        {
            PerformShotgunShot();
            return;
        }

        // Calcular la dirección del disparo desde el centro de la pantalla con dispersión
        Vector3 shootDirection = CalculateShootDirection();

        // Origen del raycast = centro de la cámara
        Vector3 rayOrigin = mainCamera.transform.position;

        // Punto de inicio del trazador = punta del arma (visual)
        Vector3 tracerOrigin = muzzlePoint != null ? muzzlePoint.position : rayOrigin;

        // Dibujar debug ray en el editor
        Debug.DrawRay(rayOrigin, shootDirection * weaponData.maxRange, Color.red, 0.5f);

        if (weaponData.canPenetrate)
        {
            PerformPenetratingShot(rayOrigin, shootDirection, tracerOrigin);
        }
        else
        {
            PerformSingleShot(rayOrigin, shootDirection, tracerOrigin);
        }
    }

    /// <summary>
    /// Disparo simple (sin penetración) — la mayoría de armas
    /// </summary>
    private void PerformSingleShot(Vector3 origin, Vector3 direction, Vector3 tracerOrigin)
    {
        Vector3 endPoint;

        // Usar RaycastAll para poder filtrar al jugador
        RaycastHit[] allHits = Physics.RaycastAll(origin, direction, weaponData.maxRange);
        System.Array.Sort(allHits, (a, b) => a.distance.CompareTo(b.distance));

        // Buscar el primer impacto que NO sea el propio jugador
        RaycastHit? validHit = null;
        foreach (var h in allHits)
        {
            if (!IsOwnPlayer(h.collider))
            {
                validHit = h;
                break;
            }
        }

        if (validHit.HasValue)
        {
            RaycastHit hit = validHit.Value;
            endPoint = hit.point;

            // Calcular daño con caída por distancia
            int finalDamage = CalculateDamage(hit.distance);

            // Intentar aplicar daño
            bool wasEnemy = TryApplyDamage(hit, finalDamage, direction);

            // Registrar impacto
            lastHitResults.Add(new HitInfo(hit, wasEnemy, finalDamage));

            // Notificar impacto para efectos visuales
            OnBulletImpact?.Invoke(hit.point, hit.normal, wasEnemy);

            Debug.Log($"[DISPARO] Impacto en: {hit.collider.name} | Daño: {finalDamage} | Distancia: {hit.distance:F1}m | Enemigo: {wasEnemy}");
        }
        else
        {
            // No impactó nada — punto final en el alcance máximo
            endPoint = origin + direction * weaponData.maxRange;
        }

        // Notificar trazador
        if (weaponData.showBulletTracer)
        {
            OnShootWithTracer?.Invoke(tracerOrigin, endPoint, lastHitResults.Count > 0);
        }
    }

    /// <summary>
    /// Disparo con penetración — atraviesa múltiples objetivos
    /// </summary>
    private void PerformPenetratingShot(Vector3 origin, Vector3 direction, Vector3 tracerOrigin)
    {
        // RaycastAll para obtener todos los objetos en la línea de disparo
        RaycastHit[] hits = Physics.RaycastAll(origin, direction, weaponData.maxRange);

        // Ordenar por distancia
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        int penetrationCount = 0;
        float damageMultiplier = 1f;
        Vector3 lastHitPoint = origin + direction * weaponData.maxRange;

        for (int i = 0; i < hits.Length && penetrationCount <= weaponData.maxPenetrations; i++)
        {
            RaycastHit hit = hits[i];

            // Ignorar colliders del propio jugador
            if (IsOwnPlayer(hit.collider))
                continue;

            // Calcular daño con caída por distancia y penetración
            int baseDamage = CalculateDamage(hit.distance);
            int finalDamage = Mathf.RoundToInt(baseDamage * damageMultiplier);

            // Intentar aplicar daño
            bool wasEnemy = TryApplyDamage(hit, finalDamage, direction);

            // Registrar impacto
            lastHitResults.Add(new HitInfo(hit, wasEnemy, finalDamage));

            // Notificar impacto
            OnBulletImpact?.Invoke(hit.point, hit.normal, wasEnemy);

            lastHitPoint = hit.point;

            Debug.Log($"[PENETRACIÓN {penetrationCount}] {hit.collider.name} | Daño: {finalDamage} | Mult: {damageMultiplier:F2}");

            // Reducir daño para la siguiente penetración
            damageMultiplier *= weaponData.penetrationDamageRetention;
            penetrationCount++;
        }

        // Notificar trazador
        if (weaponData.showBulletTracer)
        {
            OnShootWithTracer?.Invoke(tracerOrigin, lastHitPoint, lastHitResults.Count > 0);
        }
    }

    // =========================================================================
    // DISPARO ESCOPETA (PERDIGONES)
    // =========================================================================

    /// <summary>
    /// Disparo de escopeta: lanza múltiples perdigones en un cono de dispersión.
    /// Cada perdigón hace un raycast independiente.
    /// </summary>
    protected virtual void PerformShotgunShot()
    {
        Vector3 rayOrigin = mainCamera.transform.position;
        Vector3 tracerOrigin = muzzlePoint != null ? muzzlePoint.position : rayOrigin;
        Vector3 baseDirection = mainCamera.transform.forward;

        int pelletCount = weaponData.pelletsPerShot;
        float spreadAngle = weaponData.pelletSpreadAngle;
        int pelletDmg = weaponData.pelletDamage > 0 
            ? weaponData.pelletDamage 
            : Mathf.Max(1, GetUpgradedDamage() / pelletCount);

        for (int i = 0; i < pelletCount; i++)
        {
            // Dirección aleatoria dentro del cono
            Vector2 randomCircle = Random.insideUnitCircle * Mathf.Tan(spreadAngle * Mathf.Deg2Rad * 0.5f);
            Vector3 pelletDirection = baseDirection + 
                mainCamera.transform.right * randomCircle.x + 
                mainCamera.transform.up * randomCircle.y;
            pelletDirection.Normalize();

            Debug.DrawRay(rayOrigin, pelletDirection * weaponData.maxRange, Color.yellow, 0.3f);

            // Raycast por perdigón
            RaycastHit[] allHits = Physics.RaycastAll(rayOrigin, pelletDirection, weaponData.maxRange);
            System.Array.Sort(allHits, (a, b) => a.distance.CompareTo(b.distance));

            RaycastHit? validHit = null;
            foreach (var h in allHits)
            {
                if (!IsOwnPlayer(h.collider))
                {
                    validHit = h;
                    break;
                }
            }

            // Punto final del trazador: impacto o máximo rango
            Vector3 pelletEndPoint = validHit.HasValue
                ? validHit.Value.point
                : rayOrigin + pelletDirection * weaponData.maxRange;

            if (validHit.HasValue)
            {
                RaycastHit hit = validHit.Value;

                // Daño con caída por distancia (por perdigón)
                int finalDamage = CalculatePelletDamage(pelletDmg, hit.distance);

                bool wasEnemy = TryApplyDamage(hit, finalDamage, pelletDirection);
                lastHitResults.Add(new HitInfo(hit, wasEnemy, finalDamage));
                OnBulletImpact?.Invoke(hit.point, hit.normal, wasEnemy);
            }

            // Trazador individual por perdigón
            if (weaponData.showBulletTracer)
            {
                OnShootWithTracer?.Invoke(tracerOrigin, pelletEndPoint, validHit.HasValue);
            }
        }

        Debug.Log($"[SHOTGUN] {pelletCount} perdigones disparados. Impactos: {lastHitResults.Count}");
    }

    /// <summary>
    /// Calcula el daño de un perdigón individual con caída por distancia
    /// </summary>
    private int CalculatePelletDamage(int basePelletDmg, float distance)
    {
        if (!weaponData.enableDamageFalloff) return basePelletDmg;
        if (distance <= weaponData.falloffStartDistance) return basePelletDmg;

        float falloffRange = weaponData.maxRange - weaponData.falloffStartDistance;
        if (falloffRange <= 0f) return basePelletDmg;

        float distancePastStart = distance - weaponData.falloffStartDistance;
        float falloffFactor = 1f - (distancePastStart / falloffRange);
        falloffFactor = Mathf.Clamp(falloffFactor, weaponData.minDamageMultiplier, 1f);

        return Mathf.Max(1, Mathf.RoundToInt(basePelletDmg * falloffFactor));
    }

    /// <summary>
    /// Calcula la dirección del disparo con dispersión aplicada
    /// </summary>
    private Vector3 CalculateShootDirection()
    {
        // Dirección base = centro exacto de la pantalla (forward de la cámara)
        Vector3 baseDirection = mainCamera.transform.forward;

        // Calcular dispersión efectiva
        float effectiveSpread = currentSpread;

        // Multiplicador por movimiento
        if (cachedPlayerMovement != null)
        {
            if (!cachedPlayerMovement.IsGrounded)
            {
                effectiveSpread *= weaponData.airSpreadMultiplier;
            }
            else if (cachedPlayerMovement.IsMoving)
            {
                effectiveSpread *= weaponData.movingSpreadMultiplier;
            }
        }

        // Si no hay dispersión, disparar recto
        if (effectiveSpread <= 0.01f)
            return baseDirection;

        // Aplicar dispersión aleatoria en un cono
        float spreadAngle = effectiveSpread * 0.5f; // Half-angle del cono
        Vector2 randomCircle = Random.insideUnitCircle * Mathf.Tan(spreadAngle * Mathf.Deg2Rad);

        Vector3 spreadDirection = baseDirection +
            mainCamera.transform.right * randomCircle.x +
            mainCamera.transform.up * randomCircle.y;

        return spreadDirection.normalized;
    }

    /// <summary>
    /// Calcula el daño final con caída por distancia
    /// </summary>
    private int CalculateDamage(float distance)
    {
        int baseDmg = GetUpgradedDamage();

        if (!weaponData.enableDamageFalloff)
            return baseDmg;

        if (distance <= weaponData.falloffStartDistance)
            return baseDmg;

        // Calcular factor de caída (lineal entre falloffStart y maxRange)
        float falloffRange = weaponData.maxRange - weaponData.falloffStartDistance;
        if (falloffRange <= 0f)
            return baseDmg;

        float distancePastStart = distance - weaponData.falloffStartDistance;
        float falloffFactor = 1f - (distancePastStart / falloffRange);
        falloffFactor = Mathf.Clamp(falloffFactor, weaponData.minDamageMultiplier, 1f);

        return Mathf.Max(1, Mathf.RoundToInt(baseDmg * falloffFactor));
    }

    /// <summary>
    /// Intenta aplicar daño al objeto impactado.
    /// Solo aplica daño a objetos que implementen IDamageable (enemigos, destructibles, etc.)
    /// </summary>
    private bool TryApplyDamage(RaycastHit hit, int damage, Vector3 direction)
    {
        // Buscar en el collider y sus padres
        IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
        if (damageable != null && damageable.IsAlive())
        {
            damageable.TakeDamage(damage, hit.point, direction);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Verifica si el collider pertenece al propio jugador (para ignorar en raycasts)
    /// </summary>
    private bool IsOwnPlayer(Collider col)
    {
        if (playerRoot == null) return false;

        // Verificar si el collider es hijo del jugador o es el jugador mismo
        return col.transform == playerRoot || col.transform.IsChildOf(playerRoot);
    }

    /// <summary>
    /// Obtiene los resultados del último disparo
    /// </summary>
    public List<HitInfo> GetLastHitResults()
    {
        return lastHitResults;
    }

    /// <summary>
    /// Obtiene la dispersión actual del arma (0 = perfecta)
    /// </summary>
    public float GetCurrentSpread()
    {
        return currentSpread;
    }

    /// <summary>
    /// Obtiene el punto de salida del arma (MuzzlePoint)
    /// </summary>
    public Transform GetMuzzlePoint()
    {
        return muzzlePoint;
    }

    // =========================================================================
    // SISTEMA DE MEJORAS (UPGRADES)
    // =========================================================================

    /// <summary>
    /// Nivel actual de mejora del arma
    /// </summary>
    public int UpgradeLevel => upgradeLevel;

    /// <summary>
    /// ¿Se puede mejorar más?
    /// </summary>
    public bool CanUpgrade => upgradeLevel < MAX_UPGRADE_LEVEL;

    /// <summary>
    /// Nivel máximo de mejora
    /// </summary>
    public int MaxUpgradeLevel => MAX_UPGRADE_LEVEL;

    /// <summary>
    /// Coste de la siguiente mejora — determinado por tipo de arma.
    /// Rifle: 4000 / 9000 | Pistola: 2500 / 6000
    /// Escopetas: 3500 / 7500 | BurstRifle: 4000 / 8500 | SemiAutoRifle: 3500 / 7000
    /// </summary>
    public int GetUpgradeCost()
    {
        if (!CanUpgrade) return -1;

        // Precios por tipo de arma
        bool isRifle = this is Weapon_Rifle;
        bool isShotgun = this is Weapon_Shotgun || this is Weapon_ShotgunAuto;
        bool isBurst = this is Weapon_BurstRifle;
        bool isSemiRifle = this is Weapon_SemiAutoRifle;

        if (upgradeLevel == 0)
        {
            if (isRifle || isBurst) return 4000;
            if (isShotgun) return 3500;
            if (isSemiRifle) return 3500;
            return 2500; // Pistola
        }
        else
        {
            if (isRifle || isBurst) return 9000;
            if (isShotgun) return 7500;
            if (isSemiRifle) return 7000;
            return 6000; // Pistola
        }
    }

    /// <summary>
    /// Mejora el arma al siguiente nivel. Retorna true si tuvo éxito.
    /// Rellena TODA la munición (cargador + reserva al nuevo máximo).
    /// No verifica monedas — eso lo hace WeaponUpgradeStation.
    /// </summary>
    public bool ApplyUpgrade()
    {
        if (!CanUpgrade) return false;

        upgradeLevel++;

        // Recalcular nuevos máximos con la mejora
        int newMaxMag = GetUpgradedMagazineSize();
        int newMaxReserve = GetUpgradedMaxReserve();

        // Rellenar TODA la munición al mejorar
        currentAmmoInMagazine = newMaxMag;
        currentReserveAmmo = newMaxReserve;

        // Resetear dispersión con los nuevos valores
        currentSpread = GetUpgradedBaseSpread();

        OnWeaponUpgraded?.Invoke(upgradeLevel);
        OnAmmoChanged?.Invoke(currentAmmoInMagazine, currentReserveAmmo);

        Debug.Log($"[UPGRADE] {weaponData.weaponName} mejorada a nivel {upgradeLevel}/{MAX_UPGRADE_LEVEL}. " +
                  $"Daño: {GetUpgradedDamage()} | Cargador: {newMaxMag} | Reserva: {newMaxReserve}");
        return true;
    }

    /// <summary>Daño efectivo con mejoras aplicadas</summary>
    public int GetUpgradedDamage()
    {
        return Mathf.RoundToInt(weaponData.damage * damageMult[upgradeLevel]);
    }

    /// <summary>Cadencia efectiva con mejoras (menor = más rápido)</summary>
    public float GetUpgradedFireRate()
    {
        return weaponData.fireRate * fireRateMult[upgradeLevel];
    }

    /// <summary>Tamaño de cargador con mejoras</summary>
    public int GetUpgradedMagazineSize()
    {
        return Mathf.RoundToInt(weaponData.maxAmmoInMagazine * magazineMult[upgradeLevel]);
    }

    /// <summary>Reserva máxima con mejoras</summary>
    public int GetUpgradedMaxReserve()
    {
        return Mathf.RoundToInt(weaponData.maxReserveAmmo * magazineMult[upgradeLevel]);
    }

    /// <summary>Dispersión base con mejoras</summary>
    public float GetUpgradedBaseSpread()
    {
        if (weaponData == null) return 0f;
        return weaponData.baseSpread * spreadMult[upgradeLevel];
    }

    /// <summary>Dispersión máxima con mejoras</summary>
    public float GetUpgradedMaxSpread()
    {
        return weaponData.maxSpread * spreadMult[upgradeLevel];
    }

    /// <summary>Tiempo de recarga con mejoras y perks (Speed Cola)</summary>
    public float GetUpgradedReloadTime()
    {
        float baseTime = weaponData.reloadTime * reloadTimeMult[upgradeLevel];

        // Aplicar Speed Cola si está activa
        if (PlayerPerkManager.Instance != null)
        {
            baseTime *= PlayerPerkManager.Instance.ReloadMultiplier;
        }

        return baseTime;
    }

    /// <summary>
    /// Texto resumen de las mejoras del siguiente nivel
    /// </summary>
    public string GetUpgradeDescription()
    {
        if (!CanUpgrade) return "NIVEL MÁXIMO";

        int nextLvl = upgradeLevel + 1;
        string desc = $"<b>Mejora {nextLvl}/{MAX_UPGRADE_LEVEL}</b>\n";
        desc += $"  Daño: <color=#FF6666>{weaponData.damage}</color> → <color=#66FF66>{Mathf.RoundToInt(weaponData.damage * damageMult[nextLvl])}</color>\n";
        desc += $"  Cadencia: <color=#FF6666>{weaponData.fireRate:F2}s</color> → <color=#66FF66>{weaponData.fireRate * fireRateMult[nextLvl]:F2}s</color>\n";
        desc += $"  Cargador: <color=#FF6666>{weaponData.maxAmmoInMagazine}</color> → <color=#66FF66>{Mathf.RoundToInt(weaponData.maxAmmoInMagazine * magazineMult[nextLvl])}</color>\n";
        desc += $"  Reserva: <color=#FF6666>{weaponData.maxReserveAmmo}</color> → <color=#66FF66>{Mathf.RoundToInt(weaponData.maxReserveAmmo * magazineMult[nextLvl])}</color>\n";
        desc += $"  <color=#88CCFF>+ Recarga toda la munición</color>";
        return desc;
    }

    // =========================================================================
    // MELEE ANIMATION
    // =========================================================================

    /// <summary>
    /// Inicia la animación procedural de golpe cuerpo a cuerpo.
    /// El arma hace un swing fluido con spring physics.
    /// </summary>
    public void PlayMeleeAnimation()
    {
        if (isMeleeing) return;
        if (meleeCoroutine != null) StopCoroutine(meleeCoroutine);
        meleeCoroutine = StartCoroutine(MeleeAnimation());
    }

    /// <summary>
    /// ¿Está ejecutando la animación de melee?
    /// </summary>
    public bool IsMeleeing() => isMeleeing;

    /// <summary>
    /// Animación procedural de melee — 5 fases con curvas físicas orgánicas.
    /// Preparación → Wind-up → Strike → Impact → Recovery
    /// Duración total: ~0.9s — ritmo pesado, con arco de movimiento realista
    /// </summary>
    private System.Collections.IEnumerator MeleeAnimation()
    {
        isMeleeing = true;

        // Guardar estado de springs y congelarlos durante la animación
        Vector3 savedSpringPos = recoilPositionSpring;
        Vector3 savedSpringVel = recoilPositionVelocity;
        Vector3 savedSpringRot = recoilRotationSpring;
        Vector3 savedSpringRotVel = recoilRotationVelocity;
        recoilPositionSpring = Vector3.zero;
        recoilPositionVelocity = Vector3.zero;
        recoilRotationSpring = Vector3.zero;
        recoilRotationVelocity = Vector3.zero;

        Vector3 startPos = originalWeaponPosition;
        Quaternion startRot = originalWeaponRotation;

        // Duraciones de cada fase
        float prepTime     = 0.07f;  // Micro-hundimiento — tomar impulso
        float windUpTime   = 0.16f;  // Anticipación clara y legible
        float strikeTime   = 0.09f;  // Golpe acelerado con peso
        float impactTime   = 0.14f;  // Contacto + micro-rebote
        float recoveryTime = 0.48f;  // Retorno pesado con inercia

        // ═══════ FASE 0: PREP (hundimiento + agarre) ═══════
        // Sutil bajada + ligera compresión lateral — como agarrar el arma con fuerza
        Vector3 prepPos = startPos + new Vector3(0.008f, -0.018f, -0.025f);
        Quaternion prepRot = startRot * Quaternion.Euler(4f, 1f, -3f);

        float elapsed = 0f;
        while (elapsed < prepTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / prepTime);
            transform.localPosition = Vector3.Lerp(startPos, prepPos, t);
            transform.localRotation = Quaternion.Slerp(startRot, prepRot, t);
            yield return null;
        }

        // ═══════ FASE 1: WIND-UP (anticipación con arco) ═══════
        // El arma sube, retrocede y rota — arco de culatazo levantando el codo
        Vector3 windUpPos = startPos + new Vector3(0.03f, 0.065f, -0.12f);
        Quaternion windUpRot = startRot * Quaternion.Euler(22f, -12f, -9f);

        // Punto intermedio del arco (bezier) — el arma pasa por arriba-atrás
        Vector3 windUpMid = startPos + new Vector3(0.015f, 0.08f, -0.06f);

        elapsed = 0f;
        while (elapsed < windUpTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / windUpTime;
            // Ease-out cúbico suave
            float eased = 1f - (1f - t) * (1f - t) * (1f - t);

            // Bezier cuadrático: P = (1-t)²·A + 2(1-t)t·B + t²·C
            float u = 1f - eased;
            Vector3 bezierPos = u * u * prepPos + 2f * u * eased * windUpMid + eased * eased * windUpPos;

            transform.localPosition = bezierPos;
            transform.localRotation = Quaternion.Slerp(prepRot, windUpRot, eased);
            yield return null;
        }

        // ═══════ FASE 2: STRIKE (golpe con arco descendente) ═══════
        // Movimiento en arco: de arriba-atrás hacia adelante-abajo
        Vector3 strikePos = startPos + new Vector3(-0.04f, -0.08f, 0.22f);
        Quaternion strikeRot = startRot * Quaternion.Euler(-45f, 16f, 12f);

        // Punto medio del arco de strike — pasa por arriba antes de bajar
        Vector3 strikeMid = startPos + new Vector3(-0.01f, 0.02f, 0.10f);

        elapsed = 0f;
        while (elapsed < strikeTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / strikeTime;
            // Ease-in cuadrático — acelera como un brazo lanzando
            float eased = t * t;

            // Bezier cuadrático para el arco del golpe
            float u = 1f - eased;
            Vector3 bezierPos = u * u * windUpPos + 2f * u * eased * strikeMid + eased * eased * strikePos;

            // Overshoot al final — momentum del golpe
            float overshoot = 1f + Mathf.Sin(eased * Mathf.PI) * 0.05f;
            transform.localPosition = Vector3.LerpUnclamped(startPos, bezierPos, overshoot);
            transform.localRotation = Quaternion.SlerpUnclamped(windUpRot, strikeRot, eased);
            yield return null;
        }

        // ═══════ FASE 3: IMPACT (contacto + micro-rebote) ═══════
        // El arma vibra y hace un micro-rebote hacia atrás al contactar
        Vector3 impactBasePos = strikePos;
        Quaternion impactBaseRot = strikeRot;

        // Posición de rebote (el arma retrocede un poco tras el impacto)
        Vector3 bouncePos = strikePos + new Vector3(0.01f, 0.03f, -0.04f);

        elapsed = 0f;
        while (elapsed < impactTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / impactTime;

            // Micro-rebote: lerp rápido hacia bouncePos en el primer 40%
            float bounceT = Mathf.Clamp01(t / 0.4f);
            bounceT = 1f - (1f - bounceT) * (1f - bounceT); // ease-out
            Vector3 basePos = Vector3.Lerp(impactBasePos, bouncePos, bounceT);
            Quaternion baseRot = Quaternion.Slerp(impactBaseRot, 
                impactBaseRot * Quaternion.Euler(5f, -3f, -2f), bounceT);

            // Vibración superpuesta que decae exponencialmente
            float intensity = Mathf.Exp(-5f * t);
            float shakeX = Mathf.Sin(t * 32f) * 0.008f * intensity;
            float shakeY = Mathf.Cos(t * 38f) * 0.006f * intensity;
            float shakeZ = Mathf.Sin(t * 25f + 1.1f) * 0.004f * intensity;

            float rotShakeX = Mathf.Sin(t * 35f) * 2.2f * intensity;
            float rotShakeY = Mathf.Cos(t * 28f) * 1.3f * intensity;

            transform.localPosition = basePos + new Vector3(shakeX, shakeY, shakeZ);
            transform.localRotation = baseRot * Quaternion.Euler(rotShakeX, rotShakeY, 0f);
            yield return null;
        }

        // ═══════ FASE 4: RECOVERY (retorno pesado con inercia) ═══════
        // Spring amortiguado — el arma vuelve lentamente con peso real
        Vector3 recoveryStart = transform.localPosition;
        Quaternion recoveryStartRot = transform.localRotation;

        elapsed = 0f;
        while (elapsed < recoveryTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / recoveryTime;

            // Spring con damping fuerte y 1.5 oscilaciones — pesado y orgánico
            // f(t) = 1 - e^(-4t) * cos(1.8πt)
            float spring = 1f - Mathf.Exp(-4f * t) * Mathf.Cos(t * Mathf.PI * 1.8f);
            spring = Mathf.Clamp01(spring);

            transform.localPosition = Vector3.LerpUnclamped(recoveryStart, startPos, spring);
            transform.localRotation = Quaternion.SlerpUnclamped(recoveryStartRot, startRot, spring);
            yield return null;
        }

        // Asegurar posición final exacta
        transform.localPosition = originalWeaponPosition;
        transform.localRotation = originalWeaponRotation;

        isMeleeing = false;
        meleeCoroutine = null;
    }

    // =========================================================================
    // RECARGA
    // =========================================================================

    /// <summary>
    /// Inicia la recarga
    /// </summary>
    public virtual bool Reload()
    {
        if (isReloading) return false;
        if (currentReserveAmmo <= 0) return false;
        if (currentAmmoInMagazine >= GetUpgradedMagazineSize()) return false;

        isReloading = true;
        OnReloadStarted?.Invoke();

        // Animación procedural de recarga
        StartCoroutine(ReloadAnimation());

        // Reproducir sonido de recarga con pitch ajustado a la velocidad real
        // Si la recarga es más rápida (por Plussy u otras mejoras), el sonido se acelera
        float reloadSpeedRatio = weaponData.reloadTime / GetUpgradedReloadTime();
        if (audioSource != null)
        {
            audioSource.pitch = reloadSpeedRatio;
        }
        PlaySound(weaponData.reloadSound);

        // Llamar a FinishReload después del tiempo de recarga
        Invoke(nameof(FinishReload), GetUpgradedReloadTime());

        return true;
    }

    /// <summary>
    /// Animación procedural de recarga - multi-fase con spring physics
    /// Fases: Tilt/Eject → Magazine Out → Magazine In → Chamber/Snap Back
    /// </summary>
    protected System.Collections.IEnumerator ReloadAnimation()
    {
        float totalTime = GetUpgradedReloadTime();

        // Proporciones de cada fase (suman 1.0)
        float phase1Duration = totalTime * 0.18f; // Tilt - rotar arma para acceso al cargador
        float phase2Duration = totalTime * 0.25f; // Mag Out - sacar cargador (bajar + rotar)
        float phase3Duration = totalTime * 0.30f; // Mag In - insertar cargador nuevo (subir + snap)
        float phase4Duration = totalTime * 0.27f; // Chamber - tirar de la corredera y volver

        // Posiciones y rotaciones clave
        Vector3 startPos = originalWeaponPosition;
        Quaternion startRot = originalWeaponRotation;

        // Dirección lateral de la recarga: si el arma define 0 → usa 1 (por defecto)
        // Poner -1 en WeaponData.reloadDirectionMultiplier para invertir la dirección
        float dir = (weaponData != null && weaponData.reloadDirectionMultiplier != 0f)
            ? weaponData.reloadDirectionMultiplier
            : 1f;

        // Fase 1: Tilt - inclinar arma para exponer el cargador
        Vector3 tiltPos = startPos + new Vector3(-0.04f * dir, 0.02f, -0.03f);
        Quaternion tiltRot = startRot * Quaternion.Euler(-8f, -5f * dir, 25f * dir);

        // Fase 2: Mag Out - el cargador "se extrae" → arma baja y rota más
        Vector3 magOutPos = startPos + new Vector3(-0.06f * dir, -0.15f, -0.05f);
        Quaternion magOutRot = startRot * Quaternion.Euler(-15f, -8f * dir, 35f * dir);

        // Fase 3: Mag In - insertar nuevo cargador → arma vuelve parcialmente con snap
        Vector3 magInPos = startPos + new Vector3(-0.03f * dir, -0.02f, -0.02f);
        Quaternion magInRot = startRot * Quaternion.Euler(-5f, -3f * dir, 12f * dir);

        // Fase 4: Chamber - slide rack → poke forward luego spring back a posición original
        Vector3 chamberPos = startPos + new Vector3(0f, 0.01f, 0.04f);
        Quaternion chamberRot = startRot * Quaternion.Euler(-3f, 0f, -2f * dir);

        float elapsed;

        // ═══════ FASE 1: TILT (inclinar) ═══════
        elapsed = 0f;
        while (elapsed < phase1Duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / phase1Duration);
            transform.localPosition = Vector3.Lerp(startPos, tiltPos, t);
            transform.localRotation = Quaternion.Slerp(startRot, tiltRot, t);
            yield return null;
        }

        // ═══════ FASE 2: MAGAZINE OUT (sacar cargador) ═══════
        elapsed = 0f;
        while (elapsed < phase2Duration)
        {
            elapsed += Time.deltaTime;
            float raw = elapsed / phase2Duration;
            // Ease-in-out con un pop al final
            float t = raw < 0.7f
                ? Mathf.SmoothStep(0f, 1f, raw / 0.7f)
                : 1f + Mathf.Sin((raw - 0.7f) / 0.3f * Mathf.PI) * 0.04f; // micro-bounce
            
            transform.localPosition = Vector3.LerpUnclamped(tiltPos, magOutPos, t);
            transform.localRotation = Quaternion.SlerpUnclamped(tiltRot, magOutRot, Mathf.Min(t, 1f));
            yield return null;
        }

        // ═══════ FASE 3: MAGAZINE IN (insertar cargador) ═══════
        elapsed = 0f;
        while (elapsed < phase3Duration)
        {
            elapsed += Time.deltaTime;
            float raw = elapsed / phase3Duration;

            // Movimiento lento al principio, snap rápido + overshoot al final (inserción)
            float t;
            if (raw < 0.6f)
            {
                // Acercar lentamente
                t = Mathf.SmoothStep(0f, 0.7f, raw / 0.6f);
            }
            else
            {
                // Snap + overshoot (spring)
                float springT = (raw - 0.6f) / 0.4f;
                float overshoot = 1f + Mathf.Exp(-6f * springT) * Mathf.Sin(springT * Mathf.PI * 3f) * 0.12f;
                t = Mathf.LerpUnclamped(0.7f, 1f, overshoot);
            }

            transform.localPosition = Vector3.LerpUnclamped(magOutPos, magInPos, t);
            transform.localRotation = Quaternion.SlerpUnclamped(magOutRot, magInRot, Mathf.Clamp01(t));
            yield return null;
        }

        // ═══════ FASE 4: CHAMBER (corredera + spring back) ═══════
        elapsed = 0f;
        while (elapsed < phase4Duration)
        {
            elapsed += Time.deltaTime;
            float raw = elapsed / phase4Duration;

            Vector3 targetPos;
            Quaternion targetRot;

            if (raw < 0.35f)
            {
                // Empujar hacia delante (tirar corredera)
                float t = Mathf.SmoothStep(0f, 1f, raw / 0.35f);
                targetPos = Vector3.Lerp(magInPos, chamberPos, t);
                targetRot = Quaternion.Slerp(magInRot, chamberRot, t);
            }
            else
            {
                // Spring back a posición original con overshoot
                float springRaw = (raw - 0.35f) / 0.65f;
                float decay = Mathf.Exp(-8f * springRaw);
                float oscillation = Mathf.Sin(springRaw * Mathf.PI * 4f);
                float springT = 1f - decay * (1f - 0.15f * oscillation);

                targetPos = Vector3.LerpUnclamped(chamberPos, startPos, springT);
                targetRot = Quaternion.SlerpUnclamped(chamberRot, startRot, Mathf.Clamp01(springT));
            }

            transform.localPosition = targetPos;
            transform.localRotation = targetRot;
            yield return null;
        }

        // Asegurar posición final exacta
        transform.localPosition = originalWeaponPosition;
        transform.localRotation = originalWeaponRotation;
    }

    /// <summary>
    /// Finaliza la recarga
    /// </summary>
    protected virtual void FinishReload()
    {
        int ammoNeeded = GetUpgradedMagazineSize() - currentAmmoInMagazine;
        int ammoToReload = Mathf.Min(ammoNeeded, currentReserveAmmo);

        currentAmmoInMagazine += ammoToReload;
        currentReserveAmmo -= ammoToReload;

        isReloading = false;

        // Restaurar pitch normal del AudioSource
        if (audioSource != null)
        {
            audioSource.pitch = 1f;
        }

        OnReloadFinished?.Invoke();
        OnAmmoChanged?.Invoke(currentAmmoInMagazine, currentReserveAmmo);
    }

    // =========================================================================
    // UTILIDADES
    // =========================================================================

    /// <summary>
    /// Reproduce un sonido
    /// </summary>
    protected void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    /// <summary>
    /// Reproduce un sonido con pitch personalizado (útil para recarga acelerada).
    /// NOTA: No restaura el pitch automáticamente, debe restaurarse manualmente.
    /// </summary>
    protected void PlaySoundWithPitch(AudioClip clip, float pitch)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.pitch = pitch;
            audioSource.PlayOneShot(clip);
        }
    }

    /// <summary>
    /// Reproduce el sonido de cargador vacío con cooldown para evitar que se
    /// reproduzca cada frame en armas automáticas
    /// </summary>
    protected void PlayEmptySound()
    {
        if (Time.time >= nextEmptySoundTime)
        {
            PlaySound(weaponData.emptySound);
            nextEmptySoundTime = Time.time + EMPTY_SOUND_COOLDOWN;
        }
    }

    /// <summary>
    /// Obtiene la munición actual
    /// </summary>
    public void GetAmmo(out int inMagazine, out int inReserve)
    {
        inMagazine = currentAmmoInMagazine;
        inReserve = currentReserveAmmo;
    }

    /// <summary>
    /// Obtiene los datos del arma
    /// </summary>
    public WeaponData GetWeaponData()
    {
        return weaponData;
    }

    /// <summary>
    /// Verifica si está recargando
    /// </summary>
    public bool IsReloading()
    {
        return isReloading;
    }

    /// <summary>
    /// Añade munición a la reserva
    /// </summary>
    public void AddAmmo(int amount)
    {
        currentReserveAmmo = Mathf.Min(currentReserveAmmo + amount, GetUpgradedMaxReserve());
        OnAmmoChanged?.Invoke(currentAmmoInMagazine, currentReserveAmmo);
    }

    /// <summary>
    /// Rellena toda la munición (cargador + reserva al máximo). Usado por Max Ammo.
    /// </summary>
    public void RefillAmmo()
    {
        currentAmmoInMagazine = GetUpgradedMagazineSize();
        currentReserveAmmo = GetUpgradedMaxReserve();
        OnAmmoChanged?.Invoke(currentAmmoInMagazine, currentReserveAmmo);
    }

    /// <summary>
    /// Activar arma (llamado cuando se equipa)
    /// </summary>
    public virtual void OnEquip()
    {
        gameObject.SetActive(true);
        // Asegurar que la munición está inicializada (por si Awake no se ejecutó aún)
        InitializeAmmo();
        currentSpread = GetUpgradedBaseSpread(); // Resetear dispersión al equipar
        OnAmmoChanged?.Invoke(currentAmmoInMagazine, currentReserveAmmo);
    }

    /// <summary>
    /// Desactivar arma (llamado cuando se desequipa)
    /// </summary>
    public virtual void OnUnequip()
    {
        // Cancelar recarga si está en progreso
        if (isReloading)
        {
            CancelInvoke(nameof(FinishReload));
            StopAllCoroutines();
            isReloading = false;

            // Restaurar pitch normal
            if (audioSource != null)
            {
                audioSource.pitch = 1f;
            }
        }

        // Cancelar melee si está en progreso
        isMeleeing = false;
        meleeCoroutine = null;

        // Restaurar posición original por si se canceló una animación a mitad
        transform.localPosition = originalWeaponPosition;
        transform.localRotation = originalWeaponRotation;

        gameObject.SetActive(false);
    }
}
