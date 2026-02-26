using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class GameOverLine : MonoBehaviour
{
    [SerializeField] private float graceTime = 1.0f;
    [SerializeField] private LayerMask fruitMask; // set to Fruit layer

    private readonly HashSet<Fruit> inside = new();
    private float deadline = -1f;
    private bool gameOverFired;

    private void Reset()
    {
        var c = GetComponent<Collider2D>();
        c.isTrigger = true;
    }

    private void Update()
    {
        if (gameOverFired) return;

        // If something is inside, start/continue countdown
        if (inside.Count > 0)
        {
            if (deadline < 0f) deadline = Time.time + graceTime;

            if (Time.time >= deadline)
            {
                gameOverFired = true;
                GameSignals.RaiseGameOver();
            }
        }
        else
        {
            // Nothing inside -> cancel countdown
            deadline = -1f;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & fruitMask) == 0) return;

        var fruit = other.GetComponent<Fruit>();
        if (!fruit) return;

        inside.Add(fruit);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & fruitMask) == 0) return;

        var fruit = other.GetComponent<Fruit>();
        if (!fruit) return;

        inside.Remove(fruit);
    }
}
