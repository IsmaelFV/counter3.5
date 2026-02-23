using UnityEngine;

/// <summary>
/// Gestiona el sistema completo de armas: cambio, disparo, recarga,
/// efectos de impacto, trazadores y muzzle flash.
/// Integrado con sistemas procedurales (sin Animator)
/// </summary>
public class WeaponManager : MonoBehaviour
{
    [Header("Configuración de Armas")]
    [SerializeField] private WeaponBase[] weaponSlots = new WeaponBase[3]; // 2 base + 1 Mule Kick
    [SerializeField] private int currentWeaponIndex = 0;
    private int activeSlotCount = 2; // Slots activos (2 por defecto, 3 con Mule Kick)

    [Header("Referencias")]
    [SerializeField] private Transform weaponHolder;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private AudioSource audioSource;

    [Header("Sistemas Procedurales")]
    [SerializeField] private ScreenShake screenShake;
    [SerializeField] private ProceduralFOVEffect fovEffect;

    [Header("UI")]
    [SerializeField] private WeaponUI weaponUI;
    [SerializeField] private HitmarkerUI hitmarkerUI;

    // ===== EVENTOS PARA UIManager =====
    /// <summary>Evento: arma cambiada (nombre, icono, esAutomática)</summary>
    public System.Action<string, Sprite, bool> OnWeaponSwitched;
    /// <summary>Evento: munición actualizada (enCargador, enReserva)</summary>
    public System.Action<int, int> OnAmmoUpdated;
    /// <summary>Evento: recarga iniciada</summary>
    public System.Action OnReloadStart;
    /// <summary>Evento: recarga finalizada</summary>
    public System.Action OnReloadEnd;
    /// <summary>Evento: sin munición (cargador vacío + reserva vacía)</summary>
    public System.Action OnOutOfAmmo;

    [Header("Efectos de Disparo")]
    [SerializeField] private GameObject muzzleFlashPrefab;

    [Header("Efectos de Impacto")]
    [Tooltip("Prefab del efecto de impacto genérico (paredes, suelo, etc.)")]
    [SerializeField] private GameObject bulletImpactPrefab;
    [Tooltip("Prefab del efecto de impacto en enemigos (sangre, etc.)")]
    [SerializeField] private GameObject enemyImpactPrefab;
    [Tooltip("Offset del impacto respecto a la superficie para evitar z-fighting")]
    [SerializeField] private float impactOffset = 0.01f;

    [Header("Trazador de Bala")]
    [Tooltip("Prefab del trazador (debe tener BulletTracer.cs + LineRenderer)")]
    [SerializeField] private GameObject bulletTracerPrefab;

    [Header("Sonidos de Impacto")]
    [SerializeField] private AudioClip[] wallImpactSounds;
    [SerializeField] private AudioClip[] metalImpactSounds;
    [SerializeField] private AudioClip[] fleshImpactSounds;
    [SerializeField, Range(0f, 1f)] private float impactSoundVolume = 0.4f;

    [Header("Capas de Colisión")]
    [SerializeField] private LayerMask targetLayer;
    [Tooltip("Tag de los objetos metálicos para sonido diferenciado")]
    [SerializeField] private string metalTag = "Metal";

    [Header("Input")]
    [SerializeField] private KeyCode reloadKey = KeyCode.R;
    [SerializeField] private KeyCode meleeKey = KeyCode.V;
    [SerializeField] private bool enableMouseWheelSwitch = true;

    [Header("=== ADS (Apuntar) ===")]
    [Tooltip("Multiplicador de sensibilidad del mouse al apuntar")]
    [SerializeField] private float adsSensitivityMultiplier = 0.6f;
    [Tooltip("Velocidad de transición al apuntar")]
    [SerializeField] private float adsTransitionSpeed = 8f;

    [Header("=== MELEE ===")]
    [Tooltip("Daño del golpe cuerpo a cuerpo")]
    [SerializeField] private int meleeDamage = 50;
    [Tooltip("Rango del golpe (raycast desde cámara)")]
    [SerializeField] private float meleeRange = 2.5f;
    [Tooltip("Fuerza de empuje al golpear")]
    [SerializeField] private float meleePushForce = 12f;
    [Tooltip("Cooldown entre golpes")]
    [SerializeField] private float meleeCooldown = 0.9f;
    [Tooltip("Capas que el melee puede golpear (incluir enemigos y entorno)")]
    [SerializeField] private LayerMask meleeTargetLayer = ~0;
    [SerializeField] private AudioClip meleeSwingSound;
    [SerializeField] private AudioClip meleeHitSound;

    private WeaponBase currentWeapon;
    private float scrollInput = 0f;

    // ADS state
    private bool isAiming = false;
    private float originalMouseSensitivity;
    private PlayerMovement playerMovement;

    // Melee state
    private float meleeNextUseTime = 0f;

    void Start()
    {
        // Validar configuración
        if (weaponSlots.Length < 2)
        {
            Debug.LogError("WeaponManager requiere al menos 2 slots de armas");
            return;
        }

        // Buscar referencias si no están asignadas
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // Buscar sistemas procedurales si no están asignados
        if (screenShake == null)
            screenShake = GetComponentInChildren<ScreenShake>();

        if (fovEffect == null)
            fovEffect = GetComponentInChildren<ProceduralFOVEffect>();

        // Buscar HitmarkerUI si no está asignado
        if (hitmarkerUI == null)
            hitmarkerUI = GetComponent<HitmarkerUI>();
        // Auto-añadir HitmarkerUI si no existe (necesario para hitmarkers y headshots)
        if (hitmarkerUI == null)
        {
            hitmarkerUI = gameObject.AddComponent<HitmarkerUI>();
            Debug.Log("[WeaponManager] HitmarkerUI añadido automáticamente.");
        }

        // Buscar PlayerMovement para control de sensibilidad
        playerMovement = GetComponentInParent<PlayerMovement>();
        if (playerMovement != null)
            originalMouseSensitivity = playerMovement.GetMouseSensitivity();

        // Equipar primera arma (desactivar las demás)
        for (int i = 0; i < weaponSlots.Length; i++)
        {
            if (weaponSlots[i] != null && i != 0)
                weaponSlots[i].gameObject.SetActive(false);
        }
        EquipWeapon(0);
    }

    void Update()
    {
        if (currentWeapon == null) return;

        HandleWeaponSwitch();
        HandleADS();
        HandleShooting();
        HandleMelee();
        HandleReload();
    }

    // =========================================================================
    // INPUT
    // =========================================================================

    /// <summary>
    /// Maneja el cambio de armas
    /// </summary>
    private void HandleWeaponSwitch()
    {
        // Cambio con teclas 1, 2 y 3
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            EquipWeapon(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            EquipWeapon(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3) && activeSlotCount >= 3)
        {
            EquipWeapon(2);
        }

        // Cambio con rueda del ratón (deshabilitado mientras apunta)
        if (enableMouseWheelSwitch && !isAiming)
        {
            scrollInput = Input.GetAxis("Mouse ScrollWheel");

            if (scrollInput > 0f)
            {
                // Buscar el siguiente slot válido (que tenga arma)
                int nextIndex = FindNextValidSlot(currentWeaponIndex, 1);
                if (nextIndex != currentWeaponIndex)
                    EquipWeapon(nextIndex);
            }
            else if (scrollInput < 0f)
            {
                // Buscar el anterior slot válido (que tenga arma)
                int prevIndex = FindNextValidSlot(currentWeaponIndex, -1);
                if (prevIndex != currentWeaponIndex)
                    EquipWeapon(prevIndex);
            }
        }
    }

    /// <summary>
    /// Maneja el sistema ADS (apuntar con zoom)
    /// </summary>
    private void HandleADS()
    {
        bool wantsAim = Input.GetMouseButton(1); // Botón derecho

        isAiming = wantsAim;

        // Comunicar al FOV effect
        // Actualizar velocidad de transición FOV con adsTransitionSpeed
        if (fovEffect != null)
        {
            fovEffect.SetAiming(isAiming);
            fovEffect.SetTransitionSpeed(isAiming ? adsTransitionSpeed : 10f);
        }

        // Reducir sensibilidad al apuntar
        if (playerMovement != null)
        {
            if (isAiming)
            {
                playerMovement.SetMouseSensitivity(originalMouseSensitivity * adsSensitivityMultiplier);
            }
            else
            {
                // Restaurar inmediatamente (sin drift asintótico)
                playerMovement.SetMouseSensitivity(originalMouseSensitivity);
            }
        }
    }

    /// <summary>
    /// Maneja el disparo — semiautomático o automático según el arma
    /// </summary>
    private void HandleShooting()
    {
        WeaponData data = currentWeapon.GetWeaponData();
        if (data == null)
        {
            Debug.LogWarning($"[WeaponManager] El arma '{currentWeapon.name}' no tiene WeaponData asignado. Asigna un WeaponData en el prefab.");
            return;
        }

        // Burst rifle y semi-auto siempre usan GetMouseButtonDown
        // Automáticas usan GetMouseButton (mantener)
        bool wantsShoot;
        if (data.fireMode == FireMode.Burst || !data.isAutomatic)
        {
            wantsShoot = Input.GetMouseButtonDown(0);
        }
        else
        {
            wantsShoot = Input.GetMouseButton(0);
        }

        if (wantsShoot)
        {
            bool didShoot = currentWeapon.TryShoot();
            
            if (didShoot)
            {
                // Muzzle flash
                SpawnMuzzleFlash();
                
                // Screen shake procedural
                if (screenShake != null)
                {
                    screenShake.ShootShake();
                }

                // FOV punch al disparar
                if (fovEffect != null)
                {
                    fovEffect.ShootPunch();
                }
            }
        }
    }

    /// <summary>
    /// Maneja el ataque cuerpo a cuerpo (melee)
    /// </summary>
    private void HandleMelee()
    {
        if (Input.GetKeyDown(meleeKey) && Time.time >= meleeNextUseTime)
        {
            // No melee durante recarga o melee en curso
            if (currentWeapon != null && (currentWeapon.IsReloading() || currentWeapon.IsMeleeing()))
                return;

            PerformMelee();
            meleeNextUseTime = Time.time + meleeCooldown;
        }
    }

    /// <summary>
    /// Ejecuta el ataque melee
    /// </summary>
    private void PerformMelee()
    {
        // Animación procedural del arma
        if (currentWeapon != null)
            currentWeapon.PlayMeleeAnimation();

        // Sonido de golpe
        if (audioSource != null && meleeSwingSound != null)
            audioSource.PlayOneShot(meleeSwingSound, 0.7f);

        // Null check
        if (mainCamera == null) return;

        // Raycast desde el centro de la cámara
        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, meleeRange, meleeTargetLayer))
        {
            // Aplicar daño
            IDamageable damageable = hit.collider.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(meleeDamage);

                // Sonido de impacto
                if (audioSource != null && meleeHitSound != null)
                    audioSource.PlayOneShot(meleeHitSound, 0.8f);

                // Efecto de impacto enemigo
                if (enemyImpactPrefab != null)
                {
                    GameObject impact = Instantiate(enemyImpactPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                    Destroy(impact, 2f);
                }
            }
            else
            {
                // Impacto en pared/objeto
                if (bulletImpactPrefab != null)
                {
                    GameObject impact = Instantiate(bulletImpactPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                    Destroy(impact, 2f);
                }
            }

            // Empuje físico
            Rigidbody rb = hit.collider.GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic)
            {
                Vector3 pushDirection = (hit.point - mainCamera.transform.position).normalized;
                rb.AddForce(pushDirection * meleePushForce, ForceMode.Impulse);
            }

            // Screen shake del golpe
            if (screenShake != null)
                screenShake.Shake(0.03f, 1.5f, 0.15f);
        }
    }

    /// <summary>
    /// Maneja la recarga
    /// </summary>
    private void HandleReload()
    {
        if (Input.GetKeyDown(reloadKey))
        {
            currentWeapon.Reload();
        }
    }

    // =========================================================================
    // EQUIPAMIENTO DE ARMAS
    // =========================================================================

    /// <summary>
    /// Equipa un arma específica
    /// </summary>
    private void EquipWeapon(int index)
    {
        if (index < 0 || index >= activeSlotCount || index >= weaponSlots.Length) return;
        if (weaponSlots[index] == null)
        {
            Debug.LogWarning($"Weapon slot {index} está vacío");
            return;
        }

        // No cambiar si es la misma arma
        if (index == currentWeaponIndex && currentWeapon != null) return;

        // Si el arma actual estaba recargando, ocultar indicador de recarga antes de desuscribirse
        if (currentWeapon != null && currentWeapon.IsReloading())
        {
            if (weaponUI != null)
                weaponUI.ShowReloadIndicator(false);
            OnReloadEnd?.Invoke();
        }

        // Desuscribirse del arma anterior
        UnsubscribeFromWeaponEvents();

        // Resetear ADS si estaba apuntando
        if (isAiming && playerMovement != null)
        {
            playerMovement.SetMouseSensitivity(originalMouseSensitivity);
        }
        isAiming = false;
        if (fovEffect != null)
            fovEffect.SetAiming(false);

        // Desequipar arma actual
        if (currentWeapon != null)
        {
            currentWeapon.OnUnequip();
        }

        // Equipar nueva arma
        currentWeaponIndex = index;
        currentWeapon = weaponSlots[index];

        // Suscribirse a eventos ANTES de OnEquip para capturar OnAmmoChanged
        SubscribeToWeaponEvents();

        currentWeapon.OnEquip();

        // Actualizar UI (lectura directa de munición por si el evento no llegó)
        UpdateWeaponUI();

        // Notificar cambio de arma al UIManager
        WeaponData data2 = currentWeapon.GetWeaponData();
        if (data2 != null)
        {
            // Nombre con fallback por tipo de arma
            string name = data2.weaponName;
            if (string.IsNullOrEmpty(name) || name == "Weapon")
            {
                if (currentWeapon is Weapon_Rifle) name = "Rifle de Asalto";
                else if (currentWeapon is Weapon_Pistol) name = "Pistola";
                else if (currentWeapon is Weapon_Shotgun) name = "Escopeta";
                else if (currentWeapon is Weapon_ShotgunAuto) name = "Escopeta Automática";
                else if (currentWeapon is Weapon_BurstRifle) name = "Rifle de Ráfagas";
                else if (currentWeapon is Weapon_SemiAutoRifle) name = "Rifle Semi-Auto";
                else name = currentWeapon.GetType().Name;
            }

            OnWeaponSwitched?.Invoke(name, data2.weaponIcon, data2.isAutomatic);
            Debug.Log($"Arma equipada: {name}");
        }
    }

    // =========================================================================
    // SUSCRIPCIÓN A EVENTOS
    // =========================================================================

    /// <summary>
    /// Suscribirse a todos los eventos del arma actual
    /// </summary>
    private void SubscribeToWeaponEvents()
    {
        if (currentWeapon == null) return;

        currentWeapon.OnAmmoChanged += OnAmmoChanged;
        currentWeapon.OnReloadStarted += OnReloadStarted;
        currentWeapon.OnReloadFinished += OnReloadFinished;
        currentWeapon.OnBulletImpact += OnBulletImpact;
        currentWeapon.OnShootWithTracer += OnShootWithTracer;
    }

    /// <summary>
    /// Desuscribirse de todos los eventos del arma actual
    /// </summary>
    private void UnsubscribeFromWeaponEvents()
    {
        if (currentWeapon == null) return;

        currentWeapon.OnAmmoChanged -= OnAmmoChanged;
        currentWeapon.OnReloadStarted -= OnReloadStarted;
        currentWeapon.OnReloadFinished -= OnReloadFinished;
        currentWeapon.OnBulletImpact -= OnBulletImpact;
        currentWeapon.OnShootWithTracer -= OnShootWithTracer;
    }

    // =========================================================================
    // CALLBACKS DE EVENTOS
    // =========================================================================

    /// <summary>
    /// Callback: munición cambió
    /// </summary>
    private void OnAmmoChanged(int inMagazine, int inReserve)
    {
        if (weaponUI != null)
        {
            weaponUI.UpdateAmmo(inMagazine, inReserve);
        }

        OnAmmoUpdated?.Invoke(inMagazine, inReserve);

        if (inMagazine == 0 && inReserve == 0)
        {
            OnOutOfAmmo?.Invoke();
        }
    }

    /// <summary>
    /// Callback: recarga empezó
    /// </summary>
    private void OnReloadStarted()
    {
        if (weaponUI != null)
        {
            weaponUI.ShowReloadIndicator(true);
        }
        OnReloadStart?.Invoke();
    }

    /// <summary>
    /// Callback: recarga terminó
    /// </summary>
    private void OnReloadFinished()
    {
        if (weaponUI != null)
        {
            weaponUI.ShowReloadIndicator(false);
        }
        OnReloadEnd?.Invoke();
    }

    /// <summary>
    /// Callback: bala impactó un objeto — instanciar efecto visual de impacto
    /// </summary>
    private void OnBulletImpact(Vector3 hitPoint, Vector3 hitNormal, bool wasEnemy)
    {
        try
        {
            SpawnImpactEffect(hitPoint, hitNormal, wasEnemy);
            PlayImpactSound(hitPoint, wasEnemy);

            // Hitmarker al impactar enemigo
            if (wasEnemy && hitmarkerUI != null)
            {
                // Detectar headshot: buscar si el collider más cercano al punto de impacto tiene tag "Head"
                bool isHeadshot = false;
                try
                {
                    // Aumentar ligeramente el radio para mayor precisión en la detección del headshot
                    Collider[] nearbyColliders = Physics.OverlapSphere(hitPoint, 0.2f);
                    foreach (var col in nearbyColliders)
                    {
                        if (col != null && col.CompareTag("Head"))
                        {
                            isHeadshot = true;
                            break;
                        }
                    }
                }
                catch (System.Exception) { /* Tag 'Head' no existe o error en detección — ignorar */ }
                
                // Activar hitmarker (rojo si es headshot)
                hitmarkerUI.ShowHitmarker(isHeadshot);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[WeaponManager] Error en OnBulletImpact: {e.Message}");
        }
    }

    /// <summary>
    /// Callback: disparo realizado — crear trazador visual
    /// </summary>
    private void OnShootWithTracer(Vector3 muzzlePos, Vector3 endPoint, bool didHit)
    {
        SpawnBulletTracer(muzzlePos, endPoint);
    }

    // =========================================================================
    // EFECTOS VISUALES
    // =========================================================================

    /// <summary>
    /// Instancia el efecto de muzzle flash en la punta del arma
    /// </summary>
    private void SpawnMuzzleFlash()
    {
        if (muzzleFlashPrefab == null) return;

        Transform muzzlePoint = GetCurrentMuzzlePoint();
        if (muzzlePoint == null) return;

        Instantiate(muzzleFlashPrefab, muzzlePoint.position, muzzlePoint.rotation);
    }

    /// <summary>
    /// Instancia el efecto de impacto de bala en la superficie
    /// </summary>
    private void SpawnImpactEffect(Vector3 hitPoint, Vector3 hitNormal, bool wasEnemy)
    {
        // Seleccionar prefab según el tipo de impacto
        GameObject prefab = wasEnemy ? enemyImpactPrefab : bulletImpactPrefab;
        if (prefab == null) prefab = bulletImpactPrefab; // Fallback

        if (prefab == null) return;

        // Instanciar con rotación orientada a la normal de la superficie
        Vector3 spawnPos = hitPoint + hitNormal * impactOffset;
        Quaternion spawnRot = Quaternion.LookRotation(hitNormal);

        // Auto-destruir para evitar leak de memoria
        GameObject fx = Instantiate(prefab, spawnPos, spawnRot);
        Destroy(fx, 3f);
    }

    /// <summary>
    /// Crea el trazador de bala visual (LineRenderer)
    /// </summary>
    private void SpawnBulletTracer(Vector3 startPos, Vector3 endPos)
    {
        if (currentWeapon == null) return;

        WeaponData data = currentWeapon.GetWeaponData();
        if (!data.showBulletTracer) return;

        if (bulletTracerPrefab != null)
        {
            // Usar prefab existente con BulletTracer
            GameObject tracerObj = Instantiate(bulletTracerPrefab, startPos, Quaternion.identity);
            BulletTracer tracer = tracerObj.GetComponent<BulletTracer>();
            if (tracer != null)
            {
                tracer.Initialize(startPos, endPos, data.tracerColor, data.tracerSpeed);
            }
        }
        else
        {
            // Crear trazador dinámicamente si no hay prefab
            CreateDynamicTracer(startPos, endPos, data);
        }
    }

    /// <summary>
    /// Crea un trazador dinámico cuando no hay prefab asignado
    /// </summary>
    private void CreateDynamicTracer(Vector3 start, Vector3 end, WeaponData data)
    {
        GameObject tracerObj = new GameObject("BulletTracer");
        tracerObj.AddComponent<LineRenderer>();
        BulletTracer tracer = tracerObj.AddComponent<BulletTracer>();
        tracer.Initialize(start, end, data.tracerColor, data.tracerSpeed);
    }

    /// <summary>
    /// Reproduce sonido de impacto según el tipo de superficie
    /// </summary>
    private void PlayImpactSound(Vector3 position, bool wasEnemy, Collider hitCollider = null)
    {
        AudioClip clip = null;

        if (wasEnemy && fleshImpactSounds != null && fleshImpactSounds.Length > 0)
        {
            clip = fleshImpactSounds[Random.Range(0, fleshImpactSounds.Length)];
        }
        else if (hitCollider != null && hitCollider.CompareTag(metalTag) && metalImpactSounds != null && metalImpactSounds.Length > 0)
        {
            clip = metalImpactSounds[Random.Range(0, metalImpactSounds.Length)];
        }
        else if (wallImpactSounds != null && wallImpactSounds.Length > 0)
        {
            clip = wallImpactSounds[Random.Range(0, wallImpactSounds.Length)];
        }

        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, position, impactSoundVolume);
        }
    }

    // =========================================================================
    // UTILIDADES
    // =========================================================================

    /// <summary>
    /// Obtiene el punto de disparo del arma actual
    /// </summary>
    private Transform GetCurrentMuzzlePoint()
    {
        if (currentWeapon == null) return null;

        // Primero intentar con el getter del arma
        Transform muzzle = currentWeapon.GetMuzzlePoint();
        if (muzzle != null) return muzzle;

        // Fallback: buscar por nombre
        return currentWeapon.transform.Find("MuzzlePoint");
    }

    /// <summary>
    /// Encuentra el siguiente slot válido (con arma) en la dirección dada.
    /// Evita saltar a slots vacíos o fuera de rango.
    /// </summary>
    private int FindNextValidSlot(int currentIndex, int direction)
    {
        int maxSlots = Mathf.Min(activeSlotCount, weaponSlots.Length);
        for (int i = 1; i < maxSlots; i++)
        {
            int checkIndex = (currentIndex + direction * i + maxSlots) % maxSlots;
            if (checkIndex >= 0 && checkIndex < weaponSlots.Length && weaponSlots[checkIndex] != null)
                return checkIndex;
        }
        return currentIndex; // No se encontró otro válido
    }

    // =========================================================================
    // UI
    // =========================================================================

    /// <summary>
    /// Actualiza la UI con información del arma actual
    /// </summary>
    private void UpdateWeaponUI()
    {
        if (weaponUI == null || currentWeapon == null) return;

        WeaponData data = currentWeapon.GetWeaponData();
        currentWeapon.GetAmmo(out int inMag, out int inReserve);

        // Nombre del arma: usar weaponName del ScriptableObject, con fallback por tipo
        string displayName = data.weaponName;
        if (string.IsNullOrEmpty(displayName) || displayName == "Weapon")
        {
            if (currentWeapon is Weapon_Rifle)
                displayName = "Rifle de Asalto";
            else if (currentWeapon is Weapon_Pistol)
                displayName = "Pistola";
            else if (currentWeapon is Weapon_Shotgun)
                displayName = "Escopeta";
            else if (currentWeapon is Weapon_ShotgunAuto)
                displayName = "Escopeta Automática";
            else if (currentWeapon is Weapon_BurstRifle)
                displayName = "Rifle de Ráfagas";
            else if (currentWeapon is Weapon_SemiAutoRifle)
                displayName = "Rifle Semi-Auto";
            else
                displayName = currentWeapon.GetType().Name;
        }

        weaponUI.UpdateWeaponInfo(displayName, data.weaponIcon);
        weaponUI.UpdateFireMode(data.isAutomatic);
        weaponUI.UpdateAmmo(inMag, inReserve);
    }

    /// <summary>
    /// Añade munición al arma actual
    /// </summary>
    public void AddAmmo(int amount)
    {
        if (currentWeapon != null)
        {
            currentWeapon.AddAmmo(amount);
        }
    }

    /// <summary>
    /// Obtiene el arma actual
    /// </summary>
    public WeaponBase GetCurrentWeapon()
    {
        return currentWeapon;
    }

    /// <summary>
    /// Obtiene un arma específica por índice
    /// </summary>
    public WeaponBase GetWeapon(int index)
    {
        if (index >= 0 && index < weaponSlots.Length)
            return weaponSlots[index];
        return null;
    }

    /// <summary>
    /// Obtiene el número de slots activos
    /// </summary>
    public int GetActiveSlotCount() => activeSlotCount;

    /// <summary>
    /// Obtiene el índice del slot actualmente equipado (para la caja misteriosa, etc.)
    /// </summary>
    public int GetCurrentWeaponIndex() => currentWeaponIndex;

    /// <summary>
    /// Activa o desactiva el tercer slot de arma (Mule Kick).
    /// Al desactivar, si el jugador tiene equipada el arma 3, cambia a la 1.
    /// </summary>
    public void EnableThirdSlot(bool enable)
    {
        if (enable)
        {
            // Solo activar si el array tiene al menos 3 slots
            if (weaponSlots.Length >= 3)
            {
                activeSlotCount = 3;
                Debug.Log("[WEAPON] Tercer slot de arma desbloqueado (Mule Kick)");
            }
            else
            {
                Debug.LogWarning("[WEAPON] No se puede activar el 3er slot: weaponSlots solo tiene " + weaponSlots.Length + " elementos. Amplía el array en el Inspector a 3.");
            }
        }
        else
        {
            // Si tiene equipada la 3ª arma, cambiar a la 1ª
            if (currentWeaponIndex >= 2)
            {
                EquipWeapon(0);
            }
            activeSlotCount = 2;
            Debug.Log("[WEAPON] Tercer slot de arma bloqueado");
        }
    }

    /// <summary>
    /// Cambia al arma del slot indicado (público para uso desde MysteryBox, wall buys, etc.)
    /// </summary>
    public void SwitchToWeapon(int index)
    {
        EquipWeapon(index);
    }

    /// <summary>
    /// Asigna un arma a un slot específico (para caja misteriosa, wall buys, etc.)
    /// </summary>
    public void SetWeaponInSlot(int slotIndex, WeaponBase weapon)
    {
        if (slotIndex < 0 || slotIndex >= weaponSlots.Length) return;

        // Si ya hay arma en ese slot, desequiparla
        if (weaponSlots[slotIndex] != null && weaponSlots[slotIndex] == currentWeapon)
        {
            UnsubscribeFromWeaponEvents();
            currentWeapon.OnUnequip();
        }

        weaponSlots[slotIndex] = weapon;

        // Si es el slot actual, equipar la nueva
        if (slotIndex == currentWeaponIndex)
        {
            currentWeapon = weapon;
            if (currentWeapon != null)
            {
                currentWeapon.OnEquip();
                SubscribeToWeaponEvents();
                UpdateWeaponUI();
            }
        }
    }

    /// <summary>
    /// Rellena toda la munición de todas las armas (Max Ammo power-up).
    /// </summary>
    public void RefillAllAmmo()
    {
        for (int i = 0; i < activeSlotCount; i++)
        {
            if (weaponSlots[i] != null)
            {
                weaponSlots[i].RefillAmmo();
            }
        }
        Debug.Log("[WEAPON] Toda la munición recargada");
    }

    void OnDisable()
    {
        UnsubscribeFromWeaponEvents();
    }
}
