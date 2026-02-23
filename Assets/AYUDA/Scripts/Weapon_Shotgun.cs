using UnityEngine;

/// <summary>
/// Escopeta semi-automática — dispara perdigones, un clic por disparo.
/// Cada cartucho suelta múltiples perdigones en un cono de dispersión.
/// Configurar en WeaponData:
///   - fireMode = Shotgun
///   - pelletsPerShot = 8 (o lo que quieras)
///   - pelletSpreadAngle = 6-10 grados
///   - isAutomatic = false (semi-auto)
///   - fireRate = 0.6-0.8 (más lento que un rifle)
///   - damage = daño TOTAL del cartucho (se divide entre perdigones)
///   - pelletDamage = daño por perdigón individual (override opcional)
/// </summary>
public class Weapon_Shotgun : WeaponBase
{
    private bool hasFiredThisClick = false;

    protected override void Update()
    {
        base.Update(); // Retroceso procedural + dispersión recovery

        // Resetear flag cuando se suelta el botón del ratón
        if (!Input.GetMouseButton(0))
        {
            hasFiredThisClick = false;
        }
    }

    /// <summary>
    /// Intenta disparar — Semi-automática: un clic = un cartucho = X perdigones
    /// </summary>
    public override bool TryShoot()
    {
        if (hasFiredThisClick)
            return false;

        if (!CanShoot())
        {
            if (currentAmmoInMagazine <= 0 && !isReloading)
            {
                PlayEmptySound();
                return false;
            }
            return false;
        }

        PerformShoot();
        hasFiredThisClick = true;
        return true;
    }

    /// <summary>
    /// Override para aplicar retroceso extra de escopeta (más fuerte)
    /// </summary>
    protected override void ApplyRecoil()
    {
        base.ApplyRecoil();

        // Retroceso extra para escopeta — más kick
        if (mainCamera != null)
        {
            // Push extra hacia atrás
            float extraKick = recoilAmount * 0.4f;
            Vector3 extraOffset = -mainCamera.transform.InverseTransformDirection(mainCamera.transform.forward) * extraKick;
            // Se suma al spring del arma con un impulso adicional
        }
    }

    public override void OnEquip()
    {
        base.OnEquip();
        hasFiredThisClick = false;
    }
}
