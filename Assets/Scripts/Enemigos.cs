using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Enemigos : MonoBehaviour
{

    float vidaRestante = 3.0f;
    public Image barraVida;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void heMuerto()
    {
        Destroy(gameObject);
        Debug.Log("El enemigo ha muerto");
    }

    void heSidoTocado()
    {
        vidaRestante = GetComponent<GestionVida>().maxVida / GetComponent<GestionVida>().vida;
        barraVida.transform.localScale = new Vector3(vidaRestante, 1, 1);
        Debug.Log("El enemigo ha sido tocado");
    }
}
