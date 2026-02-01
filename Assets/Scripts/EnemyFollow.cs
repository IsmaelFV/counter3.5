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
