using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using TMPro;
using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;

// Menu: Jungle Cafe > Set Up Stations In Open Scene
// Adds the station cameras, grill slots, fridge items, order/serve systems, save system and HUD
// to whatever scene is open. It finds the register, bell, grill and fridge by their names.
// Safe to run again: anything it already made is left alone, so camera points you've tuned stay put.
public static class JungleCafeSetup
{
    private const string SystemsName = "Cafe Systems";
    private const string CameraPointsName = "Camera Points";
    private const string GrillSlotsName = "Grill Slots";
    private const string HudName = "Cafe HUD";
    private const string RecipeFolder = "Assets/Data/Recipes";
    private const string FoodFolder = "Assets/Data/Foods";

    private static readonly Color ButtonColor = new Color(1f, 0.97f, 0.9f);
    private static readonly Color ButtonTextColor = new Color(0.25f, 0.15f, 0.05f);
    private static readonly Color PanelColor = new Color(0f, 0f, 0f, 0.55f);

    private static List<string> notes;

    [MenuItem("Jungle Cafe/Set Up Stations In Open Scene")]
    public static void SetUpStations()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Jungle Cafe", "Stop Play mode first.", "OK");
            return;
        }

        Camera cam = Camera.main != null ? Camera.main : Object.FindFirstObjectByType<Camera>();
        if (cam == null)
        {
            EditorUtility.DisplayDialog("Jungle Cafe", "This scene has no camera. Add one first.", "OK");
            return;
        }

        notes = new List<string>();

        RecipeData steak = GetOrCreateRecipe("Steak", "Assets/Models/rawsteak.fbx", "Assets/Models/cookedsteak.fbx", true, 12);
        RecipeData banana = GetOrCreateRecipe("Banana", "Assets/Models/banana.fbx", null, false, 5);

        GameObject systems = FindRoot(SystemsName) ?? CreateRoot(SystemsName);
        var cameras = GetOrAdd<StationCameraController>(systems);
        GetOrAdd<ClickInput>(systems);
        PlayerHands hands = GetOrAdd<PlayerHands>(systems);
        OrderManager orders = GetOrAdd<OrderManager>(systems);
        GetOrAdd<OrderTaker>(systems);
        ServeWindow serve = GetOrAdd<ServeWindow>(systems);
        CafeProgress progress = GetOrAdd<CafeProgress>(systems);

        SetRef(cameras, "targetCamera", cam);
        SetArray(orders, "menu", new Object[] { steak, banana });
        var spawner = Object.FindFirstObjectByType<CustomerSpawner>();
        if (spawner != null) SetRef(serve, "spawner", spawner);
        else notes.Add("No CustomerSpawner in this scene, so no customers will show up.");

        SetUpCameraPoints(cam, cameras, spawner);
        SetUpGrill();
        SetUpFridge(steak, banana);
        SetUpHud(hands, progress);
        SetUpEventSystem();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = systems;

        string summary = "Stations are set up. Press Play to try it, and save the scene to keep it.";
        if (notes.Count > 0) summary += "\n\nCheck these:\n- " + string.Join("\n- ", notes);
        Debug.Log("[Jungle Cafe] " + summary.Replace("\n", " "));
        if (!Application.isBatchMode) EditorUtility.DisplayDialog("Jungle Cafe", summary, "OK");
    }

    // ---------- Camera points ----------

    private static void SetUpCameraPoints(Camera cam, StationCameraController cameras, CustomerSpawner spawner)
    {
        Transform register = FindInScene("register");
        Transform bell = FindInScene("bell");
        Transform grill = FindInScene("grill");
        Transform fridge = FindInScene("fridge");
        Transform stand = FindInScene("tempstand", "Jungle_Cafe_Stand", "stand");

        // One "unit" = the gap between the register and the bell, so views scale with the stand
        float unit = 2.7f;
        if (register != null && bell != null)
            unit = Mathf.Clamp(Vector3.Distance(Center(register), Center(bell)), 0.5f, 50f);

        // Which way is outside: the way the customer line runs, or else from the stand out to the register
        Vector3 outward = Vector3.zero;
        Transform firstLineSlot = spawner != null ? GetArrayRef(spawner, "lineSlots", 0) as Transform : null;
        Transform lastLineSlot = spawner != null ? GetArrayRef(spawner, "lineSlots", 2) as Transform : null;
        if (firstLineSlot != null && lastLineSlot != null) outward = Flat(lastLineSlot.position - firstLineSlot.position);
        if (outward == Vector3.zero && firstLineSlot != null && register != null) outward = Flat(firstLineSlot.position - Center(register));
        if (outward == Vector3.zero && register != null && stand != null) outward = Flat(Center(register) - Center(stand));
        if (outward == Vector3.zero) outward = Flat(cam.transform.forward);
        if (outward == Vector3.zero) outward = Vector3.forward;

        Vector3 standCenter = stand != null ? Center(stand) : (register != null ? Center(register) : cam.transform.position);

        Transform root = (FindRoot(CameraPointsName) ?? CreateRoot(CameraPointsName)).transform;
        var points = new List<(Station, Transform)>
        {
            (Station.Order, GetOrCreatePoint(root, Station.Order, register, cam, p => WindowView(p, Center(register), outward, unit))),
            (Station.Serve, GetOrCreatePoint(root, Station.Serve, bell, cam, p => WindowView(p, Center(bell), outward, unit))),
            // Cook looks down at the grill top; Fridge looks straight at the shelves
            (Station.Cook, GetOrCreatePoint(root, Station.Cook, grill, cam, p => ApplianceView(p, Bounds(grill), true, standCenter, outward, unit))),
            (Station.Fridge, GetOrCreatePoint(root, Station.Fridge, fridge, cam, p => ApplianceView(p, FridgeBounds(fridge), false, standCenter, outward, unit))),
        };

        var so = new SerializedObject(cameras);
        SerializedProperty views = so.FindProperty("views");
        views.arraySize = points.Count;
        for (int i = 0; i < points.Count; i++)
        {
            SerializedProperty view = views.GetArrayElementAtIndex(i);
            view.FindPropertyRelative("station").enumValueIndex = (int)points[i].Item1;
            view.FindPropertyRelative("viewPoint").objectReferenceValue = points[i].Item2;
        }
        so.ApplyModifiedProperties();

        // Start the scene looking through the Order window, like the game does
        Transform orderPoint = points[0].Item2;
        Undo.RecordObject(cam.transform, "Move camera to Order view");
        cam.transform.SetPositionAndRotation(orderPoint.position, orderPoint.rotation);
    }

    private static Transform GetOrCreatePoint(Transform root, Station station, Transform anchor, Camera cam, Action<Transform> place)
    {
        string pointName = $"CamPoint_{station}";
        Transform point = root.Find(pointName);
        if (point != null) return point; // already there, maybe hand-tuned

        point = new GameObject(pointName).transform;
        point.SetParent(root, false);
        Undo.RegisterCreatedObjectUndo(point.gameObject, "Create camera point");

        if (anchor != null)
        {
            place(point);
            notes.Add($"{pointName} was placed automatically. Fine-tune it: select it, move the Scene view, then GameObject > Align With View.");
        }
        else
        {
            point.SetPositionAndRotation(cam.transform.position, cam.transform.rotation);
            notes.Add($"Couldn't find the {station} station's model, so {pointName} is at the camera. Move it by hand.");
        }
        return point;
    }

    // Behind the counter looking out through the window at the customers
    private static void WindowView(Transform point, Vector3 counter, Vector3 outward, float unit)
    {
        Vector3 position = counter - outward * (1.85f * unit) + Vector3.up * (0.7f * unit);
        Vector3 lookAt = counter + outward * unit + Vector3.up * (0.55f * unit);
        point.SetPositionAndRotation(position, Quaternion.LookRotation(lookAt - position));
    }

    // From the middle of the stand, looking at the appliance (from above when lookAtTop is on)
    private static void ApplianceView(Transform point, Bounds target, bool lookAtTop, Vector3 standCenter, Vector3 outward, float unit)
    {
        Vector3 viewDirection = Flat(target.center - standCenter);
        if (viewDirection == Vector3.zero) viewDirection = -outward;

        float size = Mathf.Max(target.size.x, target.size.y, target.size.z);
        Vector3 lookAt = lookAtTop ? new Vector3(target.center.x, target.max.y, target.center.z) : target.center;
        float distance = Mathf.Max(unit, size * (lookAtTop ? 0.95f : 1.6f));
        Vector3 position = lookAt - viewDirection * distance + Vector3.up * (size * (lookAtTop ? 0.75f : 0.5f));
        point.SetPositionAndRotation(position, Quaternion.LookRotation(lookAt - position));
    }

    // ---------- Grill ----------

    private static void SetUpGrill()
    {
        Transform grill = FindInScene("grill");
        if (grill == null)
        {
            notes.Add("Couldn't find a 'grill' object, so there are no grill slots.");
            return;
        }

        Transform root = (FindRoot(GrillSlotsName) ?? CreateRoot(GrillSlotsName)).transform;
        if (root.childCount > 0) return;

        Bounds bounds = Bounds(grill);
        bool alongX = bounds.size.x >= bounds.size.z;
        float length = alongX ? bounds.size.x : bounds.size.z;
        float width = alongX ? bounds.size.z : bounds.size.x;
        float foodSize = Mathf.Min(length * 0.36f, width * 0.7f);
        Vector3 axis = alongX ? Vector3.right : Vector3.forward;
        Vector3 top = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);

        for (int i = 0; i < 2; i++)
        {
            var slot = new GameObject($"Grill Slot {i + 1}");
            slot.transform.SetParent(root, false);
            Vector3 position = top + axis * ((i == 0 ? -0.22f : 0.22f) * length);
            position.y = SurfaceHeight(grill, position, bounds.max.y);
            slot.transform.position = position;

            var box = slot.AddComponent<BoxCollider>();
            box.size = alongX
                ? new Vector3(length * 0.4f, foodSize, width * 0.8f)
                : new Vector3(width * 0.8f, foodSize, length * 0.4f);
            box.center = Vector3.up * (foodSize * 0.5f);

            GrillSlot grillSlot = slot.AddComponent<GrillSlot>();
            SetEnum(grillSlot, "station", (int)Station.Cook);
            SetFloat(grillSlot, "foodSize", foodSize);
            SetFloat(grillSlot, "labelHeight", foodSize * 0.9f);
        }
        notes.Add("If food floats above or sinks into the grill, move the Grill Slots object up or down.");
    }

    // Height of the grill's cooking surface under a point. The model has no colliders,
    // so add temporary mesh colliders, raycast down onto them, then remove them again.
    private static float SurfaceHeight(Transform grill, Vector3 point, float fallback)
    {
        var added = new List<MeshCollider>();
        foreach (MeshFilter mesh in grill.GetComponentsInChildren<MeshFilter>())
            if (mesh.sharedMesh != null && mesh.GetComponent<Collider>() == null)
                added.Add(mesh.gameObject.AddComponent<MeshCollider>());
        Physics.SyncTransforms();

        float height = fallback;
        float closest = float.MaxValue;
        var from = new Vector3(point.x, fallback + 1f, point.z);
        foreach (RaycastHit hit in Physics.RaycastAll(from, Vector3.down, 100f))
        {
            if (hit.transform.IsChildOf(grill) && hit.distance < closest)
            {
                closest = hit.distance;
                height = hit.point.y;
            }
        }

        foreach (MeshCollider collider in added) Object.DestroyImmediate(collider);
        return height;
    }

    // ---------- Fridge ----------

    private static void SetUpFridge(RecipeData steak, RecipeData banana)
    {
        Transform fridge = FindInScene("fridge");
        MakeFridgeItem(FindInScene("rawsteak"), steak, fridge, 0);
        MakeFridgeItem(FindInScene("banana"), banana, fridge, 1);
    }

    private static void MakeFridgeItem(Transform item, RecipeData recipe, Transform fridge, int index)
    {
        if (item == null)
        {
            if (fridge == null || recipe.rawModel == null)
            {
                notes.Add($"No '{recipe.DisplayName}' or fridge in the scene, so you can't pick up {recipe.DisplayName}.");
                return;
            }

            // Put a copy of the model just in front of the fridge
            item = ((GameObject)PrefabUtility.InstantiatePrefab(recipe.rawModel)).transform;
            Undo.RegisterCreatedObjectUndo(item.gameObject, "Create fridge item");
            Bounds fridgeBounds = Bounds(fridge);
            float size = Mathf.Max(fridgeBounds.size.x, fridgeBounds.size.z) * 0.3f;
            Bounds itemBounds = Bounds(item);
            float largest = Mathf.Max(itemBounds.size.x, itemBounds.size.y, itemBounds.size.z);
            if (largest > 0.0001f) item.localScale *= size / largest;
            item.position = fridgeBounds.center + Vector3.right * ((index - 0.5f) * size * 1.4f);
            notes.Add($"Made a {recipe.DisplayName} by the fridge. Move it where it looks right.");
        }

        FridgeItem fridgeItem = GetOrAdd<FridgeItem>(item.gameObject);
        SetEnum(fridgeItem, "station", (int)Station.Fridge);
        SetRef(fridgeItem, "recipe", recipe);
    }

    private static Bounds FridgeBounds(Transform fridge)
    {
        // Frame the fridge together with the food sitting by it
        Bounds bounds = Bounds(fridge);
        foreach (string itemName in new[] { "rawsteak", "banana" })
        {
            Transform item = FindInScene(itemName);
            if (item != null && Vector3.Distance(Center(item), bounds.center) < bounds.size.magnitude * 1.5f)
                bounds.Encapsulate(Bounds(item));
        }
        return bounds;
    }

    // ---------- Recipes ----------

    private static RecipeData GetOrCreateRecipe(string dishName, string rawModelPath, string cookedModelPath, bool needsCooking, int price)
    {
        string path = $"{RecipeFolder}/{dishName}.asset";
        var recipe = AssetDatabase.LoadAssetAtPath<RecipeData>(path);
        if (recipe != null) return recipe;

        EnsureFolder(RecipeFolder);
        recipe = ScriptableObject.CreateInstance<RecipeData>();
        recipe.food = GetOrCreateFood(dishName);
        recipe.rawModel = AssetDatabase.LoadAssetAtPath<GameObject>(rawModelPath);
        recipe.cookedModel = cookedModelPath != null ? AssetDatabase.LoadAssetAtPath<GameObject>(cookedModelPath) : null;
        recipe.needsCooking = needsCooking;
        recipe.price = price;
        AssetDatabase.CreateAsset(recipe, path);
        AssetDatabase.SaveAssets();
        return recipe;
    }

    private static FoodData GetOrCreateFood(string foodName)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:FoodData"))
        {
            var food = AssetDatabase.LoadAssetAtPath<FoodData>(AssetDatabase.GUIDToAssetPath(guid));
            if (food != null && string.Equals(food.foodName, foodName, StringComparison.OrdinalIgnoreCase)) return food;
        }

        EnsureFolder(FoodFolder);
        var created = ScriptableObject.CreateInstance<FoodData>();
        created.foodName = foodName;
        AssetDatabase.CreateAsset(created, $"{FoodFolder}/{foodName}.asset");
        return created;
    }

    // ---------- HUD ----------

    private static void SetUpHud(PlayerHands hands, CafeProgress progress)
    {
        if (FindRoot(HudName) != null) return;

        GameObject canvasObj = CreateRoot(HudName);
        canvasObj.layer = LayerMask.NameToLayer("UI");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();
        Transform root = canvasObj.transform;

        // Station tabs along the bottom
        RectTransform tabs = NewUI("Station Tabs", root, typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        Place(tabs, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), Vector2.zero);
        var tabLayout = tabs.GetComponent<HorizontalLayoutGroup>();
        tabLayout.spacing = 14f;
        tabLayout.childControlWidth = tabLayout.childControlHeight = true;
        tabLayout.childForceExpandWidth = tabLayout.childForceExpandHeight = false;
        FitToContent(tabs);
        string[] tabNames = { "1  Order", "2  Serve", "3  Cook", "4  Fridge" };
        Station[] tabStations = { Station.Order, Station.Serve, Station.Cook, Station.Fridge };
        for (int i = 0; i < tabNames.Length; i++)
        {
            Button tab = NewButton($"{tabStations[i]} Tab", tabs, tabNames[i]);
            var size = tab.gameObject.AddComponent<LayoutElement>();
            size.preferredWidth = 210f;
            size.preferredHeight = 72f;
            SetEnum(tab.gameObject.AddComponent<StationTabButton>(), "station", (int)tabStations[i]);
        }

        // Money and held item, top left
        RectTransform info = NewPanel("Info Panel", root);
        Place(info, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -30f), Vector2.zero);
        TMP_Text money = NewText("Money", info, "$0   Day 1", 44f, new Color(1f, 0.85f, 0.3f));
        money.fontStyle = FontStyles.Bold;
        TMP_Text held = NewText("Holding", info, "Holding: Nothing", 30f, Color.white);

        // Order tickets, top right
        RectTransform ticketPanel = NewPanel("Tickets Panel", root);
        Place(ticketPanel, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -30f), Vector2.zero);
        TMP_Text tickets = NewText("Tickets", ticketPanel, "No orders yet", 28f, Color.white);

        // Messages, top middle
        RectTransform messagePanel = NewPanel("Message Panel", root);
        Place(messagePanel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), Vector2.zero);
        var messageGroup = messagePanel.gameObject.AddComponent<CanvasGroup>();
        messageGroup.blocksRaycasts = false;
        TMP_Text message = NewText("Message", messagePanel, "", 34f, Color.white);
        message.alignment = TextAlignmentOptions.Center;

        // Throw away (bottom left) and Save (bottom right)
        Button trash = NewButton("Throw Away Button", root, "Throw Away");
        Place((RectTransform)trash.transform, Vector2.zero, Vector2.zero, new Vector2(30f, 24f), new Vector2(230f, 72f));
        UnityEventTools.AddPersistentListener(trash.onClick, new UnityAction(hands.Discard));

        Button save = NewButton("Save Button", root, "Save");
        Place((RectTransform)save.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-30f, 24f), new Vector2(160f, 72f));
        UnityEventTools.AddPersistentListener(save.onClick, new UnityAction(progress.Save));

        CafeHUD hud = canvasObj.AddComponent<CafeHUD>();
        SetRef(hud, "moneyText", money);
        SetRef(hud, "heldText", held);
        SetRef(hud, "ticketsText", tickets);
        SetRef(hud, "messageText", message);
        SetRef(hud, "messagePanel", messageGroup);
        canvasObj.AddComponent<SpeechBubbles>();
    }

    private static void SetUpEventSystem()
    {
        // The project only has the new Input System turned on, so UI needs its input module
        EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            eventSystem = CreateRoot("EventSystem").AddComponent<EventSystem>();
        }

        foreach (BaseInputModule module in eventSystem.GetComponents<BaseInputModule>())
            if (!(module is InputSystemUIInputModule)) Undo.DestroyObjectImmediate(module);
        GetOrAdd<InputSystemUIInputModule>(eventSystem.gameObject);
    }

    private static RectTransform NewUI(string objName, Transform parent, params Type[] components)
    {
        var obj = new GameObject(objName, typeof(RectTransform));
        obj.layer = LayerMask.NameToLayer("UI");
        obj.transform.SetParent(parent, false);
        foreach (Type component in components) obj.AddComponent(component);
        return (RectTransform)obj.transform;
    }

    private static RectTransform NewPanel(string objName, Transform parent)
    {
        RectTransform panel = NewUI(objName, parent, typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        var image = panel.GetComponent<Image>();
        image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        image.type = Image.Type.Sliced;
        image.color = PanelColor;
        image.raycastTarget = false;
        var layout = panel.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 14, 14);
        layout.spacing = 6f;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;
        FitToContent(panel);
        return panel;
    }

    private static TMP_Text NewText(string objName, Transform parent, string text, float size, Color color)
    {
        RectTransform rect = NewUI(objName, parent);
        TextMeshProUGUI tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static Button NewButton(string objName, Transform parent, string label)
    {
        RectTransform rect = NewUI(objName, parent, typeof(Image), typeof(Button));
        var image = rect.GetComponent<Image>();
        image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        image.type = Image.Type.Sliced;
        image.color = ButtonColor;
        var button = rect.GetComponent<Button>();
        button.targetGraphic = image;

        TMP_Text text = NewText("Label", rect, label, 30f, ButtonTextColor);
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold;
        var textRect = (RectTransform)text.transform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = textRect.offsetMax = Vector2.zero;
        return button;
    }

    private static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        if (size != Vector2.zero) rect.sizeDelta = size;
    }

    private static void FitToContent(RectTransform rect)
    {
        var fitter = rect.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    // ---------- Scene helpers ----------

    private static GameObject FindRoot(string rootName)
    {
        foreach (GameObject obj in SceneManager.GetActiveScene().GetRootGameObjects())
            if (obj.name == rootName) return obj;
        return null;
    }

    private static GameObject CreateRoot(string rootName)
    {
        var obj = new GameObject(rootName);
        Undo.RegisterCreatedObjectUndo(obj, $"Create {rootName}");
        return obj;
    }

    // Finds a scene object by name: exact names first, then names that contain it; shallowest wins
    private static Transform FindInScene(params string[] names)
    {
        var all = new List<Transform>();
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name == SystemsName || root.name == CameraPointsName || root.name == GrillSlotsName || root.name == HudName) continue;
            all.AddRange(root.GetComponentsInChildren<Transform>(true));
        }

        foreach (bool exact in new[] { true, false })
            foreach (string wanted in names)
            {
                Transform best = null;
                int bestDepth = int.MaxValue;
                foreach (Transform t in all)
                {
                    bool match = exact
                        ? string.Equals(t.name, wanted, StringComparison.OrdinalIgnoreCase)
                        : t.name.IndexOf(wanted, StringComparison.OrdinalIgnoreCase) >= 0;
                    int depth = Depth(t);
                    if (match && depth < bestDepth)
                    {
                        best = t;
                        bestDepth = depth;
                    }
                }
                if (best != null) return best;
            }
        return null;
    }

    private static int Depth(Transform t)
    {
        int depth = 0;
        for (Transform p = t.parent; p != null; p = p.parent) depth++;
        return depth;
    }

    private static Bounds Bounds(Transform t)
    {
        Renderer[] renderers = t.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(t.position, Vector3.one * 0.5f);
        Bounds bounds = renderers[0].bounds;
        foreach (Renderer r in renderers) bounds.Encapsulate(r.bounds);
        return bounds;
    }

    private static Vector3 Center(Transform t) => Bounds(t).center;

    private static Vector3 Flat(Vector3 v)
    {
        v.y = 0f;
        return v.sqrMagnitude > 0.000001f ? v.normalized : Vector3.zero;
    }

    private static T GetOrAdd<T>(GameObject obj) where T : Component
    {
        T component = obj.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(obj);
    }

    private static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    // Private [SerializeField] fields can only be set through SerializedObject
    private static void SetRef(Object target, string property, Object value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(property).objectReferenceValue = value;
        so.ApplyModifiedProperties();
    }

    private static void SetEnum(Object target, string property, int value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(property).enumValueIndex = value;
        so.ApplyModifiedProperties();
    }

    private static void SetFloat(Object target, string property, float value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(property).floatValue = value;
        so.ApplyModifiedProperties();
    }

    private static void SetArray(Object target, string property, Object[] values)
    {
        var so = new SerializedObject(target);
        SerializedProperty array = so.FindProperty(property);
        array.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        so.ApplyModifiedProperties();
    }

    private static Object GetArrayRef(Object target, string property, int index)
    {
        SerializedProperty array = new SerializedObject(target).FindProperty(property);
        return array != null && array.isArray && index < array.arraySize ? array.GetArrayElementAtIndex(index).objectReferenceValue : null;
    }
}
