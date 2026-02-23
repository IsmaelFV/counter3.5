using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controla la UI de la barra de vida del jugador
/// </summary>
public class HealthBarUI : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private Image healthBarFill;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private Image lowHealthWarning; // Opcional: imagen de advertencia
    
    [Header("Configuración Visual")]
    [SerializeField] private Color healthyColor = Color.green;
    [SerializeField] private Color lowHealthColor = Color.red;
    [SerializeField] private float lowHealthThreshold = 0.3f;
    [SerializeField] private float animationSpeed = 5f;

    [Header("Efecto de Parpadeo (Vida Baja)")]
    [SerializeField] private bool enableLowHealthBlink = true;
    [SerializeField] private float blinkSpeed = 2f;

    private float targetFillAmount;
    private bool isHealthLow = false;

    void Start()
    {
        // Inicializar valores
        if (healthBarFill != null)
            targetFillAmount = healthBarFill.fillAmount;

        // Ocultar advertencia de vida baja al inicio
        if (lowHealthWarning != null)
            lowHealthWarning.enabled = false;
    }

    void Update()
    {
        // Animar la barra suavemente hacia el valor objetivo
        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = Mathf.Lerp(
                healthBarFill.fillAmount, 
                targetFillAmount, 
                Time.deltaTime * animationSpeed
            );
        }

        // Efecto de parpadeo cuando la vida es baja
        if (enableLowHealthBlink && isHealthLow && lowHealthWarning != null)
        {
            float alpha = Mathf.PingPong(Time.time * blinkSpeed, 1f);
            Color warningColor = lowHealthWarning.color;
            warningColor.a = alpha;
            lowHealthWarning.color = warningColor;
        }
    }

    /// <summary>
    /// Actualiza la barra de vida (llamado desde PlayerHealth)
    /// </summary>
    public void UpdateHealthBar(int currentHealth, int maxHealth)
    {
        // Calcular porcentaje
        float healthPercentage = (float)currentHealth / maxHealth;
        targetFillAmount = healthPercentage;

        // Actualizar texto
        if (healthText != null)
        {
            healthText.text = $"{currentHealth}/{maxHealth}";
        }

        // Cambiar color según el nivel de salud
        if (healthBarFill != null)
        {
            healthBarFill.color = healthPercentage <= lowHealthThreshold 
                ? lowHealthColor 
                : healthyColor;
        }

        // Mostrar/ocultar advertencia de vida baja
        isHealthLow = healthPercentage <= lowHealthThreshold;
        if (lowHealthWarning != null)
        {
            lowHealthWarning.enabled = isHealthLow;
        }
    }

    /// <summary>
    /// Configuración inicial desde otro script
    /// </summary>
    public void Initialize(Image fillImage, TextMeshProUGUI text, Image warningImage = null)
    {
        healthBarFill = fillImage;
        healthText = text;
        lowHealthWarning = warningImage;

        if (lowHealthWarning != null)
            lowHealthWarning.enabled = false;
    }
}
