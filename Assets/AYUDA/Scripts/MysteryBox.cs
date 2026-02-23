using System.Collections;
using UnityEngine;

/// <summary>
/// Caja Misteriosa estilo CoD Zombies.
/// Los modelos de armas ciclan girando sobre la caja, se frenan en uno aleatorio
/// y el jugador lo recoge pulsando E.
///
/// SETUP EN UNITY:
///   1. Crear un GameObject "MysteryBox" en la escena.
///   2. Crear un hijo vacío "DisplayPoint" encima del modelo de la caja
///      (donde aparecerán las armas girando).
///   3. Añadir este script al GameObject "MysteryBox".
///   4. En Weapon Pool: añadir cada arma con su Display Prefab (modelo visual)
///      y su Weapon Prefab (el prefab real con el script WeaponBase).
///   5. Asignar Display Point, Weapon Holder del jugador y Weapon Manager.
/// </summary>
public class MysteryBox : MonoBehaviour
{
    // =========================================================================
    // ENTRADA DE ARMA EN EL POOL
    // =========================================================================

    [System.Serializable]
    public class WeaponEntry
    {
        [Tooltip("Modelo 3D que aparece girando sobre la caja (solo visual, puede ser el mismo prefab del arma)")]
        public GameObject displayPrefab;

        [Tooltip("Prefab del arma real con WeaponBase que recibirá el jugador")]
        public WeaponBase weaponPrefab;
    }

    // =========================================================================
    // INSPECTOR
    // =========================================================================

    [Header("Pool de Armas")]
    [Tooltip("Lista de armas disponibles en la caja")]
    [SerializeField] private WeaponEntry[] weaponPool;

    [Header("Referencias")]
    [Tooltip("Punto donde aparece el arma girando (hijo vacío encima de la caja)")]
    [SerializeField] private Transform displayPoint;

    [Tooltip("El WeaponHolder del jugador (padre de todas las armas)")]
    [SerializeField] private Transform weaponHolder;

    [Tooltip("WeaponManager del jugador")]
    [SerializeField] private WeaponManager weaponManager;

    [Tooltip("PlayerEconomy (se busca automáticamente si no se asigna)")]
    [SerializeField] private PlayerEconomy playerEconomy;

    [Header("Configuración")]
    [Tooltip("Coste en puntos para usar la caja")]
    [SerializeField] private int cost = 950;

    [Tooltip("Distancia máxima para interactuar")]
    [SerializeField] private float interactionDistance = 2.5f;

    [Tooltip("Tecla de interacción")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    [Header("Ciclo de Animación")]
    [Tooltip("Intervalo inicial entre cambios de arma (rápido)")]
    [SerializeField] private float cycleStartInterval = 0.07f;

    [Tooltip("Intervalo final entre cambios de arma (lento)")]
    [SerializeField] private float cycleEndInterval = 0.55f;

    [Tooltip("Duración total del ciclo rápido en segundos")]
    [SerializeField] private float cycleDuration = 3.5f;

    [Tooltip("Segundos que se muestra el arma final antes de que 'escape'")]
    [SerializeField] private float resultDisplayTime = 4f;

    [Header("Visual del Modelo")]
    [Tooltip("Velocidad de rotación del modelo sobre la caja (grados/seg)")]
    [SerializeField] private float modelRotateSpeed = 120f;

    [Tooltip("Altura del modelo sobre el displayPoint")]
    [SerializeField] private float modelHoverHeight = 0.3f;

    [Tooltip("Amplitud del efecto bobbing arriba/abajo")]
    [SerializeField] private float modelBobAmount = 0.08f;

    [Tooltip("Velocidad del efecto bobbing")]
    [SerializeField] private float modelBobSpeed = 2f;

    [Header("Efectos de Luz (opcional)")]
    [Tooltip("Luz puntual de la caja (se activa al usarla)")]
    [SerializeField] private Light boxLight;

    [Tooltip("Color de la luz cuando está ciclando")]
    [SerializeField] private Color cyclingLightColor = new Color(0.5f, 0f, 1f);

    [Tooltip("Color de la luz cuando muestra el resultado")]
    [SerializeField] private Color resultLightColor = Color.yellow;

    [Header("Sonidos")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip cycleTickSound;
    [SerializeField] private AudioClip resultSound;
    [SerializeField] private AudioClip pickupSound;
    [SerializeField] private AudioClip expiredSound;

    // =========================================================================
    // ESTADO INTERNO
    // =========================================================================

    private enum BoxState { Idle, Cycling, ShowingResult }
    private BoxState state = BoxState.Idle;

    private GameObject currentDisplayModel;
    private WeaponEntry selectedEntry;
    private Transform player;
    private float bobTimer;

    // Sistema de aleatorio mejorado (shuffle bag + historial)
    private int[] shuffledIndices;
    private int shufflePosition;
    private int lastFinalResultIndex = -1;
    private int secondLastFinalResultIndex = -1;

    // =========================================================================
    // INICIALIZACIÓN
    // =========================================================================

    private void Start()
    {
        // Buscar jugador por tag
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        // Auto-buscar referencias si no asignadas
        if (weaponManager == null)
            weaponManager = FindObjectOfType<WeaponManager>();

        // ===== AUTO-BUSCAR weaponHolder =====
        // Necesario cuando MysteryBox y Player son prefabs separados
        // y la referencia cruzada se rompió al crear los prefab variants.
        if (weaponHolder == null && weaponManager != null)
        {
            // Buscar "WeaponHolder" en los hijos del WeaponManager
            weaponHolder = weaponManager.transform.parent;
            // El WeaponHolder normalmente es el padre del WeaponManager en la jerarquía,
            // pero también podría estar como hijo del jugador
            Transform holder = weaponManager.transform;
            // Subir hasta encontrar un objeto llamado "WeaponHolder"
            while (holder != null)
            {
                if (holder.name == "WeaponHolder")
                {
                    weaponHolder = holder;
                    Debug.Log("[MYSTERY BOX] WeaponHolder encontrado automáticamente (vía WeaponManager).");
                    break;
                }
                holder = holder.parent;
            }

            // Si no se encontró subiendo, buscar en hijos del jugador
            if (weaponHolder == null && player != null)
            {
                weaponHolder = FindChildRecursive(player, "WeaponHolder");
                if (weaponHolder != null)
                    Debug.Log("[MYSTERY BOX] WeaponHolder encontrado automáticamente (vía Player).");
            }

            // Fallback: buscar en toda la escena
            if (weaponHolder == null)
            {
                GameObject holderObj = GameObject.Find("WeaponHolder");
                if (holderObj != null)
                {
                    weaponHolder = holderObj.transform;
                    Debug.Log("[MYSTERY BOX] WeaponHolder encontrado automáticamente (búsqueda global).");
                }
            }

            if (weaponHolder == null)
                Debug.LogWarning("[MYSTERY BOX] No se encontró WeaponHolder. Las armas recogidas no se verán.");
        }

        if (playerEconomy == null && PlayerEconomy.Instance != null)
            playerEconomy = PlayerEconomy.Instance;

        // Auto-buscar o crear AudioSource si no está asignado
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
        }

        // Apagar luz por defecto
        if (boxLight != null) boxLight.enabled = false;

        // Inicializar shuffle bag
        InitShuffleBag();
    }

    /// <summary>
    /// Busca recursivamente un hijo por nombre en la jerarquía.
    /// </summary>
    private Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;
            Transform found = FindChildRecursive(child, name);
            if (found != null)
                return found;
        }
        return null;
    }

    // =========================================================================
    // UPDATE — INTERACCIÓN Y ROTACIÓN DEL MODELO
    // =========================================================================

    private void Update()
    {
        HandleInteractionPrompt();
        HandleModelAnimation();
    }

    private void HandleInteractionPrompt()
    {
        if (player == null) return;

        float dist = Vector3.Distance(player.position, transform.position);
        bool inRange = dist <= interactionDistance;

        if (state == BoxState.Idle)
        {
            if (inRange)
            {
                bool canAfford = playerEconomy != null && playerEconomy.CanAfford(cost);
                string prompt = canAfford
                    ? $"[{interactKey}] Caja Misteriosa — {cost} pts"
                    : $"Caja Misteriosa — {cost} pts (sin fondos)";
                InteractionUI.Show(prompt);

                if (canAfford && Input.GetKeyDown(interactKey))
                    StartBox();
            }
            else
            {
                InteractionUI.Hide();
            }
        }
        else if (state == BoxState.Cycling)
        {
            // Mientras cicla, mostrar que está en uso pero NO permitir interacción
            if (inRange)
                InteractionUI.Show("Abriendo caja...");
            else
                InteractionUI.Hide();
        }
        else if (state == BoxState.ShowingResult)
        {
            if (inRange)
            {
                InteractionUI.Show($"[{interactKey}] ¡Coger arma!");

                if (Input.GetKeyDown(interactKey))
                    GiveWeaponToPlayer();
            }
            else
            {
                InteractionUI.Show("¡Acércate para coger el arma!");
            }
        }
        else
        {
            // Ciclando — no mostrar prompt de compra
            InteractionUI.Hide();
        }
    }

    private void HandleModelAnimation()
    {
        if (currentDisplayModel == null || displayPoint == null) return;

        // Rotación continua
        currentDisplayModel.transform.Rotate(Vector3.up, modelRotateSpeed * Time.deltaTime, Space.World);

        // Efecto bobbing vertical
        bobTimer += Time.deltaTime * modelBobSpeed;
        float bobOffset = Mathf.Sin(bobTimer) * modelBobAmount;
        currentDisplayModel.transform.position = displayPoint.position +
            Vector3.up * (modelHoverHeight + bobOffset);
    }

    // =========================================================================
    // LÓGICA DE LA CAJA
    // =========================================================================

    private void StartBox()
    {
        if (weaponPool == null || weaponPool.Length == 0)
        {
            Debug.LogWarning("[MYSTERY BOX] El weaponPool está vacío. Añade armas en el Inspector.");
            return;
        }

        playerEconomy.SpendCoins(cost);
        PlaySound(openSound);

        if (boxLight != null)
        {
            boxLight.enabled = true;
            boxLight.color = cyclingLightColor;
        }

        state = BoxState.Cycling;
        StartCoroutine(CycleRoutine());
    }

    private IEnumerator CycleRoutine()
    {
        float elapsed = 0f;
        int lastIndex = -1;

        // ── FASE 1: ciclo rápido que va frenando gradualmente ──
        while (elapsed < cycleDuration)
        {
            float t = elapsed / cycleDuration;
            // La primera mitad permanece rápido, la segunda mitad frena
            float interval = t < 0.5f
                ? cycleStartInterval
                : Mathf.Lerp(cycleStartInterval, cycleEndInterval * 0.6f, (t - 0.5f) * 2f);

            int index = PickRandomIndex(lastIndex);
            lastIndex = index;

            ShowDisplayModel(weaponPool[index].displayPrefab);
            PlaySound(cycleTickSound);

            yield return new WaitForSeconds(interval);
            elapsed += interval;
        }

        // ── FASE 2: frenado dramático final (4 pasos cada vez más lentos) ──
        int slowSteps = 4;
        for (int i = 0; i < slowSteps; i++)
        {
            float slowT = (float)i / (slowSteps - 1);
            float slowInterval = Mathf.Lerp(cycleEndInterval * 0.6f, cycleEndInterval, slowT);

            int index = PickRandomIndex(lastIndex);
            lastIndex = index;

            ShowDisplayModel(weaponPool[index].displayPrefab);
            PlaySound(cycleTickSound);
            yield return new WaitForSeconds(slowInterval);
        }

        // ── FASE 3: resultado final (con protección anti-repetición) ──
        int finalIndex = PickFinalResultIndex(lastIndex);
        selectedEntry = weaponPool[finalIndex];
        ShowDisplayModel(selectedEntry.displayPrefab);
        PlaySound(resultSound);
        state = BoxState.ShowingResult;

        if (boxLight != null)
            boxLight.color = resultLightColor;

        // Esperar a que el jugador recoja o se acabe el tiempo
        float resultElapsed = 0f;
        while (resultElapsed < resultDisplayTime)
        {
            resultElapsed += Time.deltaTime;
            yield return null;
        }

        // Tiempo agotado — el arma "escapa"
        WeaponEscaped();
    }

    private void GiveWeaponToPlayer()
    {
        if (selectedEntry?.weaponPrefab == null)
        {
            ResetBox();
            return;
        }

        // Comprobar si el jugador ya tiene esta arma (comparando WeaponData)
        WeaponData selectedData = selectedEntry.weaponPrefab.GetWeaponData();
        int existingSlot = FindExistingWeaponSlot(selectedData);

        if (existingSlot >= 0)
        {
            // Ya tiene el arma — solo rellenar munición
            WeaponBase existingWeapon = weaponManager.GetWeapon(existingSlot);
            if (existingWeapon != null)
                existingWeapon.RefillAmmo();

            PlaySound(pickupSound);
            Debug.Log($"[MYSTERY BOX] El jugador ya tiene '{selectedEntry.weaponPrefab.name}' → munición recargada");
            ResetBox();
            return;
        }

        // No tiene el arma — darla normalmente
        int targetSlot = FindTargetSlot();

        // Destruir arma antigua en ese slot si existe
        WeaponBase oldWeapon = weaponManager.GetWeapon(targetSlot);
        if (oldWeapon != null)
            Destroy(oldWeapon.gameObject);

        // Instanciar nueva arma bajo el weaponHolder del jugador.
        // No sobreescribimos localPosition/localRotation: cada prefab tiene
        // guardada su propia posición en mano correcta.
        WeaponBase newWeapon = Instantiate(selectedEntry.weaponPrefab, weaponHolder);
        newWeapon.gameObject.SetActive(false); // Desactivar hasta que se equipe

        // Asignar al slot en WeaponManager
        weaponManager.SetWeaponInSlot(targetSlot, newWeapon);

        // Cambiar directamente al arma nueva
        weaponManager.SwitchToWeapon(targetSlot);

        PlaySound(pickupSound);
        Debug.Log($"[MYSTERY BOX] Arma obtenida: {selectedEntry.weaponPrefab.name} → slot {targetSlot}");

        ResetBox();
    }

    /// <summary>
    /// Busca si el jugador ya tiene un arma con el mismo WeaponData.
    /// Devuelve el índice del slot, o -1 si no la tiene.
    /// </summary>
    private int FindExistingWeaponSlot(WeaponData data)
    {
        if (data == null) return -1;

        int slots = weaponManager.GetActiveSlotCount();
        for (int i = 0; i < slots; i++)
        {
            WeaponBase w = weaponManager.GetWeapon(i);
            if (w != null && w.GetWeaponData() == data)
                return i;
        }
        return -1;
    }

    private void WeaponEscaped()
    {
        PlaySound(expiredSound);
        Debug.Log("[MYSTERY BOX] El arma se fue...");
        ResetBox();
    }

    // =========================================================================
    // HELPERS
    // =========================================================================

    /// <summary>
    /// Busca el slot destino: primero slot vacío, si no hay reemplaza el actual.
    /// </summary>
    private int FindTargetSlot()
    {
        int activeSlots = weaponManager.GetActiveSlotCount();

        // Buscar slot vacío (sin arma)
        for (int i = 0; i < activeSlots; i++)
        {
            if (weaponManager.GetWeapon(i) == null)
                return i;
        }

        // No hay hueco — reemplazar el arma actual
        return weaponManager.GetCurrentWeaponIndex();
    }

    /// <summary>
    /// Inicializa la shuffle bag (baraja) con todos los índices del pool.
    /// </summary>
    private void InitShuffleBag()
    {
        if (weaponPool == null || weaponPool.Length == 0) return;
        shuffledIndices = new int[weaponPool.Length];
        for (int i = 0; i < weaponPool.Length; i++)
            shuffledIndices[i] = i;
        ShuffleArray(shuffledIndices);
        shufflePosition = 0;
    }

    /// <summary>
    /// Fisher-Yates shuffle usando System.Random con seed basado en tiempo
    /// para mejor distribución que UnityEngine.Random.
    /// </summary>
    private void ShuffleArray(int[] array)
    {
        // Usar System.Random con seed de alta entropía (ticks + hash del frame)
        int seed = System.Environment.TickCount ^ (Time.frameCount * 397);
        System.Random rng = new System.Random(seed);

        for (int i = array.Length - 1; i > 0; i--)
        {
            int j = rng.Next(0, i + 1);
            int temp = array[i];
            array[i] = array[j];
            array[j] = temp;
        }
    }

    /// <summary>
    /// Obtiene el siguiente índice de la shuffle bag para la animación de ciclo.
    /// Garantiza que todas las armas se muestran antes de repetir.
    /// Evita que salga el mismo que el último mostrado.
    /// </summary>
    private int PickRandomIndex(int lastIndex)
    {
        if (weaponPool.Length == 1) return 0;

        int index;
        int attempts = 0;
        do
        {
            if (shufflePosition >= shuffledIndices.Length)
            {
                ShuffleArray(shuffledIndices);
                shufflePosition = 0;
            }
            index = shuffledIndices[shufflePosition];
            shufflePosition++;
            attempts++;
            // Seguridad: si tras recorrer toda la baraja no encuentra uno diferente, salir
            if (attempts > shuffledIndices.Length * 2) break;
        }
        while (index == lastIndex);

        return index;
    }

    /// <summary>
    /// Elige el resultado FINAL de la caja con protección anti-repetición.
    /// Evita que salga la misma arma que las últimas 2 tiradas.
    /// </summary>
    private int PickFinalResultIndex(int lastDisplayIndex)
    {
        if (weaponPool.Length <= 2)
        {
            // Con 1-2 armas no podemos evitar mucha repetición
            return PickRandomIndex(lastDisplayIndex);
        }

        // Usar System.Random para el resultado final (más impredecible)
        int seed = System.Environment.TickCount ^ (Time.frameCount * 1031) ^ (int)(Time.realtimeSinceStartup * 100000f);
        System.Random rng = new System.Random(seed);

        int index;
        int attempts = 0;
        do
        {
            index = rng.Next(0, weaponPool.Length);
            attempts++;
            if (attempts > 50) break; // Seguridad
        }
        while (index == lastDisplayIndex || index == lastFinalResultIndex || index == secondLastFinalResultIndex);

        // Actualizar historial
        secondLastFinalResultIndex = lastFinalResultIndex;
        lastFinalResultIndex = index;

        return index;
    }

    /// <summary>
    /// Instancia el displayPrefab en el displayPoint con todos los scripts desactivados.
    /// </summary>
    private void ShowDisplayModel(GameObject prefab)
    {
        // Destruir modelo anterior
        if (currentDisplayModel != null)
            Destroy(currentDisplayModel);

        if (prefab == null || displayPoint == null) return;

        currentDisplayModel = Instantiate(prefab, displayPoint.position, Quaternion.identity);

        // Desactivar scripts de arma (no queremos que disparen ni inicialicen)
        foreach (var mb in currentDisplayModel.GetComponentsInChildren<MonoBehaviour>())
        {
            if (mb is WeaponBase || mb is WeaponManager)
                mb.enabled = false;
        }

        // Quitar colisores y físicas para que no interfiera
        foreach (var col in currentDisplayModel.GetComponentsInChildren<Collider>())
            col.enabled = false;

        foreach (var rb in currentDisplayModel.GetComponentsInChildren<Rigidbody>())
            rb.isKinematic = true;

        bobTimer = 0f;
    }

    private void ResetBox()
    {
        state = BoxState.Idle;
        selectedEntry = null;

        if (currentDisplayModel != null)
        {
            Destroy(currentDisplayModel);
            currentDisplayModel = null;
        }

        if (boxLight != null)
            boxLight.enabled = false;

        InteractionUI.Hide();
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }

    private void OnDestroy()
    {
        if (currentDisplayModel != null)
            Destroy(currentDisplayModel);
    }

    // =========================================================================
    // GIZMOS
    // =========================================================================

    private void OnDrawGizmosSelected()
    {
        // Radio de interacción
        Gizmos.color = new Color(1f, 0f, 1f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, interactionDistance);

        // Punto de display
        if (displayPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(displayPoint.position, 0.2f);
            Gizmos.DrawLine(transform.position, displayPoint.position);
        }
    }
}
