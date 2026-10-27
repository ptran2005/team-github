using UnityEngine;
using System;

// The one food item the player is carrying between stations.
public class PlayerHands : MonoBehaviour
{
    public static PlayerHands Instance { get; private set; }

    public RecipeData Recipe { get; private set; }
    public CookLevel CookLevel { get; private set; }
    public bool IsEmpty => Recipe == null;
    public string Description => IsEmpty ? "Nothing" : Recipe.Describe(CookLevel);

    public event Action Changed;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public bool PickUp(RecipeData recipe, CookLevel level)
    {
        if (!IsEmpty || recipe == null) return false;
        Recipe = recipe;
        CookLevel = level;
        Changed?.Invoke();
        return true;
    }

    public void Clear()
    {
        if (IsEmpty) return;
        Recipe = null;
        Changed?.Invoke();
    }

    // Hooked up to the HUD's Throw Away button
    public void Discard()
    {
        if (IsEmpty) return;
        CafeHUD.ShowMessage($"Threw away the {Description}");
        Clear();
    }
}
