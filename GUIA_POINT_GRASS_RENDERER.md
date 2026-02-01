# Guía de Implementación Profesional: Point Grass Renderer (Micah Wilder)

Esta guía detalla el proceso de integración, configuración y optimización del asset **Point Grass Renderer** en Unity, con un enfoque específico en su uso con el sistema de **Terrain**.

## 1. Visión General
Point Grass Renderer es una solución de renderizado de vegetación basada en **Compute Shaders** y **GPU Instancing**. A diferencia del sistema de hierba nativo de Unity, este asset permite renderizar millones de briznas de hierba con un impacto mínimo en la CPU, delegando la carga a la GPU.

### Componentes Principales
*   **PointGrassRenderer**: El componente core que gestiona la generación y renderizado de la hierba.
*   **PointGrassDisplacementManager**: Singleton opcional para gestionar la interactividad (pisadas, colisiones).
*   **PointGrassDisplacer**: Componente para objetos que interactúan con la hierba.
*   **PointGrass_SHAD**: Shader Graph optimizado para recibir los buffers de datos de la GPU.

---

## 2. Integración con Unity Terrain

El asset tiene soporte nativo para `TerrainData`, lo que permite poblar automáticamente la superficie del terreno basándose en sus capas de textura (Splatmaps).

### Paso 1: Configuración Inicial
1.  Crea un **Empty GameObject** en tu escena y nómbralo `Grass Renderer` (o similar).
2.  Añade el componente `PointGrassRenderer` a este objeto.

### Paso 2: Enlace con el Terreno
En el inspector de `PointGrassRenderer`, localiza la sección **Distribution Parameters**:
1.  **Dist Source**: Cambia este valor a `Terrain Data`.
2.  **Terrain**:
    *   **Opción A (Recomendada)**: Arrastra tu **GameObject Terrain** de la jerarquía al nuevo campo de ayuda que he añadido ("target form scene").
    *   **Opción B (Manual)**: Arrastra el **archivo Asset** de tu terreno (ej. `New Terrain`) desde el panel de proyecto al campo "Terrain Data". *No arrastres el objeto de la escena directamente a este campo original, ya que espera datos, no el componente.*
3.  **Terrain Layers**: Este es el paso crucial. Debes añadir a esta lista las **Terrain Layers** específicas donde quieres que crezca la hierba.
    *   *Nota*: El sistema usa el "peso" (alpha) de estas texturas en el terreno para determinar la densidad y altura de la hierba. Si pintas el terreno con una capa que está en esta lista, la hierba aparecerá automáticamente.

### Paso 3: Configuración de Chunking
*   **Chunk Count**: Define en cuántas secciones se divide el terreno para el procesamiento (por defecto `8x8`).
    *   *Recomendación*: Para terrenos grandes (1000x1000 o más), aumenta esto a `16x16` o `32x32`. Esto mejora el **Frustum Culling** (Unity no renderizará los chunks que estén fuera de la cámara), ahorrando rendimiento.

---

## 3. Configuración Visual y Materiales

### Material de la Hierba
1.  Crea un nuevo **Material**.
2.  Asigna el shader `Point Grass/PointGrass_SHAD` (o el Shader Graph incluido en el paquete: `Packages/Point-Grass-Renderer/Runtime/Shaders/PointGrass_SHAD.shadergraph`).
3.  Asigna este material al campo **Material** (o `Materials` si usas múltiples) en el componente `PointGrassRenderer`.

### Malla de la Hierba (Blade Mesh)
En **Grass Parameters**:
*   **Blade Type**:
    *   `Flat`: Quad simple (más rápido).
    *   `Double Quad` / `Mesh`: Para mayor detalle.
*   **Grass Blade Mesh**: Si seleccionas `Mesh`, asigna aquí tu modelo 3D personalizado de brizna o flor.

---

## 4. Control de Densidad y Rendimiento

La gestión del rendimiento es vital al usar Compute Shaders.

### Point Count (¡IMPORTANTE!)
El parámetro **Point Count** define la cantidad de briznas **POR CHUNK**, no en total.
*   *Fórmula*: `Total Briznas = Point Count * (Chunk X * Chunk Y)`
*   *Ejemplo*: Si tienes `Chunk Count = 8x8 (64 chunks)` y `Point Count = 1000`, generarás **64,000** instancias.
*   Ajusta este valor con cuidado. Empieza bajo (ej. 1000) y sube gradualmente mientras monitorizas los FPS.

### Uso de Mapas de Densidad (Splatmaps)
El sistema lee automáticamente la intensidad de las texturas del terreno.
*   **Use Density**: Si está activado, las áreas con poca opacidad en la textura del terreno tendrán menos hierba.
*   **Density Cutoff**: Umbral mínimo para que aparezca hierba. Aumentar esto elimina "briznas sueltas" en los bordes de la textura.
*   **Use Length**: Si está activado, la hierba será más corta en áreas donde la textura del terreno sea más transparente.

---

## 5. Interactividad (Displacement)

Para que la hierba se aparte cuando el jugador o vehículos pasen por encima:

1.  **Manager**:
    *   Crea un objeto vacío en la escena llamado `DisplacementManager`.
    *   Añade el componente `PointGrassDisplacementManager`.
    *   Asegúrate de que solo haya uno en la escena.

2.  **Displacers**:
    *   En tu Player o Vehículo, añade el componente `PointGrassDisplacer`.
    *   Configura el **Radius** (radio de efecto) y **Strength** (fuerza de empuje).
    *   *Limitación*: Por defecto soporta hasta **32** objetos interactivos simultáneos (definido en el script).

---

## 6. Solución de Problemas Comunes (Troubleshooting)

### La hierba no aparece
*   Verifica que el shader del material sea compatible (usa `PointGrass_SHAD`).
*   Asegúrate de que la **Terrain Layer** correcta está añadida a la lista en `PointGrassRenderer`.
*   Comprueba que el terreno está pintado con esa textura en la zona donde miras.
*   Verifica que `Point Count` > 0.

### La hierba aparece totalmente negra
*   El shader podría no estar recibiendo luz correctamente o faltan datos de color del terreno.
*   Verifica que el Compute Shader está compilando (mira la consola de Unity por errores de shader).

### Rendimiento bajo
*   Reduce `Point Count`.
*   Aumenta `Chunk Count` para mejorar el Culling.
*   Reduce `Blade Divisions` si usas generación procedural de malla.
*   Usa `Blade Type: Flat` en lugar de meshes complejos.

### Errores de "Compute Buffer" en consola
*   Asegúrate de que tu plataforma soporta **Compute Shaders** (DX11+, Metal, Vulkan). No funcionará en móviles muy antiguos o WebGL 1.0.
