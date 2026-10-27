using UnityEngine;
using System.Collections;

// In the Order view, click the customer at the register to take their order.
// Stand-in until the real ordering UI is hooked up; that just needs to call
// OrderManager.Instance.AddTicket(customer, recipe) and then customer.OrderPlaced().
public class OrderTaker : MonoBehaviour
{
    [SerializeField] private float orderTalkTime = 1.5f;

    private ClickInput clicks;

    private void Start()
    {
        clicks = ClickInput.Instance;
        if (clicks != null) clicks.CustomerClicked += HandleCustomerClicked;
    }

    private void OnDestroy()
    {
        if (clicks != null) clicks.CustomerClicked -= HandleCustomerClicked;
    }

    private void HandleCustomerClicked(Customer customer)
    {
        if (StationCameraController.Instance != null && StationCameraController.Instance.Current != Station.Order) return;

        if (customer.State == CustomerState.InLine || customer.State == CustomerState.Entering)
            CafeHUD.ShowMessage("Take the order of the customer at the register first.");
        else if (customer.State == CustomerState.AtRegister)
            StartCoroutine(TakeOrder(customer));
    }

    private IEnumerator TakeOrder(Customer customer)
    {
        OrderManager orders = OrderManager.Instance;
        RecipeData recipe = orders != null ? orders.ChooseRecipeFor(customer) : null;
        if (recipe == null)
        {
            Debug.LogWarning("OrderManager has no recipes on its menu, so customers can't order anything.", this);
            yield break;
        }

        customer.StartOrdering();
        SpeechBubbles.Show(customer, $"One {recipe.DisplayName}, please!", orderTalkTime + 1.5f);

        yield return new WaitForSeconds(orderTalkTime);
        if (customer == null || customer.State != CustomerState.Ordering) yield break;

        orders.AddTicket(customer, recipe);
        customer.OrderPlaced(); // the spawner walks them over to the serve window
        CafeHUD.ShowMessage($"New order: {recipe.DisplayName}");
    }
}
