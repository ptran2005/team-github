using UnityEngine;

// In the Serve view, click a waiting customer to hand them the food you're holding.
// Pays the recipe price, plus a tip for cooking it right and serving it fast.
public class ServeWindow : MonoBehaviour
{
    [SerializeField] private CustomerSpawner spawner;
    [Tooltip("Tip as a share of the price when served with a full patience bar")]
    [SerializeField] private float maxTipPercent = 0.5f;
    [Tooltip("Share of the price paid for raw or burnt food")]
    [SerializeField] private float badFoodPayPercent = 0.5f;

    private ClickInput clicks;

    public event System.Action<Customer, int> CustomerServed; // customer, money earned

    private void Start()
    {
        if (spawner == null) spawner = FindFirstObjectByType<CustomerSpawner>();
        clicks = ClickInput.Instance;
        if (clicks != null) clicks.CustomerClicked += HandleCustomerClicked;
    }

    private void OnDestroy()
    {
        if (clicks != null) clicks.CustomerClicked -= HandleCustomerClicked;
    }

    private void HandleCustomerClicked(Customer customer)
    {
        if (StationCameraController.Instance != null && StationCameraController.Instance.Current != Station.Serve) return;
        if (customer.State != CustomerState.WaitingForFood) return;

        OrderManager.Ticket ticket = OrderManager.Instance != null ? OrderManager.Instance.GetTicket(customer) : null;
        if (ticket == null) return;

        PlayerHands hands = PlayerHands.Instance;
        if (hands == null || hands.IsEmpty)
        {
            CafeHUD.ShowMessage($"{customer.name} is waiting for a {ticket.recipe.DisplayName}.");
            return;
        }
        if (hands.Recipe != ticket.recipe)
        {
            SpeechBubbles.Show(customer, $"That's not what I ordered! I want a {ticket.recipe.DisplayName}.");
            return;
        }

        Serve(customer, ticket, hands);
    }

    private void Serve(Customer customer, OrderManager.Ticket ticket, PlayerHands hands)
    {
        RecipeData recipe = ticket.recipe;
        int earned;
        string reaction;

        if (hands.CookLevel == CookLevel.Cooked)
        {
            int tip = Mathf.RoundToInt(recipe.price * maxTipPercent * customer.PatiencePercent);
            earned = recipe.price + tip;
            reaction = tip > 0 ? $"Yum! Here's a ${tip} tip!" : "Thanks!";
        }
        else
        {
            earned = Mathf.RoundToInt(recipe.price * badFoodPayPercent);
            reaction = hands.CookLevel == CookLevel.Raw ? "Ew... this is raw." : "This is burnt...";
        }

        hands.Clear();
        OrderManager.Instance.CloseTicket(customer);
        SpeechBubbles.Show(customer, reaction);
        CafeHUD.ShowMessage($"+${earned}");
        if (CafeProgress.Instance != null) CafeProgress.Instance.RecordCustomerServed(earned);
        CustomerServed?.Invoke(customer, earned);

        if (spawner != null) spawner.ServeCustomer(customer);
        else Debug.LogWarning("ServeWindow has no CustomerSpawner, so the customer won't walk away.", this);
    }
}
