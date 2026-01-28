# FPS Zombie Survival - Proyecto Unity

## Estado del Proyecto

Actualmente el proyecto cuenta con las siguientes mecánicas fundamentales implementadas:

*   **Terreno y Entorno**: Escenario básico con terreno y props (Puertas de madera, farolas).
*   **Control del Personaje (Player Controller)**:
    *   Movimiento en primera persona (WASD + Salto).
    *   Cámara controlada por ratón.
    *   Sonidos de pasos y salto.
*   **Sistema de Disparo (WeaponController)**:
    *   Lógica de Raycast (Hitscan) para precisión inmediata.
    *   Gestión de munición, recarga y cadencia de fuego.
    *   Instanciación de proyectiles visuales desde la punta del cañón.
    *   Feedback visual de impacto (Debug Logs).
    *   Movimiento procedimental del arma (Weapon Sway).
*   **Integración de Modelos 3D**:
    *   Personaje: Soldado "John Ranger" completamente riggeado.
    *   Arma: Modelo AK-74 low poly integrado en la mano del personaje (Bone Socket System).
*   **Sistema de Animaciones (Animator)**:
    *   Controlador de animaciones (`PlayerAnimator`) con Blend Tree para transiciones suaves (Idle -> Walk -> Run).
    *   Trigger de disparo ("Fire") sincronizado con la acción del jugador.
    *   Scripting robusto para detectar y controlar el Animator correcto en la jerarquía.
*   **Corrección de Errores**:
    *   Solucionado conflicto de "doble Animator" (Padre vs Hijo).
    *   Soporte para armas sin componente de animación legacy.

## Créditos de Assets

El proyecto utiliza las siguientes librerías y assets de la Unity Asset Store:

*   **Standard Assets**: Utilidades base de Unity (Characters, CrossPlatformInput).
*   **TWO_SAPIENS_2S_JOHN_BASIC**: Modelo 3D del personaje principal.
*   **Kevin Iglesias**: Pack de animaciones (Human Soldier Animations).
*   **Low Poly Weapons VOL.1**: Modelos de armas (AK-74, etc.).
*   **Weapons_ChamferZone**: (Posible uso futuro / Referencia).
*   **Free Wood Door Pack**: Props de entorno.
*   **SpaceZeta_StreetLamps2**: Props de iluminación.
*   **DelthorGames**: (Dependencias internas).
*   **Ishikawa1116**: (Dependencias internas).
*   **AmmoBox**: Props de munición.
*   **Disparos**: Recursos de audio y sprites para el sistema de combate.

---
*Documentación generada automáticamente por tu Asistente de IA.*
