# GUÍA VISUAL: DANDO VIDA AL PERSONAJE

Sigue esta guía EXACTA para ver a tu soldado moverse y disparar. No te saltes ningún paso.

## PASO 1: Poner el Modelo (El Soldado)

1.  Ve a `Assets > TWO_SAPIENS_2S_JOHN_BASIC > 2S_Prefab_John_Ranger`.
    *   *(Si no lo encuentras ahí, busca "John_Ranger" en el buscador de abajo).*
2.  Busca el Prefab (el cubo azul) y ARRÁSTRALO dentro de tu objeto Jugador (`FirstPersonCharacter` o `FPSController`).
    *   **IMPORTANTE**: Hazlo hijo de la CÁMARA (`FirstPersonCharacter`) si quieres ver sus brazos al girar. O hijo del `FPSController` si quieres ver el cuerpo completo bajo la cámara.
    *   *Recomendación*: Arrástralo hijo de `FPSController` (el padre de todo) para empezar.
3.  **Resetear Posición**:
    *   Con el soldado seleccionado, ve al Transform (arriba a la derecha).
    *   Click derecho en "Transform" -> **Reset**.
    *   Ahora baja el soldado (Y) para que la cámara quede a la altura de sus ojos (aprox Y = -0.8 o -1.6 dependiendo de dónde esté tu cámara). Mueve la cámara si es necesario para que parezca que ves desde su cabeza.

## PASO 2: El Cerebro de Animación (Animator Controller)

1.  Ve a tu carpeta `Assets`. Click derecho en un espacio vacío -> **Create** -> **Animator Controller**.
2.  Llámalo: `PlayerAnimator`.
3.  Doble click para abrir la ventana **Animator**.
4.  **Crear Estado de Movimiento (Blend Tree)**:
    *   Click derecho en el fondo gris -> **Create State** -> **From New Blend Tree**.
    *   Doble click en el cuadradito "Blend Tree" que apareció.
    *   Click en el nodo "Blend Tree" (gris).
    *   En el Inspector, donde dice "Parameters", cambia "Blend" por "Speed" (si no existe, ve a la pestaña Parameters a la izquierda, dale al + -> Float, y llámalo `Speed`).
    *   En la lista "Motion" (click en el + -> Add Motion Field), añade 3 campos.
    *   Arrastra tus animaciones (de `Kevin Iglesias/Human Animations/...`):
        *   Campo 1 (Threshold 0): `Idle` (busca "Idle").
        *   Campo 2 (Threshold 0.5): `Walk` (busca "Walk").
        *   Campo 3 (Threshold 1): `Run` (busca "Run").
    *   *Truco: Desmarca "Automate Thresholds" si quieres poner los valores a mano (0, 3, 6 por ejemplo, que es la velocidad).*
5.  **Crear Disparo**:
    *   Vuelve a la capa base ("Base Layer" arriba).
    *   Pestaña Parameters -> + -> **Trigger**. Llámalo `Fire`.
    *   Click derecho fondo -> **Create State** -> **Empty**. Nómbralo `FireState`.
    *   Asignale la animación de disparo (busca "Fire" o "Attack" en el campo Motion).
        *   *IMPORTANTE*: En la animación, asegúrate de que sea solo "Upper Body" si quieres moverte y disparar, pero por ahora usa una normal.
    *   Click derecho en `Blend Tree` (tu estado de andar) -> **Make Transition** -> Conecta a `FireState`.
    *   Click en la flecha blanca de la transición. En el Inspector:
        *   Desmarca "Has Exit Time".
        *   En Conditions (+), pon `Fire`.
    *   Haz otra transición de vuelta de `FireState` a `Blend Tree` (con Has Exit Time marcado, para que vuelva al terminar de disparar).

## PASO 3: Conectar el Cerebro al Script

1.  Selecciona a tu soldado en la Jerarquía.
2.  Debería tener un componente **Animator**.
3.  Arrastra tu `PlayerAnimator` (el archivo que creaste) donde dice **Controller**.
4.  Importante: En la pestaña **Rig** del modelo (Selecciona el archivo FBX original en Project -> Inspector -> Rig), asegúrate de que "Animation Type" sea **Humanoid**. Si dice Generic, cámbialo a Humanoid y dale a Apply. Haz lo mismo con los clips de animación de Kevin Iglesias si no funcionan.

## PASO 4: Asignar en los Scripts (Lo que he programado)

1.  Selecciona tu objeto Jugador (`FPSController`).
2.  Busca el script `First Person Controller`.
3.  Verás un hueco nuevo: **Animator**. Arrastra ahí a tu soldado (el objeto que tiene el Animator).
4.  Busca tu Arma (`AK74`).
5.  En el script `Weapon Controller`, verás **Character Animator**. Arrastra ahí también al soldado.

## PASO 5: Armar al Soldado (Bone Socket)

Para que el arma se mueva con la mano:

1.  Busca la mano derecha en la jerarquía del soldado (es un laberinto, paciencia):
    *   `Soldado` -> `Hips` -> `Spine` -> `Chest` -> `Shoulder.R` -> `Arm.R` -> `ForeArm.R` -> **`Hand.R`** (o nombres similares como `RightHand`).
2.  Arrastra tu objeto `AK74` (que estaba en WeaponHolder) y suéltalo DENTRO de **`Hand.R`**.
3.  Ahora el AK-47 es hijo de la mano.
4.  Resetea el Transform del AK-47 (`0,0,0`) y ajústalo (rotar/mover) para que encaje perfecto en la palma de la mano.
5.  *Opcional*: Borra el `WeaponHolder` viejo si ya no tiene nada.

¡Dale al Play! Ahora tu soldado debería andar (animación) y disparar (animación) cuando hagas click.
