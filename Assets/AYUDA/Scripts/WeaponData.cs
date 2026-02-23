using UnityEngine;

/// <summary>
/// Modo de disparo del arma
/// </summary>
public enum FireMode
{
    Normal,     // Disparo estándar (auto o semi)
    Burst,      // Ráfagas (ej: 3 balas por clic)
    Shotgun     // Múltiples perdigones por disparo
}

/// <summary>
/// ScriptableObject que define los datos de un arma
/// Permite crear diferentes configuraciones sin duplicar código
/// </summary>
[CreateAssetMenu(fileName = "New Weapon Data", menuName = "Weapon Data/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Información General")]
    [Tooltip("Nombre del arma que se muestra en la UI")]
    public string weaponName = "Weapon";
    
    [Tooltip("Icono del arma para la UI")]
    public Sprite weaponIcon;

    [Header("Estadísticas de Combate")]
    [Tooltip("Daño por disparo")]
    [Range(1, 100)]
    public int damage = 25;
    
    [Tooltip("Cadencia de fuego (segundos entre disparos)")]
    [Range(0.05f, 2f)]
    public float fireRate = 0.1f;
    
    [Tooltip("Distancia máxima de disparo efectivo")]
    [Range(10f, 500f)]
    public float maxRange = 100f;

    [Header("Sistema de Munición")]
    [Tooltip("Munición máxima en un cargador")]
    [Range(1, 100)]
    public int maxAmmoInMagazine = 30;
    
    [Tooltip("Munición máxima en reserva")]
    [Range(0, 500)]
    public int maxReserveAmmo = 120;
    
    [Tooltip("Tiempo de recarga en segundos")]
    [Range(0.5f, 5f)]
    public float reloadTime = 2f;

    [Header("Comportamiento")]
    [Tooltip("¿Es automática? (mantener clic) o semi-automática (clic por disparo)")]
    public bool isAutomatic = true;

    [Tooltip("Modo de disparo: Normal, Burst (ráfagas), Shotgun (perdigones)")]
    public FireMode fireMode = FireMode.Normal;

    [Header("Escopeta (Shotgun)")]
    [Tooltip("Número de perdigones por disparo")]
    [Range(1, 20)]
    public int pelletsPerShot = 8;

    [Tooltip("Dispersión del cono de perdigones en grados")]
    [Range(1f, 30f)]
    public float pelletSpreadAngle = 8f;

    [Tooltip("Daño individual por perdigón (si 0, usa damage/pelletsPerShot)")]
    [Range(0, 100)]
    public int pelletDamage = 0;

    [Header("Ráfaga (Burst)")]
    [Tooltip("Balas por ráfaga")]
    [Range(2, 5)]
    public int burstCount = 3;

    [Tooltip("Intervalo entre balas de la ráfaga (segundos)")]
    [Range(0.03f, 0.2f)]
    public float burstInterval = 0.06f;

    [Header("Precisión y Dispersión")]
    [Tooltip("Dispersión base del arma en grados (0 = perfecta)")]
    [Range(0f, 10f)]
    public float baseSpread = 1f;

    [Tooltip("Dispersión máxima al disparar continuamente")]
    [Range(0f, 15f)]
    public float maxSpread = 5f;

    [Tooltip("Dispersión añadida por disparo")]
    [Range(0f, 3f)]
    public float spreadPerShot = 0.5f;

    [Tooltip("Velocidad de recuperación de la dispersión")]
    [Range(1f, 20f)]
    public float spreadRecoverySpeed = 5f;

    [Tooltip("Multiplicador de dispersión al moverse")]
    [Range(1f, 3f)]
    public float movingSpreadMultiplier = 1.5f;

    [Tooltip("Multiplicador de dispersión en el aire")]
    [Range(1f, 5f)]
    public float airSpreadMultiplier = 2.5f;

    [Header("Penetración")]
    [Tooltip("¿Puede la bala atravesar objetos?")]
    public bool canPenetrate = false;

    [Tooltip("Número máximo de objetivos que puede atravesar")]
    [Range(1, 5)]
    public int maxPenetrations = 1;

    [Tooltip("Porcentaje de daño que conserva tras penetrar (0-1)")]
    [Range(0.1f, 1f)]
    public float penetrationDamageRetention = 0.5f;

    [Header("Caída de Daño por Distancia")]
    [Tooltip("¿El daño disminuye con la distancia?")]
    public bool enableDamageFalloff = true;

    [Tooltip("Distancia a la que empieza a caer el daño")]
    [Range(5f, 100f)]
    public float falloffStartDistance = 20f;

    [Tooltip("Daño mínimo (porcentaje del daño base, 0-1)")]
    [Range(0.1f, 1f)]
    public float minDamageMultiplier = 0.3f;

    [Header("Efectos Visuales")]
    [Tooltip("Retroceso de la cámara al disparar")]
    [Range(0f, 5f)]
    public float recoilAmount = 1f;

    [Tooltip("Mostrar trazador de bala")]
    public bool showBulletTracer = true;

    [Tooltip("Color del trazador")]
    public Color tracerColor = new Color(1f, 0.9f, 0.5f, 0.8f);

    [Tooltip("Velocidad del trazador (unidades/segundo). 0 = instantáneo")]
    public float tracerSpeed = 300f;

    [Header("Sonidos")]
    [Tooltip("Sonido de disparo")]
    public AudioClip shootSound;
    
    [Tooltip("Sonido de recarga")]
    public AudioClip reloadSound;
    
    [Tooltip("Sonido cuando no hay munición")]
    public AudioClip emptySound;

    [Header("Dirección de Recarga")]
    [Tooltip("Multiplica la dirección lateral de la animación de recarga. 0 = usa el valor por defecto (1). Pon -1 para invertir la dirección si el arma recarga al lado contrario.")]
    public float reloadDirectionMultiplier = 0f;

    [Header("Sistema de Mejoras (2 niveles máx)")]
    [Tooltip("Coste de la primera mejora")]
    public int upgradeCost1 = 2500;
    [Tooltip("Coste de la segunda mejora (debe ser mayor que la primera)")]
    public int upgradeCost2 = 6000;
}
