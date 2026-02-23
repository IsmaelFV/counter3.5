using UnityEngine;

/// <summary>
/// Pistola automática — dispara mientras se mantiene presionado el botón.
/// Funciona como un rifle pero con estadísticas de pistola.
/// Configurar en WeaponData:
///   - fireMode = Normal
///   - isAutomatic = true
///   - fireRate = 0.08-0.12 (cadencia rápida)
///   - damage = 15-22 (menor daño por bala que la semi)
///   - baseSpread = 1.5-2.5 (menos precisa que la semi)
///   - maxSpread = 6-8 (se descontrola al mantener)
///   - spreadPerShot = 0.6-1.0 (sube rápido la dispersión)
///   - maxAmmoInMagazine = 18-25 (cargador más grande que la semi)
///   - recoilAmount = 0.04-0.06 (retroceso moderado)
/// </summary>
public class Weapon_AutoPistol : WeaponBase
{
    /// <summary>
    /// Intenta disparar — Automática: mantener clic = disparo continuo
    /// </summary>
    public override bool TryShoot()
    {
        if (!CanShoot())
        {
            // Si no hay munición, reproducir sonido vacío
            if (currentAmmoInMagazine <= 0 && !isReloading)
            {
                PlayEmptySound();
                return false;
            }
            return false;
        }

        PerformShoot();
        return true;
    }
}
