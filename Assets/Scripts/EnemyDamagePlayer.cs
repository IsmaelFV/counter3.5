using UnityEngine;

/// <summary>
/// Hace que el enemigo dañe al jugador al tocarle.
/// Lee el daño desde EnemyStats si existe, o usa un valor por defecto.
/// Compatible con el sistema de salud AYUDA (PlayerHealth).
/// 
/// SETUP: Se añade automáticamente desde EnemyFollow.Start() o WaveManager.
/// </summary>
public class EnemyDamagePlayer : MonoBehaviour
{
    [Header("=== CONFIGURACIÓN DE DAÑO ===")]
    [Tooltip("Daño base por golpe (se sobreescribe si hay EnemyStats)")]
    [SerializeField] private int damagePerHit = 10;

    [Tooltip("Segundos entre cada golpe")]
    [SerializeField] private float hitCooldown = 1.0f;

    [Header("=== EVENTOS ===")]
    [Tooltip("Evento que se dispara cuando el enemigo logra golpear al jugador.")]
    public UnityEngine.Events.UnityEvent OnAttack;

    [Header("=== SONIDO (Opcional) ===")]
    [SerializeField] private AudioClip attackSound;

    private float lastHitTime = -999f;
    private AudioSource audioSource;

    void Start()
    {
        // Leer daño desde EnemyStats si existe (configurado por WaveManager)
        EnemyStats stats = GetComponent<EnemyStats>();
        if (stats != null && stats.damage > 0)
        {
            damagePerHit = Mathf.RoundToInt(stats.damage);
        }

        // Buscar AudioSource
        audioSource = GetComponent<AudioSource>();
    }

    void OnCollisionStay(Collision collision)
    {
        TryDamagePlayer(collision.gameObject);
    }

    void OnTriggerStay(Collider other)
    {
        TryDamagePlayer(other.gameObject);
    }

    private void TryDamagePlayer(GameObject target)
    {
        // Verificar cooldown
        if (Time.time - lastHitTime < hitCooldown) return;

        // Verificar que sea el jugador
        if (!target.CompareTag("Player"))
        {
            // También verificar en el padre (el jugador puede tener colliders hijos)
            if (target.transform.parent == null || !target.transform.parent.CompareTag("Player"))
                return;
        }

        // Buscar PlayerHealth en el jugador
        PlayerHealth playerHealth = target.GetComponent<PlayerHealth>();
        if (playerHealth == null)
            playerHealth = target.GetComponentInParent<PlayerHealth>();

        if (playerHealth == null) return;

        // Verificar que el enemigo esté vivo
        EnemyHealth enemyHealth = GetComponent<EnemyHealth>();
        if (enemyHealth != null && !enemyHealth.IsAlive()) return;

        // Aplicar daño
        playerHealth.TakeDamage(damagePerHit);
        lastHitTime = Time.time;

        // Disparar evento de ataque
        OnAttack?.Invoke();

        // Sonido
        if (audioSource != null && attackSound != null)
            audioSource.PlayOneShot(attackSound);

        Debug.Log($"[ENEMIGO] {gameObject.name} golpeó al jugador por {damagePerHit} de daño.");
    }

    /// <summary>
    /// Configura el daño por golpe desde código (usado por WaveManager).
    /// </summary>
    public void SetDamage(int damage)
    {
        damagePerHit = damage;
    }
}
