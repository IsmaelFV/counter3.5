using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Configuración individual de un punto de spawn.
/// Permite asignar un camino de salida independiente para cada spawner.
/// Añadir este componente al GameObject del SpawnPoint.
/// Los waypoints hijos definen la ruta que seguirán los zombies antes de perseguir al jugador.
/// </summary>
public class SpawnPointConfig : MonoBehaviour
{
    [Header("Camino de Salida Individual")]
    [Tooltip("Waypoints que el zombie seguirá al salir de este spawn. Si está vacío, persigue al jugador inmediatamente.")]
    public List<Transform> exitWaypoints = new List<Transform>();

    [Tooltip("Velocidad del zombie mientras sigue este camino de salida.")]
    public float exitSpeed = 3.5f;

    /// <summary>
    /// Devuelve true si este spawn point tiene un camino de salida configurado.
    /// </summary>
    public bool HasExitPath()
    {
        return exitWaypoints != null && exitWaypoints.Count > 0;
    }

    /// <summary>
    /// Auto-detecta waypoints hijos si la lista está vacía.
    /// Útil para configurar rapidamente: crea hijos con nombre "Waypoint" y se auto-detectan.
    /// </summary>
    public void AutoDetectWaypoints()
    {
        if (exitWaypoints.Count > 0) return;

        foreach (Transform child in transform)
        {
            if (child.name.StartsWith("Waypoint"))
            {
                exitWaypoints.Add(child);
            }
        }
    }

    // Visualización en el editor
    void OnDrawGizmos()
    {
        if (exitWaypoints == null || exitWaypoints.Count == 0) return;

        // Color único por spawn point basado en su posición (para distinguirlos)
        float hue = Mathf.Abs(transform.position.x * 0.1f + transform.position.z * 0.07f) % 1f;
        Gizmos.color = Color.HSVToRGB(hue, 0.8f, 1f);

        // Línea desde el spawn al primer waypoint
        if (exitWaypoints[0] != null)
        {
            Gizmos.DrawLine(transform.position, exitWaypoints[0].position);
        }

        // Dibujar waypoints y conexiones
        for (int i = 0; i < exitWaypoints.Count; i++)
        {
            if (exitWaypoints[i] == null) continue;

            // Esfera en el waypoint
            Gizmos.DrawWireSphere(exitWaypoints[i].position, 0.35f);

            // Número del waypoint (label)
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(exitWaypoints[i].position + Vector3.up * 0.5f, $"WP_{i}");
            #endif

            // Línea al siguiente
            if (i < exitWaypoints.Count - 1 && exitWaypoints[i + 1] != null)
            {
                Gizmos.DrawLine(exitWaypoints[i].position, exitWaypoints[i + 1].position);
            }
        }
    }
}
