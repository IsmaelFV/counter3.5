using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Tipos de perks disponibles en el juego.
/// </summary>
public enum PerkType
{
    Juggernog,      // Duplica la vida máxima (200 HP)
    Plussy,         // Recarga mucho más rápido
    MuleKick,       // 3er slot de arma
    StaminUp,       // Sprint más rápido + infinito
    PHDFlopper      // Slide daña, empuja y aturde enemigos
}

/// <summary>
/// Gestiona todas las perks activas del jugador.
/// Singleton accesible desde cualquier script.
/// Añadir al objeto Player.
///
/// Cada perk modifica sistemas existentes:
///   - Juggernog  → PlayerHealth.maxHealth x2
///   - Plussy    → WeaponBase.reloadTime x0.35
///   - Mule Kick  → WeaponManager amplía a 3 slots
///   - Stamin-Up  → PlayerMovement sprint más rápido + sin límite
///   - PhD Flopper→ PlayerMovement slide daña/empuja/aturde enemigos
/// </summary>
public class PlayerPerkManager : MonoBehaviour
{
    // =========================================================================
    // SINGLETON
    // =========================================================================

    public static PlayerPerkManager Instance { get; private set; }

    // =========================================================================
    // CONFIGURACIÓN
    // =========================================================================

    [Header("=== LÍMITES ===")]
    [Tooltip("Máximo de perks que puede tener el jugador a la vez (0 = ilimitado)")]
    [SerializeField] private int maxPerks = 5;

    [Header("=== JUGGERNOG ===")]
    [Tooltip("Vida máxima con Juggernog")]
    [SerializeField] private int juggernogMaxHealth = 200;

    [Header("=== PLUSSY ===")]
    [Tooltip("Multiplicador de velocidad de recarga (0.35 = casi 3x más rápido)")]
    [SerializeField] private float plussyReloadMultiplier = 0.35f;

    [Header("=== STAMIN-UP ===")]
    [Tooltip("Multiplicador de velocidad de sprint")]
    [SerializeField] private float staminUpSprintMultiplier = 1.4f;

    [Header("=== PHD FLOPPER ===")]
    [Tooltip("Daño del slide a enemigos")]
    [SerializeField] private int phdSlideDamage = 80;
    [Tooltip("Radio de detección de enemigos durante el slide")]
    [SerializeField] private float phdSlideRadius = 2.5f;
    [Tooltip("Fuerza de empuje a enemigos al deslizarse")]
    [SerializeField] private float phdSlidePushForce = 15f;
    [Tooltip("Duración del aturdimiento en segundos")]
    [SerializeField] private float phdStunDuration = 2f;
    [Tooltip("Intervalo mínimo entre hits del slide (para no dañar 60 veces/seg)")]
    [SerializeField] private float phdSlideHitInterval = 0.3f;
    [Tooltip("Capas de enemigos que el slide puede golpear")]
    [SerializeField] private LayerMask phdEnemyLayer = ~0;

    [Header("=== SONIDOS ===")]
    [SerializeField] private AudioClip perkAcquiredSound;
    [SerializeField] private AudioClip perkLostSound;
    [SerializeField] private AudioClip phdSlideHitSound;
    [SerializeField] private AudioSource audioSource;

    [Header("=== DEBUG ===")]
    [SerializeField] private bool debugMode = false;

    // =========================================================================
    // ESTADO INTERNO
    // =========================================================================

    private HashSet<PerkType> activePerks = new HashSet<PerkType>();
    private float lastPhdSlideHitTime = -999f;

    // Referencias cacheadas
    private PlayerHealth playerHealth;
    private PlayerMovement playerMovement;
    private WeaponManager weaponManager;

    // Valores originales (para restaurar al perder perks)
    private int originalMaxHealth;
    private float originalSprintSpeed;
    private bool originalValuesStored = false;

    // =========================================================================
    // EVENTOS
    // =========================================================================

    /// <summary>Evento: perk adquirida (tipo)</summary>
    public System.Action<PerkType> OnPerkAcquired;

    /// <summary>Evento: perk perdida (tipo)</summary>
    public System.Action<PerkType> OnPerkLost;

    /// <summary>Evento: todas las perks perdidas</summary>
    public System.Action OnAllPerksLost;

    /// <summary>Evento: lista de perks actualizada (lista de perks activas)</summary>
    public System.Action<List<PerkType>> OnPerksUpdated;

    // =========================================================================
    // PROPIEDADES PÚBLICAS
    // =========================================================================

    /// <summary>Perks activas actualmente</summary>
    public HashSet<PerkType> ActivePerks => activePerks;

    /// <summary>Número de perks activas</summary>
    public int PerkCount => activePerks.Count;

    /// <summary>¿Tiene una perk específica?</summary>
    public bool HasPerk(PerkType perk) => activePerks.Contains(perk);

    /// <summary>¿Puede adquirir más perks?</summary>
    public bool CanAcquireMore => maxPerks <= 0 || activePerks.Count < maxPerks;

    /// <summary>Multiplicador de recarga actual (1.0 = normal, 0.35 = casi 3x más rápido)</summary>
    public float ReloadMultiplier => HasPerk(PerkType.Plussy) ? plussyReloadMultiplier : 1f;

    /// <summary>Multiplicador de sprint actual</summary>
    public float SprintMultiplier => HasPerk(PerkType.StaminUp) ? staminUpSprintMultiplier : 1f;

    /// <summary>¿Sprint infinito?</summary>
    public bool InfiniteSprint => HasPerk(PerkType.StaminUp);

    /// <summary>¿Tiene 3er slot de arma?</summary>
    public bool HasThirdWeaponSlot => HasPerk(PerkType.MuleKick);

    /// <summary>¿El slide daña enemigos?</summary>
    public bool SlideDealsDamage => HasPerk(PerkType.PHDFlopper);

    // =========================================================================
    // LIFECYCLE
    // =========================================================================

    void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        // Cachear referencias
        playerHealth = GetComponent<PlayerHealth>();
        playerMovement = GetComponent<PlayerMovement>();
        weaponManager = GetComponentInChildren<WeaponManager>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // Guardar valores originales
        StoreOriginalValues();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // =========================================================================
    // GESTIÓN DE PERKS
    // =========================================================================

    /// <summary>
    /// Intenta adquirir una perk. Retorna true si tuvo éxito.
    /// No verifica monedas — eso lo hace PerkMachine.
    /// </summary>
    public bool AcquirePerk(PerkType perk)
    {
        // Ya la tiene
        if (activePerks.Contains(perk))
        {
            if (debugMode) Debug.Log($"[PERKS] Ya tienes {perk}");
            return false;
        }

        // Límite de perks
        if (!CanAcquireMore)
        {
            if (debugMode) Debug.Log($"[PERKS] Máximo de perks alcanzado ({maxPerks})");
            return false;
        }

        activePerks.Add(perk);
        ApplyPerkEffect(perk);

        // Sonido
        PlaySound(perkAcquiredSound);

        // Eventos
        OnPerkAcquired?.Invoke(perk);
        OnPerksUpdated?.Invoke(new List<PerkType>(activePerks));

        if (debugMode) Debug.Log($"[PERKS] ¡{perk} adquirida! Total: {activePerks.Count}");

        return true;
    }

    /// <summary>
    /// Pierde una perk específica.
    /// </summary>
    public void LosePerk(PerkType perk)
    {
        if (!activePerks.Contains(perk)) return;

        activePerks.Remove(perk);
        RemovePerkEffect(perk);

        PlaySound(perkLostSound);

        OnPerkLost?.Invoke(perk);
        OnPerksUpdated?.Invoke(new List<PerkType>(activePerks));

        if (debugMode) Debug.Log($"[PERKS] {perk} perdida. Total: {activePerks.Count}");
    }

    /// <summary>
    /// Pierde TODAS las perks (al morir, por ejemplo).
    /// </summary>
    public void LoseAllPerks()
    {
        if (activePerks.Count == 0) return;

        // Copiar lista para iterar con seguridad
        PerkType[] perksToRemove = new PerkType[activePerks.Count];
        activePerks.CopyTo(perksToRemove);

        foreach (PerkType perk in perksToRemove)
        {
            RemovePerkEffect(perk);
        }
        activePerks.Clear();

        PlaySound(perkLostSound);

        OnAllPerksLost?.Invoke();
        OnPerksUpdated?.Invoke(new List<PerkType>(activePerks));

        if (debugMode) Debug.Log("[PERKS] Todas las perks perdidas.");
    }

    // =========================================================================
    // APLICAR / REMOVER EFECTOS
    // =========================================================================

    private void StoreOriginalValues()
    {
        if (originalValuesStored) return;

        if (playerHealth != null)
            originalMaxHealth = playerHealth.GetMaxHealth();

        if (playerMovement != null)
            originalSprintSpeed = playerMovement.GetSprintSpeed();

        originalValuesStored = true;
    }

    /// <summary>
    /// Aplica el efecto de una perk al adquirirla.
    /// </summary>
    private void ApplyPerkEffect(PerkType perk)
    {
        StoreOriginalValues();

        switch (perk)
        {
            case PerkType.Juggernog:
                if (playerHealth != null)
                {
                    playerHealth.SetMaxHealth(juggernogMaxHealth, true); // true = curar también
                }
                break;

            case PerkType.Plussy:
                // El multiplicador se lee en tiempo real desde WeaponBase
                // via PlayerPerkManager.Instance.ReloadMultiplier
                break;

            case PerkType.MuleKick:
                if (weaponManager != null)
                {
                    weaponManager.EnableThirdSlot(true);
                }
                break;

            case PerkType.StaminUp:
                if (playerMovement != null)
                {
                    playerMovement.SetSprintSpeed(originalSprintSpeed * staminUpSprintMultiplier);
                }
                break;

            case PerkType.PHDFlopper:
                // El efecto se aplica en el slide (HandlePHDSlide)
                // No fall damage se aplica automáticamente
                break;
        }
    }

    /// <summary>
    /// Remueve el efecto de una perk al perderla.
    /// </summary>
    private void RemovePerkEffect(PerkType perk)
    {
        switch (perk)
        {
            case PerkType.Juggernog:
                if (playerHealth != null)
                {
                    playerHealth.SetMaxHealth(originalMaxHealth, false); // false = no curar
                }
                break;

            case PerkType.Plussy:
                // Se deja de leer el multiplicador automáticamente
                break;

            case PerkType.MuleKick:
                if (weaponManager != null)
                {
                    weaponManager.EnableThirdSlot(false);
                }
                break;

            case PerkType.StaminUp:
                if (playerMovement != null)
                {
                    playerMovement.SetSprintSpeed(originalSprintSpeed);
                }
                break;

            case PerkType.PHDFlopper:
                // Se deja de aplicar automáticamente
                break;
        }
    }

    // =========================================================================
    // PHD FLOPPER — SLIDE DAMAGE
    // =========================================================================

    /// <summary>
    /// Llamado por PlayerMovement durante el slide cuando PhD Flopper está activa.
    /// Detecta enemigos cercanos y les aplica daño + empuje + aturdimiento.
    /// </summary>
    public void HandlePHDSlide(Vector3 playerPosition, Vector3 slideDirection)
    {
        if (!HasPerk(PerkType.PHDFlopper)) return;

        // Cooldown entre hits
        if (Time.time - lastPhdSlideHitTime < phdSlideHitInterval) return;

        // Detectar enemigos en el radio
        Collider[] hits = Physics.OverlapSphere(playerPosition, phdSlideRadius, phdEnemyLayer);

        bool hitSomething = false;

        foreach (Collider col in hits)
        {
            // No dañarse a sí mismo
            if (col.transform.IsChildOf(transform) || col.transform == transform) continue;

            // Intentar aplicar daño
            IDamageable damageable = col.GetComponentInParent<IDamageable>();
            if (damageable != null && damageable.IsAlive())
            {
                // Calcular dirección de empuje (desde jugador hacia enemigo)
                Vector3 pushDir = (col.transform.position - playerPosition).normalized;
                pushDir.y = 0.3f; // Ligeramente hacia arriba
                pushDir.Normalize();

                // Aplicar daño
                damageable.TakeDamage(phdSlideDamage, playerPosition, pushDir);

                // Empuje físico
                Rigidbody rb = col.GetComponent<Rigidbody>();
                if (rb == null) rb = col.GetComponentInParent<Rigidbody>();
                if (rb != null && !rb.isKinematic)
                {
                    rb.AddForce(pushDir * phdSlidePushForce, ForceMode.Impulse);
                }

                // Aturdimiento — buscar EnemyHealth y aplicar stun
                EnemyHealth enemyHealth = col.GetComponentInParent<EnemyHealth>();
                if (enemyHealth != null)
                {
                    enemyHealth.ApplyStun(phdStunDuration);
                }

                hitSomething = true;

                if (debugMode)
                    Debug.Log($"[PHD] Slide golpeó a {col.name}: {phdSlideDamage} daño + empuje + stun {phdStunDuration}s");
            }
        }

        if (hitSomething)
        {
            lastPhdSlideHitTime = Time.time;
            PlaySound(phdSlideHitSound);
        }
    }

    /// <summary>
    /// ¿El jugador tiene inmunidad al daño por caída? (PhD Flopper)
    /// </summary>
    public bool HasFallDamageImmunity()
    {
        return HasPerk(PerkType.PHDFlopper);
    }

    // =========================================================================
    // UTILIDADES
    // =========================================================================

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
        else if (clip != null)
        {
            // Fallback: buscar cualquier AudioSource
            AudioSource fallback = GetComponent<AudioSource>();
            if (fallback != null) fallback.PlayOneShot(clip);
        }
    }

    /// <summary>
    /// Obtiene el coste de una perk.
    /// </summary>
    public static int GetPerkCost(PerkType perk)
    {
        switch (perk)
        {
            case PerkType.Juggernog:  return 5000;
            case PerkType.Plussy:    return 4000;
            case PerkType.MuleKick:   return 6000;
            case PerkType.StaminUp:   return 3500;
            case PerkType.PHDFlopper: return 4500;
            default: return 5000;
        }
    }

    /// <summary>
    /// Obtiene la descripción de una perk para UI.
    /// </summary>
    public static string GetPerkDescription(PerkType perk)
    {
        switch (perk)
        {
            case PerkType.Juggernog:
                return "Duplica tu vida máxima a 200 HP";
            case PerkType.Plussy:
                return "Recargas casi 3 veces más rápido";
            case PerkType.MuleKick:
                return "Desbloquea un 3er slot de arma";
            case PerkType.StaminUp:
                return "Sprint 40% más rápido + infinito";
            case PerkType.PHDFlopper:
                return "El slide daña, empuja y aturde enemigos. Sin daño por caída";
            default:
                return "";
        }
    }

    /// <summary>
    /// Obtiene el color temático de una perk (para UI).
    /// </summary>
    public static Color GetPerkColor(PerkType perk)
    {
        switch (perk)
        {
            case PerkType.Juggernog:  return new Color(1.0f, 0.3f, 0.3f);   // Rojo
            case PerkType.Plussy:    return new Color(0.3f, 1.0f, 0.3f);   // Verde
            case PerkType.MuleKick:   return new Color(0.3f, 0.8f, 0.3f);   // Verde lima
            case PerkType.StaminUp:   return new Color(1.0f, 0.8f, 0.2f);   // Amarillo
            case PerkType.PHDFlopper: return new Color(0.6f, 0.2f, 1.0f);   // Morado
            default: return Color.white;
        }
    }

    /// <summary>
    /// Lista de todas las perks activas (para UI).
    /// </summary>
    public List<PerkType> GetActivePerksList()
    {
        return new List<PerkType>(activePerks);
    }
}
