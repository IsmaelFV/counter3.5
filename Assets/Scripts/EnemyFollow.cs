using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyFollow : MonoBehaviour
{
    public float speed = 3.0f;
    private Transform _playerTransform;
    private Rigidbody _rb;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        
        // --- AUTO-FIX: Ensure Physics works ---
        if (_rb != null)
        {
            _rb.useGravity = true;        // Ensure gravity is ON
            _rb.isKinematic = false;      // Ensure it can fall (disable Kinematic)
            _rb.constraints = RigidbodyConstraints.FreezeRotation; // Keep upright
        }

        // --- AUTO-FIX: Ensure it can be hit ---
        if (GetComponent<Collider>() == null)
        {
            Debug.LogWarning($"EnemyFollow: {name} missing Collider! Adding CapsuleCollider automatically.");
            var col = gameObject.AddComponent<CapsuleCollider>();
            col.height = 2.0f;
            col.center = new Vector3(0, 1f, 0); // Assuming pivot is at feet
        }

        // --- AUTO-FIX: Añadir componentes AYUDA si faltan ---
        // EnemyHealth: permite recibir daño del sistema de armas AYUDA (IDamageable)
        if (GetComponent<EnemyHealth>() == null)
        {
            gameObject.AddComponent<EnemyHealth>();
            Debug.Log($"[EnemyFollow] EnemyHealth añadido automáticamente a {name}");
        }

        // EnemyDamagePlayer: permite dañar al jugador al tocarle
        if (GetComponent<EnemyDamagePlayer>() == null)
        {
            gameObject.AddComponent<EnemyDamagePlayer>();
            Debug.Log($"[EnemyFollow] EnemyDamagePlayer añadido automáticamente a {name}");
        }

        // AudioSource: para sonidos de daño y muerte
        if (GetComponent<AudioSource>() == null)
        {
            AudioSource audio = gameObject.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.spatialBlend = 1f;
        }
        // --------------------------------------

        if (Camera.main != null)
        {
            _playerTransform = Camera.main.transform;
        }
    }

    void FixedUpdate()
    {
        if (_playerTransform != null)
        {
            // Look at player (ignore Y axis to prevent tilting and flying up)
            Vector3 targetPosition = new Vector3(_playerTransform.position.x, transform.position.y, _playerTransform.position.z);
            transform.LookAt(targetPosition);

            // Move forward using Rigidbody position to respect physics/gravity
            Vector3 velocity = transform.forward * speed;
            velocity.y = _rb.velocity.y; // Preserve gravity's vertical pull
            _rb.velocity = velocity;
        }
        else
        {
             // Retry finding player if lost (or not found at start)
            if (Camera.main != null)
            {
                _playerTransform = Camera.main.transform;
            }
        }
    }
}
