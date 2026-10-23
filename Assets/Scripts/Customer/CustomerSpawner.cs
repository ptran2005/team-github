using UnityEngine;
using System.Collections;

public class CustomerSpawner : MonoBehaviour
{
    [SerializeField] private GameObject customerPrefab;
    [SerializeField] private CustomerData[] possibleCustomers;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float minSpawnInterval = 5f;
    [SerializeField] private float maxSpawnInterval = 12f;

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

    private void SpawnCustomer()
    {
        if (spawnPoints.Length == 0 || possibleCustomers.Length == 0 || customerPrefab == null) return;

        Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];
        CustomerData data = possibleCustomers[Random.Range(0, possibleCustomers.Length)];

        GameObject obj = Instantiate(customerPrefab, point.position, Quaternion.identity);
        obj.GetComponent<Customer>().Initialize(data);
    }
}