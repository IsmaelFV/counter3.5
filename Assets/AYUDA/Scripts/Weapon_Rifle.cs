using UnityEngine;

/// <summary>
/// Rifle automático - Dispara mientras se mantiene presionado
/// </summary>
public class Weapon_Rifle : WeaponBase
{
    /// <summary>
    /// Intenta disparar - Rifle es automático
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
