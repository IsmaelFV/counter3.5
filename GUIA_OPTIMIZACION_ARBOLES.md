# Guía de Optimización de Árboles (Partículas)

Para mejorar el rendimiento y evitar que todos los árboles generen hojas a la vez, hemos creado el script `ParticleDistanceOptimizer`.

## Pasos para aplicar la optimización:

1.  **Localiza tu Prefab de Árbol**:
    *   Ve a la carpeta de tu proyecto donde tienes el árbol con partículas (probablemente la carpeta `Prefabs`).
    *   Haz doble clic en el Prefab para abrirlo en modo edición.

2.  **Añadir el Script**:
    *   Busca el componente `ParticleSystem` en tu árbol. Puede estar en el objeto raíz o en un hijo (como "Leaves").
    *   En ese mismo objeto, pulsa **Add Component**.
    *   Busca `ParticleDistanceOptimizer` y selecciónalo.

3.  **Configuración**:
    *   **Max Distance**: Define a qué distancia (en metros) quieres que empiecen a caer las hojas. Un valor de `30` o `40` suele estar bien.
    *   **Check Interval**: Cada cuánto tiempo comprueba la distancia. `1` segundo es suficiente y ahorra recursos.

4.  **Guardar**:
    *   Sal del modo edición del Prefab asegurándote de que los cambios se guarden (Auto Save suele estar activado).

¡Listo! Ahora solo los árboles cercanos al jugador generarán partículas, lo que debería subir los FPS considerablemente.
