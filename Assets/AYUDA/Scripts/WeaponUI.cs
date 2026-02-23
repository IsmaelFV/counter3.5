using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Controla la interfaz visual del sistema de armas.
/// Panel inferior derecho: icono, nombre, munición, modo de disparo.
/// </summary>
public class WeaponUI : MonoBehaviour
{
    [Header("=== REFERENCIAS UI ===")]
    [SerializeField] private Image weaponIcon;
    [SerializeField] private TextMeshProUGUI weaponNameText;
    [SerializeField] private TextMeshProUGUI ammoText;
    [SerializeField] private TextMeshProUGUI fireModeText;
    [SerializeField] private GameObject reloadIndicator;
    [SerializeField] private TextMeshProUGUI reloadText;

    [Header("=== CONFIGURACIÓN VISUAL ===")]
    [SerializeField] private Color normalAmmoColor = Color.white;
    [SerializeField] private Color lowAmmoColor = new Color(1f, 0.3f, 0.3f, 1f);
    [SerializeField] private Color noAmmoColor = new Color(1f, 0f, 0f, 1f);
    [SerializeField] private int lowAmmoThreshold = 5;

    [Header("Modo de Disparo")]
    [SerializeField] private Color autoModeColor = new Color(1f, 0.6f, 0f, 1f);  // Naranja
    [SerializeField] private Color semiModeColor = new Color(0.5f, 0.8f, 1f, 1f); // Azul claro

    [Header("Efectos")]
    [SerializeField] private float shootFeedbackScale = 1.15f;
    [SerializeField] private float shootFeedbackDuration = 0.08f;

    [Header("=== LOW AMMO WARNING ===")]
    [Tooltip("Velocidad del parpadeo cuando hay poca munición (pulsos/seg)")]
    [SerializeField] private float lowAmmoPulseSpeed = 3f;
    [Tooltip("Escala mínima del pulso")]
    [SerializeField] private float lowAmmoPulseMinScale = 0.95f;
    [Tooltip("Escala máxima del pulso")]
    [SerializeField] private float lowAmmoPulseMaxScale = 1.12f;
    [Tooltip("Alpha mínima del pulso")]
    [SerializeField] private float lowAmmoPulseMinAlpha = 0.5f;

    private Coroutine shootFeedbackCoroutine;
    private Vector3 originalIconScale;
    private Vector3 originalAmmoScale = Vector3.one;
    private bool isLowAmmo = false;
    private bool isNoAmmo = false;

    private void Start()
    {
        if (weaponIcon != null)
            originalIconScale = weaponIcon.transform.localScale;

        if (ammoText != null)
        {
            originalAmmoScale = ammoText.transform.localScale;
            // Protección: si la escala guardada es cero, usar (1,1,1)
            if (originalAmmoScale.sqrMagnitude < 0.001f)
                originalAmmoScale = Vector3.one;
        }

        if (reloadIndicator != null)
            reloadIndicator.SetActive(false);
    }

    private void Update()
    {
        // Pulso de low ammo
        if (ammoText != null && (isLowAmmo || isNoAmmo))
        {
            float pulse = Mathf.Sin(Time.time * lowAmmoPulseSpeed * Mathf.PI * 2f) * 0.5f + 0.5f; // 0→1

            // Escala pulsante
            float scale = Mathf.Lerp(lowAmmoPulseMinScale, lowAmmoPulseMaxScale, pulse);
            ammoText.transform.localScale = originalAmmoScale * scale;

            // Alpha pulsante (solo si no tiene munición total)
            if (isNoAmmo)
            {
                float alpha = Mathf.Lerp(lowAmmoPulseMinAlpha, 1f, pulse);
                Color c = ammoText.color;
                c.a = alpha;
                ammoText.color = c;
            }
        }
        else if (ammoText != null)
        {
            ammoText.transform.localScale = originalAmmoScale;
        }
    }

    /// <summary>
    /// Actualiza la información del arma (nombre e icono)
    /// </summary>
    public void UpdateWeaponInfo(string weaponName, Sprite icon)
    {
        if (weaponNameText != null)
        {
            weaponNameText.text = weaponName;
        }

        if (weaponIcon != null)
        {
            // Si hay un sprite en WeaponData, usarlo
            // Si no hay, conservar el sprite que ya tenga la Image en el Editor
            if (icon != null)
            {
                weaponIcon.sprite = icon;
            }
            weaponIcon.enabled = true;
        }
    }

    /// <summary>
    /// Actualiza el modo de disparo (AUTO / SEMI)
    /// </summary>
    public void UpdateFireMode(bool isAutomatic)
    {
        if (fireModeText == null) return;

        if (isAutomatic)
        {
            fireModeText.text = "AUTO";
            fireModeText.color = autoModeColor;
        }
        else
        {
            fireModeText.text = "SEMI";
            fireModeText.color = semiModeColor;
        }
    }

    /// <summary>
    /// Actualiza el contador de munición
    /// </summary>
    public void UpdateAmmo(int currentAmmo, int reserveAmmo)
    {
        if (ammoText == null) return;

        // Formato: "30 / 120"
        ammoText.text = $"{currentAmmo} / {reserveAmmo}";

        // Actualizar estado de warning
        isNoAmmo = (currentAmmo == 0 && reserveAmmo == 0);
        isLowAmmo = !isNoAmmo && (currentAmmo <= lowAmmoThreshold);

        // Color según nivel de munición
        if (isNoAmmo)
        {
            ammoText.color = noAmmoColor;
        }
        else if (isLowAmmo)
        {
            ammoText.color = lowAmmoColor;
        }
        else
        {
            ammoText.color = normalAmmoColor;
            // Restaurar escala cuando hay munición suficiente
            ammoText.transform.localScale = originalAmmoScale;
        }
    }

    /// <summary>
    /// Muestra u oculta el indicador de recarga
    /// </summary>
    public void ShowReloadIndicator(bool show)
    {
        if (reloadIndicator != null)
        {
            reloadIndicator.SetActive(show);
        }

        if (reloadText != null)
        {
            reloadText.gameObject.SetActive(show);
            if (show) reloadText.text = "RECARGANDO...";
        }
    }

    /// <summary>
    /// Animación de feedback al disparar (pequeño rebote del icono)
    /// </summary>
    public void PlayShootFeedback()
    {
        if (weaponIcon == null) return;

        if (shootFeedbackCoroutine != null)
            StopCoroutine(shootFeedbackCoroutine);

        shootFeedbackCoroutine = StartCoroutine(ShootFeedbackCoroutine());
    }

    private IEnumerator ShootFeedbackCoroutine()
    {
        // Escalar icono brevemente
        weaponIcon.transform.localScale = originalIconScale * shootFeedbackScale;

        float elapsed = 0f;
        while (elapsed < shootFeedbackDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / shootFeedbackDuration;
            weaponIcon.transform.localScale = Vector3.Lerp(
                originalIconScale * shootFeedbackScale,
                originalIconScale,
                t
            );
            yield return null;
        }

        weaponIcon.transform.localScale = originalIconScale;
    }
}
