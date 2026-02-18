using UnityEngine;

[CreateAssetMenu(fileName = "LevelConfig", menuName = "Scriptable Objects/LevelConfig")]
public class LevelConfig : ScriptableObject
{
    [Header("General")]
    public string levelName;

    [Header("Enemies")]
    public GameObject[] enemyPrefabs;
    public int enemiesToKill = 5;
    public float spawnRate = 1.5f;

    [Header("Player")]
    public Sprite projectileSprite;

    [Header("Audio")]
    public AudioClip levelMusic;
}
