using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CustomerSpawner : MonoBehaviour
{
    [SerializeField] private GameObject customerPrefab;
    [SerializeField] private CustomerData[] possibleCustomers;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float minSpawnInterval = 5f;
    [SerializeField] private float maxSpawnInterval = 12f;

    [SerializeField] private int maxCustomers = 3;
    private int currentCount;

    private readonly Dictionary<Customer, Transform> occupied = new Dictionary<Customer, Transform>();

    private void Start()
    {
        StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minSpawnInterval, maxSpawnInterval));
            SpawnCustomer();
        }
    }

    [ContextMenu("Spawn Customer Now")]
    private void SpawnCustomer()
    {
        // clean up destroyed customers
        var dead = new List<Customer>();
        foreach (var kv in occupied)
            if (kv.Key == null) dead.Add(kv.Key);
        foreach (var d in dead) occupied.Remove(d);

        if (occupied.Count >= maxCustomers) return;
        if (spawnPoints.Length == 0 || possibleCustomers.Length == 0 || customerPrefab == null) return;

        // find free spawn points
        var free = new List<Transform>();
        foreach (var p in spawnPoints)
            if (!occupied.ContainsValue(p)) free.Add(p);
        if (free.Count == 0) return;

        Transform point = free[Random.Range(0, free.Count)];
        CustomerData data = possibleCustomers[Random.Range(0, possibleCustomers.Length)];

        GameObject obj = Instantiate(customerPrefab, point.position, point.rotation);
        Customer customer = obj.GetComponent<Customer>();
        customer.Initialize(data);
        occupied.Add(customer, point);
    }

    private void HandleCustomerLeft(Customer customer)
    {
        customer.Left -= HandleCustomerLeft;
        currentCount--;
    }
}