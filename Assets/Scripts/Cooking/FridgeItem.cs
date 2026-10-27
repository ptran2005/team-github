using UnityEngine;

// Click to take one of this ingredient out of the fridge. It never runs out.
public class FridgeItem : StationInteractable
{
    [SerializeField] private RecipeData recipe;

    private void Reset()
    {
        station = Station.Fridge;
    }

    public override void OnClicked()
    {
        PlayerHands hands = PlayerHands.Instance;
        if (hands == null || recipe == null) return;

        if (!hands.IsEmpty)
        {
            CafeHUD.ShowMessage($"Your hands are full! You're holding a {hands.Description}.");
            return;
        }

        hands.PickUp(recipe, recipe.needsCooking ? CookLevel.Raw : CookLevel.Cooked);
        CafeHUD.ShowMessage(recipe.needsCooking
            ? $"Got a {hands.Description}. Take it to the grill!"
            : $"Got a {hands.Description}. It's ready to serve!");
    }
}
