using UnityEngine;
using System.Collections;

/// <summary>
/// Pistola de ráfagas — un clic dispara una ráfaga de balas con intervalo corto.
/// Configurar en WeaponData:
///   - fireMode = Burst
///   - burstCount = 3 (balas por ráfaga)
///   - burstInterval = 0.05-0.07 (intervalo entre balas de la ráfaga)
///   - isAutomatic = false (un clic = una ráfaga)
///   - fireRate = 0.35-0.5 (tiempo entre ráfagas completas)
///   - damage = 18-28 (daño por bala individual)
///   - baseSpread = 1.0-1.8
///   - maxAmmoInMagazine = 21-30 (divisible entre burstCount idealmente)
///   - recoilAmount = 0.04-0.06
/// </summary>
public class Weapon_BurstPistol : WeaponBase
{
    private bool hasFiredThisClick = false;
    private bool isBursting = false;
    private Coroutine burstCoroutine;

    protected override void Update()
    {
        base.Update();

        // Resetear flag cuando se suelta el botón
        if (!Input.GetMouseButton(0))
        {
            hasFiredThisClick = false;
        }
    }

    /// <summary>
    /// Intenta disparar — Un clic = una ráfaga
    /// </summary>
    public override bool TryShoot()
    {
        if (hasFiredThisClick || isBursting)
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

        // Iniciar ráfaga
        hasFiredThisClick = true;
        burstCoroutine = StartCoroutine(BurstFireCoroutine());
        return true;
    }

    /// <summary>
    /// Corrutina que ejecuta la ráfaga de balas con intervalos
    /// </summary>
    private IEnumerator BurstFireCoroutine()
    {
        isBursting = true;

        int burstCount = weaponData.burstCount;
        float interval = weaponData.burstInterval;

        for (int i = 0; i < burstCount; i++)
        {
            // Verificar que aún hay munición
            if (currentAmmoInMagazine <= 0)
            {
                PlayEmptySound();
                break;
            }

            // Verificar que no se inició una recarga
            if (isReloading) break;

            // Disparar una bala de la ráfaga
            PerformShoot();

            // Esperar el intervalo entre balas (excepto la última)
            if (i < burstCount - 1)
            {
                yield return new WaitForSeconds(interval);
            }
        }

        // Cooldown entre ráfagas (desde el FINAL de la ráfaga)
        nextFireTime = Time.time + GetUpgradedFireRate();

        isBursting = false;
        burstCoroutine = null;
    }

    /// <summary>
    /// Resetear estado al equipar
    /// </summary>
    public override void OnEquip()
    {
        base.OnEquip();
        hasFiredThisClick = false;
        isBursting = false;
    }

    /// <summary>
    /// Cancelar ráfaga al desequipar
    /// </summary>
    public override void OnUnequip()
    {
        if (burstCoroutine != null)
        {
            StopCoroutine(burstCoroutine);
            burstCoroutine = null;
        }
        isBursting = false;
        base.OnUnequip();
    }
}
