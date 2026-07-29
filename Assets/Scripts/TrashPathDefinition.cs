using UnityEngine;

[CreateAssetMenu(fileName = "TrashPathDefinition", menuName = "Pumpkins/Trash/Path Definition")]
public class TrashPathDefinition : ScriptableObject
{
    [SerializeField, Min(0.01f)] private float movementSpeed = 1f;

    public float MovementSpeed => movementSpeed;

    private void OnValidate()
    {
        movementSpeed = Mathf.Max(0.01f, movementSpeed);
    }
}
