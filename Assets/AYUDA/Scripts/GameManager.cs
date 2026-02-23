using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Gestiona el estado del juego, Game Over y reinicio
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Referencias UI")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI gameOverText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button quitButton;

    [Header("Configuración")]
    [SerializeField] private float gameOverDelay = 2f;

    private bool isGameOver = false;

    void Awake()
    {
        // Singleton
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // ===== AUTO-BUSCAR REFERENCIAS SI NO ESTÁN ASIGNADAS =====
        // Esto ocurre cuando se convierten objetos de escena en prefab variants
        // separados y las referencias cruzadas entre prefabs se pierden.
        FindReferences();

        // Ocultar panel de Game Over al inicio
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        // Configurar botones
        if (restartButton != null)
            restartButton.onClick.AddListener(RestartGame);

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);

        // Asegurar que el tiempo esté corriendo
        Time.timeScale = 1f;
    }

    /// <summary>
    /// Auto-busca las referencias de UI si no están asignadas en el Inspector.
    /// Necesario cuando GameManager y PlayerUI son prefabs separados.
    /// </summary>
    private void FindReferences()
    {
        if (gameOverPanel == null)
        {
            // Buscar el GameOverPanel en cualquier Canvas de la escena
            Canvas[] canvases = FindObjectsOfType<Canvas>(true);
            foreach (Canvas canvas in canvases)
            {
                Transform panel = canvas.transform.Find("GameOverPanel");
                if (panel != null)
                {
                    gameOverPanel = panel.gameObject;
                    Debug.Log("[GameManager] GameOverPanel encontrado automáticamente.");
                    break;
                }
            }

            // Buscar recursivamente si no se encontró en el primer nivel
            if (gameOverPanel == null)
            {
                foreach (Canvas canvas in canvases)
                {
                    foreach (Transform child in canvas.GetComponentsInChildren<Transform>(true))
                    {
                        if (child.name == "GameOverPanel")
                        {
                            gameOverPanel = child.gameObject;
                            Debug.Log("[GameManager] GameOverPanel encontrado automáticamente (búsqueda recursiva).");
                            break;
                        }
                    }
                    if (gameOverPanel != null) break;
                }
            }
        }

        // Buscar botones y texto dentro del GameOverPanel
        if (gameOverPanel != null)
        {
            if (restartButton == null)
            {
                Transform restartT = gameOverPanel.transform.Find("RestartButton");
                if (restartT != null)
                {
                    restartButton = restartT.GetComponent<Button>();
                    Debug.Log("[GameManager] RestartButton encontrado automáticamente.");
                }
            }

            if (quitButton == null)
            {
                Transform quitT = gameOverPanel.transform.Find("QuitButton");
                if (quitT != null)
                {
                    quitButton = quitT.GetComponent<Button>();
                    Debug.Log("[GameManager] QuitButton encontrado automáticamente.");
                }
            }

            if (gameOverText == null)
            {
                gameOverText = gameOverPanel.GetComponentInChildren<TextMeshProUGUI>(true);
                if (gameOverText != null)
                    Debug.Log("[GameManager] GameOverText encontrado automáticamente.");
            }
        }
        else
        {
            Debug.LogWarning("[GameManager] No se encontró GameOverPanel en la escena. La pantalla de Game Over no funcionará.");
        }
    }

    /// <summary>
    /// Llamado cuando el jugador muere
    /// </summary>
    public void ShowGameOver()
    {
        if (isGameOver) return;

        isGameOver = true;
        Invoke(nameof(DisplayGameOverScreen), gameOverDelay);
    }

    private void DisplayGameOverScreen()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            
            // Pausar el juego
            Time.timeScale = 0f;
            
            // Desbloquear y mostrar cursor
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        Debug.Log("GAME OVER - Presiona R para reiniciar");
    }

    /// <summary>
    /// Reinicia la escena actual
    /// </summary>
    public void RestartGame()
    {
        Time.timeScale = 1f;
        isGameOver = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// Cierra el juego
    /// </summary>
    public void QuitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    void Update()
    {
        // Atajo de teclado para reiniciar
        if (isGameOver && Input.GetKeyDown(KeyCode.R))
        {
            RestartGame();
        }
    }
}
