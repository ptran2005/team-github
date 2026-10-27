using UnityEngine;
using TMPro;
using System.Text;

// On-screen info: money, what you're holding, open orders, and short messages.
public class CafeHUD : MonoBehaviour
{
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private TMP_Text heldText;
    [SerializeField] private TMP_Text ticketsText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private CanvasGroup messagePanel; // optional backdrop that fades with the message
    [SerializeField] private float messageTime = 2.5f;

    private static CafeHUD instance;
    private float messageTimer;
    private float ticketRefreshTimer;
    private readonly StringBuilder builder = new StringBuilder();

    // Anything can call this; it just logs if there's no HUD in the scene
    public static void ShowMessage(string message)
    {
        if (instance == null || instance.messageText == null)
        {
            Debug.Log(message);
            return;
        }
        instance.messageText.text = message;
        instance.SetMessageAlpha(1f);
        instance.messageTimer = instance.messageTime;
    }

    private void Awake()
    {
        instance = this;
        if (messageText != null) messageText.text = "";
        SetMessageAlpha(0f);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
        if (PlayerHands.Instance != null) PlayerHands.Instance.Changed -= RefreshHeld;
        if (CafeProgress.Instance != null) CafeProgress.Instance.Changed -= RefreshMoney;
        if (OrderManager.Instance != null) OrderManager.Instance.TicketsChanged -= RefreshTickets;
    }

    private void Start()
    {
        if (PlayerHands.Instance != null) PlayerHands.Instance.Changed += RefreshHeld;
        if (CafeProgress.Instance != null) CafeProgress.Instance.Changed += RefreshMoney;
        if (OrderManager.Instance != null) OrderManager.Instance.TicketsChanged += RefreshTickets;
        RefreshHeld();
        RefreshMoney();
        RefreshTickets();
    }

    private void Update()
    {
        if (messageTimer > 0f)
        {
            messageTimer -= Time.unscaledDeltaTime;
            SetMessageAlpha(Mathf.Clamp01(messageTimer / 0.5f));
        }

        // Patience keeps dropping, so redraw the tickets a few times a second
        ticketRefreshTimer -= Time.unscaledDeltaTime;
        if (ticketRefreshTimer <= 0f)
        {
            ticketRefreshTimer = 0.25f;
            RefreshTickets();
        }
    }

    private void SetMessageAlpha(float alpha)
    {
        if (messagePanel != null) messagePanel.alpha = alpha;
        else if (messageText != null) messageText.alpha = alpha;
    }

    private void RefreshHeld()
    {
        if (heldText == null) return;
        heldText.text = PlayerHands.Instance != null ? $"Holding: {PlayerHands.Instance.Description}" : "";
    }

    private void RefreshMoney()
    {
        if (moneyText == null) return;
        SaveData data = CafeProgress.Instance != null ? CafeProgress.Instance.Data : null;
        moneyText.text = data != null ? $"${data.money}   Day {data.day}" : "";
    }

    private void RefreshTickets()
    {
        if (ticketsText == null) return;
        OrderManager orders = OrderManager.Instance;
        if (orders == null || orders.Tickets.Count == 0)
        {
            ticketsText.text = "No orders yet";
            return;
        }

        builder.Clear();
        builder.Append("<b>Orders</b>");
        foreach (OrderManager.Ticket ticket in orders.Tickets)
        {
            if (ticket.customer == null) continue;
            int patience = Mathf.RoundToInt(ticket.customer.PatiencePercent * 100f);
            builder.Append($"\n#{ticket.number} {ticket.customer.name}: {ticket.recipe.DisplayName}  ({patience}%)");
        }
        ticketsText.text = builder.ToString();
    }
}
