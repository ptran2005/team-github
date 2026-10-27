using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System;

// Turns left clicks into "clicked this station object" or "clicked this customer".
// Uses the Input System directly because OnMouseDown doesn't fire with this project's input settings.
public class ClickInput : MonoBehaviour
{
    [SerializeField] private float maxDistance = 500f;

    private readonly RaycastHit[] hits = new RaycastHit[32];

    public static ClickInput Instance { get; private set; }

    // OrderTaker and ServeWindow listen for this and decide what a click on a customer means
    public event Action<Customer> CustomerClicked;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return; // clicked a UI button

        StationCameraController cameras = StationCameraController.Instance;
        if (cameras != null && cameras.IsSliding) return;

        Camera cam = cameras != null ? cameras.Camera : Camera.main;
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
        int count = Physics.RaycastNonAlloc(ray, hits, maxDistance, ~0, QueryTriggerInteraction.Collide);

        // Closest thing under the mouse that can be clicked from this station
        float closest = float.MaxValue;
        StationInteractable clickedThing = null;
        Customer clickedCustomer = null;

        for (int i = 0; i < count; i++)
        {
            if (hits[i].distance >= closest) continue;

            Customer customer = hits[i].collider.GetComponentInParent<Customer>();
            StationInteractable thing = customer == null ? hits[i].collider.GetComponentInParent<StationInteractable>() : null;
            if (thing != null && cameras != null && thing.Station != cameras.Current) thing = null;
            if (customer == null && thing == null) continue;

            closest = hits[i].distance;
            clickedCustomer = customer;
            clickedThing = thing;
        }

        if (clickedCustomer != null) CustomerClicked?.Invoke(clickedCustomer);
        else if (clickedThing != null) clickedThing.OnClicked();
    }
}
