using UnityEngine;

/// <summary>
/// Interfaz para cualquier objeto que pueda recibir daño.
/// Implementar en enemigos, objetos destructibles, barriles explosivos, etc.
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// Aplica daño al objeto
    /// </summary>
    /// <param name="damage">Cantidad de daño</param>
    void TakeDamage(int damage);

    /// <summary>
    /// Aplica daño con información adicional del impacto
    /// </summary>
    /// <param name="damage">Cantidad de daño</param>
    /// <param name="hitPoint">Punto de impacto en el mundo</param>
    /// <param name="hitDirection">Dirección del disparo</param>
    void TakeDamage(int damage, Vector3 hitPoint, Vector3 hitDirection);

    /// <summary>
    /// ¿Está vivo/activo todavía?
    /// </summary>
    bool IsAlive();
}
