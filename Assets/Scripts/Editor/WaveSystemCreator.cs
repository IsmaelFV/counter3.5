using UnityEngine;
using UnityEditor;

public class WaveSystemCreator : EditorWindow
{
    [MenuItem("Tools/Crear Sistema de Oleadas (Zombies)")]
    public static void CreateWaveSystem()
    {
        // 1. Crear el objeto principal de la Oleada
        GameObject waveCenter = new GameObject("Wave_Center");
        waveCenter.transform.position = Vector3.zero;

        // 2. Añadir el BoxCollider (Trigger) y configurarlo
        BoxCollider trigger = waveCenter.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = new Vector3(15f, 5f, 15f);

        // 3. Añadir el script WaveManager
        WaveManager waveManager = waveCenter.AddComponent<WaveManager>();

        // 4. Crear un objeto contenedor para los Spawns
        GameObject spawnsContainer = new GameObject("Spawns");
        spawnsContainer.transform.SetParent(waveCenter.transform);
        spawnsContainer.transform.localPosition = Vector3.zero;

        // 5. Crear spawn points con caminos de salida individuales
        Vector3[] defaultSpawnPositions = new Vector3[]
        {
            new Vector3(10, 0, 10),
            new Vector3(-10, 0, 10),
            new Vector3(10, 0, -10),
            new Vector3(-10, 0, -10)
        };

        for (int s = 0; s < defaultSpawnPositions.Length; s++)
        {
            // Crear el spawn point
            GameObject spawnPoint = new GameObject($"SpawnPoint_{s}");
            spawnPoint.transform.SetParent(spawnsContainer.transform);
            spawnPoint.transform.localPosition = defaultSpawnPositions[s];
            waveManager.spawnPoints.Add(spawnPoint.transform);

            // Añadir SpawnPointConfig con waypoints individuales
            SpawnPointConfig config = spawnPoint.AddComponent<SpawnPointConfig>();

            // Crear 3 waypoints de ejemplo como hijos del spawn point
            // Forman un camino recto desde el spawn hacia el centro (simulando salida de una casa)
            Vector3 dirToCenter = -defaultSpawnPositions[s].normalized;
            for (int w = 0; w < 3; w++)
            {
                GameObject waypoint = new GameObject($"Waypoint_{w}");
                waypoint.transform.SetParent(spawnPoint.transform);
                // Cada waypoint avanza 3 unidades hacia el centro
                waypoint.transform.localPosition = dirToCenter * (w + 1) * 3f;
                config.exitWaypoints.Add(waypoint.transform);
            }
        }

        // 6. Seleccionar el objeto creado en el Editor
        Selection.activeGameObject = waveCenter;
        
        // 7. Enfocar la cámara de la escena en el nuevo objeto
        if (SceneView.lastActiveSceneView != null)
        {
            SceneView.lastActiveSceneView.FrameSelected();
        }

        Debug.Log("¡Sistema de oleadas creado! Cada SpawnPoint tiene su propio camino de salida con 3 waypoints. Muévelos para ajustar las rutas.");
    }
}
