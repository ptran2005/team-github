using UnityEngine;
using UnityEngine.InputSystem;
using System;

// Slides one camera between view points, one per station.
// Switch with the tab buttons (StationTabButton), the number keys, or the left/right arrows.
public class StationCameraController : MonoBehaviour
{
    [Serializable]
    public class StationView
    {
        public Station station;
        public Transform viewPoint;
        [Tooltip("0 = keep the camera's field of view")]
        public float fieldOfView;
    }

    [SerializeField] private Camera targetCamera;
    [SerializeField] private StationView[] views;
    [SerializeField] private Station startingStation = Station.Order;
    [SerializeField] private float slideTime = 0.35f;
    [SerializeField] private bool keyboardShortcuts = true; // 1-4 = views in list order, arrows = prev/next

    private StationView targetView;
    private Vector3 fromPosition;
    private Quaternion fromRotation;
    private float fromFieldOfView;
    private float slideTimer;

    public static StationCameraController Instance { get; private set; }
    public Camera Camera => targetCamera;
    public Station Current { get; private set; }
    public bool IsSliding => targetView != null && slideTimer < slideTime;

    public event Action<Station> StationChanged;

    private void Awake()
    {
        Instance = this;
        if (targetCamera == null) targetCamera = Camera.main;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        if (targetCamera == null)
        {
            Debug.LogError("StationCameraController has no camera to move.", this);
            enabled = false;
            return;
        }

        Current = startingStation;
        targetView = FindView(startingStation);
        if (targetView != null) Snap(targetView);
        StationChanged?.Invoke(Current);
    }

    public bool HasView(Station station) => FindView(station) != null;

    public void SwitchTo(Station station)
    {
        StationView view = FindView(station);
        if (view == null)
        {
            Debug.LogWarning($"No camera view point set up for the {station} station.", this);
            return;
        }
        if (station == Current && targetView == view) return;

        fromPosition = targetCamera.transform.position;
        fromRotation = targetCamera.transform.rotation;
        fromFieldOfView = targetCamera.fieldOfView;
        targetView = view;
        slideTimer = 0f;

        Current = station;
        StationChanged?.Invoke(station);
    }

    // For UI Button OnClick in the Inspector, which can't pass an enum
    public void SwitchToIndex(int index)
    {
        if (views != null && index >= 0 && index < views.Length) SwitchTo(views[index].station);
    }

    public void Next() => Step(1);
    public void Previous() => Step(-1);

    private void Step(int direction)
    {
        if (views == null || views.Length == 0) return;
        int index = Array.FindIndex(views, v => v.station == Current);
        index = (index + direction + views.Length) % views.Length;
        SwitchTo(views[index].station);
    }

    private void Update()
    {
        if (!keyboardShortcuts) return;
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.digit1Key.wasPressedThisFrame) SwitchToIndex(0);
        else if (keyboard.digit2Key.wasPressedThisFrame) SwitchToIndex(1);
        else if (keyboard.digit3Key.wasPressedThisFrame) SwitchToIndex(2);
        else if (keyboard.digit4Key.wasPressedThisFrame) SwitchToIndex(3);
        else if (keyboard.rightArrowKey.wasPressedThisFrame) Next();
        else if (keyboard.leftArrowKey.wasPressedThisFrame) Previous();
    }

    private void LateUpdate()
    {
        if (!IsSliding) return;

        slideTimer += Time.unscaledDeltaTime;
        float t = slideTime > 0f ? Mathf.SmoothStep(0f, 1f, slideTimer / slideTime) : 1f;

        Transform cam = targetCamera.transform;
        cam.SetPositionAndRotation(
            Vector3.Lerp(fromPosition, targetView.viewPoint.position, t),
            Quaternion.Slerp(fromRotation, targetView.viewPoint.rotation, t));

        if (targetView.fieldOfView > 0f)
            targetCamera.fieldOfView = Mathf.Lerp(fromFieldOfView, targetView.fieldOfView, t);
    }

    private void Snap(StationView view)
    {
        targetCamera.transform.SetPositionAndRotation(view.viewPoint.position, view.viewPoint.rotation);
        if (view.fieldOfView > 0f) targetCamera.fieldOfView = view.fieldOfView;
        slideTimer = slideTime;
    }

    private StationView FindView(Station station)
    {
        if (views == null) return null;
        foreach (StationView view in views)
            if (view.station == station && view.viewPoint != null) return view;
        return null;
    }

    // Right-click the component header to line the camera up with a view while not playing
    [ContextMenu("Preview Starting View")]
    private void PreviewStartingView()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        StationView view = FindView(startingStation);
        if (targetCamera != null && view != null) Snap(view);
    }
}
