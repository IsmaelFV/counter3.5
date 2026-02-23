# 🎯 GUÍA RÁPIDA DE IMPLEMENTACIÓN - ZOMBIE FPS

## ⚡ IMPLEMENTACIÓN EN 30 MINUTOS

### FASE 1: Setup Inicial (5 min)

#### 1. Importar Input System
```
Edit > Project Settings > Input Manager
- Verificar que Fire1 = Mouse Button 0
- Jump = Space
- Horizontal/Vertical configurados
```

#### 2. Configurar Layers y Tags
```csharp
// Layers (Edit > Project Settings > Tags and Layers)
6: Player
7: Enemy
8: Ground
9: Obstacles

// Tags
- Player
- Enemy
- SpawnPoint
```

---

### FASE 2: Crear ScriptableObjects (5 min)

#### Armas
```
Assets/ScriptableObjects/Weapons/

1. Click Derecho > Create > ZombieFPS > Weapons > Weapon Config
2. Crear 3 archivos:
   - SO_AssaultRifle
   - SO_Pistol
   - SO_Sniper

3. Copiar valores de ARQUITECTURA_COMPLETA.md
```

#### Zombies
```
Assets/ScriptableObjects/Enemies/

1. Create > ZombieFPS > Enemies > Zombie Config
2. Crear 3:
   - SO_ZombieNormal
   - SO_ZombieFast
   - SO_ZombieTank

3. Configurar según tabla de valores
```

---

### FASE 3: Player Prefab (10 min)

```
Player (GameObject)
│
├── CharacterController
│   • Center: (0, 1, 0)
│   • Radius: 0.5
│   • Height: 2.0
│   • Skin Width: 0.08
│   • Layer: Player
│
├── Scripts:
│   • PlayerController.cs
│   • PlayerHealth.cs
│   • WeaponController.cs
│   • GrenadeController.cs
│
├── Camera (Child)
│   └── Main Camera
│       • Position: (0, 1.6, 0)
│       • AudioListener
│       • CameraShake.cs
│
├── WeaponHolder (Child)
│   │   Position: (0.3, 1.4, 0.5)
│   │
│   ├── Weapon_AssaultRifle
│   │   ├── AssaultRifle.cs
│   │   │   • Config: SO_AssaultRifle
│   │   │   • Fire Point: crear transform hijo
│   │   │   • Player Camera: Main Camera
│   │   │   • Hit Layers: Everything EXCEPTO Player
│   │   └── Model (mesh visual)
│   │
│   ├── Weapon_Pistol
│   │   ├── Pistol.cs
│   │   │   • Config: SO_Pistol
│   │   └── Model
│   │
│   ├── Weapon_Sniper
│   │   ├── SniperRifle.cs
│   │   │   • Config: SO_Sniper
│   │   └── Model
│   │
│   └── Weapon_Melee
│       ├── MeleeWeapon.cs
│       │   • Melee Range: 2.0
│       │   • Melee Damage: 50
│       │   • Headshot Damage: 200
│       └── Model
│
└── GrenadeController
    • Grenade Prefab: [asignar prefab]
    • Throw Point: Camera transform
    • Max Grenades: 3
```

#### WeaponController Configuration:
```
Primary Weapons List:
[0] Weapon_AssaultRifle
[1] Weapon_Pistol
[2] Weapon_Sniper

Melee Weapon: Weapon_Melee
Grenade Controller: GrenadeController
Camera Transform: Main Camera
```

---

### FASE 4: Zombie Prefab (5 min)

```
Zombie_Normal (GameObject)
│
├── Capsule
│   • Radius: 0.4
│   • Height: 2.0
│   • Layer: Enemy
│
├── NavMeshAgent
│   • Speed: 3.0
│   • Acceleration: 8
│   • Stopping Distance: 1.5
│   • Auto Braking: true
│   • Agent Type: Humanoid
│
├── Scripts:
│   • ZombieAI.cs
│   │   • Config: SO_ZombieNormal
│   │   • Eye Position: crear transform hijo (altura 1.6)
│   │   • Attack Point: transform (posición frontal)
│   │   • Obstacle Layers: Ground + Obstacles
│   │
│   └── ZombieHealth.cs
│       • Config: SO_ZombieNormal
│       • Head HitBox: [asignar]
│       • Body HitBox: [asignar]
│       • Limb HitBoxes: [asignar array]
│
├── HitBoxes (Children):
│   │
│   ├── HitBox_Head
│   │   • Sphere Collider (radius: 0.2)
│   │   • Position: (0, 1.7, 0)
│   │   • HitBox.cs
│   │   │   • Hit Type: Head
│   │   │   • Parent Damageable: ZombieHealth
│   │
│   ├── HitBox_Body
│   │   • Capsule Collider
│   │   • Position: (0, 1.0, 0)
│   │   • HitBox.cs
│   │   │   • Hit Type: Body
│   │
│   └── HitBox_Limbs (x4)
│       • Small Capsule Colliders
│       • HitBox.cs
│           • Hit Type: Limb
│
└── Model (visual mesh)
    • Animator (opcional)
```

**IMPORTANTE**: Los colliders de HitBox deben ser **triggers** o estar en un layer separado.

---

### FASE 5: Scene Setup (5 min)

```
Scene Hierarchy:

Managers (GameObject vacío)
├── GameManager (GameObject)
│   └── GameManager.cs
│
└── AudioManager (GameObject)
    ├── AudioManager.cs
    │   • Initial Pool Size: 20
    │   • Master Volume: 1.0
    └── MusicSource (Child con AudioSource)

───────────────────────────────

GameWorld (GameObject vacío)
├── Player (Prefab)
│
├── Ground
│   • Static
│   • Navigation Static ✓
│   • Layer: Ground
│
├── Walls
│   • Navigation Static ✓
│   • Layer: Obstacles
│
└── SpawnPoints (GameObject vacío)
    ├── SpawnPoint_1 (Tag: SpawnPoint)
    ├── SpawnPoint_2
    ├── SpawnPoint_3
    └── SpawnPoint_4

───────────────────────────────

WaveSystem (GameObject)
└── WaveManager.cs
    • Spawn Points: [array de 4 transforms]
    • Normal Zombie Config: SO_ZombieNormal
    • Fast Zombie Config: SO_ZombieFast
    • Tank Zombie Config: SO_ZombieTank
    • Zombie Prefabs: [3 prefabs]
    • Time Between Waves: 20

───────────────────────────────

UI (Canvas - Screen Space Overlay)
├── HUD (Panel)
│   ├── HUDManager.cs [script]
│   │
│   ├── TopLeft (Group)
│   │   ├── HealthBar (Image + Fill)
│   │   ├── HealthText (TextMeshProUGUI)
│   │   └── AmmoText (TextMeshProUGUI)
│   │
│   ├── TopRight (Group)
│   │   ├── MoneyText (TextMeshProUGUI)
│   │   ├── ScoreText (TextMeshProUGUI)
│   │   └── WaveText (TextMeshProUGUI)
│   │
│   └── Center
│       ├── Crosshair (Image)
│       └── HitMarker (Image)
│
└── ShopPanel (Panel - Inactive)
    ├── ShopSystem.cs
    ├── Title (Text)
    ├── BuyAmmo (Button + Text)
    ├── BuyGrenades (Button + Text)
    ├── BuyHealth (Button + Text)
    └── CloseButton (Button)
```

---

## 🔨 BAKE NAVMESH

### Configuración:
```
1. Window > AI > Navigation

2. Object Tab:
   - Seleccionar Ground
   - ✓ Navigation Static
   - Navigation Area: Walkable

3. Bake Tab:
   - Agent Radius: 0.5
   - Agent Height: 2.0
   - Max Slope: 45
   - Step Height: 0.4
   - Click "Bake"

4. Verificar:
   - Superficie azul = navegable
   - Debe cubrir todo el suelo
   - No debe subir por paredes
```

---

## ⚙️ CONFIGURACIONES CRÍTICAS

### Input Manager (Edit > Project Settings > Input)
```
Fire1:
  - Positive Button: mouse 0
  - Type: Key or Mouse Button

Jump:
  - Positive Button: space
  
Horizontal:
  - Negative Button: a
  - Positive Button: d
  
Vertical:
  - Negative Button: s
  - Positive Button: w
```

### Physics Settings (Edit > Project Settings > Physics)
```
Layer Collision Matrix:
  Player x Enemy = ✓ (colisionan)
  Player x Ground = ✓
  Enemy x Ground = ✓
  Enemy x Enemy = ✗ (NO colisionan entre sí)
```

---

## 🐛 TROUBLESHOOTING RÁPIDO

### ❌ "Armas no disparan"
```
Checklist:
□ WeaponConfig asignado en Inspector
□ Fire Point creado y asignado
□ Player Camera asignada
□ Hit Layers = Everything EXCEPTO Layer 6 (Player)
□ Input Manager configurado (Fire1 = mouse 0)
```

### ❌ "Zombies no persiguen"
```
Checklist:
□ NavMesh bakeado (azul en Scene view)
□ ZombieAI.config asignado
□ Player tiene Tag "Player"
□ NavMeshAgent.enabled = true
□ Obstacle Layers configuradas
```

### ❌ "HUD no se actualiza"
```
Checklist:
□ HUDManager en escena
□ TextMeshPro instalado (Window > Package Manager)
□ Referencias asignadas en Inspector
□ Player tiene PlayerHealth y WeaponController
```

### ❌ "Granadas no explotan"
```
Checklist:
□ Grenade tiene Rigidbody (not kinematic)
□ Grenade tiene SphereCollider
□ Damage Layers incluye Enemy layer
□ Fuse Time > 0
```

### ❌ "Audio no suena"
```
Checklist:
□ AudioManager en escena
□ AudioClips asignados en SO
□ Volume > 0
□ AudioListener en cámara
□ No hay múltiples AudioListeners
```

---

## 🎮 TEST CHECKLIST

### Pruebas Básicas:
```
□ Player se mueve con WASD
□ Cámara gira con mouse
□ Sprint funciona (Shift)
□ Salto funciona (Space)

□ Arma dispara (Click izquierdo)
□ Recarga funciona (R)
□ Cambio de arma (1, 2, 3 o Scroll)
□ Melee ataca (V)
□ Granada lanza (G - hold)

□ HUD muestra vida
□ HUD muestra munición
□ HUD muestra dinero/puntos
□ Crosshair visible

□ Zombies spawnen
□ Zombies persiguen
□ Zombies atacan en rango
□ Zombies mueren y dan recompensas

□ Oleadas avanzan
□ Tienda se abre entre oleadas
□ Compras funcionan
□ Tiempo entre oleadas
```

---

## 📊 VALORES QUICK TEST

### Para Testear Rápido:
```csharp
// En GameManager:
Starting Money: 5000 (para testear tienda)

// En ZombieConfig Normal:
Max Health: 30 (mueren rápido para testing)

// En WaveManager:
Time Between Waves: 5 (menos espera)

// En AssaultRifle Config:
Magazine Size: 999 (munición infinita para testing)
```

**IMPORTANTE**: Volver a valores normales después de testear!

---

## 🚀 OPTIMIZACIÓN FINAL

### Para 60 FPS estables:

#### 1. Object Pooling
```csharp
// Crear pools en Start():
zombiePool = new ObjectPool<Zombie>(zombiePrefab, 30);
effectPool = new ObjectPool<ParticleSystem>(effectPrefab, 50);
```

#### 2. Update Optimization
```csharp
// En ZombieAI, ya implementado:
if (Time.frameCount % 3 != updateOffset) return;
```

#### 3. Occlusion Culling
```
1. Window > Rendering > Occlusion Culling
2. Marcar objetos grandes como "Occluder Static"
3. Bake
```

#### 4. Quality Settings
```
Edit > Project Settings > Quality
- Shadow Distance: 50
- Pixel Light Count: 4
- Anti Aliasing: 2x
- VSync: Off (o On para limitar a 60)
```

---

## ✅ CHECKLIST FINAL MVP

### Sistemas Core:
- [x] GameManager funcionando
- [x] AudioManager con pooling
- [x] Eventos funcionando

### Combat:
- [x] 3 armas primarias funcionan
- [x] Melee funciona
- [x] Granadas explotan
- [x] Headshots detectados
- [x] Damage feedback visual

### Player:
- [x] Movimiento fluido
- [x] Vida con regeneración
- [x] Cambio de armas suave

### Enemies:
- [x] Zombies persiguen
- [x] Zombies atacan
- [x] 3 variantes diferentes
- [x] Pooling implementado

### Waves:
- [x] Sistema de oleadas
- [x] Spawn progresivo
- [x] Dificultad escalable

### UI:
- [x] HUD funcional
- [x] Tienda entre oleadas
- [x] Feedback de combate

### Economy:
- [x] Sistema de puntos
- [x] Sistema de dinero
- [x] Compras funcionan

---

## 🎓 MEJORAS OPCIONALES (Post-MVP)

### Semana 2-3:
1. **Perks/Upgrades**
   - Damage boost temporal
   - Velocity boost
   - Double points

2. **Power-ups**
   - Insta-kill (30s)
   - Max ammo
   - Nuke (mata todos)

3. **Más Contenido**
   - Boss zombie (oleada 10)
   - 4ta arma (Shotgun)
   - Mapa alternativo

4. **Polish**
   - Post-processing (Bloom, Color Grading)
   - Particle effects mejorados
   - Animaciones para armas
   - Música dinámica

---

## 🏆 CRITERIOS DE EVALUACIÓN

### Funcionalidad (40%):
- ✅ Todos los requisitos implementados
- ✅ Sin bugs críticos
- ✅ Gameplay fluido

### Calidad de Código (30%):
- ✅ Arquitectura modular
- ✅ Comentarios claros
- ✅ Patterns aplicados correctamente

### Polish (20%):
- ✅ Feedback satisfactorio
- ✅ UI legible y funcional
- ✅ Audio bien balanceado

### Innovación (10%):
- ✅ 3 mecánicas extra implementadas
- ✅ Features únicas

---

¡CON ESTA GUÍA TIENES TODO PARA COMPLETAR TU PROYECTO UNIVERSITARIO CON ÉXITO! 🎯🎮

**Tiempo estimado total**: 2-3 horas para MVP básico funcional
**Tiempo para proyecto pulido**: 1-2 semanas

¡Buena suerte! 🚀
