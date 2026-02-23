# Guía de Configuración de Enemigos (Actualizada)

Hemos mejorado los enemigos para que tengan gravedad y mueran de un tiro.

## 1. Actualizar el Prefab del Enemigo

Si ya creaste el Prefab `EnemigoCapsula` anteriormente, sigue estos pasos para actualizarlo:

1.  **Abrir Prefab**:
    *   Ve a tu carpeta de **Prefabs** y haz doble clic en `EnemigoCapsula`.
2.  **Añadir Rigidbody**:
    *   En el inspector, pulsa **Add Component**.
    *   Busca `Rigidbody` y añádelo.
    *   **Configuración Importante**:
        *   `Mass`: 1 (Pudes dejarlo así).
        *   `Use Gravity`: **Activado** (True).
        *   `Is Kinematic`: **Desactivado** (False).
        *   **Constraints** (Despliega la flecha):
            *   Freeze Rotation: Marca **X** y **Z**. (Importante para que no se caiga de lado, solo queremos que gire en Y).
3.  **Verificar Scripts**:
    *   Asegúrate de que los scripts `EnemyFollow`, `GestionVida` y `Enemigos` siguen ahí.
4.  **Guardar**:
    *   Sal del modo Prefab.

## 2. Probar

1.  Dale a **Play**.
2.  Si el enemigo aparece en el aire, **debería caer al suelo** gracias a la gravedad.
3.  Si le disparas una vez, **debería morir** instantáneamente (hemos subido el daño de la bala).

¡Ahora tus zombies son físicos y mortales!
