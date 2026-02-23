using UnityEngine;

/// <summary>
/// Escopeta automática — dispara perdigones manteniendo el botón pulsado.
/// Cada cartucho suelta múltiples perdigones en un cono de dispersión.
/// Configurar en WeaponData:
///   - fireMode = Shotgun
///   - pelletsPerShot = 6-8
///   - pelletSpreadAngle = 7-12 grados
///   - isAutomatic = true
///   - fireRate = 0.35-0.5 (cadencia de auto-shotgun)
///   - damage = daño TOTAL del cartucho
///   - pelletDamage = daño por perdigón (override opcional)
/// </summary>
public class Weapon_ShotgunAuto : WeaponBase
{
    /// <summary>
    /// Intenta disparar — Automática: mantener = disparar continuamente
    /// </summary>
    public override bool TryShoot()
    {
        if (!CanShoot())
        {
            if (currentAmmoInMagazine <= 0 && !isReloading)
            {
                PlaySound(weaponData.emptySound);
                return false;
            }
            return false;
        }

        PerformShoot();
        return true;
    }

    /// <summary>
    /// Override para retroceso extra de escopeta automática
    /// Menos retroceso que la semi-auto para compensar la cadencia
    /// </summary>
    protected override void ApplyRecoil()
    {
        base.ApplyRecoil();
    }
}
