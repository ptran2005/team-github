using UnityEngine;

[CreateAssetMenu(menuName = "Jungle Cafe/Customer")]
public class CustomerData : ScriptableObject
{
    public string customerName;
    public string animalType;
    public Sprite customerSprite;
    public FoodData[] preferredFoods;
}