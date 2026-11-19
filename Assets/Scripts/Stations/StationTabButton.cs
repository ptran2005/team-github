using UnityEngine;
using UnityEngine.UI;

// Put on a UI Button to make it a station tab. It lights up while that station is showing.
[RequireComponent(typeof(Button))]
public class StationTabButton : MonoBehaviour
{
    [SerializeField] private Station station;
    [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.85f);
    [SerializeField] private Color selectedColor = new Color(1f, 0.82f, 0.3f, 1f);

    private Button button;
    private StationCameraController cameras;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(() => { if (cameras != null) cameras.SwitchTo(station); });
    }

    private void Start()
    {
        cameras = StationCameraController.Instance;
        if (cameras == null)
        {
            Debug.LogWarning("StationTabButton couldn't find a StationCameraController in the scene.", this);
            return;
        }

        cameras.StationChanged += Highlight;
        Highlight(cameras.Current);
        if (!cameras.HasView(station)) gameObject.SetActive(false); // no view point for this station yet
    }

    private void OnDestroy()
    {
        if (cameras != null) cameras.StationChanged -= Highlight;
    }

    private void Highlight(Station current)
    {
        if (button.targetGraphic != null)
            button.targetGraphic.color = current == station ? selectedColor : normalColor;
    }
}
