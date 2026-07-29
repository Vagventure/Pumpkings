using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class VisualNovelPanelBindings : MonoBehaviour
{
    [Header("Dialogue")]
    [SerializeField] private Transform lineContainer;
    [SerializeField] private Transform choiceContainer;

    [Header("Reward")]
    [SerializeField] private Transform rewardContainer;
    [SerializeField] private RewardItemView rewardPrefab;

    [Header("Prefabs")]
    [SerializeField] private DialogueLineView npcLinePrefab;
    [SerializeField] private DialogueLineView playerLinePrefab;
    [SerializeField] private DialogueChoiceView choiceSlotPrefab;
    [FormerlySerializedAs("continueButtonPrefab")]
    [SerializeField] private Button continueButton;

    [Header("Speakers")]
    [SerializeField] private GameObject leftCharacterRoot;
    [SerializeField] private Image leftPortraitImage;
    [SerializeField] private SpriteRenderer leftPortraitRenderer;
    [SerializeField] private GameObject rightCharacterRoot;
    [SerializeField] private Image rightPortraitImage;
    [SerializeField] private SpriteRenderer rightPortraitRenderer;
    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private Color inactiveColor = Color.black;

    [Header("Audio")]
    [SerializeField] private AudioSource voiceSource;

    public Transform LineContainer => lineContainer;
    public Transform ChoiceContainer => choiceContainer;
    public Transform RewardContainer => rewardContainer;
    public RewardItemView RewardPrefab => rewardPrefab;
    public DialogueLineView NpcLinePrefab => npcLinePrefab;
    public DialogueLineView PlayerLinePrefab => playerLinePrefab;
    public DialogueChoiceView ChoiceSlotPrefab => choiceSlotPrefab;
    public Button ContinueButton => continueButton;
    public GameObject LeftCharacterRoot => ResolveCharacterRoot(leftCharacterRoot, leftPortraitImage, leftPortraitRenderer);
    public Image LeftPortraitImage => leftPortraitImage;
    public SpriteRenderer LeftPortraitRenderer => leftPortraitRenderer;
    public GameObject RightCharacterRoot => ResolveCharacterRoot(rightCharacterRoot, rightPortraitImage, rightPortraitRenderer);
    public Image RightPortraitImage => rightPortraitImage;
    public SpriteRenderer RightPortraitRenderer => rightPortraitRenderer;
    public Color ActiveColor => activeColor;
    public Color InactiveColor => inactiveColor;
    public AudioSource VoiceSource => voiceSource;

    public bool HasRequiredBindings()
    {
        return lineContainer != null
            && choiceContainer != null
            && npcLinePrefab != null
            && playerLinePrefab != null
            && choiceSlotPrefab != null
            && continueButton != null;
    }

    public bool HasRewardBindings()
    {
        return rewardContainer != null && rewardPrefab != null;
    }

    private static GameObject ResolveCharacterRoot(GameObject explicitRoot, Image portraitImage, SpriteRenderer portraitRenderer)
    {
        if (explicitRoot != null)
        {
            return explicitRoot;
        }

        if (portraitImage != null)
        {
            return portraitImage.gameObject;
        }

        return portraitRenderer == null ? null : portraitRenderer.gameObject;
    }
}
