using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class BeeLifeController : MonoBehaviour
{
    [SerializeField] LookAtConstraint lookConstraint;
    Transform player;
    [SerializeField] GameObject parent;
    [SerializeField] private float life;

    private void Start()
    {
        ConstraintSource source = new ConstraintSource();
        source.sourceTransform = player.transform;
        source.weight = 1;

        lookConstraint.AddSource(source);
        lookConstraint.constraintActive = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("FireBall"))
        {
            Destroy(other.gameObject);
            life -= 10f;
            if (life <= 0)
                Destroy(parent);
        }
    }

    public void SetPlayer(Transform player)
    {
        this.player = player;
    }
}
