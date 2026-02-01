using UnityEngine;

public class ParticleDistanceOptimizer : MonoBehaviour
{
    [Tooltip("The maximum distance at which particles will be generated.")]
    public float maxDistance = 40f;
    
    [Tooltip("How often (in seconds) to check the distance. Higher values improve performance.")]
    public float checkInterval = 1.0f;

    private ParticleSystem _particleSystem;
    private Transform _playerTransform;
    private Transform _transform;
    private bool _isActive = true;

    void Start()
    {
        _particleSystem = GetComponent<ParticleSystem>();
        _transform = transform;

        if (_particleSystem == null)
        {
            // Try to find it in children if not on the same object
            _particleSystem = GetComponentInChildren<ParticleSystem>();
        }

        if (_particleSystem == null)
        {
            Debug.LogWarning($"ParticleDistanceOptimizer: No ParticleSystem found on {gameObject.name}");
            enabled = false;
            return;
        }

        // Invoke the check periodically to save performance
        InvokeRepeating(nameof(CheckDistance), Random.Range(0f, checkInterval), checkInterval);
    }

    void CheckDistance()
    {
        if (_playerTransform == null)
        {
            // Find the camera tagged as MainCamera
            if (Camera.main != null)
            {
                _playerTransform = Camera.main.transform;
            }
            else
            {
                return; // Wait until camera is available
            }
        }

        float distanceSqr = (_playerTransform.position - _transform.position).sqrMagnitude;
        float maxDistSqr = maxDistance * maxDistance;

        bool shouldBeActive = distanceSqr < maxDistSqr;

        if (shouldBeActive != _isActive)
        {
            _isActive = shouldBeActive;
            var emission = _particleSystem.emission;
            emission.enabled = _isActive;
        }
    }
}
