using FMOD.Studio;
using FMODUnity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.HID;
using UnityEngine.Playables;

public class AttacksEmmiter : MonoBehaviour
{
    [SerializeField] SerpienteBoss snake;
    [SerializeField] PlayableDirector playableDirector;
    [SerializeField] GameObject hud;
    public void closeAttackEmmiter()
    {
        ManaSystem.instance.TakeDamage(snake.dañoMordida);
        EventInstance instance = RuntimeManager.CreateInstance(snake.closeAttack);
        instance.set3DAttributes(RuntimeUtils.To3DAttributes(snake.snakePosition));
        instance.start();
        instance.release();
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

        EventInstance instance = RuntimeManager.CreateInstance(snake.distAttack);
        instance.set3DAttributes(RuntimeUtils.To3DAttributes(snake.snakePosition));
        instance.start();
        instance.release();
    }

    public void startDeathCinematic() //Comenca animació/cinematica
    {
        hud.SetActive(false);
        // Disable player input during cinematic
        if (UserInput.instance != null)
        {
            UserInput.instance.switchActionMap(UserInput.ActionMap.InCinematic);
        }

        // Optional: Start a coroutine to restore player input after a delay
        StartCoroutine(RestorePlayerInputAfterDelay((float)playableDirector.duration)); //playableDirector.duration
    }

    private IEnumerator RestorePlayerInputAfterDelay(float delay) //Quan acaba cinemàtica
    {
        yield return new WaitForSeconds(delay);

        hud.SetActive(true);

        if (UserInput.instance != null)
        {
            UserInput.instance.switchActionMap(UserInput.ActionMap.InGame);
        }
    }

}
