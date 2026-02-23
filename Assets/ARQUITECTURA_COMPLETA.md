# 🎮 ZOMBIE FPS - ARQUITECTURA COMPLETA Y GUÍA DE IMPLEMENTACIÓN

## 📋 ÍNDICE
1. [Resumen Ejecutivo](#resumen-ejecutivo)
2. [Arquitectura del Proyecto](#arquitectura-del-proyecto)
3. [Sistemas Implementados](#sistemas-implementados)
4. [Sistemas Pendientes de Implementar](#sistemas-pendientes)
5. [Valores Recomendados (Inspector)](#valores-recomendados)
6. [Guía de Setup Paso a Paso](#guía-de-setup)
7. [Troubleshooting Común](#troubleshooting)
8. [Optimizaciones](#optimizaciones)

---

## 🎯 RESUMEN EJECUTIVO

### ✅ SISTEMAS CORE COMPLETADOS (100%)

#### 1. **GameManager** - Control de flujo del juego
- ✅ Singleton con persistencia entre escenas
- ✅ Estados del juego (MainMenu, Playing, Paused, Shopping, GameOver)
- ✅ Sistema de puntuación con multiplicadores
- ✅ Sistema de economía (dinero virtual)
- ✅ Tracking de kills y headshots
- ✅ Sistema de oleadas
- ✅ Events system desacoplado

#### 2. **AudioManager** - Gestión de audio optimizada
- ✅ Object Pooling para AudioSources (20 iniciales)
- ✅ Sonido 3D espacial para combate
- ✅ Sonido 2D para UI
- ✅ Sistema de música con crossfade
- ✅ Control de volumen (Master, SFX, Music)

#### 3. **ObjectPool<T>** - Sistema genérico de pooling
- ✅ Reutilización de zombies, proyectiles, efectos
- ✅ Interfaz IPoolable para objetos reusables
- ✅ Auto-expansión cuando se agota el pool

#### 4. **Sistema de Combate** - El corazón del juego

##### **WeaponBase** - Clase abstracta para armas
- ✅ Sistema de munición (cargador + reserva)
- ✅ Recarga con progreso
- ✅ Spread dinámico con recuperación
- ✅ Recoil configurable
- ✅ Raycast con detección de hitboxes
- ✅ Damage falloff por distancia
- ✅ Efectos visuales y audio

##### **Armas Primarias**
1. **AssaultRifle** - Rifle automático balanceado
   - Alto rate of fire
   - Bonus de precisión en ráfagas cortas (5 disparos)
   - Ideal para combate a media distancia

2. **Pistol** - Pistola semi-automática precisa
   - Primera bala ultra precisa
   - Buen daño por disparo
   - Perfecta para headshots

3. **SniperRifle** - Francotirador de alto daño
   - Sistema de zoom con FOV dinámico
   - Daño masivo a distancia
   - Penalización por movimiento

##### **Arma Secundaria**
4. **MeleeWeapon** - Cuchillo táctico
   - Instakill en headshots melee
   - Animación de ataque con curve
   - Slash effects y blood spatter
   - Screen shake en impacto
   - Sin munición requerida

##### **Sistema de Granadas**
- **Grenade** - Granada de fragmentación
  - Lanzamiento con física realista
  - Timer visual (parpadeo acelerado)
  - Explosión con daño en área
  - Falloff curve configurable
  - Line-of-sight check (paredes bloquean daño)
  
- **GrenadeController** - Controlador de inventario
  - Sistema de carga (hold to throw harder)
  - Preview de trayectoria con LineRenderer
  - Máximo 3 granadas por defecto

##### **Sistema de Hitboxes**
- **HitBox** - Componente para zombies
  - Head (2.5x damage)
  - Body (1x damage)
  - Limb (0.7x damage)
- **IDamageable** - Interfaz para objetos dañables

---

#### 5. **Sistema de Jugador**

##### **PlayerController** - Movimiento FPS
- ✅ Movimiento con CharacterController
- ✅ Sprint, crouch, salto
- ✅ Head bob al caminar
- ✅ FOV dinámico (sprint effect)
- ✅ Control de cámara suave
- ✅ Sistema de impulso (knockback)

##### **PlayerHealth** - Sistema de vida
- ✅ Regeneración automática (5s delay)
- ✅ Damage overlay (flash rojo)
- ✅ Low health vignette pulsante
- ✅ Heartbeat audio al estar bajo de vida
- ✅ Inmunidad temporal post-daño (0.5s)
- ✅ Sistema de revive para coop

##### **WeaponController** - Gestión de armas
- ✅ Cambio entre 3 armas primarias
- ✅ Rueda del ratón + teclas numéricas
- ✅ Auto-reload cuando se vacía
- ✅ Sistema de recoil de cámara
- ✅ Integración con melee y granadas

---

#### 6. **Sistema de Enemigos (Base)**
- ✅ **ZombieConfig** - ScriptableObject para variantes
  - Normal, Fast, Tank
  - Escaling de vida por oleada
  - Configuración completa de IA

---

## 📐 ARQUITECTURA DEL PROYECTO

```
Assets/
├── Scripts/
│   ├── Core/
│   │   ├── GameManager.cs ✅
│   │   ├── AudioManager.cs ✅
│   │   └── ObjectPool.cs ✅
│   │
│   ├── Player/
│   │   ├── PlayerController.cs ✅
│   │   ├── PlayerHealth.cs ✅
│   │   └── WeaponController.cs ✅
│   │
│   ├── Weapons/
│   │   ├── WeaponConfig.cs (ScriptableObject) ✅
│   │   ├── WeaponBase.cs ✅
│   │   ├── AssaultRifle.cs ✅
│   │   ├── Pistol.cs ✅
│   │   ├── SniperRifle.cs ✅
│   │   ├── MeleeWeapon.cs ✅
│   │   ├── Grenade.cs ✅
│   │   └── GrenadeController.cs ✅
│   │
│   ├── Enemies/
│   │   ├── ZombieConfig.cs (ScriptableObject) ✅
│   │   ├── ZombieAI.cs ⏳ PENDIENTE
│   │   ├── ZombieHealth.cs ⏳ PENDIENTE
│   │   └── ZombiePool.cs ⏳ PENDIENTE
│   │
│   ├── Waves/
│   │   ├── WaveConfig.cs (ScriptableObject) ⏳ PENDIENTE
│   │   ├── WaveManager.cs ⏳ PENDIENTE
│   │   └── SpawnPoint.cs ⏳ PENDIENTE
│   │
│   ├── UI/
│   │   ├── HUDManager.cs ⏳ PENDIENTE
│   │   ├── ShopUI.cs ⏳ PENDIENTE
│   │   ├── DamageIndicator.cs ⏳ PENDIENTE
│   │   └── HitMarker.cs ⏳ PENDIENTE
│   │
│   ├── Economy/
│   │   ├── ShopSystem.cs ⏳ PENDIENTE
│   │   └── ShopItem.cs (ScriptableObject) ⏳ PENDIENTE
│   │
│   └── Utilities/
│       ├── SingletonMonoBehaviour.cs ✅
│       ├── CameraShake.cs ⏳ PENDIENTE
│       └── Extensions.cs ⏳ PENDIENTE
│
├── ScriptableObjects/
│   ├── Weapons/
│   │   ├── SO_AssaultRifle.asset
│   │   ├── SO_Pistol.asset
│   │   └── SO_Sniper.asset
│   │
│   ├── Enemies/
│   │   ├── SO_ZombieNormal.asset
│   │   ├── SO_ZombieFast.asset
│   │   └── SO_ZombieTank.asset
│   │
│   └── Waves/
│       ├── SO_Wave_01.asset
│       ├── SO_Wave_05.asset
│       └── SO_Wave_10.asset
│
├── Prefabs/
│   ├── Player/
│   │   └── Player.prefab
│   ├── Weapons/
│   │   ├── Weapon_AssaultRifle.prefab
│   │   ├── Weapon_Pistol.prefab
│   │   ├── Weapon_Sniper.prefab
│   │   └── Grenade.prefab
│   ├── Enemies/
│   │   ├── Zombie_Normal.prefab
│   │   ├── Zombie_Fast.prefab
│   │   └── Zombie_Tank.prefab
│   └── Effects/
│       ├── FX_MuzzleFlash.prefab
│       ├── FX_BulletImpact.prefab
│       ├── FX_BloodSplatter.prefab
│       └── FX_Explosion.prefab
│
└── Audio/
    ├── Weapons/
    ├── Zombies/
    ├── UI/
    └── Music/
```

---

## ⚙️ VALORES RECOMENDADOS (INSPECTOR)

### 🎮 GameManager
```
Initial Money: 500
Points per Kill: 100
Money per Kill: 10
Headshot Multiplier: 1.5
```

### 🔫 ARMAS - Valores Balanceados

#### **Assault Rifle** (Arma principal balanceada)
```
Base Damage: 25
Headshot Multiplier: 2.5
Fire Rate: 10 (600 RPM)
Magazine Size: 30
Max Ammo Reserve: 210
Reload Time: 2.0s
Base Spread: 1.0
Recoil Spread Increment: 0.2
Vertical Recoil: 2.0
Horizontal Recoil: 1.0
Effective Range: 100m
Damage Dropoff: 0.01 per meter
Ammo Cost: 50
```

#### **Pistol** (Precisa, menor daño)
```
Base Damage: 35
Headshot Multiplier: 3.0
Fire Rate: 4 (semi-auto)
Magazine Size: 12
Max Ammo Reserve: 72
Reload Time: 1.5s
Base Spread: 0.5
First Shot Accuracy Bonus: 1.5
Vertical Recoil: 3.0
Horizontal Recoil: 1.5
Effective Range: 50m
Ammo Cost: 30
```

#### **Sniper Rifle** (Alto daño, baja cadencia)
```
Base Damage: 120
Headshot Multiplier: 4.0
Fire Rate: 1.5
Magazine Size: 5
Max Ammo Reserve: 25
Reload Time: 3.0s
Base Spread: 0.3
Zoomed Spread Reduction: 0.2
Vertical Recoil: 8.0
Horizontal Recoil: 2.0
Zoomed FOV: 20
Effective Range: 200m
Ammo Cost: 100
```

#### **Melee Weapon (Cuchillo)**
```
Melee Damage: 50
Headshot Damage: 200 (instakill)
Melee Range: 2.0m
Melee Radius: 0.5m
Attack Cooldown: 0.6s
Attack Duration: 0.2s
```

#### **Grenades**
```
Explosion Radius: 8m
Explosion Damage: 150
Explosion Force: 700
Fuse Time: 3.0s
Min Throw Force: 10
Max Throw Force: 30
Charge Time: 2.0s
Max Grenades: 3
```

---

### 🧟 ZOMBIES - Configuración por Tipo

#### **Zombie Normal** (Balanceado)
```
Max Health: 100
Move Speed: 3.0
Attack Damage: 20
Attack Range: 2.0
Attack Cooldown: 1.5s
Detection Range: 15m
Hearing Range: 25m
Kill Money: 10
Kill Score: 100
```

#### **Zombie Fast** (Rápido, frágil)
```
Max Health: 50 (×0.5)
Move Speed: 5.0 (×1.67)
Attack Damage: 15 (×0.75)
Attack Range: 1.8
Attack Cooldown: 1.0s (más rápido)
Detection Range: 20m (mejor detección)
Kill Money: 15 (recompensa mayor)
Kill Score: 150
```

#### **Zombie Tank** (Lento, resistente)
```
Max Health: 250 (×2.5)
Move Speed: 2.0 (×0.67)
Attack Damage: 35 (×1.75)
Attack Range: 2.5
Attack Cooldown: 2.0s
Detection Range: 12m
Kill Money: 25 (mejor recompensa)
Kill Score: 200
```

---

### 👤 PLAYER

#### **PlayerController**
```
Walk Speed: 5.0
Sprint Speed: 8.0
Crouch Speed: 2.5
Jump Force: 8.0
Gravity: 20.0
Mouse Sensitivity: 2.0
Max Look Angle: 90
Head Bob Frequency: 10
Normal FOV: 60
Sprint FOV: 70
```

#### **PlayerHealth**
```
Max Health: 100
Regen Delay: 5.0s
Regen Rate: 5.0 HP/s
Regen Tick Interval: 0.1s
Damage Immunity: 0.5s
Low Health Threshold: 30%
```

---

## 🔧 GUÍA DE SETUP PASO A PASO

### PASO 1: Crear ScriptableObjects

#### A. Configuraciones de Armas
1. Click derecho en `Assets/ScriptableObjects/Weapons/`
2. `Create > ZombieFPS > Weapons > Weapon Config`
3. Crear 3 archivos:
   - `SO_AssaultRifle`
   - `SO_Pistol`
   - `SO_Sniper`
4. Asignar valores de la sección anterior

#### B. Configuraciones de Zombies
1. En `Assets/ScriptableObjects/Enemies/`
2. `Create > ZombieFPS > Enemies > Zombie Config`
3. Crear:
   - `SO_ZombieNormal`
   - `SO_ZombieFast`
   - `SO_ZombieTank`

### PASO 2: Setup del Player Prefab

#### Hierarchy del Player:
```
Player (GameObject)
├── CharacterController
├── PlayerController.cs
├── PlayerHealth.cs
├── WeaponController.cs
├── GrenadeController.cs
│
├── Camera (Child)
│   ├── Camera component
│   └── AudioListener
│
├── WeaponHolder (Child)
│   ├── Weapon_AssaultRifle (Child)
│   │   ├── AssaultRifle.cs
│   │   └── Model
│   ├── Weapon_Pistol (Child)
│   │   ├── Pistol.cs
│   │   └── Model
│   ├── Weapon_Sniper (Child)
│   │   ├── SniperRifle.cs
│   │   └── Model
│   └── Weapon_Melee (Child)
│       ├── MeleeWeapon.cs
│       └── Model
│
└── UI (Canvas - World Space)
    ├── DamageOverlay
    └── LowHealthVignette
```

#### Configuración de Capas (Layers):
```
Layer 6: Player
Layer 7: Enemy
Layer 8: Ground
Layer 9: Projectiles
```

#### Configuración de Tags:
```
Tag: Player
Tag: Enemy
Tag: Ground
```

### PASO 3: Setup de Managers (Scene Root)

#### GameManager Setup:
1. Crear GameObject vacío: "GameManager"
2. Añadir `GameManager.cs`
3. Configurar valores iniciales

#### AudioManager Setup:
1. Crear GameObject: "AudioManager"
2. Añadir `AudioManager.cs`
3. Configurar:
   - Initial Pool Size: 20
   - Master Volume: 1.0
   - SFX Volume: 1.0
   - Music Volume: 0.7
4. Añadir Child: "MusicSource" con AudioSource

### PASO 4: Setup de UI

#### Canvas Principal (Screen Space - Overlay):
```
Canvas
├── HUD (Panel)
│   ├── HealthBar
│   ├── AmmoText
│   ├── WeaponIcon
│   ├── GrenadeCount
│   ├── MoneyText
│   ├── ScoreText
│   └── WaveText
│
├── DamageOverlay (Full screen Image)
├── LowHealthVignette (Full screen Image)
├── Crosshair
└── HitMarker
```

### PASO 5: Input Configuration

#### Project Settings > Input Manager:
```
Fire1: Mouse Button 0
Jump: Space
Horizontal: A/D, Arrow Left/Right
Vertical: W/S, Arrow Up/Down
Mouse X: Mouse X
Mouse Y: Mouse Y
```

---

## 🐛 TROUBLESHOOTING COMÚN

### Problema: "NullReferenceException en GameManager"
**Solución**: Asegúrate de que el GameManager esté en la escena y que sea el único.

### Problema: "Las armas no disparan"
**Checklist**:
- ✅ WeaponConfig asignado en Inspector
- ✅ Fire Point configurado
- ✅ Player Camera asignada
- ✅ Hit Layers configuradas (Everything excepto Player)
- ✅ Input Manager configurado

### Problema: "El jugador atraviesa paredes"
**Solución**:
- CharacterController.radius = 0.5
- CharacterController.height = 2.0
- Colliders en paredes con Layer correcto

### Problema: "Audio no se escucha"
**Checklist**:
- ✅ AudioManager en escena
- ✅ AudioClips asignados en WeaponConfig
- ✅ Volúmenes > 0
- ✅ AudioListener en cámara

### Problema: "Granadas no explotan"
**Solución**:
- Grenade tiene Rigidbody (no kinematic)
- Grenade tiene SphereCollider
- Damage Layers configuradas

---

## ⚡ OPTIMIZACIONES

### Rendimiento Target: **60 FPS** en hardware universitario típico

#### 1. Object Pooling (Ya implementado)
- Zombies: Pool de 30-50
- Proyectiles: Pool de 100
- Audio Sources: Pool de 20
- Efectos de partículas: Pool de 50

#### 2. LOD (Level of Detail)
```csharp
// Implementar en Zombies
- LOD 0 (0-15m): Full detail
- LOD 1 (15-30m): Medium detail
- LOD 2 (30-50m): Low detail
- LOD 3 (50m+): Impostor/Culled
```

#### 3. Culling
- Frustum Culling: Automático
- Occlusion Culling: Bake en nivel
- Distance Culling para zombies >100m

#### 4. Optimización de IA
```csharp
// Update zombies en stagger
void Update()
{
    if (Time.frameCount % updateInterval == zombieID % updateInterval)
    {
        UpdateAI();
    }
}
```

#### 5. Batching
- Static Batching para entorno
- Dynamic Batching para zombies (mismo material)
- GPU Instancing para props repetitivos

---

## 📊 CHECKLIST DE IMPLEMENTACIÓN

### ✅ FASE 1: CORE SYSTEMS (COMPLETADO)
- [x] GameManager con estados
- [x] AudioManager con pooling
- [x] ObjectPool genérico
- [x] Sistema de eventos

### ✅ FASE 2: COMBATE (COMPLETADO)
- [x] WeaponBase abstracta
- [x] 3 Armas primarias (Rifle, Pistol, Sniper)
- [x] Melee weapon (Cuchillo)
- [x] Sistema de granadas
- [x] Sistema de hitboxes
- [x] Damage system

### ✅ FASE 3: JUGADOR (COMPLETADO)
- [x] PlayerController con movimiento
- [x] PlayerHealth con regeneración
- [x] WeaponController
- [x] GrenadeController

### ⏳ FASE 4: ENEMIGOS (PENDIENTE)
- [ ] ZombieAI con NavMesh
- [ ] ZombieHealth
- [ ] 3 Variantes configuradas
- [ ] ZombiePool manager

### ⏳ FASE 5: OLEADAS (PENDIENTE)
- [ ] WaveManager
- [ ] WaveConfig ScriptableObjects
- [ ] SpawnPoint system
- [ ] Dificultad progresiva

### ⏳ FASE 6: UI/UX (PENDIENTE)
- [ ] HUD completo
- [ ] Shop UI
- [ ] Damage indicators
- [ ] Hit markers
- [ ] Wave transitions

### ⏳ FASE 7: ECONOMÍA (PENDIENTE)
- [ ] Shop System
- [ ] Purchasable items
- [ ] Upgrades temporales

### ⏳ FASE 8: POLISH (PENDIENTE)
- [ ] Camera shake
- [ ] Screen effects
- [ ] Particle systems
- [ ] Sound mixing
- [ ] Post-processing

---

## 🎓 PRÓXIMOS PASOS RECOMENDADOS

### PRIORIDAD ALTA (Semana 1)
1. **Implementar ZombieAI con NavMeshAgent**
   - Buscar jugador
   - Perseguir
   - Atacar en rango
   
2. **WaveManager básico**
   - Spawn progresivo
   - Contador de enemigos
   - Oleadas simples

3. **HUD funcional**
   - Vida, munición, oleada
   - Dinero y puntos

### PRIORIDAD MEDIA (Semana 2)
4. **Shop System**
   - Comprar munición
   - Comprar granadas
   - Upgrades simples

5. **Polish de combate**
   - Screen shake
   - Mejores hit markers
   - Blood effects

6. **Balanceo**
   - Testear y ajustar valores
   - Curva de dificultad

### PRIORIDAD BAJA (Semana 3)
7. **Features extra**
   - Power-ups temporales
   - Más variantes de zombies
   - Mapa alternativo

---

## 📝 NOTAS FINALES

### Arquitectura Escalable
El código está diseñado para ser **modular y extensible**:
- Añadir nuevas armas: Heredar de `WeaponBase`
- Añadir nuevos zombies: Crear nuevo `ZombieConfig`
- Añadir nuevos items: Implementar `IShopItem`

### Mejores Prácticas Aplicadas
- ✅ SOLID principles
- ✅ Composition over inheritance
- ✅ Event-driven communication
- ✅ ScriptableObject para configuración
- ✅ Object Pooling para rendimiento
- ✅ Comentarios explicativos

### Recursos Adicionales Recomendados
- Unity NavMesh documentation
- Brackeys FPS tutorial (YouTube)
- Game Developer's Conference talks sobre "game feel"

---

## 🚀 ¡MANOS A LA OBRA!

El proyecto ya tiene una **base sólida** con sistemas core y de combate completamente funcionales. Los próximos pasos son implementar la IA de enemigos, el sistema de oleadas y la UI para tener un MVP jugable.

**Tiempo estimado para MVP completo**: 1-2 semanas con dedicación diaria.

**¡Éxito con tu proyecto universitario! 🎮🧟💀**

---

*Documentación generada como parte del sistema de arquitectura FPS Zombie Survival*
*Lead Developer: AI Assistant | Framework: Unity 2021+ | Language: C# 9.0*
