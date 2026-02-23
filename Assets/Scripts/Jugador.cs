using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Jugador : MonoBehaviour
{
    Transform salida;

    float proximoDisparo = 0.0f;
    public float tiempoEntreDisparo = 0.5f;
    public GameObject bala;
    // Start is called before the first frame update
    void Start()
    {
        salida = gameObject.transform.GetChild(0).transform;
    }

    // Update is called once per frame
    void Update()
    {
        if(Time.time >= proximoDisparo && Input.GetMouseButtonDown(0))
        {
            proximoDisparo = Time.time + tiempoEntreDisparo;
            Instantiate(bala, salida.position, salida.rotation);
        }
    }
}
