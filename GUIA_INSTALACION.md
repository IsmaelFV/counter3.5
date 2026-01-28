# GUÍA DE INSTALACIÓN: SISTEMA DE DISPARO (VERSIÓN 2.0 - ASSETS REALES)

Sigue estos pasos para actualizar tu sistema de cubos a armas reales sin errores.

## PASO 1: Limpieza (Borrar lo viejo)

1.  Ve a la jerarquía de Unity.
2.  Busca tu objeto `WeaponHolder` (dentro de la Cámara del Jugador).
3.  Borra el cubo `Rifle` que creamos antes (Click Derecho -> Delete).
    *   *Vamos a poner el arma de verdad ahí.*

## PASO 2: Poner el AK-47

1.  En la carpeta **Project** (abajo), busca: `Assets > Low Poly Weapons VOL.1 > Prefabs`.
2.  Arrastra el archivo **AK74** dentro de tu `WeaponHolder` en la jerarquía.
3.  Con el AK74 seleccionado, ajusta su **Transform** para que se vea bien en cámara:
    *   **Position**: X: `0.2`, Y: `-0.2`, Z: `0.5` (Aproximado, ajústalo a tu gusto).
    *   **Rotation**: X: `0`, Y: `0`, Z: `0`.
    *   **Scale**: Si se ve gigante o enano, ajústalo (ej. `1`, `1`, `1`).

## PASO 3: Crear el "Punto de Disparo" (FirePoint)

1.  Haz **Click Derecho** sobre tu nuevo objeto **AK74** en la jerarquía.
2.  Selecciona **Create Empty**.
3.  Renómbralo a: `FirePoint`.
4.  Mueve este objeto (`FirePoint`) hasta la **punta del cañón** del arma (usando las flechas de movimiento).
    *   *Aquí es donde aparecerán las balas.*
    *   *Asegúrate de que la flecha AZUL (Z) del FirePoint apunte hacia adelante (hacia donde dispara el arma).*

## PASO 4: Preparar la Bala (El Prefab)

Tienes un modelo de bala descargado. Vamos a convertirlo en un "Prefab" funcional.

1.  Arrastra tu modelo de bala (el archivo 3D) a la escena (en cualquier lado).
2.  En el Inspector, dale a **Add Component** y escribe: `Rigidbody`.
    *   Esto hace que tenga física.
    *   *Opcional: Desmarca "Use Gravity" si quieres que vuele recto como un láser.*
3.  (Opcional) Si la bala es muy pequeña, aumenta su **Scale**.
4.  Crea una carpeta en Assets llamada `Prefabs` (si no existe).
5.  **Arrastra el objeto Bala de la jerarquía hacia esa carpeta `Prefabs`**.
6.  Ahora se pondrá de color azul. ¡Ya es un Prefab!
7.  Borra la bala que está en la escena (la de la jerarquía).

## PASO 5: Configurar el Nuevo Script

1.  Selecciona tu objeto **AK74** en la jerarquía.
2.  Arrastra el script `WeaponController.cs` al AK74.
3.  Arrastra el script `WeaponSway.cs` al AK74.
4.  Rellena los campos en el Inspector:
    *   **Bullet Prefab**: Arrastra tu nuevo prefab de Bala (desde la carpeta, no la escena).
    *   **Fire Point**: Arrastra el objeto `FirePoint` que creaste en la punta del cañón.
    *   **Fps Camera**: Arrastra tu cámara (`FirstPersonCharacter`).
    *   **Weapon Animation**: (Opcional) Si tu arma tiene animaciones, arrastra el componente Animation aquí. Si no, ¡déjalo vacío! El script ya no dará error.

## PASO 6: ¡DISPARA!

Dale al Play. Ahora deberías ver:
1.  El modelo del AK-47.
2.  Al disparar, salen balas desde la punta del cañón.
3.  Si apuntas a algo, la consola te dice qué has dado.
