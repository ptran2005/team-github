using UnityEngine;

// How to make one menu item: which models to show and how long it takes on the grill.
[CreateAssetMenu(menuName = "Jungle Cafe/Recipe")]
public class RecipeData : ScriptableObject
{
    [Tooltip("The menu item customers order")]
    public FoodData food;

    [Header("Models")]
    public GameObject rawModel;
    [Tooltip("Leave empty to keep showing the raw model once it's cooked")]
    public GameObject cookedModel;

    [Header("Cooking")]
    [Tooltip("Off for things that are ready straight from the fridge, like bananas")]
    public bool needsCooking = true;
    [Tooltip("Seconds on the grill until it's cooked")]
    public float cookTime = 6f;
    [Tooltip("Seconds on the grill until it burns")]
    public float burnTime = 14f;

    [Header("Money")]
    public int price = 10;

    public string DisplayName => food != null && !string.IsNullOrWhiteSpace(food.foodName) ? food.foodName : name;

    public CookLevel LevelAfter(float secondsOnGrill)
    {
        if (secondsOnGrill >= burnTime) return CookLevel.Burnt;
        if (secondsOnGrill >= cookTime) return CookLevel.Cooked;
        return CookLevel.Raw;
    }

    public string Describe(CookLevel level)
    {
        if (!needsCooking) return DisplayName;
        switch (level)
        {
            case CookLevel.Raw: return $"Raw {DisplayName}";
            case CookLevel.Burnt: return $"Burnt {DisplayName}";
            default: return $"Cooked {DisplayName}";
        }
    }
}

public enum CookLevel { Raw, Cooked, Burnt }
