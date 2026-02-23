using UnityEngine;

/// <summary>
/// Pistola semi-automática - Requiere un clic por disparo
/// </summary>
public class Weapon_Pistol : WeaponBase
{
    private bool hasFiredThisClick = false;

    protected override void Update()
    {
        base.Update(); // Llamar a Update de WeaponBase para retroceso procedural
        
        // Resetear flag cuando se suelta el botón del ratón
        if (!Input.GetMouseButton(0))
        {
            hasFiredThisClick = false;
        }
    }

    /// <summary>
    /// Intenta disparar - Pistola es semi-automática
    /// </summary>
    public override bool TryShoot()
    {
        // No permitir disparo si ya se disparó con este clic
        if (hasFiredThisClick)
            return false;

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
        hasFiredThisClick = true;
        return true;
    }

    /// <summary>
    /// Resetear flag al equipar
    /// </summary>
    public override void OnEquip()
    {
        base.OnEquip();
        hasFiredThisClick = false;
    }
}
