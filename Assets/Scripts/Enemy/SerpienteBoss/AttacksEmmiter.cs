using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttacksEmmiter : MonoBehaviour
{
    [SerializeField] SerpienteBoss snake;
    
    public void closeAttackEmmiter()
    {
        ManaSystem.instance.TakeDamage(snake.dañoMordida);
    }

    public void rangeAttackEmmiter()
    {
        // Instanciamos el proyectil adelantado para evitar colisión con el lanzador
        GameObject veneno = Instantiate(snake.proyectilVenenoPrefab,
            snake.puntoDisparo.position,
            Quaternion.identity);

        veneno.GetComponent<ProyectilVeneno>().IniciarVeneno(
            (snake.objetivo.position - snake.puntoDisparo.position).normalized,
            snake.dañoVeneno,
            snake.duracionVeneno,
            snake.charcoVenenoPrefab,
            snake.tiempoCharcoVeneno
            );
    }
}
