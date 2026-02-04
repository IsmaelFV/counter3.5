# Guía para Cambiar el Modelo del Jugador por "John"

Esta guía explica paso a paso cómo reemplazar el modelo del personaje en el asset `AlterunaFPS` por el modelo de "John" (`TwoSapiens`).

## 1. Localizar los archivos
Asegúrate de tener localizados:
- **Prefab del Jugador actual**: `Assets/AlterunaFPS/Prefab/Player.prefab`
- **Prefab de John**: `Assets/TwoSapiens/2S_John_Basic/2S_Prefab_John_Ranger/John_Basic_version.prefab` (la ruta puede variar, busca "John_Basic_version" en el buscador del proyecto).

## 2. Abrir la Escena de Prueba
1. Ve a `Assets/AlterunaFPS/Scenes`.
2. Abre la escena **Offline** (para probar sin necesidad de red) o **SampleScene** si es la que usas.

## 3. Reemplazar el Modelo en el Prefab
Para evitar romper el juego, modificaremos el prefab del jugador de forma segura.

1. Arrastra el prefab `Player.prefab` a la Jerarquía de la escena si no está ya ahí.
2. Haz clic derecho sobre el objeto `Player` en la Jerarquía -> **Prefab** -> **Unpack Completely** (Desempaquetar completamente). *Esto nos permite editarlo libremente sin afectar al original por ahora.*
3. Despliega el objeto `Player`.
4. Busca el objeto hijo llamado `Armature_Mesh` o `Body`. Este contiene el modelo antiguo (el robot/policía).
5. **Desactiva** el objeto `Armature_Mesh` (desmarca la casilla junto al nombre en el Inspector). *No lo borres aún por si acaso.*

## 4. Integrar a John
1. Busca el prefab `John_Basic_version` en tu carpeta de Proyecto.
2. Arrástralo **dentro** del objeto `Player` en la Jerarquía (hazlo hijo de `Player`).
3. Resetea su Transform: Asegúrate de que su Posición sea `(0, 0, 0)` y Rotación `(0, 0, 0)`.
4. Ajusta la Escala si es necesario (compara el tamaño con el modelo antiguo activándolo momentáneamente).

## 5. Configurar Animaciones (Importante)
El personaje necesita moverse. El `Player` original suele tener un componente **Animator** (a veces en el objeto raíz o en el hijo del modelo).

1. **Si el Animator está en el raíz (`Player`):**
   - Selecciona `Player`.
   - Busca el componente **Animator**.
   - En el campo **Avatar**, arrastra el Avatar de John (lo encontrarás dentro del prefab de John o en sus carpetas, suele llamarse `John_Avatar` o similar).
   - Asegúrate de que el **Controller** sigue siendo el de Alteruna (para que se mueva igual). *Nota: Si el esqueleto de John es muy diferente, las animaciones pueden verse raras y necesitarás retargeting, pero los humanoides suelen ser compatibles.*

2. **Si el Animator falta o no funciona:**
   - Asegúrate de que el objeto `John_Basic_version` que añadiste tenga un componente **Animator**.
   - Si el script de movimiento (`PlayerController`) busca el Animator, puede que necesites asignarlo manualmente en el Inspector del script.

## 6. Probar
1. Dale a **Play**.
2. Comprueba si el personaje se mueve y se ve como John.
3. Si el arma no aparece en la mano correcta, tendrás que buscar el objeto "Hand" o "WeaponHolder" dentro del esqueleto de John (en la jerarquía de huesos: `Hips -> Spine -> ... -> RightShoulder -> RightArm -> RightHand`) y mover los objetos del arma antigua ahí.

## 7. Guardar Cambios
Si todo funciona bien:
1. Arrastra tu objeto `Player` modificado desde la Jerarquía a una carpeta de tus Prefabs (crea una nueva carpeta `Assets/MisPrefabs`).
2. Crea un nuevo Prefab original (Variant o Original).
3. Usa este nuevo prefab en tus escenas.

---
> [!TIP]
> Si las animaciones no funcionan (el personaje desliza en T-Pose), verifica que el "Avatar" en el componente Animator corresponda al esqueleto de John y que el "Controller" sea el de Alteruna.
