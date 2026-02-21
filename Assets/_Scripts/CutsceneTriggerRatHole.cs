using UnityEngine;
using UnityEngine.Playables;

public class CutsceneTriggerRatHole : MonoBehaviour
{
    [SerializeField] private PlayableDirector director;
    private bool hasPlayed = false;

    private void Awake()
    {
        // Starts disabled — LevelZoneManager enables it on level complete
        GetComponent<Collider2D>().enabled = false;
    }

    public void EnableTrigger()
    {
        GetComponent<Collider2D>().enabled = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasPlayed || !other.CompareTag("Player")) return;
        hasPlayed = true;
        director.Play();
    }
}
