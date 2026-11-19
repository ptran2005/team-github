using UnityEngine;
using TMPro;

// One spot on the grill. Put raw food on it, wait, and grab it before it burns.
public class GrillSlot : StationInteractable
{
    [SerializeField] private float foodSize = 0.3f; // the food model gets scaled to about this many units across
    [SerializeField] private float labelHeight = 0.3f;
    [SerializeField] private Color burntTint = new Color(0.2f, 0.12f, 0.08f);

    private RecipeData recipe;
    private float cookTimer;
    private CookLevel shownLevel;
    private GameObject foodModel;
    private TextMeshPro label;

    public bool IsEmpty => recipe == null;
    public CookLevel Level => recipe != null ? recipe.LevelAfter(cookTimer) : CookLevel.Raw;

    private void Reset()
    {
        station = Station.Cook;
    }

    protected override void Awake()
    {
        base.Awake();
        label = CreateLabel();
        label.gameObject.SetActive(false);
    }

    public override void OnClicked()
    {
        PlayerHands hands = PlayerHands.Instance;
        if (hands == null) return;

        if (IsEmpty) PutDown(hands);
        else PickUp(hands);
    }

    private void PutDown(PlayerHands hands)
    {
        if (hands.IsEmpty)
        {
            CafeHUD.ShowMessage("Grab something from the fridge first.");
            return;
        }
        if (!hands.Recipe.needsCooking)
        {
            CafeHUD.ShowMessage($"The {hands.Recipe.DisplayName} doesn't need cooking. Serve it!");
            return;
        }
        if (hands.CookLevel == CookLevel.Burnt)
        {
            CafeHUD.ShowMessage("That's already burnt. Throw it away.");
            return;
        }

        recipe = hands.Recipe;
        cookTimer = hands.CookLevel == CookLevel.Cooked ? recipe.cookTime : 0f;
        hands.Clear();
        ShowFood(Level);
        label.gameObject.SetActive(true);
    }

    private void PickUp(PlayerHands hands)
    {
        if (!hands.IsEmpty)
        {
            CafeHUD.ShowMessage($"Your hands are full! You're holding a {hands.Description}.");
            return;
        }

        hands.PickUp(recipe, Level);
        CafeHUD.ShowMessage($"Picked up a {hands.Description}.");
        recipe = null;
        cookTimer = 0f;
        if (foodModel != null) Destroy(foodModel);
        label.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (IsEmpty) return;

        cookTimer += Time.deltaTime;
        CookLevel level = Level;
        if (level != shownLevel) ShowFood(level);
        UpdateLabel(level);
    }

    private void ShowFood(CookLevel level)
    {
        shownLevel = level;
        if (foodModel != null) Destroy(foodModel);

        GameObject model = level != CookLevel.Raw && recipe.cookedModel != null ? recipe.cookedModel : recipe.rawModel;
        if (model == null) return;

        foodModel = Instantiate(model, transform);
        foreach (Collider c in foodModel.GetComponentsInChildren<Collider>()) Destroy(c); // the slot handles clicks
        FitOnSlot(foodModel);
        if (level == CookLevel.Burnt) Tint(foodModel, burntTint);
    }

    private void FitOnSlot(GameObject model)
    {
        model.transform.localPosition = Vector3.zero;
        Bounds bounds = WorldBounds(model);
        float largest = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        if (largest > 0.0001f) model.transform.localScale *= foodSize / largest;

        // Sit it on top of the slot instead of halfway through the grill
        bounds = WorldBounds(model);
        model.transform.position += new Vector3(
            transform.position.x - bounds.center.x,
            transform.position.y - bounds.min.y,
            transform.position.z - bounds.center.z);
    }

    private static Bounds WorldBounds(GameObject model)
    {
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(model.transform.position, Vector3.zero);
        Bounds bounds = renderers[0].bounds;
        foreach (Renderer r in renderers) bounds.Encapsulate(r.bounds);
        return bounds;
    }

    private static void Tint(GameObject model, Color tint)
    {
        var block = new MaterialPropertyBlock();
        foreach (Renderer r in model.GetComponentsInChildren<Renderer>())
        {
            Material mat = r.sharedMaterial;
            Color baseColor = mat != null && mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : Color.white;
            r.GetPropertyBlock(block);
            block.SetColor("_BaseColor", baseColor * tint);
            block.SetColor("_Color", baseColor * tint);
            r.SetPropertyBlock(block);
        }
    }

    private void UpdateLabel(CookLevel level)
    {
        switch (level)
        {
            case CookLevel.Raw:
                label.text = $"Cooking... {Mathf.FloorToInt(100f * cookTimer / recipe.cookTime)}%";
                label.color = new Color(1f, 0.85f, 0.3f);
                break;
            case CookLevel.Cooked:
                label.text = $"Done! Burns in {Mathf.CeilToInt(recipe.burnTime - cookTimer)}";
                label.color = new Color(0.45f, 1f, 0.45f);
                break;
            default:
                label.text = "Burnt!";
                label.color = new Color(1f, 0.35f, 0.3f);
                break;
        }

        // Face the camera so it's readable from the Cook view
        Camera cam = StationCameraController.Instance != null ? StationCameraController.Instance.Camera : Camera.main;
        if (cam != null) label.transform.rotation = cam.transform.rotation;
    }

    private TextMeshPro CreateLabel()
    {
        var obj = new GameObject("Cook Label");
        obj.transform.SetParent(transform, false);
        obj.transform.localPosition = Vector3.up * labelHeight;

        TextMeshPro text = obj.AddComponent<TextMeshPro>();
        text.fontSize = foodSize * 2f;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.outlineWidth = 0.2f;
        text.outlineColor = Color.black;
        text.rectTransform.sizeDelta = new Vector2(foodSize * 6f, foodSize);
        return text;
    }
}
