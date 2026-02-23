using UnityEngine;

/// <summary>
/// Guía a un zombie por una serie de waypoints antes de dejarlo perseguir al jugador.
/// Se añade dinámicamente por WaveManager al spawnear dentro de una casa.
/// Al completar el camino, reactiva EnemyFollow y se autodestruye.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class EnemyExitPath : MonoBehaviour
{
    [Header("Configuración del Camino")]
    [Tooltip("Velocidad a la que el zombie sigue el camino de salida.")]
    [SerializeField] private float pathSpeed = 3.5f;

    [Tooltip("Distancia mínima para considerar que llegó a un waypoint.")]
    [SerializeField] private float waypointReachDistance = 0.5f;

    [Tooltip("Velocidad de rotación hacia el siguiente waypoint.")]
    [SerializeField] private float rotationSpeed = 8f;

    // Waypoints asignados dinámicamente por WaveManager
    private Transform[] waypoints;
    private int currentWaypointIndex = 0;
    private Rigidbody rb;
    private EnemyFollow enemyFollow;
    private bool pathCompleted = false;

    /// <summary>
    /// Inicializa el camino de salida con los waypoints proporcionados.
    /// Llamar inmediatamente después de AddComponent.
    /// </summary>
    public void SetPath(Transform[] exitWaypoints, float speed = -1f)
    {
        waypoints = exitWaypoints;
        if (speed > 0f) pathSpeed = speed;

        rb = GetComponent<Rigidbody>();

        // --- SOLUCIÓN: Asegurar colisiones y física antes de desactivar EnemyFollow ---
        // Si desactivamos EnemyFollow inmediatamente, su Start() no se ejecuta y el enemigo
        // no recibe su CapsuleCollider, causando que atraviese el suelo.
        InitializeEnemyPhysics();

        // Desactivar persecución al jugador mientras sigue el camino
        enemyFollow = GetComponent<EnemyFollow>();
        if (enemyFollow != null)
        {
            enemyFollow.enabled = false;
        }

        // Si no hay waypoints, completar inmediatamente
        if (waypoints == null || waypoints.Length == 0)
        {
            CompletePath();
        }
    }

    private void InitializeEnemyPhysics()
    {
        // 1. Configurar Rigidbody
        if (rb != null)
        {
            rb.useGravity = true;
            rb.isKinematic = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }

        // 2. Asegurar que tiene Collider (copiado de la lógica de EnemyFollow)
        if (GetComponent<Collider>() == null)
        {
            var col = gameObject.AddComponent<CapsuleCollider>();
            col.height = 2.0f;
            col.center = new Vector3(0, 1f, 0);
            Debug.Log($"[EnemyExitPath] Collider añadido automáticamente a {name} para evitar caída.");
        }
    }

    void FixedUpdate()
    {
        if (pathCompleted || waypoints == null || waypoints.Length == 0) return;

        Transform target = waypoints[currentWaypointIndex];
        if (target == null)
        {
            // Waypoint destruido o nulo — saltar al siguiente
            AdvanceWaypoint();
            return;
        }

        // Calcular dirección al waypoint (ignorando eje Y para no volar)
        Vector3 targetPos = new Vector3(target.position.x, transform.position.y, target.position.z);
        Vector3 direction = (targetPos - transform.position);
        float distance = direction.magnitude;

        // ¿Llegó al waypoint actual?
        if (distance <= waypointReachDistance)
        {
            AdvanceWaypoint();
            return;
        }

        // Rotar suavemente hacia el waypoint
        direction.Normalize();
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.fixedDeltaTime * rotationSpeed);

        // Mover hacia el waypoint preservando la gravedad
        Vector3 velocity = direction * pathSpeed;
        velocity.y = rb.velocity.y; // Preservar gravedad vertical
        rb.velocity = velocity;
    }

    private void AdvanceWaypoint()
    {
        currentWaypointIndex++;

        if (currentWaypointIndex >= waypoints.Length)
        {
            CompletePath();
        }
    }

    private void CompletePath()
    {
        pathCompleted = true;

        // Reactivar persecución al jugador
        if (enemyFollow != null)
        {
            enemyFollow.enabled = true;
        }

        // Este componente ya no es necesario
        Destroy(this);
    }

    // Visualización en editor para depuración
    void OnDrawGizmosSelected()
    {
        if (waypoints == null) return;

        Gizmos.color = Color.cyan;
        for (int i = currentWaypointIndex; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;
            Gizmos.DrawWireSphere(waypoints[i].position, waypointReachDistance);
            if (i > currentWaypointIndex && waypoints[i - 1] != null)
            {
                Gizmos.DrawLine(waypoints[i - 1].position, waypoints[i].position);
            }
        }
    }
}
