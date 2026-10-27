using UnityEngine;
using System;
using System.Collections.Generic;

// Keeps an order ticket for each customer until they're served or walk out.
// Whatever takes the order (OrderTaker for now) calls AddTicket; ServeWindow checks tickets.
public class OrderManager : MonoBehaviour
{
    public class Ticket
    {
        public int number;
        public Customer customer;
        public RecipeData recipe;
    }

    [Tooltip("Everything customers can order")]
    [SerializeField] private RecipeData[] menu;

    private readonly List<Ticket> tickets = new List<Ticket>();
    private int nextTicketNumber = 1;

    public static OrderManager Instance { get; private set; }
    public IReadOnlyList<Ticket> Tickets => tickets;

    public event Action TicketsChanged;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // Picks one of the customer's favorite foods if any are on the menu, otherwise anything on the menu
    public RecipeData ChooseRecipeFor(Customer customer)
    {
        if (menu == null || menu.Length == 0) return null;

        var favorites = new List<RecipeData>();
        FoodData[] preferred = customer != null && customer.Data != null ? customer.Data.preferredFoods : null;
        if (preferred != null)
            foreach (RecipeData recipe in menu)
                if (recipe != null && Array.IndexOf(preferred, recipe.food) >= 0) favorites.Add(recipe);

        if (favorites.Count > 0) return favorites[UnityEngine.Random.Range(0, favorites.Count)];
        return menu[UnityEngine.Random.Range(0, menu.Length)];
    }

    public Ticket AddTicket(Customer customer, RecipeData recipe)
    {
        CloseTicket(customer);

        var ticket = new Ticket { number = nextTicketNumber++, customer = customer, recipe = recipe };
        tickets.Add(ticket);
        customer.Expired += HandleExpired;
        customer.Left += CloseTicket;
        TicketsChanged?.Invoke();
        return ticket;
    }

    public Ticket GetTicket(Customer customer)
    {
        return tickets.Find(t => t.customer == customer);
    }

    public void CloseTicket(Customer customer)
    {
        Ticket ticket = GetTicket(customer);
        if (ticket == null) return;

        tickets.Remove(ticket);
        customer.Expired -= HandleExpired;
        customer.Left -= CloseTicket;
        TicketsChanged?.Invoke();
    }

    private void HandleExpired(Customer customer)
    {
        SpeechBubbles.Show(customer, "I've waited too long!");
        if (CafeProgress.Instance != null) CafeProgress.Instance.RecordCustomerLost();
        CloseTicket(customer);
    }
}
