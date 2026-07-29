using UnityEngine;

[CreateAssetMenu(fileName = "SpeakerDefinition", menuName = "Pumpkins/Speaker Definition")]
public class SpeakerDefinition : ScriptableObject
{
    [SerializeField] private string displayName;
    [SerializeField] private string role;
    [SerializeField] private Sprite neutralPortrait;
    [SerializeField] private Sprite happyPortrait;
    [SerializeField] private Sprite sadPortrait;

    public string DisplayName => displayName;
    public string Role => role;
    public Sprite NeutralPortrait => neutralPortrait;
    public Sprite HappyPortrait => happyPortrait;
    public Sprite SadPortrait => sadPortrait;

    public Sprite GetPortrait(SpeakerExpression expression)
    {
        Sprite portrait = expression switch
        {
            SpeakerExpression.Happy => happyPortrait,
            SpeakerExpression.Sad => sadPortrait,
            _ => neutralPortrait
        };

        return portrait == null ? neutralPortrait : portrait;
    }
}
