using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CustomerSpawner : MonoBehaviour
{
    [SerializeField] private GameObject customerPrefab;
    [SerializeField] private CustomerData[] possibleCustomers;
    [SerializeField] private Transform entrance;
    [SerializeField] private Transform[] lineSlots;     // slot 0 = at the register
    [SerializeField] private Transform[] waitingSpots;  // slot 0 = front of the waiting area
    [SerializeField] private Transform exit;
    [SerializeField] private float delayBetweenCustomers = 3f;
    [SerializeField] private int maxCustomers = 3;

    private readonly List<Customer> line = new List<Customer>();
    private readonly List<Customer> waiting = new List<Customer>();

    public Customer Front => line.Count > 0 ? line[0] : null;
    public int LineCount => line.Count;
    private int TotalCustomers => line.Count + waiting.Count;

    private void Start()
    {
        StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitUntil(() => TotalCustomers < maxCustomers && line.Count < lineSlots.Length);
            yield return new WaitForSeconds(delayBetweenCustomers);
            SpawnCustomer();
        }
    }

    [ContextMenu("Spawn Customer Now")]
    private void SpawnCustomer()
    {
        if (TotalCustomers >= maxCustomers || line.Count >= lineSlots.Length) return;
        if (possibleCustomers.Length == 0 || customerPrefab == null) return;
        if (entrance == null || exit == null) return;

        CustomerData data = possibleCustomers[Random.Range(0, possibleCustomers.Length)];
        GameObject obj = Instantiate(customerPrefab, entrance.position, entrance.rotation);

        Customer customer = obj.GetComponent<Customer>();
        customer.Initialize(data);
        customer.OrderConfirmed += HandleOrderConfirmed;
        customer.Expired += HandleExpired;
        customer.Left += HandleLeft;

        line.Add(customer);
        int slot = line.Count - 1;
        customer.MoveTo(lineSlots[slot].position, slot == 0 ? CustomerState.AtRegister : CustomerState.InLine);
    }

    private void AdvanceLine()
    {
        for (int i = 0; i < line.Count; i++)
            line[i].MoveTo(lineSlots[i].position, i == 0 ? CustomerState.AtRegister : CustomerState.InLine);
    }

    private void AdvanceWaiting()
    {
        for (int i = 0; i < waiting.Count; i++)
        {
            int spot = Mathf.Min(i, waitingSpots.Length - 1);
            Debug.Log($"{waiting[i].name} -> waiting spot {spot}");
            waiting[i].MoveTo(waitingSpots[spot].position, CustomerState.WaitingForFood);
        }
    }

    private void ReleaseWaitingSpot(Customer customer)
    {
        if (waiting.Remove(customer))
            AdvanceWaiting();
    }

    private void HandleOrderConfirmed(Customer customer)
    {
        line.Remove(customer);

        waiting.Add(customer);
        if (waiting.Count > waitingSpots.Length)
            Debug.LogWarning("More waiting customers than waiting spots, they will overlap.");
        AdvanceWaiting();

        AdvanceLine();
    }

    private void HandleExpired(Customer customer)
    {
        ReleaseWaitingSpot(customer);
        customer.WalkOut(exit.position);
    }

    private void HandleLeft(Customer customer)
    {
        customer.OrderConfirmed -= HandleOrderConfirmed;
        customer.Expired -= HandleExpired;
        customer.Left -= HandleLeft;
        ReleaseWaitingSpot(customer);
        line.Remove(customer);
    }

    // The serving system calls this after a correct dish is delivered
    public void ServeCustomer(Customer customer)
    {
        if (customer != null && customer.State == CustomerState.WaitingForFood)
        {
            ReleaseWaitingSpot(customer);
            customer.WalkOut(exit.position);
        }
    }

    // --- Test helpers ---
    [ContextMenu("Test: Start Ordering (front of line)")]
    private void TestStartOrdering() { if (Front != null) Front.StartOrdering(); }

    [ContextMenu("Test: Place Order (front of line)")]
    private void TestPlaceOrder() { if (Front != null) Front.OrderPlaced(); }

    [ContextMenu("Test: Serve Oldest Waiting Customer")]
    private void TestServe()
    {
        if (waiting.Count > 0) ServeCustomer(waiting[0]);
    }

    private void OnDrawGizmos()
    {
        DrawPoints(lineSlots, Color.green);
        DrawPoints(waitingSpots, Color.cyan);
        if (entrance != null) { Gizmos.color = Color.yellow; Gizmos.DrawSphere(entrance.position, 0.25f); }
        if (exit != null) { Gizmos.color = Color.red; Gizmos.DrawSphere(exit.position, 0.25f); }
    }

    private void DrawPoints(Transform[] points, Color color)
    {
        if (points == null) return;
        Gizmos.color = color;
        foreach (var p in points)
            if (p != null) Gizmos.DrawSphere(p.position, 0.2f);
    }
}