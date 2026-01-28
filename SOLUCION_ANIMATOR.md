# SOLUCIÓN DE PROBLEMAS: ANIMATOR Y DISPARO

Si la consola te grita avisos amarillos o el disparo no va, es porque Unity se está liando con dos "Cerebros" (Animators).

## ¿Qué está pasando?

Es muy probable que tengas **DOS componentes Animator**:
1.  Uno en el padre (`FPSController` o `Player`) -> **Este suele estar VACÍO y causa el error.**
2.  Otro en el hijo (`John_Ranger` o tu modelo) -> **Este es el BUENO.**

Mis scripts intentaban hablar con el primero que encontraban, y al encontrar el vacío, fallaban. He actualizado el código para que sea más listo, pero **necesitamos limpiar tu escena**.

---

## PASO 1: Operación Limpieza (Borrar el "Cerebro" zombi)

1.  Selecciona tu objeto raíz **`FPSController`** (el padre de todo).
2.  Mira en el Inspector. ¿Ves un componente **Animator**?
3.  Si lo ves, y en "Controller" pone "None" (o está vacío):
    *   Haz Click Derecho sobre el título "Animator".
    *   Dale a **Remove Component**.
    *   *¡Fuera! No lo queremos ahí.*

## PASO 2: Verificar el Bueno

1.  Busca a tu soldado (`John_Ranger` o el modelo 3D hijo).
2.  Mira su componente **Animator**.
3.  Asegúrate de que en **Controller** pone **`PlayerAnimator`** (el archivo que creamos antes).
4.  Asegúrate de que la casilla "Apply Root Motion" está **DESMARCADA** (normalmente da problemas en FPS).

## PASO 3: Probar (Debug)

1.  Dale al Play.
2.  Mira la consola (`Console`).
3.  Deberías ver mensajes blancos diciendo:
    *   `FirstPersonController: Animator found on John_Ranger` (o el nombre de tu modelo).
    *   `WeaponController: Character Animator found on John_Ranger`.
4.  Haz Click para disparar.
    *   Deberías ver: `Intentando disparar...`.
    *   Y luego: `Hit: ...`.

Si ves esos mensajes, ¡ENHORABUENA! Lo has arreglado.
