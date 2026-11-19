using UnityEngine;

public class GameStart : MonoBehaviour
{
    [SerializeField] private SpriteRenderer customerPrefab;
    [SerializeField] private Transform customerSpawn;
    [SerializeField] private CustomerData firstCustomer;
    [Header("Five Minute Clock")]
    [SerializeField] private FiveMinuteClock fiveMinuteClock;

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

        //Start the clock for the day
         if (fiveMinuteClock != null)
        {
            fiveMinuteClock.StartClock();
        }
        else
        {
            Debug.LogError("Five Minute Clock is not assigned!");
        }
    }
}