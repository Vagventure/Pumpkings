using UnityEngine;
using UnityEngine.Serialization;

public enum RewardPath
{
    None,
    Posters,
    SchoolLectures,
    CleanUp,
    PrintingCompany,
    PlasticRecycling,
    RecyclingPatrol
}

public abstract class RewardItem : ScriptableObject
{
    [Header("Progression")]
    [SerializeField] private RewardPath path;
    [Min(1)] [SerializeField] private int level = 1;

    [Header("Display")]
    [SerializeField] private Sprite icon;
    [SerializeField] private Sprite effectIcon;
    [FormerlySerializedAs("displayName")]
    [SerializeField] private string title;
    [SerializeField] private string subtitle;
    [TextArea] [SerializeField] private string description;

    public Sprite Icon => icon;
    public Sprite EffectIcon => effectIcon;
    public string Title => title;
    public string Subtitle => subtitle;
    public string Description => description;
    public RewardPath Path => path;
    public int Level => level;

    public string DisplayName => title;
}
