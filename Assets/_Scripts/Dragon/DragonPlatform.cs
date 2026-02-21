using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class DragonPlatform : MonoBehaviour
{
    [SerializeField] float platformDuration = 10.0f;
    [SerializeField] GameObject platform;
    [SerializeField] GameObject dragon;

    private bool isOnPlatform = false;
    private float timeOnPlatform = 0.0f;

    private float opacity = 1.0f;
    private bool isPhasingOut = false;

    private void FixedUpdate()
    {
        if (!isPhasingOut)
        {
            timeOnPlatform = Math.Max(timeOnPlatform + Time.deltaTime * (isOnPlatform ? 1.0f : -1.0f), 0.0f);
            // Debug.Log(timeOnPlatform);

            if (timeOnPlatform >= platformDuration)
            {
                Debug.Log("Trigger Roar");
                // dragon.GetComponent<EnemyDragon>().Roar();

                timeOnPlatform = 0.0f;
            }
        }

        opacity = Math.Clamp(opacity + (isPhasingOut ? -1.0f : 1.0f) * 1.5f * Time.deltaTime, 0.0f, 1.0f);

        SpriteRenderer renderer = GetComponentInParent<SpriteRenderer>();
        Color newColor = renderer.color;
        newColor.a = opacity;
        renderer.color = newColor;

        if (opacity <= 0.1f)
        {
            transform.parent.GetComponent<BoxCollider2D>().enabled = false;
            isPhasingOut = false;
        }
        else if (opacity >= 0.9f)
        {
            transform.parent.GetComponent<BoxCollider2D>().enabled = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        isOnPlatform = true;
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        isOnPlatform = true;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        isOnPlatform = false;
    }

    public void PhasePlatform()
    {
        isPhasingOut = true;
    }
}