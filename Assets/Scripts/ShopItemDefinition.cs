using UnityEngine;

[CreateAssetMenu(fileName = "ShopItemDefinition", menuName = "Pumpkins/Shop/Shop Item Definition")]
public class ShopItemDefinition : RewardItem
{
    [Header("Shop Item")]
    [SerializeField] private int cost = 10;
    [SerializeField] private int awarenessValue = 10;

    public int Cost => cost;
    public int AwarenessValue => awarenessValue;

    protected virtual void OnValidate()
    {
        cost = Mathf.Max(0, cost);
        awarenessValue = Mathf.Max(0, awarenessValue);
    }
}
