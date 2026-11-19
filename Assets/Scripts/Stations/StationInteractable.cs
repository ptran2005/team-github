using UnityEngine;

// Base for things the player clicks while looking at a station (fridge items, grill slots...).
// ClickInput only sends clicks to the ones that belong to the station that's showing.
public abstract class StationInteractable : MonoBehaviour
{
    [SerializeField] protected Station station = Station.Cook;

    public Station Station => station;

    protected virtual void Awake()
    {
        // Clicks are raycasts, and imported models don't come with colliders
        if (GetComponentInChildren<Collider>() == null) AddFittedCollider();
    }

    public abstract void OnClicked();

    private void AddFittedCollider()
    {
        BoxCollider box = gameObject.AddComponent<BoxCollider>();
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds bounds = renderers[0].bounds;
        foreach (Renderer r in renderers) bounds.Encapsulate(r.bounds);

        Vector3 scale = transform.lossyScale;
        box.center = transform.InverseTransformPoint(bounds.center);
        box.size = new Vector3(
            bounds.size.x / Mathf.Max(Mathf.Abs(scale.x), 0.0001f),
            bounds.size.y / Mathf.Max(Mathf.Abs(scale.y), 0.0001f),
            bounds.size.z / Mathf.Max(Mathf.Abs(scale.z), 0.0001f));
    }
}
