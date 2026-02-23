using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Muestra el contador de botiquines en la UI
/// </summary>
public class MedkitUI : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private TextMeshProUGUI medkitCountText;
    [SerializeField] private Image medkitIcon;

    [Header("Configuración Visual")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color emptyColor = Color.red;
    [SerializeField] private int maxMedkits = 3;

    /// <summary>
    /// Actualiza el contador de botiquines
    /// </summary>
    public void UpdateMedkitCount(int current)
    {
        if (medkitCountText != null)
        {
            medkitCountText.text = $"Botiquines: {current}/{maxMedkits}";
            
            // Cambiar color si no hay botiquines
            medkitCountText.color = current > 0 ? normalColor : emptyColor;
        }

        if (medkitIcon != null)
        {
            medkitIcon.color = current > 0 ? normalColor : emptyColor;
        }
    }
}
