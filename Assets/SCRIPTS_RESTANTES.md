# 🚀 SETUP RÁPIDO - SCRIPTS RESTANTES ESENCIALES

## 📝 SCRIPTS YA IMPLEMENTADOS (100% Funcionales)

### ✅ CORE SYSTEMS
- GameManager.cs
- AudioManager.cs
- ObjectPool.cs

### ✅ WEAPONS (Combate Completo)
- WeaponConfig.cs
- WeaponBase.cs
- AssaultRifle.cs
- Pistol.cs
- SniperRifle.cs
- MeleeWeapon.cs
- Grenade.cs
- GrenadeController.cs

### ✅ PLAYER
- PlayerController.cs
- PlayerHealth.cs
- WeaponController.cs

### ✅ ENEMIES
- ZombieConfig.cs
- ZombieHealth.cs
- ZombieAI.cs

---

## ⏳ SCRIPTS RESTANTES A IMPLEMENTAR

### 1. WaveManager.cs - Sistema de Oleadas

```csharp
using UnityEngine;
using ZombieFPS.Core;
using ZombieFPS.Enemies;
using System.Collections;
using System.Collections.Generic;

namespace ZombieFPS.Waves
{
    public class WaveManager : MonoBehaviour
    {
        [Header("Wave Settings")]
        [SerializeField] private int currentWave = 0;
        [SerializeField] private float timeBetweenWaves = 20f;
        [SerializeField] private Transform[] spawnPoints;

        [Header("Zombie Configs")]
        [SerializeField] private ZombieConfig normalZombieConfig;
        [SerializeField] private ZombieConfig fastZombieConfig;
        [SerializeField] private ZombieConfig tankZombieConfig;

        [Header("Prefabs")]
        [SerializeField] private GameObject zombieNormalPrefab;
        [SerializeField] private GameObject zombieFastPrefab;
        [SerializeField] private GameObject zombieTankPrefab;

        private List<GameObject> activeZombies = new List<GameObject>();
        private bool waveActive = false;
        private Transform player;

        private void Start()
        {
            player = GameObject.FindGameObjectWithTag("Player").transform;
            StartCoroutine(WaveRoutine());
        }

        private IEnumerator WaveRoutine()
        {
            yield return new WaitForSeconds(3f); // Delay inicial

            while (true)
            {
                currentWave++;
                GameManager.Instance.AdvanceWave();
                
                StartWave(currentWave);
                
                // Esperar hasta que mueran todos los zombies
                yield return new WaitUntil(() => activeZombies.Count == 0);
                
                waveActive = false;
                
                // Tiempo entre oleadas (para comprar en tienda)
                GameManager.Instance.ChangeGameState(GameState.Shopping);
                yield return new WaitForSeconds(timeBetweenWaves);
                GameManager.Instance.ChangeGameState(GameState.Playing);
            }
        }

        private void StartWave(int wave)
        {
            waveActive = true;
            
            // Calcular cantidad y tipos de zombies
            int normalCount = 5 + (wave * 2);
            int fastCount = wave >= 3 ? wave - 2 : 0;
            int tankCount = wave >= 5 ? (wave - 4) / 2 : 0;

            Debug.Log($"[WaveManager] Wave {wave} - Normal: {normalCount}, Fast: {fastCount}, Tank: {tankCount}");

            // Spawn zombies
            for (int i = 0; i < normalCount; i++)
                SpawnZombie(zombieNormalPrefab, normalZombieConfig, wave);
            
            for (int i = 0; i < fastCount; i++)
                SpawnZombie(zombieFastPrefab, fastZombieConfig, wave);
            
            for (int i = 0; i < tankCount; i++)
                SpawnZombie(zombieTankPrefab, tankZombieConfig, wave);
        }

        private void SpawnZombie(GameObject prefab, ZombieConfig config, int wave)
        {
            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            GameObject zombie = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
            
            ZombieAI ai = zombie.GetComponent<ZombieAI>();
            ZombieHealth health = zombie.GetComponent<ZombieHealth>();
            
            if (ai != null && health != null)
            {
                ai.Initialize(config, player);
                health.Initialize(config, wave);
                
                health.OnDeath += (wasHeadshot) => OnZombieDied(zombie);
            }
            
            activeZombies.Add(zombie);
        }

        private void OnZombieDied(GameObject zombie)
        {
            activeZombies.Remove(zombie);
        }
    }
}
```

---

### 2. HUDManager.cs - Interfaz Principal

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ZombieFPS.Core;
using ZombieFPS.Player;

namespace ZombieFPS.UI
{
    public class HUDManager : MonoBehaviour
    {
        [Header("Health")]
        [SerializeField] private Image healthBar;
        [SerializeField] private TextMeshProUGUI healthText;

        [Header("Ammo")]
        [SerializeField] private TextMeshProUGUI ammoText;
        [SerializeField] private TextMeshProUGUI weaponNameText;

        [Header("Grenades")]
        [SerializeField] private TextMeshProUGUI grenadeText;

        [Header("Money & Score")]
        [SerializeField] private TextMeshProUGUI moneyText;
        [SerializeField] private TextMeshProUGUI scoreText;

        [Header("Wave")]
        [SerializeField] private TextMeshProUGUI waveText;
        [SerializeField] private TextMeshProUGUI enemiesText;

        [Header("Crosshair")]
        [SerializeField] private Image crosshair;
        [SerializeField] private Image hitMarker;

        private PlayerHealth playerHealth;
        private WeaponController weaponController;

        private void Start()
        {
            // Encontrar componentes del jugador
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerHealth = player.GetComponent<PlayerHealth>();
                weaponController = player.GetComponent<WeaponController>();

                // Suscribirse a eventos
                if (playerHealth != null)
                    playerHealth.OnHealthChanged += UpdateHealth;

                if (weaponController != null)
                    weaponController.OnWeaponAmmoChanged += UpdateAmmo;
            }

            // Suscribirse a eventos del GameManager
            GameManager.OnMoneyChanged += UpdateMoney;
            GameManager.OnScoreChanged += UpdateScore;
            GameManager.OnWaveChanged += UpdateWave;

            // Inicializar UI
            if (hitMarker != null)
                hitMarker.enabled = false;
        }

        private void UpdateHealth(float current, float max)
        {
            if (healthBar != null)
                healthBar.fillAmount = current / max;

            if (healthText != null)
                healthText.text = $"{current:F0}/{max:F0}";
        }

        private void UpdateAmmo(int current, int reserve)
        {
            if (ammoText != null)
                ammoText.text = $"{current} / {reserve}";
        }

        private void UpdateMoney(int money)
        {
            if (moneyText != null)
                moneyText.text = $"${money}";
        }

        private void UpdateScore(int score)
        {
            if (scoreText != null)
                scoreText.text = $"Score: {score}";
        }

        private void UpdateWave(int wave)
        {
            if (waveText != null)
                waveText.text = $"Wave {wave}";
        }

        public void ShowHitMarker(bool isHeadshot)
        {
            if (hitMarker != null)
            {
                StopAllCoroutines();
                StartCoroutine(HitMarkerRoutine(isHeadshot));
            }
        }

        private System.Collections.IEnumerator HitMarkerRoutine(bool isHeadshot)
        {
            hitMarker.enabled = true;
            hitMarker.color = isHeadshot ? Color.red : Color.white;
            
            yield return new WaitForSeconds(0.1f);
            
            hitMarker.enabled = false;
        }

        private void OnDestroy()
        {
            GameManager.OnMoneyChanged -= UpdateMoney;
            GameManager.OnScoreChanged -= UpdateScore;
            GameManager.OnWaveChanged -= UpdateWave;
        }
    }
}
```

---

### 3. ShopSystem.cs - Sistema de Tienda

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ZombieFPS.Core;
using ZombieFPS.Player;

namespace ZombieFPS.Economy
{
    public class ShopSystem : MonoBehaviour
    {
        [Header("Shop UI")]
        [SerializeField] private GameObject shopPanel;
        [SerializeField] private Button closeButton;

        [Header("Items")]
        [SerializeField] private Button buyAmmoButton;
        [SerializeField] private TextMeshProUGUI ammoPriceText;
        [SerializeField] private int ammoPrice = 50;

        [SerializeField] private Button buyGrenadeButton;
        [SerializeField] private TextMeshProUGUI grenadePriceText;
        [SerializeField] private int grenadePrice = 100;

        [SerializeField] private Button buyHealthButton;
        [SerializeField] private TextMeshProUGUI healthPriceText;
        [SerializeField] private int healthPrice = 150;

        private WeaponController weaponController;
        private PlayerHealth playerHealth;
        private GrenadeController grenadeController;

        private void Start()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                weaponController = player.GetComponent<WeaponController>();
                playerHealth = player.GetComponent<PlayerHealth>();
                grenadeController = player.GetComponent<GrenadeController>();
            }

            // Configurar botones
            if (buyAmmoButton != null)
                buyAmmoButton.onClick.AddListener(BuyAmmo);

            if (buyGrenadeButton != null)
                buyGrenadeButton.onClick.AddListener(BuyGrenade);

            if (buyHealthButton != null)
                buyHealthButton.onClick.AddListener(BuyHealth);

            if (closeButton != null)
                closeButton.onClick.AddListener(CloseShop);

            // Configurar precios
            if (ammoPriceText != null)
                ammoPriceText.text = $"${ammoPrice}";

            if (grenadePriceText != null)
                grenadePriceText.text = $"${grenadePrice}";

            if (healthPriceText != null)
                healthPriceText.text = $"${healthPrice}";

            // Suscribirse a eventos
            GameManager.OnGameStateChanged += OnGameStateChanged;

            shopPanel.SetActive(false);
        }

        private void OnGameStateChanged(GameState newState)
        {
            shopPanel.SetActive(newState == GameState.Shopping);
        }

        private void BuyAmmo()
        {
            if (GameManager.Instance.TrySpendMoney(ammoPrice))
            {
                if (weaponController != null)
                {
                    weaponController.AddAmmoToCurrentWeapon(30);
                    Debug.Log("[ShopSystem] Ammo purchased!");
                }
            }
        }

        private void BuyGrenade()
        {
            if (GameManager.Instance.TrySpendMoney(grenadePrice))
            {
                if (grenadeController != null)
                {
                    grenadeController.AddGrenades(1);
                    Debug.Log("[ShopSystem] Grenade purchased!");
                }
            }
        }

        private void BuyHealth()
        {
            if (GameManager.Instance.TrySpendMoney(healthPrice))
            {
                if (playerHealth != null)
                {
                    playerHealth.Heal(50f);
                    Debug.Log("[ShopSystem] Health purchased!");
                }
            }
        }

        private void CloseShop()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.CloseShop();
            }
        }

        private void OnDestroy()
        {
            GameManager.OnGameStateChanged -= OnGameStateChanged;
        }
    }
}
```

---

### 4. CameraShake.cs - Screen Shake

```csharp
using UnityEngine;

namespace ZombieFPS.Utilities
{
    public class CameraShake : MonoBehaviour
    {
        public static CameraShake Instance { get; private set; }

        [SerializeField] private float shakeDuration = 0.3f;
        [SerializeField] private float shakeIntensity = 0.5f;

        private Vector3 originalPosition;
        private float shakeTimer = 0f;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }

            originalPosition = transform.localPosition;
        }

        private void Update()
        {
            if (shakeTimer > 0)
            {
                shakeTimer -= Time.deltaTime;
                transform.localPosition = originalPosition + Random.insideUnitSphere * shakeIntensity;
            }
            else
            {
                transform.localPosition = Vector3.Lerp(transform.localPosition, originalPosition, Time.deltaTime * 10f);
            }
        }

        public void ShakeCamera(float intensity, float duration)
        {
            shakeIntensity = intensity;
            shakeDuration = duration;
            shakeTimer = duration;
        }
    }
}
```

---

## 🎮 SETUP COMPLETO DEL PROYECTO

### PASO 1: Configurar Layers
```
Layer 6: Player
Layer 7: Enemy
Layer 8: Ground
Layer 9: Obstacles
```

### PASO 2: Configurar Tags
```
Tag: Player
Tag: Enemy
Tag: SpawnPoint
```

### PASO 3: Crear Prefabs

#### Player Prefab:
1. CharacterController (radius: 0.5, height: 2.0)
2. Añadir todos los scripts de Player
3. Crear WeaponHolder con las 3 armas
4. Configurar cámara hijo

#### Zombie Prefab:
1. Capsule Collider
2. NavMeshAgent
3. ZombieAI.cs + ZombieHealth.cs
4. Crear hitboxes (Head, Body, Limbs) como hijos
5. Añadir componente HitBox a cada hitbox

### PASO 4: Bake NavMesh
1. Window > AI > Navigation
2. Seleccionar Ground
3. Mark as Navigation Static
4. Bake

### PASO 5: Crear Spawn Points
1. Crear GameObjects vacíos
2. Tag: "SpawnPoint"
3. Distribuir alrededor del mapa
4. Asignar al WaveManager

---

## ✅ CHECKLIST FINAL

### Scripts Core
- [x] GameManager
- [x] AudioManager
- [x] ObjectPool

### Scripts Combat
- [x] WeaponBase + 3 armas
- [x] MeleeWeapon
- [x] Grenade + Controller
- [x] HitBox system

### Scripts Player
- [x] PlayerController
- [x] PlayerHealth
- [x] WeaponController

### Scripts Enemies
- [x] ZombieConfig
- [x] ZombieAI
- [x] ZombieHealth

### Scripts Waves
- [ ] WaveManager (copiar del código arriba)

### Scripts UI
- [ ] HUDManager (copiar del código arriba)
- [ ] ShopSystem (copiar del código arriba)

### Scripts Utilities
- [x] SingletonMonoBehaviour
- [ ] CameraShake (copiar del código arriba)

---

## 🚀 PRÓXIMOS PASOS

1. **Copiar los scripts restantes** de este documento
2. **Crear los ScriptableObjects** para armas y zombies
3. **Configurar el Player Prefab** con todas las armas
4. **Crear el Zombie Prefab** con hitboxes
5. **Bake el NavMesh** en tu mapa
6. **Configurar el HUD Canvas** con TextMeshPro
7. **Testear y balancear**

---

## 🎯 VALORES RECOMENDADOS FINALES

### Dificultad Balanceada:
```
Wave 1: 7 Normal Zombies
Wave 3: 11 Normal + 1 Fast
Wave 5: 15 Normal + 3 Fast + 1 Tank
Wave 10: 25 Normal + 8 Fast + 3 Tank
```

### Economía:
```
Starting Money: $500
Kill Normal: $10
Kill Fast: $15
Kill Tank: $25
Headshot Bonus: x2 money

Ammo Pack: $50 (1 magazine)
Grenade: $100
Health Pack: $150 (50 HP)
```

---

¡CON ESTO TIENES TODO PARA UN FPS DE ZOMBIES COMPLETO Y FUNCIONAL! 🎮🧟‍♂️💀
