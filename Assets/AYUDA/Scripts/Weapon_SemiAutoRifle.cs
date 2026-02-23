using UnityEngine;

/// <summary>
/// Rifle semi-automático — un clic por disparo, mayor daño y precisión que el automático.
/// Ideal para distancias medias-largas with higher per-shot damage.
/// Configurar en WeaponData:
///   - fireMode = Normal
///   - isAutomatic = false
///   - fireRate = 0.25-0.4 (más lento que automático pero más rápido que escopeta)
///   - damage = 45-60 (mayor daño por bala que el rifle auto)
///   - baseSpread = 0.3-0.8 (más preciso)
///   - maxAmmoInMagazine = 12-20 (cargador más pequeño)
/// </summary>
public class Weapon_SemiAutoRifle : WeaponBase
{
    private bool hasFiredThisClick = false;

    protected override void Update()
    {
        base.Update(); // Retroceso procedural + dispersión recovery

        // Resetear flag cuando se suelta el botón
        if (!Input.GetMouseButton(0))
        {
            hasFiredThisClick = false;
        }
    }

    /// <summary>
    /// Intenta disparar — Semi-automático: un clic = un disparo
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

    public override void OnEquip()
    {
        base.OnEquip();
        hasFiredThisClick = false;
    }
}
