using UnityEngine;

/// <summary>
/// Script TEMPORAL de pruebas para los sistemas de Números de Daño e Indicador Direccional.
/// Añadir al jugador (mismo GameObject que PlayerHealth).
/// 
/// CONTROLES:
///   T = Número de daño normal (frente al jugador)
///   Y = Número de daño headshot (frente al jugador)
///   U = Número de daño crítico
///   I = Número de curación
///   1 = Daño desde ADELANTE  (indicador direccional)
///   2 = Daño desde ATRÁS
///   3 = Daño desde IZQUIERDA
///   4 = Daño desde DERECHA
///   5 = Daño desde posición ALEATORIA
///
/// BORRAR ESTE SCRIPT CUANDO TERMINES DE PROBAR.
/// </summary>
public class TestDamageNumbersAndIndicator : MonoBehaviour
{
    [Header("Configuración de Test")]
    [Tooltip("Distancia a la que aparecen los números de prueba")]
    [SerializeField] private float spawnDistance = 5f;

    [Tooltip("Daño que se aplica al probar indicador direccional")]
    [SerializeField] private int testDamage = 10;

    private PlayerHealth playerHealth;

    void Start()
    {
        playerHealth = GetComponent<PlayerHealth>();
        if (playerHealth == null)
        {
            Debug.LogError("[TEST] No se encontró PlayerHealth en este GameObject. " +
                "Añade este script al mismo objeto que tiene PlayerHealth.");
        }

        Debug.Log("═══════════════════════════════════════════════════");
        Debug.Log("[TEST] Script de pruebas activo. Controles:");
        Debug.Log("  T/Y/U/I = Números de daño (Normal/Headshot/Crítico/Heal)");
        Debug.Log("  1/2/3/4/5 = Indicador direccional (Adelante/Atrás/Izq/Der/Random)");
        Debug.Log("  BORRAR este script cuando termines de probar.");
        Debug.Log("═══════════════════════════════════════════════════");
    }

    void Update()
    {
        // =====================================================================
        // NÚMEROS DE DAÑO FLOTANTES
        // =====================================================================

        if (Input.GetKeyDown(KeyCode.T))
        {
            Vector3 pos = transform.position + transform.forward * spawnDistance;
            int dmg = Random.Range(15, 80);
            DamageNumberManager.Instance.SpawnDamage(pos, dmg, DamageNumberType.Normal);
            Debug.Log($"[TEST] Número normal: {dmg}");
        }

        if (Input.GetKeyDown(KeyCode.Y))
        {
            Vector3 pos = transform.position + transform.forward * spawnDistance;
            int dmg = Random.Range(100, 350);
            DamageNumberManager.Instance.SpawnDamage(pos, dmg, DamageNumberType.Headshot);
            Debug.Log($"[TEST] Número headshot: {dmg}");
        }

        if (Input.GetKeyDown(KeyCode.U))
        {
            Vector3 pos = transform.position + transform.forward * spawnDistance;
            int dmg = Random.Range(50, 150);
            DamageNumberManager.Instance.SpawnDamage(pos, dmg, DamageNumberType.Critical);
            Debug.Log($"[TEST] Número crítico: {dmg}");
        }

        if (Input.GetKeyDown(KeyCode.I))
        {
            Vector3 pos = transform.position + transform.forward * spawnDistance;
            int heal = Random.Range(10, 50);
            DamageNumberManager.Instance.SpawnDamage(pos, heal, DamageNumberType.Heal);
            Debug.Log($"[TEST] Número curación: {heal}");
        }

        // =====================================================================
        // INDICADOR DIRECCIONAL DE DAÑO
        // =====================================================================

        if (playerHealth == null) return;

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Vector3 source = transform.position + transform.forward * spawnDistance;
            playerHealth.TakeDamage(testDamage, source);
            Debug.Log("[TEST] Daño desde ADELANTE");
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Vector3 source = transform.position - transform.forward * spawnDistance;
            playerHealth.TakeDamage(testDamage, source);
            Debug.Log("[TEST] Daño desde ATRÁS");
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Vector3 source = transform.position - transform.right * spawnDistance;
            playerHealth.TakeDamage(testDamage, source);
            Debug.Log("[TEST] Daño desde IZQUIERDA");
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            Vector3 source = transform.position + transform.right * spawnDistance;
            playerHealth.TakeDamage(testDamage, source);
            Debug.Log("[TEST] Daño desde DERECHA");
        }

        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            // Dirección aleatoria en el plano horizontal
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
            Vector3 source = transform.position + dir * spawnDistance;
            playerHealth.TakeDamage(testDamage, source);
            Debug.Log($"[TEST] Daño desde dirección aleatoria ({angle * Mathf.Rad2Deg:F0}°)");
        }
    }
}
