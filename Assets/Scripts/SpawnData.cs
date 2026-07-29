using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SpawnData", menuName = "Pumpkins/Spawn/Spawn Data")]
public class SpawnData : ScriptableObject
{
    [Header("Trash")]
    [SerializeField] private Trash prefab;
    [SerializeField] private float spawnInterval = 0.2f;
    [SerializeField] private int spawnLimit = 10;
    [SerializeField] private List<Sprite> sprites = new List<Sprite>();

    public Trash Prefab => prefab;
    public float SpawnInterval => spawnInterval;
    public int SpawnLimit => spawnLimit;
    public IReadOnlyList<Sprite> Sprites => sprites;

    private void OnValidate()
    {
        spawnInterval = Mathf.Max(0.01f, spawnInterval);
        spawnLimit = Mathf.Max(1, spawnLimit);
    }
}
