using UnityEngine;

public class GameStart : MonoBehaviour
{
    [SerializeField] private SpriteRenderer customerPrefab;
    [SerializeField] private Transform customerSpawn;
    [SerializeField] private CustomerData firstCustomer;

    private void Start()
    {
        if (customerPrefab == null ||
            customerSpawn == null ||
            firstCustomer == null)
        {
            Debug.LogError("Assign all GameStart fields in the Inspector.");
            return;
        }

        SpriteRenderer customer = Instantiate(
            customerPrefab,
            customerSpawn.position,
            customerSpawn.rotation
        );

        customer.sprite = firstCustomer.customerSprite;
        customer.gameObject.name = firstCustomer.customerName;
    }
}