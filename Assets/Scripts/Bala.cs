using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bala : MonoBehaviour
{

    public float velocidad = 5.0f;
    public float valorHerida = 10.0f; // Increased for one-shot kill
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float movDistancia = velocidad * Time.deltaTime;
        transform.Translate(Vector3.forward * movDistancia);
    }

    // Handle Trigger Colliders (IsTrigger = true)
    void OnTriggerEnter(Collider other)
    {
        HitTarget(other.gameObject);
    }

    // Handle Solid Colliders (IsTrigger = false)
    void OnCollisionEnter(Collision collision)
    {
        HitTarget(collision.gameObject);
    }

    void HitTarget(GameObject target)
    {
        // Try to get the health component directly (or in parents if collider is on a child bone)
        GestionVida vida = target.GetComponentInParent<GestionVida>();
        if (vida != null)
        {
            vida.tocado(valorHerida);
        }
        else
        {
            // Fallback just in case
            target.SendMessage("tocado", valorHerida, SendMessageOptions.DontRequireReceiver);
        }
        
        Destroy(gameObject);
    }
}
