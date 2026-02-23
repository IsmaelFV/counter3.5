using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WaveConfig
{
    [Tooltip("El prefab del enemigo a generar.")]
    public GameObject enemyPrefab;
    [Tooltip("Cantidad de enemigos de este tipo a generar en esta oleada.")]
    public int amount = 5;
    [Tooltip("Vida que tendrá este enemigo.")]
    public float health = 10f;
    [Tooltip("Daño que hará este enemigo (requiere que el script de ataque del enemigo lo lea).")]
    public float damage = 1f;
}

[RequireComponent(typeof(Collider))]
public class WaveManager : MonoBehaviour
{
    [Header("Configuración de la Oleada")]
    public List<WaveConfig> enemiesToSpawn = new List<WaveConfig>();
    
    [Tooltip("Tiempo en segundos entre la aparición de cada enemigo.")]
    public float spawnDelay = 2f;

    [Header("Puntos de Aparición (Spawns)")]
    [Tooltip("Arrastra aquí los Transforms hijos donde aparecerán los zombies. Cada uno puede tener un SpawnPointConfig con su propio camino de salida.")]
    public List<Transform> spawnPoints = new List<Transform>();

    private bool waveStarted = false;
    private int totalEnemiesAlive = 0;

    void Start()
    {
        // Asegurarnos de que el collider es un trigger para detectar al jugador
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Verificar si el jugador entra al área
        if (!waveStarted && other.CompareTag("Player"))
        {
            StartWave();
        }
    }

    public void StartWave()
    {
        if (waveStarted) return;
        
        if (spawnPoints.Count == 0)
        {
            Debug.LogWarning("WaveManager: No hay puntos de spawn configurados en " + gameObject.name);
            return;
        }

        waveStarted = true;
        Debug.Log("Oleada iniciada en " + gameObject.name);
        StartCoroutine(SpawnWaveRoutine());
    }

    IEnumerator SpawnWaveRoutine()
    {
        foreach (WaveConfig wave in enemiesToSpawn)
        {
            for (int i = 0; i < wave.amount; i++)
            {
                SpawnEnemy(wave);
                yield return new WaitForSeconds(spawnDelay);
            }
        }
    }

    void SpawnEnemy(WaveConfig config)
    {
        if (config.enemyPrefab == null) return;

        // Elegir un punto de spawn aleatorio
        Transform randomSpawn = spawnPoints[Random.Range(0, spawnPoints.Count)];

        // Generar el enemigo
        GameObject newEnemy = Instantiate(config.enemyPrefab, randomSpawn.position, randomSpawn.rotation);
        totalEnemiesAlive++;

        // --- CAMINO DE SALIDA INDIVIDUAL ---
        // Cada spawn point puede tener su propio SpawnPointConfig con waypoints independientes
        SpawnPointConfig spawnConfig = randomSpawn.GetComponent<SpawnPointConfig>();
        if (spawnConfig != null && spawnConfig.HasExitPath())
        {
            EnemyExitPath exitPath = newEnemy.AddComponent<EnemyExitPath>();
            exitPath.SetPath(spawnConfig.exitWaypoints.ToArray(), spawnConfig.exitSpeed);
        }

        // --- Modificar Vida (sistema original) ---
        GestionVida gestionVida = newEnemy.GetComponent<GestionVida>();
        if (gestionVida != null)
        {
            gestionVida.vida = config.health;
            gestionVida.maxVida = config.health;
        }

        // --- Añadir EnemyHealth del sistema AYUDA (para recibir daño de armas) ---
        EnemyHealth enemyHealth = newEnemy.GetComponent<EnemyHealth>();
        if (enemyHealth == null)
        {
            enemyHealth = newEnemy.AddComponent<EnemyHealth>();
        }
        // Configurar la vida desde WaveConfig usando reflection (el campo es privado+serialized)
        var maxHealthField = typeof(EnemyHealth).GetField("maxHealth", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (maxHealthField != null)
        {
            maxHealthField.SetValue(enemyHealth, Mathf.RoundToInt(config.health));
        }
        var currentHealthField = typeof(EnemyHealth).GetField("currentHealth", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (currentHealthField != null)
        {
            currentHealthField.SetValue(enemyHealth, Mathf.RoundToInt(config.health));
        }

        // --- Añadir EnemyDamagePlayer (para dañar al jugador al tocarle) ---
        EnemyDamagePlayer damagePlayer = newEnemy.GetComponent<EnemyDamagePlayer>();
        if (damagePlayer == null)
        {
            damagePlayer = newEnemy.AddComponent<EnemyDamagePlayer>();
        }
        damagePlayer.SetDamage(Mathf.RoundToInt(config.damage));

        // --- Añadir AudioSource si no tiene (para sonidos de daño y muerte) ---
        if (newEnemy.GetComponent<AudioSource>() == null)
        {
            AudioSource audio = newEnemy.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.spatialBlend = 1f;
        }

        // Opcional: Agregar un componente genérico para almacenar las stats
        EnemyStats stats = newEnemy.GetComponent<EnemyStats>();
        if (stats == null)
        {
            stats = newEnemy.AddComponent<EnemyStats>();
        }
        stats.damage = config.damage;
        stats.health = config.health;
    }

    // Dibujar el área en el editor
    void OnDrawGizmos()
    {
        // Dibujar área del trigger
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = new Color(1, 0, 0, 0.3f);
            Gizmos.matrix = transform.localToWorldMatrix;
            
            if (col is BoxCollider)
            {
                BoxCollider box = (BoxCollider)col;
                Gizmos.DrawCube(box.center, box.size);
            }
            else if (col is SphereCollider)
            {
                SphereCollider sphere = (SphereCollider)col;
                Gizmos.DrawSphere(sphere.center, sphere.radius);
            }

            Gizmos.matrix = Matrix4x4.identity;
        }

        // Los caminos individuales se dibujan desde SpawnPointConfig.OnDrawGizmos()
    }
}

// Clase de utilidad para almacenar las stats fácilmente en el enemigo
public class EnemyStats : MonoBehaviour
{
    public float health;
    public float damage;
}
