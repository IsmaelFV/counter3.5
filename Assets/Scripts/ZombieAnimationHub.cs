using UnityEngine;
using System.Collections;

/// <summary>
/// Hub de Animaciones para Zombies (Plug & Play).
/// Permite arrastrar controladores individuales (Idle, Walk, Attack, etc.)
/// e intercambiarlos automáticamente según el estado del zombie.
/// </summary>
[RequireComponent(typeof(Animator))]
public class ZombieAnimationHub : MonoBehaviour
{
    [Header("=== CONTROLADORES (Arrastrar aquí) ===")]
    public RuntimeAnimatorController idleController;
    public RuntimeAnimatorController walkController;
    public RuntimeAnimatorController attackController;
    public RuntimeAnimatorController damagesController;
    public RuntimeAnimatorController deathController;

    [Header("=== CONFIGURACIÓN ===")]
    [Tooltip("Umbral de velocidad para pasar de Idle a Walk.")]
    [SerializeField] private float walkThreshold = 0.2f;

    private Animator animator;
    private EnemyHealth enemyHealth;
    private EnemyDamagePlayer enemyAttack;
    private Rigidbody rb;

    private bool isDead = false;
    private bool isExecutingAction = false; // Bloquea el cambio a Idle/Walk durante ataques o daño
    private RuntimeAnimatorController lastLocomotionController;

    void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        enemyHealth = GetComponent<EnemyHealth>();
        enemyAttack = GetComponent<EnemyDamagePlayer>();

        // Set inicial
        if (idleController != null)
            animator.runtimeAnimatorController = idleController;
    }

    void OnEnable()
    {
        // Suscribirse a eventos de salud
        if (enemyHealth != null)
        {
            enemyHealth.OnDamaged.AddListener(TriggerDamage);
            enemyHealth.OnDeath.AddListener(TriggerDeath);
        }

        // Suscribirse a eventos de ataque
        if (enemyAttack != null)
        {
            enemyAttack.OnAttack.AddListener(TriggerAttack);
        }
    }

    void OnDisable()
    {
        if (enemyHealth != null)
        {
            enemyHealth.OnDamaged.RemoveListener(TriggerDamage);
            enemyHealth.OnDeath.RemoveListener(TriggerDeath);
        }

        if (enemyAttack != null)
        {
            enemyAttack.OnAttack.RemoveListener(TriggerAttack);
        }
    }

    void Update()
    {
        if (isDead || isExecutingAction) return;

        HandleLocomotion();
    }

    private void HandleLocomotion()
    {
        if (rb == null || idleController == null || walkController == null) return;

        float speed = new Vector3(rb.velocity.x, 0, rb.velocity.z).magnitude;
        RuntimeAnimatorController target = (speed > walkThreshold) ? walkController : idleController;

        if (animator.runtimeAnimatorController != target)
        {
            animator.runtimeAnimatorController = target;
        }
    }

    private void TriggerDamage()
    {
        if (isDead || damagesController == null) return;
        StartCoroutine(ExecuteTemporaryAction(damagesController));
    }

    private void TriggerAttack()
    {
        if (isDead || attackController == null) return;
        StartCoroutine(ExecuteTemporaryAction(attackController));
    }

    private void TriggerDeath()
    {
        if (isDead || deathController == null) return;
        isDead = true;
        StopAllCoroutines();
        animator.runtimeAnimatorController = deathController;
    }

    /// <summary>
    /// Cambia temporalmente el controlador y vuelve a la locomoción tras un tiempo estimado.
    /// Ya que cada controlador del pack suele tener una sola animación, esperamos su duración.
    /// </summary>
    private IEnumerator ExecuteTemporaryAction(RuntimeAnimatorController actionController)
    {
        isExecutingAction = true;
        animator.runtimeAnimatorController = actionController;

        // Esperar un poco a que la animación se reproduzca. 
        // Normalmente las animaciones de Kevin Iglesias duran entre 0.8s y 1.5s
        // Intentamos obtener el tiempo del clip si es posible
        float waitTime = 1.0f; 
        if (animator.GetCurrentAnimatorStateInfo(0).length > 0)
        {
            waitTime = animator.GetCurrentAnimatorStateInfo(0).length;
        }

        yield return new WaitForSeconds(waitTime);

        isExecutingAction = false;
    }
}
