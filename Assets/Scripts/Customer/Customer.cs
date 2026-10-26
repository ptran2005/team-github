using UnityEngine;
using System;

public enum CustomerState { Entering, InLine, AtRegister, Ordering, WaitingForFood, Leaving }

public class Customer : MonoBehaviour
{
    [Header("Patience")]
    [SerializeField] private float maxPatience = 30f;
    [SerializeField] private float currentPatience;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private bool faceWalkDirection = false;

    private Vector3 target;
    private bool isMoving;
    private CustomerState stateOnArrival;

    public CustomerData Data { get; private set; }
    [field: SerializeField] public CustomerState State { get; private set; } = CustomerState.Entering;
    public float PatiencePercent => currentPatience / maxPatience;

    public event Action<Customer> ArrivedAtRegister; // reached the front of the line
    public event Action<Customer> OrderConfirmed;    // order placed, patience clock starts
    public event Action<Customer> Expired;           // patience hit zero
    public event Action<Customer> Left;              // walked out, about to be destroyed

    private void Awake()
    {
        currentPatience = maxPatience;
    }

    public void Initialize(CustomerData data)
    {
        Data = data;
        gameObject.name = data.customerName;
    }

    // --- Movement (called by the spawner) ---
    public void MoveTo(Vector3 destination, CustomerState onArrival)
    {
        if (!isMoving && (transform.position - destination).sqrMagnitude < 0.0001f) return;
        target = destination;
        stateOnArrival = onArrival;
        isMoving = true;
        if (faceWalkDirection) FaceTarget();
    }

    public void WalkOut(Vector3 exit)
    {
        State = CustomerState.Leaving;
        MoveTo(exit, CustomerState.Leaving);
    }

    // --- Ordering (called by the ordering system) ---
    public void StartOrdering()
    {
        if (State == CustomerState.AtRegister) State = CustomerState.Ordering;
    }

    public void CancelOrdering()
    {
        if (State == CustomerState.Ordering) State = CustomerState.AtRegister;
    }

    public void OrderPlaced()
    {
        if (State != CustomerState.Ordering) return;
        State = CustomerState.WaitingForFood;
        Debug.Log($"{Data.customerName} placed an order and is now waiting for food.");
        OrderConfirmed?.Invoke(this);
    }

    private void Update()
    {
        if (isMoving)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, walkSpeed * Time.deltaTime);
            if (transform.position == target) Arrive();
        }

        if (State == CustomerState.WaitingForFood) TickPatience();
    }

    private void Arrive()
    {
        isMoving = false;

        if (State == CustomerState.Leaving)
        {
            Left?.Invoke(this);
            Destroy(gameObject);
            return;
        }

        State = stateOnArrival;
        if (State == CustomerState.AtRegister) ArrivedAtRegister?.Invoke(this);
    }

    private void TickPatience()
    {
        currentPatience -= Time.deltaTime;
        if (currentPatience <= 0f)
        {
            currentPatience = 0f;
            Debug.Log($"{Data.customerName} the {Data.animalType} ran out of patience!");
            Expired?.Invoke(this); // the spawner reacts by calling WalkOut, which changes State
        }
    }

    private void FaceTarget()
    {
        Vector3 flat = new Vector3(target.x, transform.position.y, target.z);
        if (flat != transform.position) transform.LookAt(flat);
    }
}