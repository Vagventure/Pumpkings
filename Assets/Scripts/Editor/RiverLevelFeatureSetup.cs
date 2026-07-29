using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class RiverLevelFeatureSetup
{
    private const string RecycleSpritePath = "Assets/ContentLibrary/_Audrey/UI assets-20260715T061331Z-1-001/UI assets/recycle.PNG";
    private const string OpenHandSpritePath = "Assets/ContentLibrary/_Audrey/UI assets-20260715T061331Z-1-001/UI assets/open hand.PNG";
    private const string ClosedHandSpritePath = "Assets/ContentLibrary/_Audrey/UI assets-20260715T061331Z-1-001/UI assets/closed hand.PNG";
    private const string PathDefinitionPath = "Assets/ScriptableObjects/Trash/RiverPathDefinition.asset";
    private const string RiverSpawnDataPath = "Assets/ScriptableObjects/Trash/RiverBottles.asset";
    private const string PickupProgressPrefabPath = "Assets/Prefabs/UI/TrashPickupProgress.prefab";
    private const string MoneyIconPrefabPath = "Assets/Prefabs/UI/MoneyFlyIcon.prefab";

    private static readonly string[] TrashPrefabPaths =
    {
        "Assets/Prefabs/Trash/1_Bottle.prefab",
        "Assets/Prefabs/Trash/1_Glove.prefab",
        "Assets/Prefabs/Trash/1_PlasticBag.prefab"
    };

    private static readonly Vector3[] RiverPathPoints =
    {
        new Vector3(9.447f, 0.7f, 37.438f),
        new Vector3(17.260f, 0.7f, 34.051f),
        new Vector3(24.796f, 0.7f, 30.386f),
        new Vector3(32.609f, 0.7f, 26.998f),
        new Vector3(41.286f, 0.7f, 23.853f)
    };

    [MenuItem("Tools/Pumpkins/Setup River Level Features")]
    public static void Apply()
    {
        EnsureFolder("Assets/Prefabs", "UI");
        ConfigureSpriteImporter(ClosedHandSpritePath);

        Sprite recycleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(RecycleSpritePath);
        Sprite openHandSprite = AssetDatabase.LoadAssetAtPath<Sprite>(OpenHandSpritePath);
        Sprite closedHandSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ClosedHandSpritePath);

        TrashPathDefinition pathDefinition = CreateOrUpdatePathDefinition();
        SpawnData riverSpawnData = CreateOrUpdateRiverSpawnData();
        GameObject pickupProgressPrefab = CreateOrUpdatePickupProgressPrefab(recycleSprite);
        GameObject moneyIconPrefab = CreateOrUpdateMoneyIconPrefab();

        for (int i = 0; i < TrashPrefabPaths.Length; i++)
        {
            ConfigureTrashPrefab(TrashPrefabPaths[i], pickupProgressPrefab);
        }

        ConfigureScene(
            pathDefinition,
            riverSpawnData,
            moneyIconPrefab,
            openHandSprite,
            closedHandSprite);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("RiverLevelFeatureSetup: River path, pickup VFX, cursor, money VFX, and smooth bars are configured.");
    }

    private static void EnsureFolder(string parent, string name)
    {
        string path = parent + "/" + name;

        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, name);
        }
    }

    private static void ConfigureSpriteImporter(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

        if (importer == null)
        {
            return;
        }

        if (importer.textureType == TextureImporterType.Sprite
            && importer.spriteImportMode == SpriteImportMode.Single)
        {
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
    }

    private static TrashPathDefinition CreateOrUpdatePathDefinition()
    {
        TrashPathDefinition definition = AssetDatabase.LoadAssetAtPath<TrashPathDefinition>(PathDefinitionPath);

        if (definition == null)
        {
            definition = ScriptableObject.CreateInstance<TrashPathDefinition>();
            AssetDatabase.CreateAsset(definition, PathDefinitionPath);
        }

        SerializedObject serializedDefinition = new SerializedObject(definition);
        serializedDefinition.FindProperty("movementSpeed").floatValue = 1.4f;
        serializedDefinition.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(definition);
        return definition;
    }

    private static SpawnData CreateOrUpdateRiverSpawnData()
    {
        SpawnData source = AssetDatabase.LoadAssetAtPath<SpawnData>("Assets/ScriptableObjects/Trash/Bottles.asset");
        SpawnData riverData = AssetDatabase.LoadAssetAtPath<SpawnData>(RiverSpawnDataPath);

        if (riverData == null)
        {
            riverData = ScriptableObject.CreateInstance<SpawnData>();
            AssetDatabase.CreateAsset(riverData, RiverSpawnDataPath);
        }

        if (source == null)
        {
            return riverData;
        }

        SerializedObject sourceObject = new SerializedObject(source);
        SerializedObject riverObject = new SerializedObject(riverData);
        riverObject.FindProperty("prefab").objectReferenceValue = sourceObject.FindProperty("prefab").objectReferenceValue;
        riverObject.FindProperty("spawnInterval").floatValue = 1.5f;
        riverObject.FindProperty("spawnLimit").intValue = 12;

        SerializedProperty sourceSprites = sourceObject.FindProperty("sprites");
        SerializedProperty riverSprites = riverObject.FindProperty("sprites");
        riverSprites.arraySize = sourceSprites.arraySize;

        for (int i = 0; i < sourceSprites.arraySize; i++)
        {
            riverSprites.GetArrayElementAtIndex(i).objectReferenceValue =
                sourceSprites.GetArrayElementAtIndex(i).objectReferenceValue;
        }

        riverObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(riverData);
        return riverData;
    }

    private static GameObject CreateOrUpdatePickupProgressPrefab(Sprite recycleSprite)
    {
        GameObject root = new GameObject(
            "TrashPickupProgress",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(TrashPickupProgressView));

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(100f, 100f);

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 50;

        Image background = CreateImage("Background", rootRect, recycleSprite);
        background.color = new Color(0.35f, 0.35f, 0.35f, 0.9f);

        Image fill = CreateImage("Fill", rootRect, recycleSprite);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Radial360;
        fill.fillOrigin = (int)Image.Origin360.Top;
        fill.fillClockwise = true;
        fill.fillAmount = 0f;

        SerializedObject serializedView = new SerializedObject(root.GetComponent<TrashPickupProgressView>());
        serializedView.FindProperty("fillImage").objectReferenceValue = fill;
        serializedView.ApplyModifiedPropertiesWithoutUndo();

        root.SetActive(false);
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PickupProgressPrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static Image CreateImage(string name, RectTransform parent, Sprite sprite)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = imageObject.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    private static GameObject CreateOrUpdateMoneyIconPrefab()
    {
        GameObject root = new GameObject(
            "MoneyFlyIcon",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));

        RectTransform rect = root.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(48f, 48f);

        TextMeshProUGUI text = root.GetComponent<TextMeshProUGUI>();
        text.text = "$";
        text.fontSize = 42f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(1f, 0.78f, 0.12f, 1f);
        text.raycastTarget = false;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, MoneyIconPrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static void ConfigureTrashPrefab(string prefabPath, GameObject pickupProgressPrefab)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);

        try
        {
            if (root.GetComponent<TrashPathFollower>() == null)
            {
                root.AddComponent<TrashPathFollower>();
            }

            Transform existingView = root.transform.Find("PickupProgress");

            if (existingView != null)
            {
                Object.DestroyImmediate(existingView.gameObject);
            }

            GameObject viewObject = (GameObject)PrefabUtility.InstantiatePrefab(
                pickupProgressPrefab,
                root.transform);
            viewObject.name = "PickupProgress";

            BoxCollider collider = root.GetComponent<BoxCollider>();
            Vector3 rootScale = root.transform.localScale;
            float rootScaleY = Mathf.Max(0.0001f, Mathf.Abs(rootScale.y));
            float localY = collider != null
                ? collider.center.y + collider.size.y * 0.5f + 0.6f / rootScaleY
                : 1f / rootScaleY;
            float localX = collider != null ? collider.center.x : 0f;

            viewObject.transform.localPosition = new Vector3(localX, localY, 0f);
            viewObject.transform.localRotation = Quaternion.identity;
            viewObject.transform.localScale = new Vector3(
                0.008f / Mathf.Max(0.0001f, Mathf.Abs(rootScale.x)),
                0.008f / rootScaleY,
                0.008f / Mathf.Max(0.0001f, Mathf.Abs(rootScale.z)));
            viewObject.SetActive(false);

            SerializedObject serializedTrash = new SerializedObject(root.GetComponent<Trash>());
            serializedTrash.FindProperty("pickupProgressView").objectReferenceValue =
                viewObject.GetComponent<TrashPickupProgressView>();
            serializedTrash.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void ConfigureScene(
        TrashPathDefinition pathDefinition,
        SpawnData riverSpawnData,
        GameObject moneyIconPrefab,
        Sprite openHandSprite,
        Sprite closedHandSprite)
    {
        GameObject riverRoot = GameObject.Find("_____Game_Area/3D_Scene/RIVER");
        GameObject spawnServiceObject = GameObject.Find("_GameControllers/SpawnService");

        if (riverRoot == null || spawnServiceObject == null)
        {
            Debug.LogError("RiverLevelFeatureSetup: RIVER or SpawnService is missing from the active scene.");
            return;
        }

        TrashPath path = CreateOrUpdateScenePath(riverRoot.transform, pathDefinition);
        ConfigureRiverSpawn(spawnServiceObject.GetComponent<SpawnService>(), riverSpawnData, path);
        ConfigureGameplayVfxCanvas(moneyIconPrefab, openHandSprite, closedHandSprite);
        ConfigureProgressBars();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
    }

    private static TrashPath CreateOrUpdateScenePath(
        Transform riverRoot,
        TrashPathDefinition pathDefinition)
    {
        Transform existing = riverRoot.Find("TrashPath_River");

        if (existing != null)
        {
            Object.DestroyImmediate(existing.gameObject);
        }

        GameObject pathObject = new GameObject("TrashPath_River", typeof(TrashPath));
        pathObject.transform.SetParent(riverRoot, false);
        TrashPath path = pathObject.GetComponent<TrashPath>();
        Transform[] waypoints = new Transform[RiverPathPoints.Length];

        for (int i = 0; i < RiverPathPoints.Length; i++)
        {
            GameObject waypoint = new GameObject("Waypoint_" + i.ToString("00"));
            waypoint.transform.SetParent(pathObject.transform, true);
            waypoint.transform.position = RiverPathPoints[i];
            waypoints[i] = waypoint.transform;
        }

        SerializedObject serializedPath = new SerializedObject(path);
        serializedPath.FindProperty("definition").objectReferenceValue = pathDefinition;
        SerializedProperty waypointProperty = serializedPath.FindProperty("waypoints");
        waypointProperty.arraySize = waypoints.Length;

        for (int i = 0; i < waypoints.Length; i++)
        {
            waypointProperty.GetArrayElementAtIndex(i).objectReferenceValue = waypoints[i];
        }

        serializedPath.ApplyModifiedPropertiesWithoutUndo();
        return path;
    }

    private static void ConfigureRiverSpawn(
        SpawnService spawnService,
        SpawnData riverSpawnData,
        TrashPath path)
    {
        if (spawnService == null)
        {
            return;
        }

        SerializedObject serializedService = new SerializedObject(spawnService);
        SerializedProperty trashTypes = serializedService.FindProperty("trashTypes");
        SerializedProperty riverConfig = null;

        for (int i = 0; i < trashTypes.arraySize; i++)
        {
            SerializedProperty candidate = trashTypes.GetArrayElementAtIndex(i);

            if (candidate.FindPropertyRelative("data").objectReferenceValue == riverSpawnData)
            {
                riverConfig = candidate;
                break;
            }
        }

        if (riverConfig == null)
        {
            int index = trashTypes.arraySize;
            trashTypes.InsertArrayElementAtIndex(index);
            riverConfig = trashTypes.GetArrayElementAtIndex(index);
        }

        riverConfig.FindPropertyRelative("data").objectReferenceValue = riverSpawnData;
        riverConfig.FindPropertyRelative("spawnArea").objectReferenceValue = null;
        riverConfig.FindPropertyRelative("spawnMode").enumValueIndex = (int)SpawnMode.TimedSpawn;

        SerializedProperty paths = riverConfig.FindPropertyRelative("paths");
        paths.arraySize = 1;
        paths.GetArrayElementAtIndex(0).objectReferenceValue = path;

        serializedService.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(spawnService);
    }

    private static void ConfigureGameplayVfxCanvas(
        GameObject moneyIconPrefab,
        Sprite openHandSprite,
        Sprite closedHandSprite)
    {
        GameObject existingCanvas = GameObject.Find("GameplayVfxCanvas");

        if (existingCanvas != null)
        {
            Object.DestroyImmediate(existingCanvas);
        }

        GameObject canvasObject = new GameObject(
            "GameplayVfxCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(CursorController),
            typeof(MoneyFlyVfxController));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject animationLayer = new GameObject("MoneyFlyVfxLayer", typeof(RectTransform));
        RectTransform animationRoot = animationLayer.GetComponent<RectTransform>();
        animationRoot.SetParent(canvasObject.transform, false);
        animationRoot.anchorMin = Vector2.zero;
        animationRoot.anchorMax = Vector2.one;
        animationRoot.offsetMin = Vector2.zero;
        animationRoot.offsetMax = Vector2.zero;

        GameObject cursorObject = new GameObject(
            "CustomCursor",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        RectTransform cursorRect = cursorObject.GetComponent<RectTransform>();
        cursorRect.SetParent(canvasObject.transform, false);
        cursorRect.sizeDelta = new Vector2(48f, 48f);
        cursorRect.pivot = new Vector2(0.2f, 0.8f);
        cursorObject.transform.SetAsLastSibling();

        Image cursorImage = cursorObject.GetComponent<Image>();
        cursorImage.sprite = openHandSprite;
        cursorImage.preserveAspect = true;
        cursorImage.raycastTarget = false;

        SerializedObject serializedCursor = new SerializedObject(canvasObject.GetComponent<CursorController>());
        serializedCursor.FindProperty("cursorImage").objectReferenceValue = cursorImage;
        serializedCursor.FindProperty("normalSprite").objectReferenceValue = openHandSprite;
        serializedCursor.FindProperty("grabSprite").objectReferenceValue = closedHandSprite;
        serializedCursor.FindProperty("trashLayerMask").intValue = 1 << LayerMask.NameToLayer("Trash");
        serializedCursor.ApplyModifiedPropertiesWithoutUndo();

        TMP_Text moneyText = FindSceneText("MONEY");
        SerializedObject serializedMoney = new SerializedObject(canvasObject.GetComponent<MoneyFlyVfxController>());
        serializedMoney.FindProperty("animationRoot").objectReferenceValue = animationRoot;
        serializedMoney.FindProperty("moneyTarget").objectReferenceValue =
            moneyText != null ? moneyText.rectTransform : null;
        serializedMoney.FindProperty("moneyIconPrefab").objectReferenceValue =
            moneyIconPrefab != null ? moneyIconPrefab.GetComponent<RectTransform>() : null;
        serializedMoney.FindProperty("duration").floatValue = 0.6f;
        serializedMoney.ApplyModifiedPropertiesWithoutUndo();

        SetLayerRecursively(canvasObject, LayerMask.NameToLayer("UI"));
    }

    private static TMP_Text FindSceneText(string objectName)
    {
        TMP_Text[] texts = Resources.FindObjectsOfTypeAll<TMP_Text>();

        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];

            if (text != null
                && text.gameObject.scene.IsValid()
                && text.gameObject.name == objectName)
            {
                return text;
            }
        }

        return null;
    }

    private static void ConfigureProgressBars()
    {
        ProgressBarController[] progressBars =
            Object.FindObjectsByType<ProgressBarController>(
                FindObjectsInactive.Include);

        for (int i = 0; i < progressBars.Length; i++)
        {
            ProgressBarController progressBar = progressBars[i];
            SerializedObject serializedBar = new SerializedObject(progressBar);
            serializedBar.FindProperty("transitionDuration").floatValue =
                progressBar.gameObject.name.Contains("Awareness") ? 0.55f : 0.4f;
            serializedBar.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(progressBar);
        }
    }

    private static void SetLayerRecursively(GameObject root, int layer)
    {
        root.layer = layer;

        for (int i = 0; i < root.transform.childCount; i++)
        {
            SetLayerRecursively(root.transform.GetChild(i).gameObject, layer);
        }
    }
}
