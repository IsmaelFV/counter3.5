using UnityEngine;

/// <summary>
/// Script TEMPORAL para probar las 4 nuevas mecánicas:
///   - Hitmarker (al disparar enemigos de prueba)
///   - Low Ammo Warning (ya se ve al disparar)
///   - Screen Blood (al recibir daño)
///   - Combo/Racha (kills rápidas)
///
/// SETUP:
///   1. Crea 3-4 cubos en la escena (serán "enemigos")
///   2. A cada cubo: Add Component → EnemyHealth (maxHealth = 50)
///   3. Añade este script al jugador
///   4. Play → Dispara a los cubos rápido para ver combo + hitmarker
///
/// CONTROLES EXTRA (sin necesidad de enemigos):
///   4 = Forzar hitmarker normal
///   5 = Forzar hitmarker headshot (rojo)
///   6 = Simular daño al jugador (activa screen blood)
///   7 = Simular kill (sube combo sin necesidad de enemigos reales)
///   8 = Resetear vida al máximo
///   9 = Ponerte a 20% de vida (ver screen blood + pulso)
///   0 = Ponerte a 10% de vida (ver pulso crítico)
///
/// BORRAR ESTE SCRIPT CUANDO TERMINES DE PROBAR.
/// </summary>
public class TestNewMechanics : MonoBehaviour
{
    [Header("Referencias (se buscan automáticamente)")]
    [SerializeField] private HitmarkerUI hitmarkerUI;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerEconomy playerEconomy;

    void Start()
    {
        if (hitmarkerUI == null)
            hitmarkerUI = GetComponent<HitmarkerUI>();
        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();
        if (playerEconomy == null)
            playerEconomy = GetComponent<PlayerEconomy>();

        Debug.Log("══════════════════════════════════════════════════");
        Debug.Log("[TEST MECÁNICAS] Script de pruebas activo.");
        Debug.Log("  4 = Hitmarker normal");
        Debug.Log("  5 = Hitmarker headshot (rojo)");
        Debug.Log("  6 = Recibir daño (-15 HP)");
        Debug.Log("  7 = Simular kill (sube combo)");
        Debug.Log("  8 = Resetear vida al máximo");
        Debug.Log("  9 = Poner vida al 20%");
        Debug.Log("  0 = Poner vida al 10%");
        Debug.Log("  → Dispara cubos con EnemyHealth para ver todo junto");
        Debug.Log("  → BORRAR este script cuando termines.");
        Debug.Log("══════════════════════════════════════════════════");
    }

    void Update()
    {
        // =====================================================================
        // HITMARKER
        // =====================================================================

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            if (hitmarkerUI != null)
            {
                hitmarkerUI.ShowHitmarker(false);
                Debug.Log("[TEST] Hitmarker normal");
            }
            else
                Debug.LogWarning("[TEST] HitmarkerUI no encontrado. ¿Está en el jugador?");
        }

        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            if (hitmarkerUI != null)
            {
                hitmarkerUI.ShowHitmarker(true);
                Debug.Log("[TEST] Hitmarker HEADSHOT (rojo)");
            }
            else
                Debug.LogWarning("[TEST] HitmarkerUI no encontrado.");
        }

        // =====================================================================
        // SCREEN BLOOD (daño al jugador)
        // =====================================================================

        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(15);
                Debug.Log($"[TEST] -15 HP → Vida: {playerHealth.GetCurrentHealth()}/{playerHealth.GetMaxHealth()}");
            }
            else
                Debug.LogWarning("[TEST] PlayerHealth no encontrado.");
        }

        // =====================================================================
        // COMBO / RACHA
        // =====================================================================

        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            if (playerEconomy != null)
            {
                // Simula un kill registrado en PlayerEconomy
                playerEconomy.RegisterKill(0, false, false);
                Debug.Log($"[TEST] Kill simulada → Racha: x{playerEconomy.CurrentStreak}");
            }
            else
                Debug.LogWarning("[TEST] PlayerEconomy no encontrado.");
        }

        // =====================================================================
        // CONTROL DE VIDA (para probar screen blood)
        // =====================================================================

        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            if (playerHealth != null)
            {
                playerHealth.Heal(999);
                Debug.Log($"[TEST] Vida restaurada: {playerHealth.GetCurrentHealth()}/{playerHealth.GetMaxHealth()}");
            }
        }

        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            if (playerHealth != null)
            {
                int targetHP = Mathf.RoundToInt(playerHealth.GetMaxHealth() * 0.2f);
                int damage = playerHealth.GetCurrentHealth() - targetHP;
                if (damage > 0)
                    playerHealth.TakeDamage(damage);
                Debug.Log($"[TEST] Vida al 20%: {playerHealth.GetCurrentHealth()}/{playerHealth.GetMaxHealth()}");
            }
        }

        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            if (playerHealth != null)
            {
                int targetHP = Mathf.RoundToInt(playerHealth.GetMaxHealth() * 0.1f);
                int damage = playerHealth.GetCurrentHealth() - targetHP;
                if (damage > 0)
                    playerHealth.TakeDamage(damage);
                Debug.Log($"[TEST] Vida al 10% (CRÍTICO): {playerHealth.GetCurrentHealth()}/{playerHealth.GetMaxHealth()}");
            }
        }
    }
}
