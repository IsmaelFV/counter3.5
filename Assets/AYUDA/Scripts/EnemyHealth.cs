using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Sistema de salud para enemigos (zombies, etc.).
/// Implementa IDamageable para recibir daño del sistema de disparo.
/// Incluye: salud, efectos de muerte, drops y eventos.
/// </summary>
public class EnemyHealth : MonoBehaviour, IDamageable
{
    [Header("=== SALUD ===")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;

    [Header("=== DAÑO POR ZONA ===")]
    [Tooltip("Multiplicador de daño en la cabeza")]
    [SerializeField] private float headshotMultiplier = 2.5f;
    [Tooltip("Tag del collider de cabeza")]
    [SerializeField] private string headColliderTag = "Head";

    [Header("=== KNOCKBACK ===")]
    [SerializeField] private bool enableKnockback = true;
    [SerializeField] private float knockbackForce = 2f;
    private Rigidbody rb;

    [Header("=== EFECTOS DE MUERTE ===")]
    [Tooltip("Prefab que se instancia al morir (ragdoll, explosión, etc.)")]
    [SerializeField] private GameObject deathEffectPrefab;
    [Tooltip("¿Destruir el enemigo al morir?")]
    [SerializeField] private bool destroyOnDeath = true;
    [Tooltip("Tiempo antes de destruir tras morir")]
    [SerializeField] private float destroyDelay = 3f;

    [Header("=== DROPS ===")]
    [Tooltip("Probabilidad de drop (0-1)")]
    [SerializeField, Range(0f, 1f)] private float dropChance = 0.3f;
    [Tooltip("Prefabs posibles de drop")]
    [SerializeField] private GameObject[] dropPrefabs;

    [Header("=== RECOMPENSA DE MONEDAS ===")]
    [Tooltip("Monedas que da al morir (0 = usar valor base de PlayerEconomy)")]
    [SerializeField] private int coinReward = 0;
    [Tooltip("¿Es un enemigo élite? (da más monedas)")]
    [SerializeField] private bool isElite = false;
    [Tooltip("Multiplicador de monedas si es élite")]
    [SerializeField] private float eliteCoinMultiplier = 3f;

    [Header("=== SONIDOS ===")]
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private AudioSource audioSource;

    [Header("=== EFECTOS VISUALES ===")]
    [Tooltip("Flash rojo al recibir daño")]
    [SerializeField] private bool enableDamageFlash = true;
    [SerializeField] private float flashDuration = 0.1f;
    private Renderer[] renderers;
    private Color[] originalColors;

    [Header("=== EVENTOS ===")]
    public UnityEvent<int, int> OnHealthChanged;    // (current, max)
    public UnityEvent OnDamaged;
    public UnityEvent OnDeath;

    /// <summary>Evento con info del golpe (daño, punto, dirección)</summary>
    public System.Action<int, Vector3, Vector3> OnDamageReceived;

    private bool isDead = false;
    private bool lastHitWasHeadshot = false;
    private bool lastHitWasMelee = false;

    // Stun (PhD Flopper)
    private bool isStunned = false;
    private float stunEndTime = 0f;

    void Awake()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // Cachear renderers para flash de daño
        if (enableDamageFlash)
        {
            try
            {
                renderers = GetComponentsInChildren<Renderer>();
                if (renderers != null && renderers.Length > 0)
                {
                    originalColors = new Color[renderers.Length];
                    for (int i = 0; i < renderers.Length; i++)
                    {
                        try
                        {
                            if (renderers[i] != null && renderers[i].material != null 
                                && renderers[i].material.HasProperty("_Color"))
                                originalColors[i] = renderers[i].material.color;
                        }
                        catch (System.Exception) { /* Renderer sin material válido */ }
                    }
                }
            }
            catch (System.Exception)
            {
                renderers = null;
                enableDamageFlash = false;
            }
        }
    }

    void Start()
    {
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    void Update()
    {
        // Gestionar stun
        if (isStunned && Time.time >= stunEndTime)
        {
            isStunned = false;
        }
    }

    // =========================================================================
    // IDamageable IMPLEMENTATION
    // =========================================================================

    /// <summary>
    /// Aplica daño simple
    /// </summary>
    public void TakeDamage(int damage)
    {
        TakeDamage(damage, transform.position, Vector3.zero);
    }

    /// <summary>
    /// Aplica daño con información de impacto (punto y dirección)
    /// </summary>
    public void TakeDamage(int damage, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (isDead) return;

        try
        {
            // Detectar headshot por tag del collider impactado
            int finalDamage = damage;
            bool isHeadshot = false;

            try
            {
                // Buscar si el punto de impacto corresponde a un collider de cabeza
                Collider[] colliders = GetComponentsInChildren<Collider>();
                float closestDist = float.MaxValue;
                Collider closestCol = null;
                foreach (var col in colliders)
                {
                    if (col == null) continue;
                    float dist = Vector3.Distance(col.ClosestPoint(hitPoint), hitPoint);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closestCol = col;
                    }
                }

                if (closestCol != null && closestCol.gameObject.tag == headColliderTag)
                {
                    finalDamage = Mathf.RoundToInt(damage * headshotMultiplier);
                    isHeadshot = true;
                }
            }
            catch (System.Exception) { /* Tag de headshot no existe aún — usar daño normal */ }

            // Guardar info del último hit para recompensa al morir
            lastHitWasHeadshot = isHeadshot;

            // Aplicar daño
            currentHealth -= finalDamage;
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

            // Efectos
            PlaySound(hitSound);
            if (enableDamageFlash && renderers != null) StartCoroutine(DamageFlashCoroutine());

            // Knockback
            if (enableKnockback && rb != null && hitDirection != Vector3.zero)
            {
                rb.AddForce(hitDirection.normalized * knockbackForce, ForceMode.Impulse);
            }

            // Notificar
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnDamaged?.Invoke();
            OnDamageReceived?.Invoke(finalDamage, hitPoint, hitDirection);

            // Números de daño flotantes
            if (DamageNumberManager.Instance != null)
            {
                DamageNumberType dmgType = isHeadshot ? DamageNumberType.Headshot : DamageNumberType.Normal;
                DamageNumberManager.Instance.SpawnDamage(hitPoint, finalDamage, dmgType);
            }

            Debug.Log($"[ENEMIGO] {gameObject.name} recibió {finalDamage} daño{(isHeadshot ? " (HEADSHOT!)" : "")}. Salud: {currentHealth}/{maxHealth}");

            // Verificar muerte
            if (currentHealth <= 0)
            {
                Die(hitDirection);
            }
        }
        catch (System.Exception e)
        {
            // Capturar CUALQUIER error para evitar que Unity pause el juego
            Debug.LogWarning($"[EnemyHealth] Error en TakeDamage de {gameObject.name}: {e.Message}");
            // Aún así, aplicar el daño básico
            currentHealth -= damage;
            if (currentHealth <= 0) Die(hitDirection);
        }
    }

    /// <summary>
    /// Aplica daño de melee (usado por WeaponManager.HandleMelee)
    /// </summary>
    public void TakeMeleeDamage(int damage, Vector3 hitPoint, Vector3 hitDirection)
    {
        lastHitWasMelee = true;
        TakeDamage(damage, hitPoint, hitDirection);
    }

    /// <summary>
    /// ¿Está vivo el enemigo?
    /// </summary>
    public bool IsAlive()
    {
        return !isDead;
    }

    // =========================================================================
    // MUERTE
    // =========================================================================

    /// <summary>
    /// Maneja la muerte del enemigo
    /// </summary>
    private void Die(Vector3 lastHitDirection)
    {
        if (isDead) return;
        isDead = true;

        try
        {
            Debug.Log($"[ENEMIGO] {gameObject.name} ha muerto!");

            PlaySound(deathSound);

            // === RECOMPENSA DE MONEDAS ===
            if (PlayerEconomy.Instance != null)
            {
                int reward = coinReward;
                if (isElite && reward > 0)
                    reward = Mathf.RoundToInt(reward * eliteCoinMultiplier);

                PlayerEconomy.Instance.RegisterKill(reward, lastHitWasHeadshot, lastHitWasMelee);
            }

            // Evento de muerte
            OnDeath?.Invoke();

            // Efecto de muerte
            if (deathEffectPrefab != null)
            {
                Instantiate(deathEffectPrefab, transform.position, transform.rotation);
            }

            // Drop de items
            TryDropItem();

            // === CONGELAR EN SU SITIO (NO desactivar colliders para que no caiga) ===
            // Detener movimiento del enemigo
            EnemyFollow follow = GetComponent<EnemyFollow>();
            if (follow != null) follow.enabled = false;
            
            EnemyDamagePlayer dmgPlayer = GetComponent<EnemyDamagePlayer>();
            if (dmgPlayer != null) dmgPlayer.enabled = false;

            // Congelar rigidbody para que no se mueva
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            // Destruir después de un breve delay
            Destroy(gameObject, destroyDelay > 0 ? destroyDelay : 2f);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[EnemyHealth] Error en Die() de {gameObject.name}: {e.Message}");
            Destroy(gameObject, 0.5f);
        }
    }



    /// <summary>
    /// Intenta soltar un item al morir
    /// </summary>
    private void TryDropItem()
    {
        if (dropPrefabs == null || dropPrefabs.Length == 0) return;
        if (Random.value > dropChance) return;

        GameObject dropPrefab = dropPrefabs[Random.Range(0, dropPrefabs.Length)];
        if (dropPrefab != null)
        {
            Vector3 dropPos = transform.position + Vector3.up * 0.5f;
            Instantiate(dropPrefab, dropPos, Quaternion.identity);
        }
    }

    // =========================================================================
    // EFECTOS VISUALES
    // =========================================================================

    /// <summary>
    /// Flash rojo breve al recibir daño
    /// </summary>
    private System.Collections.IEnumerator DamageFlashCoroutine()
    {
        if (renderers == null || renderers.Length == 0) yield break;

        // Poner renderers en rojo
        for (int i = 0; i < renderers.Length; i++)
        {
            try
            {
                if (renderers[i] != null && renderers[i].material.HasProperty("_Color"))
                    renderers[i].material.color = Color.red;
            }
            catch (System.Exception) { /* Renderer inválido — ignorar */ }
        }

        yield return new WaitForSeconds(flashDuration);

        // Restaurar colores originales
        for (int i = 0; i < renderers.Length; i++)
        {
            try
            {
                if (renderers[i] != null && originalColors != null && i < originalColors.Length 
                    && renderers[i].material.HasProperty("_Color"))
                    renderers[i].material.color = originalColors[i];
            }
            catch (System.Exception) { /* Renderer inválido — ignorar */ }
        }
    }

    // =========================================================================
    // UTILIDADES
    // =========================================================================

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }

    /// <summary>
    /// Aplica aturdimiento al enemigo (usado por PhD Flopper).
    /// Mientras está aturdido, el enemigo no se mueve (compatible con NavMeshAgent).
    /// </summary>
    public void ApplyStun(float duration)
    {
        isStunned = true;
        stunEndTime = Time.time + duration;

        // Detener NavMeshAgent si existe
        var agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.isStopped = true;
            // Reactivar después del stun
            Invoke(nameof(EndStun), duration);
        }

        Debug.Log($"[ENEMIGO] {gameObject.name} aturdido por {duration}s");
    }

    private void EndStun()
    {
        isStunned = false;
        var agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null && agent.isActiveAndEnabled && IsAlive())
        {
            agent.isStopped = false;
        }
    }

    /// <summary>
    /// ¿Está aturdido?
    /// </summary>
    public bool IsStunned() => isStunned;

    /// <summary>
    /// Cura al enemigo (útil para mecánicas especiales)
    /// </summary>
    public void Heal(int amount)
    {
        if (isDead) return;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>
    /// Obtiene el porcentaje de salud (0-1)
    /// </summary>
    public float GetHealthPercentage()
    {
        return (float)currentHealth / maxHealth;
    }

    /// <summary>
    /// Obtiene la salud actual
    /// </summary>
    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    // Debug
    [ContextMenu("Test - Take 30 Damage")]
    private void TestDamage()
    {
        TakeDamage(30, transform.position, -transform.forward);
    }

    [ContextMenu("Test - Kill")]
    private void TestKill()
    {
        TakeDamage(maxHealth + 1);
    }
}
