using UnityEngine;

/// <summary>
/// Muestra prompts de interacción en pantalla usando OnGUI.
/// No depende de Canvas, TMP, ni ningún otro sistema de UI.
/// Se puede poner en CUALQUIER GameObject (Player, Canvas, vacío, etc.)
///
/// Uso externo:  InteractionUI.Show("texto");  InteractionUI.Hide();
/// </summary>
public class InteractionUI : MonoBehaviour
{
    // ===== API ESTÁTICA — cualquier script puede llamar esto =====
    private static InteractionUI _instance;

    /// <summary>Muestra un prompt de interacción. Llamable desde cualquier script.</summary>
    public static void Show(string message)
    {
        if (_instance != null)
        {
            _instance._pendingMessage = message;
            _instance._showRequestedThisFrame = true;
        }
    }

    /// <summary>
    /// Oculta el prompt. Ya no fuerza el ocultado inmediato:
    /// si otro script llamó Show() este frame, ese mensaje tiene prioridad.
    /// </summary>
    public static void Hide()
    {
        // No hace nada — el auto-ocultado lo gestiona LateUpdate.
        // Así evitamos que múltiples máquinas se sobreescriban entre sí.
    }

    // ===== CONFIG =====
    [Header("Detección de Medkits")]
    [SerializeField] private float checkDistance = 3f;

    [Header("Estilo Visual")]
    [SerializeField] private int fontSize = 22;
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.75f);
    [SerializeField] private Color borderColor = new Color(1f, 1f, 1f, 0.3f);

    // Estado
    private bool isShowing = false;
    private string currentMessage = "";
    private bool externalPromptActive = false;
    private Transform playerTransform;
    private bool initialized = false;

    // Sistema de prioridad por frame — evita que múltiples máquinas se sobreescriban
    private bool _showRequestedThisFrame = false;
    private string _pendingMessage = "";

    // Texturas
    private Texture2D bgTexture;
    private Texture2D borderTexture;
    private GUIStyle textStyle;
    private bool stylesReady = false;

    // =========================================================================
    // INICIALIZACIÓN — todo en Awake, sin depender de Start
    // =========================================================================

    void Awake()
    {
        // Si ya hay instancia, desactivar este componente (no destruir el GO)
        if (_instance != null && _instance != this)
        {
            enabled = false;
            return;
        }
        _instance = this;

        // Crear texturas inmediatamente
        bgTexture = new Texture2D(1, 1);
        bgTexture.SetPixel(0, 0, backgroundColor);
        bgTexture.Apply();

        borderTexture = new Texture2D(1, 1);
        borderTexture.SetPixel(0, 0, borderColor);
        borderTexture.Apply();

        initialized = true;
    }

    void Start()
    {
        FindPlayer();
        Debug.Log($"[InteractionUI] OK — Player: {(playerTransform != null ? playerTransform.name : "buscando...")}");
    }

    void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    private void FindPlayer()
    {
        // Por tag
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            return;
        }

        // Por componente
        var pm = FindObjectOfType<PlayerMovement>();
        if (pm != null) { playerTransform = pm.transform; return; }

        var cc = FindObjectOfType<CharacterController>();
        if (cc != null) { playerTransform = cc.transform; return; }
    }

    // =========================================================================
    // UPDATE — detección automática de medkits
    // =========================================================================

    void Update()
    {
        // Si un script externo solicitó Show() este frame, tiene prioridad total
        if (_showRequestedThisFrame) return;

        // Buscar player si no lo tenemos aún
        if (playerTransform == null)
        {
            FindPlayer();
            if (playerTransform == null) return;
        }

        // Buscar medkits cercanos
        MedkitPickup[] medkits = FindObjectsOfType<MedkitPickup>();
        MedkitPickup closest = null;
        float closestDist = checkDistance;

        for (int i = 0; i < medkits.Length; i++)
        {
            if (medkits[i] == null) continue;
            if (!medkits[i].CanPickup()) continue;

            float dist = Vector3.Distance(playerTransform.position, medkits[i].transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = medkits[i];
            }
        }

        if (closest != null)
        {
            _pendingMessage = "Presiona [E] para recoger Botiquín";
            _showRequestedThisFrame = true;
        }
    }

    /// <summary>
    /// LateUpdate — se ejecuta DESPUÉS de todos los Update().
    /// Aplica el resultado final del frame: mostrar o esconder.
    /// </summary>
    void LateUpdate()
    {
        if (_showRequestedThisFrame)
        {
            isShowing = true;
            currentMessage = _pendingMessage;
            externalPromptActive = true;
        }
        else
        {
            isShowing = false;
            currentMessage = "";
            externalPromptActive = false;
        }

        // Resetear para el siguiente frame
        _showRequestedThisFrame = false;
        _pendingMessage = "";
    }

    // =========================================================================
    // RENDERIZADO — OnGUI (no depende de Canvas)
    // =========================================================================

    void OnGUI()
    {
        if (!initialized || !isShowing || string.IsNullOrEmpty(currentMessage)) return;

        // Inicializar estilos (solo se puede hacer dentro de OnGUI)
        if (!stylesReady)
        {
            textStyle = new GUIStyle(GUI.skin.label);
            textStyle.fontSize = fontSize;
            textStyle.fontStyle = FontStyle.Bold;
            textStyle.alignment = TextAnchor.MiddleCenter;
            textStyle.normal.textColor = textColor;
            textStyle.wordWrap = false;
            stylesReady = true;
        }

        // Calcular tamaño
        GUIContent content = new GUIContent(currentMessage);
        Vector2 textSize = textStyle.CalcSize(content);

        float pad = 24f;
        float w = Mathf.Max(textSize.x + pad * 2f, 300f);
        float h = textSize.y + pad;

        float x = (Screen.width - w) * 0.5f;
        float y = Screen.height - h - 80f;

        // Borde
        float b = 2f;
        GUI.DrawTexture(new Rect(x - b, y - b, w + b * 2f, h + b * 2f), borderTexture);

        // Fondo
        Rect bg = new Rect(x, y, w, h);
        GUI.DrawTexture(bg, bgTexture);

        // Texto
        GUI.Label(bg, currentMessage, textStyle);
    }

    // =========================================================================
    // API PÚBLICA (para compatibilidad con UIManager)
    // =========================================================================

    public void ShowInteractionPrompt(string message)
    {
        _pendingMessage = message;
        _showRequestedThisFrame = true;
    }

    public void HideInteractionPrompt()
    {
        // No hace nada — el LateUpdate decide al final del frame
    }
}
