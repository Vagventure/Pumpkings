using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class ShopController : MonoBehaviour
{
    public event Action<RewardItemView> ItemUnlocked;

    public static ShopController Instance { get; private set; }

    [Header("Shop Items")]
    [SerializeField] private Transform shopItemsParent;
    [SerializeField] private RewardItemView shopItemPrefab;
    [SerializeField] private RectTransform shopItemUnlockedVfxTarget;

    [Header("Runtime State")]
    [FormerlySerializedAs("unlockedShopItemPrefabs")]
    [SerializeField] private List<ShopItemDefinition> unlockedShopItems = new();

    public IReadOnlyList<ShopItemDefinition> UnlockedShopItems => unlockedShopItems;
    public RectTransform ShopItemUnlockedVfxTarget => shopItemUnlockedVfxTarget == null
        ? shopItemsParent as RectTransform
        : shopItemUnlockedVfxTarget;

    private void Awake()
    {
        SetupSingleton();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public bool IsShopItemUnlocked(ShopItemDefinition shopItemDefinition)
    {
        return shopItemDefinition != null && unlockedShopItems.Contains(shopItemDefinition);
    }

    public bool UnlockShopItem(ShopItemDefinition shopItemDefinition)
    {
        if (shopItemDefinition == null)
        {
            Debug.LogWarning("ShopController: Cannot unlock a null shop item definition.");
            return false;
        }

        if (unlockedShopItems.Contains(shopItemDefinition))
        {
            return false;
        }

        if (shopItemPrefab == null)
        {
            Debug.LogWarning("ShopController: Shop Item Prefab is missing.");
            return false;
        }

        Transform parent = shopItemsParent == null ? transform : shopItemsParent;
        RewardItemView shopItem = Instantiate(shopItemPrefab, parent);
        shopItem.Configure(shopItemDefinition);
        unlockedShopItems.Add(shopItemDefinition);

        if (shopItem.TryGetComponent(out LayoutItemSlideIn slideIn))
        {
            slideIn.Play();
        }

        ItemUnlocked?.Invoke(shopItem);
        return true;
    }

    private bool SetupSingleton()
    {
        if (Instance != null && Instance != this)
        {
            enabled = false;
            Destroy(gameObject);
            return false;
        }

        Instance = this;
        return true;
    }
}
