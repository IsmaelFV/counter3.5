using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Tooltip("The Enemy Prefab to spawn.")]
    public GameObject enemyPrefab;

    [Tooltip("Time in seconds between spawns.")]
    public float spawnInterval = 5f;

    [Tooltip("Maximum distance to player to enable spawning.")]
    public float activationDistance = 10f;

    [Tooltip("Número máximo de enemigos que pueden aparecer de este spawner. 0 significa infinito.")]
    public int maxEnemies = 10;

    private float _timer;
    private Transform _playerTransform;
    private int _spawnedCount = 0;

    void Start()
    {
        _timer = spawnInterval;
    }

    void Update()
    {
        if (_playerTransform == null)
        {
            if (Camera.main != null)
            {
                _playerTransform = Camera.main.transform;
            }
            return;
        }

        float distance = Vector3.Distance(transform.position, _playerTransform.position);

        if (distance <= activationDistance)
        {
            // Comprobar límite de spawns antes de generar (si maxEnemies es mayor que 0)
            if (maxEnemies > 0 && _spawnedCount >= maxEnemies)
            {
                return;
            }

            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                SpawnEnemy();
                _timer = spawnInterval;
            }
        }
    }

    void SpawnEnemy()
    {
        if (enemyPrefab != null)
        {
            Instantiate(enemyPrefab, transform.position, Quaternion.identity);
            _spawnedCount++;
        }
    }

    // Visualization for the editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, activationDistance);
    }
}
