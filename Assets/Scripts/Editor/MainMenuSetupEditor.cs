#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Reflection;

/// <summary>
/// Editor script que genera automáticamente toda la escena del menú principal.
/// Accesible desde: Tools > Crear Menú Principal
/// </summary>
public class MainMenuSetupEditor : EditorWindow
{
    // ============================================================
    //  Colores del tema
    // ============================================================
    private static readonly Color COLOR_FONDO       = new Color(0.08f, 0.08f, 0.12f, 1f);    // Casi negro
    private static readonly Color COLOR_PANEL       = new Color(0.12f, 0.12f, 0.18f, 0.95f); // Panel oscuro
    private static readonly Color COLOR_BOTON       = new Color(0.85f, 0.55f, 0.10f, 1f);    // Naranja dorado
    private static readonly Color COLOR_BOTON_HOVER = new Color(0.95f, 0.65f, 0.15f, 1f);
    private static readonly Color COLOR_TEXTO       = Color.white;
    private static readonly Color COLOR_TITULO      = new Color(0.95f, 0.75f, 0.20f, 1f);    // Dorado
    private static readonly Color COLOR_SLIDER_BG   = new Color(0.2f, 0.2f, 0.25f, 1f);
    private static readonly Color COLOR_SLIDER_FILL = new Color(0.85f, 0.55f, 0.10f, 1f);

    private string nombreEscenaJuego = "SampleScene";

    [MenuItem("Tools/Crear Menú Principal")]
    public static void MostrarVentana()
    {
        var ventana = GetWindow<MainMenuSetupEditor>("Crear Menú Principal");
        ventana.minSize = new Vector2(350, 150);
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        GUILayout.Label("Generador de Menú Principal", EditorStyles.boldLabel);
        GUILayout.Space(5);

        nombreEscenaJuego = EditorGUILayout.TextField("Escena del juego:", nombreEscenaJuego);

        GUILayout.Space(15);

        if (GUILayout.Button("¡Crear Menú Principal!", GUILayout.Height(40)))
        {
            CrearMenuPrincipal();
        }

        GUILayout.Space(5);
        EditorGUILayout.HelpBox(
            "Se creará una nueva escena 'MainMenu' en Assets/Scenes/ " +
            "con todos los elementos del menú generados automáticamente.\n\n" +
            "Las escenas se añadirán al Build Settings automáticamente.",
            MessageType.Info);
    }

    // ============================================================
    //  Generación completa del menú
    // ============================================================
    private void CrearMenuPrincipal()
    {
        // 1. Crear nueva escena
        var nuevaEscena = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // 2. Configurar cámara de fondo
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = COLOR_FONDO;
        }

        // 3. Crear Canvas
        GameObject canvasGO = CrearCanvas();

        // 4. Crear EventSystem si no existe
        if (Object.FindObjectOfType<EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        // 5. Crear fondo
        CrearFondo(canvasGO.transform);

        // 6. Crear Panel Menú Principal
        GameObject panelMenu = CrearPanelMenuPrincipal(canvasGO.transform);

        // 7. Crear Panel Opciones
        GameObject panelOpciones = CrearPanelOpciones(canvasGO.transform);
        panelOpciones.SetActive(false);

        // 8. Añadir SettingsManager
        GameObject settingsGO = new GameObject("SettingsManager");
        settingsGO.AddComponent<SettingsManager>();

        // 9. Añadir MainMenuController y asignar referencias
        MainMenuController controller = canvasGO.AddComponent<MainMenuController>();
        controller.panelMenuPrincipal = panelMenu;
        controller.panelOpciones = panelOpciones;
        controller.nombreEscenaJuego = nombreEscenaJuego;

        // Buscar y asignar controles de opciones
        AsignarControlesOpciones(controller, panelOpciones);

        // Conectar botones
        ConectarBotones(panelMenu, panelOpciones, controller);

        // 10. Guardar escena
        string rutaEscena = "Assets/Scenes/MainMenu.unity";
        EditorSceneManager.SaveScene(nuevaEscena, rutaEscena);

        // 11. Configurar Build Settings
        ConfigurarBuildSettings(rutaEscena);

        // 12. Marcar como dirty
        EditorSceneManager.MarkSceneDirty(nuevaEscena);
        EditorSceneManager.SaveScene(nuevaEscena);

        EditorUtility.DisplayDialog("¡Menú Creado!",
            "Se ha creado la escena MainMenu.unity con éxito.\n\n" +
            "Las escenas han sido añadidas al Build Settings.\n" +
            "MainMenu = escena 0 (inicio)\n" +
            nombreEscenaJuego + " = escena 1 (juego)",
            "¡Genial!");

        Debug.Log("[MenuSetup] Menú principal creado correctamente en: " + rutaEscena);
    }

    // ============================================================
    //  Canvas
    // ============================================================
    private GameObject CrearCanvas()
    {
        GameObject canvasGO = new GameObject("Canvas_Menu");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        return canvasGO;
    }

    // ============================================================
    //  Fondo
    // ============================================================
    private void CrearFondo(Transform padre)
    {
        GameObject fondoGO = CrearElementoUI("Fondo", padre);
        Image fondoImg = fondoGO.AddComponent<Image>();
        fondoImg.color = COLOR_FONDO;
        EstirarRectTransform(fondoGO.GetComponent<RectTransform>());
    }

    // ============================================================
    //  Panel Menú Principal
    // ============================================================
    private GameObject CrearPanelMenuPrincipal(Transform padre)
    {
        // Contenedor principal
        GameObject panel = CrearElementoUI("Panel_MenuPrincipal", padre);
        EstirarRectTransform(panel.GetComponent<RectTransform>());

        // Panel central con fondo semi-transparente
        GameObject panelCentral = CrearElementoUI("PanelCentral", panel.transform);
        Image bgCentral = panelCentral.AddComponent<Image>();
        bgCentral.color = COLOR_PANEL;
        RectTransform rtCentral = panelCentral.GetComponent<RectTransform>();
        rtCentral.anchorMin = new Vector2(0.3f, 0.15f);
        rtCentral.anchorMax = new Vector2(0.7f, 0.85f);
        rtCentral.offsetMin = Vector2.zero;
        rtCentral.offsetMax = Vector2.zero;

        // Layout vertical
        VerticalLayoutGroup layout = panelCentral.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(40, 40, 40, 40);
        layout.spacing = 20;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        // Título
        CrearTexto("Titulo", panelCentral.transform, "MI JUEGO", 52, COLOR_TITULO, FontStyle.Bold, 80f);

        // Separador
        CrearSeparador(panelCentral.transform);

        // Espaciador
        CrearEspaciador(panelCentral.transform, 20f);

        // Botones
        CrearBoton("Btn_Jugar", panelCentral.transform, "JUGAR", 60f);
        CrearBoton("Btn_Opciones", panelCentral.transform, "OPCIONES", 60f);
        CrearBoton("Btn_Salir", panelCentral.transform, "SALIR", 60f);

        return panel;
    }

    // ============================================================
    //  Panel Opciones
    // ============================================================
    private GameObject CrearPanelOpciones(Transform padre)
    {
        GameObject panel = CrearElementoUI("Panel_Opciones", padre);
        EstirarRectTransform(panel.GetComponent<RectTransform>());

        // Panel central
        GameObject panelCentral = CrearElementoUI("PanelCentral_Opciones", panel.transform);
        Image bgCentral = panelCentral.AddComponent<Image>();
        bgCentral.color = COLOR_PANEL;
        RectTransform rtCentral = panelCentral.GetComponent<RectTransform>();
        rtCentral.anchorMin = new Vector2(0.2f, 0.08f);
        rtCentral.anchorMax = new Vector2(0.8f, 0.92f);
        rtCentral.offsetMin = Vector2.zero;
        rtCentral.offsetMax = Vector2.zero;

        // Layout vertical
        VerticalLayoutGroup layout = panelCentral.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(50, 50, 30, 30);
        layout.spacing = 15;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        // Título
        CrearTexto("Titulo_Opciones", panelCentral.transform, "OPCIONES", 42, COLOR_TITULO, FontStyle.Bold, 60f);
        CrearSeparador(panelCentral.transform);
        CrearEspaciador(panelCentral.transform, 10f);

        // --- Sección: Ajustes de Audio ---
        CrearTexto("Label_SeccionAudio", panelCentral.transform, "— AUDIO —", 22, COLOR_TITULO, FontStyle.Bold, 35f);

        // Slider Volumen
        CrearFilaSlider("Fila_Volumen", panelCentral.transform, "Volumen General", "Slider_Volumen", 0f, 1f, 1f);

        CrearEspaciador(panelCentral.transform, 5f);

        // --- Sección: Controles ---
        CrearTexto("Label_SeccionControles", panelCentral.transform, "— CONTROLES —", 22, COLOR_TITULO, FontStyle.Bold, 35f);

        // Slider Sensibilidad
        CrearFilaSlider("Fila_Sensibilidad", panelCentral.transform, "Sensibilidad Ratón", "Slider_Sensibilidad", 0.1f, 10f, 2f);

        CrearEspaciador(panelCentral.transform, 5f);

        // --- Sección: Gráficos ---
        CrearTexto("Label_SeccionGraficos", panelCentral.transform, "— GRÁFICOS —", 22, COLOR_TITULO, FontStyle.Bold, 35f);

        // Dropdown Calidad
        CrearFilaDropdown("Fila_Calidad", panelCentral.transform, "Calidad Gráfica", "Dropdown_Calidad");

        // Toggle Pantalla Completa
        CrearFilaToggle("Fila_PantallaCompleta", panelCentral.transform, "Pantalla Completa", "Toggle_PantallaCompleta");

        // Dropdown Resolución
        CrearFilaDropdown("Fila_Resolucion", panelCentral.transform, "Resolución", "Dropdown_Resolucion");

        CrearEspaciador(panelCentral.transform, 10f);
        CrearSeparador(panelCentral.transform);
        CrearEspaciador(panelCentral.transform, 5f);

        // Botón Volver
        CrearBoton("Btn_Volver", panelCentral.transform, "VOLVER", 55f);

        return panel;
    }

    // ============================================================
    //  Asignar controles al controller
    // ============================================================
    private void AsignarControlesOpciones(MainMenuController controller, GameObject panelOpciones)
    {
        // Buscar sliders y dropdowns por nombre
        controller.sliderVolumen = BuscarComponenteEnHijos<Slider>(panelOpciones, "Slider_Volumen");
        controller.sliderSensibilidad = BuscarComponenteEnHijos<Slider>(panelOpciones, "Slider_Sensibilidad");
        controller.dropdownCalidad = BuscarComponenteEnHijos<Dropdown>(panelOpciones, "Dropdown_Calidad");
        controller.togglePantallaCompleta = BuscarComponenteEnHijos<Toggle>(panelOpciones, "Toggle_PantallaCompleta");
        controller.dropdownResolucion = BuscarComponenteEnHijos<Dropdown>(panelOpciones, "Dropdown_Resolucion");
    }

    private T BuscarComponenteEnHijos<T>(GameObject raiz, string nombre) where T : Component
    {
        T[] componentes = raiz.GetComponentsInChildren<T>(true);
        foreach (T comp in componentes)
        {
            if (comp.gameObject.name == nombre)
                return comp;
        }
        return null;
    }

    // ============================================================
    //  Conectar botones
    // ============================================================
    private void ConectarBotones(GameObject panelMenu, GameObject panelOpciones, MainMenuController controller)
    {
        // Menú Principal
        ConectarBoton(panelMenu, "Btn_Jugar", controller, "Jugar");
        ConectarBoton(panelMenu, "Btn_Opciones", controller, "Opciones");
        ConectarBoton(panelMenu, "Btn_Salir", controller, "Salir");

        // Opciones
        ConectarBoton(panelOpciones, "Btn_Volver", controller, "Volver");
    }

    private void ConectarBoton(GameObject raiz, string nombreBoton, MainMenuController target, string metodo)
    {
        Button[] botones = raiz.GetComponentsInChildren<Button>(true);
        foreach (Button btn in botones)
        {
            if (btn.gameObject.name == nombreBoton)
            {
                // Usar UnityEvent persistent call
                UnityEditor.Events.UnityEventTools.AddPersistentListener(
                    btn.onClick,
                    (UnityEngine.Events.UnityAction)System.Delegate.CreateDelegate(
                        typeof(UnityEngine.Events.UnityAction), target, metodo));
                break;
            }
        }
    }

    // ============================================================
    //  Build Settings
    // ============================================================
    private void ConfigurarBuildSettings(string rutaMenuEscena)
    {
        // Recoger la escena del juego
        string rutaJuego = "Assets/Scenes/" + nombreEscenaJuego + ".unity";

        List<EditorBuildSettingsScene> escenas = new List<EditorBuildSettingsScene>();

        // MainMenu siempre primero (índice 0)
        escenas.Add(new EditorBuildSettingsScene(rutaMenuEscena, true));

        // Escena del juego (índice 1)
        escenas.Add(new EditorBuildSettingsScene(rutaJuego, true));

        EditorBuildSettings.scenes = escenas.ToArray();

        Debug.Log("[MenuSetup] Build Settings actualizados: " +
            rutaMenuEscena + " (0), " + rutaJuego + " (1)");
    }

    // ============================================================
    //  Helpers de UI
    // ============================================================

    private GameObject CrearElementoUI(string nombre, Transform padre)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);
        return go;
    }

    private void EstirarRectTransform(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private GameObject CrearTexto(string nombre, Transform padre, string texto, int tamano,
        Color color, FontStyle estilo = FontStyle.Normal, float altura = 40f)
    {
        GameObject go = CrearElementoUI(nombre, padre);
        Text txt = go.AddComponent<Text>();
        txt.text = texto;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = tamano;
        txt.color = color;
        txt.fontStyle = estilo;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;

        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredHeight = altura;
        le.flexibleWidth = 1;

        return go;
    }

    private GameObject CrearBoton(string nombre, Transform padre, string texto, float altura = 50f)
    {
        GameObject btnGO = CrearElementoUI(nombre, padre);
        Image btnImg = btnGO.AddComponent<Image>();
        btnImg.color = COLOR_BOTON;

        Button btn = btnGO.AddComponent<Button>();
        ColorBlock colores = btn.colors;
        colores.normalColor = COLOR_BOTON;
        colores.highlightedColor = COLOR_BOTON_HOVER;
        colores.pressedColor = new Color(0.75f, 0.45f, 0.05f, 1f);
        colores.selectedColor = COLOR_BOTON;
        btn.colors = colores;

        // Texto del botón
        GameObject txtGO = CrearElementoUI("Text", btnGO.transform);
        Text txt = txtGO.AddComponent<Text>();
        txt.text = texto;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 26;
        txt.color = COLOR_TEXTO;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        EstirarRectTransform(txtGO.GetComponent<RectTransform>());

        LayoutElement le = btnGO.AddComponent<LayoutElement>();
        le.preferredHeight = altura;
        le.flexibleWidth = 1;

        return btnGO;
    }

    private void CrearSeparador(Transform padre)
    {
        GameObject sep = CrearElementoUI("Separador", padre);
        Image img = sep.AddComponent<Image>();
        img.color = new Color(COLOR_TITULO.r, COLOR_TITULO.g, COLOR_TITULO.b, 0.3f);

        LayoutElement le = sep.AddComponent<LayoutElement>();
        le.preferredHeight = 2;
        le.flexibleWidth = 1;
    }

    private void CrearEspaciador(Transform padre, float altura)
    {
        GameObject esp = CrearElementoUI("Espaciador", padre);
        LayoutElement le = esp.AddComponent<LayoutElement>();
        le.preferredHeight = altura;
    }

    // ---------- Fila con Slider ----------
    private void CrearFilaSlider(string nombre, Transform padre, string etiqueta,
        string nombreSlider, float min, float max, float valorDefecto)
    {
        GameObject fila = CrearElementoUI(nombre, padre);
        HorizontalLayoutGroup hlg = fila.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 15;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        LayoutElement leFila = fila.AddComponent<LayoutElement>();
        leFila.preferredHeight = 40;
        leFila.flexibleWidth = 1;

        // Label
        GameObject labelGO = CrearElementoUI("Label", fila.transform);
        Text label = labelGO.AddComponent<Text>();
        label.text = etiqueta;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 20;
        label.color = COLOR_TEXTO;
        label.alignment = TextAnchor.MiddleLeft;
        LayoutElement leLabel = labelGO.AddComponent<LayoutElement>();
        leLabel.preferredWidth = 250;
        leLabel.flexibleWidth = 0;

        // Slider
        GameObject sliderGO = CrearSlider(nombreSlider, fila.transform, min, max, valorDefecto);
        LayoutElement leSlider = sliderGO.AddComponent<LayoutElement>();
        leSlider.flexibleWidth = 1;
        leSlider.preferredHeight = 30;
    }

    private GameObject CrearSlider(string nombre, Transform padre, float min, float max, float valorDefecto)
    {
        // Contenedor del slider
        GameObject sliderGO = CrearElementoUI(nombre, padre);
        RectTransform sliderRT = sliderGO.GetComponent<RectTransform>();
        sliderRT.sizeDelta = new Vector2(300, 30);

        Slider slider = sliderGO.AddComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.value = valorDefecto;

        // Background
        GameObject bgGO = CrearElementoUI("Background", sliderGO.transform);
        Image bgImg = bgGO.AddComponent<Image>();
        bgImg.color = COLOR_SLIDER_BG;
        RectTransform bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0, 0.35f);
        bgRT.anchorMax = new Vector2(1, 0.65f);
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;

        // Fill Area
        GameObject fillAreaGO = CrearElementoUI("Fill Area", sliderGO.transform);
        RectTransform fillAreaRT = fillAreaGO.GetComponent<RectTransform>();
        fillAreaRT.anchorMin = new Vector2(0, 0.35f);
        fillAreaRT.anchorMax = new Vector2(1, 0.65f);
        fillAreaRT.offsetMin = new Vector2(5, 0);
        fillAreaRT.offsetMax = new Vector2(-5, 0);

        GameObject fillGO = CrearElementoUI("Fill", fillAreaGO.transform);
        Image fillImg = fillGO.AddComponent<Image>();
        fillImg.color = COLOR_SLIDER_FILL;
        RectTransform fillRT = fillGO.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;

        // Handle Slide Area
        GameObject handleAreaGO = CrearElementoUI("Handle Slide Area", sliderGO.transform);
        RectTransform handleAreaRT = handleAreaGO.GetComponent<RectTransform>();
        handleAreaRT.anchorMin = Vector2.zero;
        handleAreaRT.anchorMax = Vector2.one;
        handleAreaRT.offsetMin = new Vector2(10, 0);
        handleAreaRT.offsetMax = new Vector2(-10, 0);

        GameObject handleGO = CrearElementoUI("Handle", handleAreaGO.transform);
        Image handleImg = handleGO.AddComponent<Image>();
        handleImg.color = Color.white;
        RectTransform handleRT = handleGO.GetComponent<RectTransform>();
        handleRT.sizeDelta = new Vector2(20, 0);

        // Asignar referencias del slider
        slider.fillRect = fillRT;
        slider.handleRect = handleRT;
        slider.targetGraphic = handleImg;

        return sliderGO;
    }

    // ---------- Fila con Dropdown ----------
    private void CrearFilaDropdown(string nombre, Transform padre, string etiqueta, string nombreDropdown)
    {
        GameObject fila = CrearElementoUI(nombre, padre);
        HorizontalLayoutGroup hlg = fila.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 15;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        LayoutElement leFila = fila.AddComponent<LayoutElement>();
        leFila.preferredHeight = 40;
        leFila.flexibleWidth = 1;

        // Label
        GameObject labelGO = CrearElementoUI("Label", fila.transform);
        Text label = labelGO.AddComponent<Text>();
        label.text = etiqueta;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 20;
        label.color = COLOR_TEXTO;
        label.alignment = TextAnchor.MiddleLeft;
        LayoutElement leLabel = labelGO.AddComponent<LayoutElement>();
        leLabel.preferredWidth = 250;
        leLabel.flexibleWidth = 0;

        // Dropdown
        GameObject ddGO = CrearDropdown(nombreDropdown, fila.transform);
        LayoutElement leDD = ddGO.AddComponent<LayoutElement>();
        leDD.flexibleWidth = 1;
        leDD.preferredHeight = 35;
    }

    private GameObject CrearDropdown(string nombre, Transform padre)
    {
        GameObject ddGO = CrearElementoUI(nombre, padre);
        RectTransform ddRT = ddGO.GetComponent<RectTransform>();
        ddRT.sizeDelta = new Vector2(300, 35);

        Image ddImg = ddGO.AddComponent<Image>();
        ddImg.color = COLOR_SLIDER_BG;

        Dropdown dd = ddGO.AddComponent<Dropdown>();

        // Label del dropdown
        GameObject labelGO = CrearElementoUI("Label", ddGO.transform);
        Text labelTxt = labelGO.AddComponent<Text>();
        labelTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelTxt.fontSize = 18;
        labelTxt.color = COLOR_TEXTO;
        labelTxt.alignment = TextAnchor.MiddleLeft;
        RectTransform labelRT = labelGO.GetComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = new Vector2(10, 0);
        labelRT.offsetMax = new Vector2(-25, 0);

        // Flecha
        GameObject arrowGO = CrearElementoUI("Arrow", ddGO.transform);
        Image arrowImg = arrowGO.AddComponent<Image>();
        arrowImg.color = COLOR_TEXTO;
        RectTransform arrowRT = arrowGO.GetComponent<RectTransform>();
        arrowRT.anchorMin = new Vector2(1f, 0.3f);
        arrowRT.anchorMax = new Vector2(1f, 0.7f);
        arrowRT.sizeDelta = new Vector2(20, 0);
        arrowRT.anchoredPosition = new Vector2(-15, 0);

        // Template (desplegable)
        GameObject templateGO = CrearElementoUI("Template", ddGO.transform);
        Image templateImg = templateGO.AddComponent<Image>();
        templateImg.color = new Color(0.15f, 0.15f, 0.2f, 1f);
        RectTransform templateRT = templateGO.GetComponent<RectTransform>();
        templateRT.anchorMin = new Vector2(0, 0);
        templateRT.anchorMax = new Vector2(1, 0);
        templateRT.pivot = new Vector2(0.5f, 1f);
        templateRT.sizeDelta = new Vector2(0, 150);

        ScrollRect scrollRect = templateGO.AddComponent<ScrollRect>();

        // Viewport
        GameObject viewportGO = CrearElementoUI("Viewport", templateGO.transform);
        Image viewportImg = viewportGO.AddComponent<Image>();
        viewportImg.color = Color.white;
        Mask mask = viewportGO.AddComponent<Mask>();
        mask.showMaskGraphic = false;
        RectTransform viewportRT = viewportGO.GetComponent<RectTransform>();
        viewportRT.anchorMin = Vector2.zero;
        viewportRT.anchorMax = Vector2.one;
        viewportRT.offsetMin = Vector2.zero;
        viewportRT.offsetMax = Vector2.zero;

        // Content
        GameObject contentGO = CrearElementoUI("Content", viewportGO.transform);
        RectTransform contentRT = contentGO.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.sizeDelta = new Vector2(0, 28);

        // Item
        GameObject itemGO = CrearElementoUI("Item", contentGO.transform);
        RectTransform itemRT = itemGO.GetComponent<RectTransform>();
        itemRT.anchorMin = new Vector2(0, 0.5f);
        itemRT.anchorMax = new Vector2(1, 0.5f);
        itemRT.sizeDelta = new Vector2(0, 28);
        Toggle itemToggle = itemGO.AddComponent<Toggle>();

        // Item Background
        GameObject itemBgGO = CrearElementoUI("Item Background", itemGO.transform);
        Image itemBgImg = itemBgGO.AddComponent<Image>();
        itemBgImg.color = COLOR_SLIDER_BG;
        EstirarRectTransform(itemBgGO.GetComponent<RectTransform>());

        // Item Checkmark
        GameObject itemCheckGO = CrearElementoUI("Item Checkmark", itemGO.transform);
        Image itemCheckImg = itemCheckGO.AddComponent<Image>();
        itemCheckImg.color = COLOR_BOTON;
        EstirarRectTransform(itemCheckGO.GetComponent<RectTransform>());

        // Item Label
        GameObject itemLabelGO = CrearElementoUI("Item Label", itemGO.transform);
        Text itemLabelTxt = itemLabelGO.AddComponent<Text>();
        itemLabelTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        itemLabelTxt.fontSize = 18;
        itemLabelTxt.color = COLOR_TEXTO;
        itemLabelTxt.alignment = TextAnchor.MiddleLeft;
        RectTransform itemLabelRT = itemLabelGO.GetComponent<RectTransform>();
        itemLabelRT.anchorMin = Vector2.zero;
        itemLabelRT.anchorMax = Vector2.one;
        itemLabelRT.offsetMin = new Vector2(10, 0);
        itemLabelRT.offsetMax = new Vector2(0, 0);

        // Configurar toggle
        itemToggle.targetGraphic = itemBgImg;
        itemToggle.graphic = itemCheckImg;
        itemToggle.isOn = true;

        // Configurar scroll
        scrollRect.content = contentRT;
        scrollRect.viewport = viewportRT;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        // Configurar dropdown
        dd.captionText = labelTxt;
        dd.itemText = itemLabelTxt;
        dd.template = templateRT;
        dd.targetGraphic = ddImg;

        templateGO.SetActive(false);

        return ddGO;
    }

    // ---------- Fila con Toggle ----------
    private void CrearFilaToggle(string nombre, Transform padre, string etiqueta, string nombreToggle)
    {
        GameObject fila = CrearElementoUI(nombre, padre);
        HorizontalLayoutGroup hlg = fila.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 15;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        LayoutElement leFila = fila.AddComponent<LayoutElement>();
        leFila.preferredHeight = 40;
        leFila.flexibleWidth = 1;

        // Label
        GameObject labelGO = CrearElementoUI("Label", fila.transform);
        Text label = labelGO.AddComponent<Text>();
        label.text = etiqueta;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 20;
        label.color = COLOR_TEXTO;
        label.alignment = TextAnchor.MiddleLeft;
        LayoutElement leLabel = labelGO.AddComponent<LayoutElement>();
        leLabel.preferredWidth = 250;
        leLabel.flexibleWidth = 0;

        // Toggle
        GameObject toggleGO = CrearElementoUI(nombreToggle, fila.transform);
        Toggle toggle = toggleGO.AddComponent<Toggle>();

        // Background del toggle
        GameObject bgGO = CrearElementoUI("Background", toggleGO.transform);
        Image bgImg = bgGO.AddComponent<Image>();
        bgImg.color = COLOR_SLIDER_BG;
        RectTransform bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0, 0.15f);
        bgRT.anchorMax = new Vector2(0, 0.85f);
        bgRT.sizeDelta = new Vector2(30, 0);
        bgRT.anchoredPosition = new Vector2(15, 0);

        // Checkmark
        GameObject checkGO = CrearElementoUI("Checkmark", bgGO.transform);
        Image checkImg = checkGO.AddComponent<Image>();
        checkImg.color = COLOR_BOTON;
        RectTransform checkRT = checkGO.GetComponent<RectTransform>();
        checkRT.anchorMin = new Vector2(0.1f, 0.1f);
        checkRT.anchorMax = new Vector2(0.9f, 0.9f);
        checkRT.offsetMin = Vector2.zero;
        checkRT.offsetMax = Vector2.zero;

        toggle.targetGraphic = bgImg;
        toggle.graphic = checkImg;
        toggle.isOn = true;

        LayoutElement leToggle = toggleGO.AddComponent<LayoutElement>();
        leToggle.flexibleWidth = 1;
        leToggle.preferredHeight = 35;
    }
}
#endif
