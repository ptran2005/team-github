using UnityEngine;

public class Customer : MonoBehaviour
{
    [Header("Patience")]
    [SerializeField] private float maxPatience = 30f;
    private float currentPatience;
    private bool isActive = true;

    public CustomerData Data { get; private set; }
    public float PatienceRemaining => currentPatience;
    public float PatiencePercent => currentPatience / maxPatience;

    public event System.Action<Customer> Expired;
    public event System.Action<Customer> Left;
    private bool hasLeft;

    private void Awake()
    {
        currentPatience = maxPatience;
    }

    // Called by the spawner right after the customer is created
    public void Initialize(CustomerData data)
{
    Data = data;
    gameObject.name = data.customerName;
}

    private void Update()
    {
        if (!isActive) return;

        currentPatience -= Time.deltaTime;

        if (currentPatience <= 0f)
        {
            currentPatience = 0f;
            isActive = false;
            OnPatienceExpired();
        }
    }

    private void OnPatienceExpired()
    {
        Debug.Log($"{Data.customerName} the {Data.animalType} ran out of patience!");
        Expired?.Invoke(this);
        Leave(); //change this once we have a mood/game over system
    }

    [ContextMenu("Leave")]
    public void Leave()
    {
        if (hasLeft) return;
        hasLeft = true;
        isActive = false;
        Left?.Invoke(this);
        Destroy(gameObject);
    }

    public void Deactivate()
    {
        isActive = false;
    }
}