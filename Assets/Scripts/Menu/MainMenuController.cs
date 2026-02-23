using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Controlador del menú principal.
/// Gestiona la navegación entre paneles y la interacción con los ajustes.
/// Todas las referencias se asignan por el Editor script MainMenuSetupEditor.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Paneles")]
    public GameObject panelMenuPrincipal;
    public GameObject panelOpciones;

    [Header("Opciones — Controles UI")]
    public Slider sliderVolumen;
    public Slider sliderSensibilidad;
    public Dropdown dropdownCalidad;
    public Toggle togglePantallaCompleta;
    public Dropdown dropdownResolucion;

    [Header("Configuración")]
    public string nombreEscenaJuego = "SampleScene";

    private Resolution[] resoluciones;

    // ===================== Inicialización =====================

    private void Start()
    {
        // Asegurar que el cursor sea visible en el menú
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        MostrarMenuPrincipal();
        InicializarOpciones();
    }

    private void InicializarOpciones()
    {
        // --- Volumen ---
        if (sliderVolumen != null)
        {
            sliderVolumen.minValue = 0f;
            sliderVolumen.maxValue = 1f;
            sliderVolumen.value = PlayerPrefs.GetFloat("Settings_Volume", 1f);
            sliderVolumen.onValueChanged.AddListener(OnVolumenCambiado);
        }

        // --- Sensibilidad ---
        if (sliderSensibilidad != null)
        {
            sliderSensibilidad.minValue = 0.1f;
            sliderSensibilidad.maxValue = 10f;
            sliderSensibilidad.value = PlayerPrefs.GetFloat("Settings_Sensitivity", 2f);
            sliderSensibilidad.onValueChanged.AddListener(OnSensibilidadCambiada);
        }

        // --- Calidad gráfica ---
        if (dropdownCalidad != null)
        {
            dropdownCalidad.ClearOptions();
            List<string> opciones = new List<string>(QualitySettings.names);
            dropdownCalidad.AddOptions(opciones);
            dropdownCalidad.value = QualitySettings.GetQualityLevel();
            dropdownCalidad.RefreshShownValue();
            dropdownCalidad.onValueChanged.AddListener(OnCalidadCambiada);
        }

        // --- Pantalla completa ---
        if (togglePantallaCompleta != null)
        {
            togglePantallaCompleta.isOn = Screen.fullScreen;
            togglePantallaCompleta.onValueChanged.AddListener(OnPantallaCompletaCambiada);
        }

        // --- Resolución ---
        if (dropdownResolucion != null)
        {
            resoluciones = Screen.resolutions;
            dropdownResolucion.ClearOptions();
            List<string> opcionesRes = new List<string>();
            int indiceActual = 0;

            for (int i = 0; i < resoluciones.Length; i++)
            {
                string opcion = resoluciones[i].width + " x " + resoluciones[i].height;
                opcionesRes.Add(opcion);

                if (resoluciones[i].width == Screen.currentResolution.width &&
                    resoluciones[i].height == Screen.currentResolution.height)
                {
                    indiceActual = i;
                }
            }

            dropdownResolucion.AddOptions(opcionesRes);
            int savedRes = PlayerPrefs.GetInt("Settings_Resolution", indiceActual);
            dropdownResolucion.value = savedRes;
            dropdownResolucion.RefreshShownValue();
            dropdownResolucion.onValueChanged.AddListener(OnResolucionCambiada);
        }
    }

    // ===================== Callbacks de Opciones =====================

    private void OnVolumenCambiado(float valor)
    {
        AudioListener.volume = valor;
        PlayerPrefs.SetFloat("Settings_Volume", valor);
    }

    private void OnSensibilidadCambiada(float valor)
    {
        PlayerPrefs.SetFloat("Settings_Sensitivity", valor);
    }

    private void OnCalidadCambiada(int indice)
    {
        QualitySettings.SetQualityLevel(indice);
        PlayerPrefs.SetInt("Settings_Quality", indice);
    }

    private void OnPantallaCompletaCambiada(bool activa)
    {
        Screen.fullScreen = activa;
        PlayerPrefs.SetInt("Settings_Fullscreen", activa ? 1 : 0);
    }

    private void OnResolucionCambiada(int indice)
    {
        if (resoluciones != null && indice >= 0 && indice < resoluciones.Length)
        {
            Resolution res = resoluciones[indice];
            Screen.SetResolution(res.width, res.height, Screen.fullScreen);
            PlayerPrefs.SetInt("Settings_Resolution", indice);
        }
    }

    // ===================== Navegación =====================

    public void MostrarMenuPrincipal()
    {
        if (panelMenuPrincipal != null) panelMenuPrincipal.SetActive(true);
        if (panelOpciones != null) panelOpciones.SetActive(false);
    }

    public void MostrarOpciones()
    {
        if (panelMenuPrincipal != null) panelMenuPrincipal.SetActive(false);
        if (panelOpciones != null) panelOpciones.SetActive(true);
    }

    // ===================== Acciones de Botones =====================

    public void Jugar()
    {
        PlayerPrefs.Save();
        SceneManager.LoadScene(nombreEscenaJuego);
    }

    public void Opciones()
    {
        MostrarOpciones();
    }

    public void Volver()
    {
        MostrarMenuPrincipal();
    }

    public void Salir()
    {
        PlayerPrefs.Save();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
