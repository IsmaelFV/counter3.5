using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Sistema de salud del jugador con eventos para UI y efectos
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Configuración de Salud")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;

    [Header("Sonidos")]
    [SerializeField] private AudioClip damageSound;
    [SerializeField] private AudioClip healSound;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private AudioSource audioSource;

    [Header("Referencias")]
    [SerializeField] private DamageEffect damageEffect;
    [SerializeField] private HitDirectionIndicator hitDirectionIndicator;

    // Eventos para la UI
    public UnityEvent<int, int> OnHealthChanged; // (currentHealth, maxHealth)
    public UnityEvent OnPlayerDeath;
    public UnityEvent OnPlayerDamaged;
    public UnityEvent OnPlayerHealed;

    private bool isDead = false;

    void Start()
    {
        // Inicializar salud completa
        currentHealth = maxHealth;
        
        // Verificar AudioSource
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // Auto-buscar HitDirectionIndicator si no está asignado
        if (hitDirectionIndicator == null)
            hitDirectionIndicator = GetComponent<HitDirectionIndicator>();
        
        // Notificar UI inicial
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>
    /// Aplica daño al jugador (sin dirección — no muestra indicador direccional)
    /// </summary>
    public void TakeDamage(int damage)
    {
        TakeDamage(damage, Vector3.zero);
    }

    /// <summary>
    /// Aplica daño al jugador desde una posición específica.
    /// Si damageSourcePosition != Vector3.zero, muestra el indicador direccional.
    /// </summary>
    public void TakeDamage(int damage, Vector3 damageSourcePosition)
    {
        if (isDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        // Efectos visuales y sonoros
        if (damageEffect != null)
            damageEffect.ShowDamageEffect();
        
        PlaySound(damageSound);

        // Indicador direccional de daño
        if (hitDirectionIndicator != null && damageSourcePosition != Vector3.zero)
        {
            hitDirectionIndicator.ShowDamageDirection(damageSourcePosition);
        }

        // Notificar cambios
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnPlayerDamaged?.Invoke();

        // Verificar muerte
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Cura al jugador
    /// </summary>
    public void Heal(int amount)
    {
        if (isDead) return;

        int healthBefore = currentHealth;
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        // Solo reproducir efectos si realmente se curó
        if (currentHealth > healthBefore)
        {
            PlaySound(healSound);
            OnPlayerHealed?.Invoke();
        }

        // Notificar cambios
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>
    /// Maneja la muerte del jugador
    /// </summary>
    private void Die()
    {
        if (isDead) return;

        isDead = true;
        PlaySound(deathSound);
        
        // Notificar muerte
        OnPlayerDeath?.Invoke();

        Debug.Log("¡El jugador ha muerto!");

        // Notificar al GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ShowGameOver();
        }

        // Desactivar controles del jugador
        DisablePlayerControls();
    }

    /// <summary>
    /// Desactiva los controles del jugador
    /// </summary>
    private void DisablePlayerControls()
    {
        // Desactivar movimiento con el nuevo sistema
        var playerMovement = GetComponent<PlayerMovement>();
        if (playerMovement != null)
            playerMovement.DisableMovement();

        // Desactivar armas
        var weaponManager = GetComponent<WeaponManager>();
        if (weaponManager != null)
            weaponManager.enabled = false;

        // Desactivar sistemas procedurales
        var animController = GetComponent<PlayerAnimationController>();
        if (animController != null)
            animController.TriggerDie();

        // Desbloquear cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// Reproduce un sonido si existe
    /// </summary>
    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    /// <summary>
    /// Verifica si el jugador está vivo
    /// </summary>
    public bool IsAlive()
    {
        return !isDead;
    }

    /// <summary>
    /// Obtiene la salud actual
    /// </summary>
    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    /// <summary>
    /// Obtiene la salud máxima
    /// </summary>
    public int GetMaxHealth()
    {
        return maxHealth;
    }

    /// <summary>
    /// Cambia la salud máxima (usado por Juggernog).
    /// Si healToNew es true, cura al nuevo máximo.
    /// Si es false, clampea la salud actual al nuevo máximo.
    /// </summary>
    public void SetMaxHealth(int newMax, bool healToNew = false)
    {
        maxHealth = Mathf.Max(1, newMax);

        if (healToNew)
        {
            currentHealth = maxHealth;
        }
        else
        {
            currentHealth = Mathf.Min(currentHealth, maxHealth);
        }

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        Debug.Log($"[SALUD] Vida máxima cambiada a {maxHealth}. Actual: {currentHealth}");
    }

    /// <summary>
    /// Obtiene el porcentaje de salud (0-1)
    /// </summary>
    public float GetHealthPercentage()
    {
        return (float)currentHealth / maxHealth;
    }

    /// <summary>
    /// Verifica si la salud está baja
    /// </summary>
    public bool IsHealthLow()
    {
        return GetHealthPercentage() <= 0.3f; // 30% o menos
    }

    // Método para testing desde el Inspector
    [ContextMenu("Test - Take 20 Damage")]
    private void TestTakeDamage()
    {
        TakeDamage(20);
    }

    [ContextMenu("Test - Heal 50 Health")]
    private void TestHeal()
    {
        Heal(50);
    }
}
