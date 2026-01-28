using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GestionVida : MonoBehaviour
{
    public float vida = 5.0f;
    // Start is called before the first frame update
    public float maxVida = 5.0f;

    public UnityEvent heSidoTocado;
    public UnityEvent heMuerto;

    public void tocado(float fuerza)
    {
        heSidoTocado.Invoke();
        if (vida <= 0.0f)
        {
            heMuerto.Invoke();
        }
    }
    public void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
