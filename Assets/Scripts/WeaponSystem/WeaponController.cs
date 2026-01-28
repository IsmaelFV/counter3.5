using UnityEngine;
using System.Collections;

public class WeaponController : MonoBehaviour
{
    [Header("Weapon Stats")]
    public float damage = 10f;
    public float range = 100f;
    public float fireRate = 0.1f; // Seconds between shots
    public int magazineSize = 30;
    public int ammoReserve = 90;

    [Header("Setup")]
    public Camera fpsCamera;
    public LayerMask ignoreLayer; // Layer to ignore (Player)

    [Header("Visuals")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public Animation weaponAnimation; // Reference to Animation component (Optional)
    public Animator characterAnimator; // Reference to the Player's main Animator (Humanoid)

    // Internal State
    private int currentAmmo;
    private float nextTimeToFire = 0f;
    private bool isReloading = false;

    void Start()
    {
        if (fpsCamera == null)
        {
            fpsCamera = Camera.main;
            if (fpsCamera == null)
            {
                Debug.LogError("WeaponController: No Camera assigned and Camera.main not found!");
            }
        }
        currentAmmo = magazineSize;

        // Try to get animation if not assigned
        if (weaponAnimation == null)
            weaponAnimation = GetComponent<Animation>();

        // ROBUST CHARACTER ANIMATOR SEARCH
        if (characterAnimator == null)
        {
            // Find the root player first
            var player = GetComponentInParent<UnityStandardAssets.Characters.FirstPerson.FirstPersonController>(); 
            // Note: Adjust namespace if needed, or just use MonoBehaviour if generic
            if (player != null)
            {
                Animator[] anims = player.GetComponentsInChildren<Animator>();
                foreach (var a in anims)
                {
                    if (a.runtimeAnimatorController != null)
                    {
                        characterAnimator = a;
                        Debug.Log("WeaponController: Character Animator found on " + a.name);
                        break;
                    }
                }
            }
        }
    }

    void OnEnable()
    {
        isReloading = false;
    }

    void Update()
    {
        if (isReloading)
            return;

        if (currentAmmo <= 0 || (currentAmmo < magazineSize && Input.GetKeyDown(KeyCode.R)))
        {
            if (ammoReserve > 0)
            {
                StartCoroutine(Reload());
                return;
            }
        }

        if (Input.GetButton("Fire1") && Time.time >= nextTimeToFire)
        {
            if (currentAmmo > 0)
            {
                nextTimeToFire = Time.time + fireRate;
                Shoot();
            }
        }
    }

    void Shoot()
    {
        Debug.Log("Intentando disparar..."); // Debug for user
        currentAmmo--;
        
        // Play Animation Safely
        if (weaponAnimation != null)
        {
            if (weaponAnimation.clip != null)
                weaponAnimation.Play();
            else if (weaponAnimation.GetClipCount() > 0) 
                 weaponAnimation.Play(); // Play default
        }

        // Play Character Animation
        if (characterAnimator != null)
        {
            characterAnimator.SetTrigger("Fire");
        }

        // Instantiate Bullet Visual
        if (bulletPrefab != null && firePoint != null)
        {
            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
            // If the bullet has a specific script to move, effective. 
            // If it's just a visual, we might want to add force if it has a rigidbody.
            Rigidbody rb = bullet.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(firePoint.forward * 50f, ForceMode.Impulse); // Adjust speed as needed
            }
            Destroy(bullet, 2f); // Cleanup
        }
        else
        {
             // Fallback or warning if user forgot to assign
             if (bulletPrefab == null) Debug.LogWarning("WeaponController: No Bullet Prefab assigned!");
             if (firePoint == null) Debug.LogWarning("WeaponController: No Fire Point assigned!");
        }

        RaycastHit hit;
        
        if (Physics.Raycast(fpsCamera.transform.position, fpsCamera.transform.forward, out hit, range, ~ignoreLayer))
        {
            Debug.Log($"Hit: {hit.transform.name}");

            // Example damage logic (requires target to have a health script)
            // Target target = hit.transform.GetComponent<Target>();
            // if (target != null) target.TakeDamage(damage);
        }
    }

    IEnumerator Reload()
    {
        isReloading = true;
        Debug.Log("Reloading...");

        // Play Reload Animation if available (Optional logic, can expand later)

        yield return new WaitForSeconds(1.5f); // Reload time

        int ammoNeeded = magazineSize - currentAmmo;
        int ammoToReload = Mathf.Min(ammoNeeded, ammoReserve);

        currentAmmo += ammoToReload;
        ammoReserve -= ammoToReload;

        isReloading = false;
        Debug.Log("Reloaded. Ammo: " + currentAmmo + " / " + ammoReserve);
    }
}
