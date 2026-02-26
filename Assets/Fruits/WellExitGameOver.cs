using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Game rule:
/// - The well trigger is the "safe zone".
/// - If ANY fruit exits the well and stays out for `timeOutsideToLose`, game over.
/// - If all fruits are inside (or none have exited yet), timer is reset.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class WellExitGameOver : MonoBehaviour
{
    [SerializeField] private LayerMask fruitMask;
    [SerializeField] private float timeOutsideToLose = 5f;

    // Fruits currently inside the well trigger
    private readonly HashSet<Fruit> inside = new();

    // Fruits currently outside AFTER having exited at least once
    private readonly HashSet<Fruit> outside = new();

    private float deadline = -1f;
    private bool fired;

    private void Reset()
    {
        var c = GetComponent<Collider2D>();
        c.isTrigger = true;
    }

    private void Update()
    {
        if (fired) return;

        // clean dead refs
        inside.RemoveWhere(f => f == null);
        outside.RemoveWhere(f => f == null);

        if (outside.Count > 0)
        {
            //Debug.Log("Time out started");
            if (deadline < 0f) deadline = Time.time + timeOutsideToLose;

            if (Time.time >= deadline)
            {
                fired = true;
                GameSignals.RaiseGameOver();
                Debug.Log("LMao u lost");
            }
        }
        else
        {
            // nobody is currently outside -> reset timer
            deadline = -1f;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & fruitMask) == 0) return;

        var fruit = other.GetComponent<Fruit>();
        if (!fruit) return;

        inside.Add(fruit);

        // If it re-entered, it's no longer outside
        outside.Remove(fruit);

        // Optional: if that was the last outside fruit, timer resets next Update()
        if (outside.Count == 0) deadline = -1f;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & fruitMask) == 0) return;

        var fruit = other.GetComponent<Fruit>();
        if (!fruit) return;

        inside.Remove(fruit);

        // exiting means it is now outside (this is what we punish)
        outside.Add(fruit);

        // start timer immediately when the first fruit leaves
        if (!fired && deadline < 0f) deadline = Time.time + timeOutsideToLose;
    }
}
