using UnityEngine;

/// <summary>
/// Singleton que gestiona los ajustes del juego.
/// Persiste entre escenas y guarda los valores en PlayerPrefs.
/// </summary>
public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    // --- Claves de PlayerPrefs ---
    private const string KEY_VOLUME = "Settings_Volume";
    private const string KEY_SENSITIVITY = "Settings_Sensitivity";
    private const string KEY_QUALITY = "Settings_Quality";
    private const string KEY_FULLSCREEN = "Settings_Fullscreen";
    private const string KEY_RESOLUTION = "Settings_Resolution";

    // --- Valores actuales ---
    public float Volumen { get; private set; } = 1f;
    public float Sensibilidad { get; private set; } = 2f;
    public int NivelCalidad { get; private set; } = 2;
    public bool PantallaCompleta { get; private set; } = true;
    public int IndiceResolucion { get; private set; } = -1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        CargarAjustes();
        AplicarAjustes();
    }

    // ===================== API Pública =====================

    public void SetVolumen(float valor)
    {
        Volumen = valor;
        AudioListener.volume = valor;
        PlayerPrefs.SetFloat(KEY_VOLUME, valor);
    }

    public void SetSensibilidad(float valor)
    {
        Sensibilidad = valor;
        PlayerPrefs.SetFloat(KEY_SENSITIVITY, valor);
    }

    public void SetCalidad(int indice)
    {
        NivelCalidad = indice;
        QualitySettings.SetQualityLevel(indice);
        PlayerPrefs.SetInt(KEY_QUALITY, indice);
    }

    public void SetPantallaCompleta(bool activa)
    {
        PantallaCompleta = activa;
        Screen.fullScreen = activa;
        PlayerPrefs.SetInt(KEY_FULLSCREEN, activa ? 1 : 0);
    }

    public void SetResolucion(int indice)
    {
        IndiceResolucion = indice;
        Resolution[] resoluciones = Screen.resolutions;
        if (indice >= 0 && indice < resoluciones.Length)
        {
            Resolution res = resoluciones[indice];
            Screen.SetResolution(res.width, res.height, PantallaCompleta);
        }
        PlayerPrefs.SetInt(KEY_RESOLUTION, indice);
    }

    public void GuardarAjustes()
    {
        PlayerPrefs.Save();
    }

    // ===================== Internos =====================

    private void CargarAjustes()
    {
        Volumen = PlayerPrefs.GetFloat(KEY_VOLUME, 1f);
        Sensibilidad = PlayerPrefs.GetFloat(KEY_SENSITIVITY, 2f);
        NivelCalidad = PlayerPrefs.GetInt(KEY_QUALITY, QualitySettings.GetQualityLevel());
        PantallaCompleta = PlayerPrefs.GetInt(KEY_FULLSCREEN, Screen.fullScreen ? 1 : 0) == 1;
        IndiceResolucion = PlayerPrefs.GetInt(KEY_RESOLUTION, -1);
    }

    private void AplicarAjustes()
    {
        AudioListener.volume = Volumen;
        QualitySettings.SetQualityLevel(NivelCalidad);
        Screen.fullScreen = PantallaCompleta;

        if (IndiceResolucion >= 0)
        {
            Resolution[] resoluciones = Screen.resolutions;
            if (IndiceResolucion < resoluciones.Length)
            {
                Resolution res = resoluciones[IndiceResolucion];
                Screen.SetResolution(res.width, res.height, PantallaCompleta);
            }
        }
    }
}
