using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

// Little text bubbles that float above customers' heads. Put on the HUD canvas.
public class SpeechBubbles : MonoBehaviour
{
    [SerializeField] private float fontSize = 30f;
    [SerializeField] private Color bubbleColor = new Color(1f, 1f, 1f, 0.92f);
    [SerializeField] private Color textColor = new Color(0.15f, 0.1f, 0.05f);
    [SerializeField] private float gapAboveHead = 12f; // pixels
    [SerializeField] private float topMargin = 170f;   // room for the money and order panels

    private class Bubble
    {
        public Transform target;
        public Renderer targetRenderer;
        public RectTransform rect;
        public TMP_Text text;
        public float timeLeft;
    }

    private static SpeechBubbles instance;
    private readonly List<Bubble> bubbles = new List<Bubble>();
    private RectTransform canvasRect;
    private Canvas canvas;

    public static void Show(Component target, string message, float seconds = 2.5f)
    {
        if (target == null) return;
        if (instance == null)
        {
            Debug.Log($"{target.name}: {message}");
            return;
        }
        instance.ShowBubble(target.transform, message, seconds);
    }

    private void Awake()
    {
        instance = this;
        canvas = GetComponentInParent<Canvas>().rootCanvas;
        canvasRect = (RectTransform)canvas.transform;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void ShowBubble(Transform target, string message, float seconds)
    {
        Bubble bubble = bubbles.Find(b => b.target == target) ?? CreateBubble(target);
        bubble.text.text = message;
        bubble.timeLeft = seconds;
        bubble.rect.gameObject.SetActive(true);
    }

    private void LateUpdate()
    {
        StationCameraController cameras = StationCameraController.Instance;
        Camera cam = cameras != null ? cameras.Camera : Camera.main;

        for (int i = bubbles.Count - 1; i >= 0; i--)
        {
            Bubble bubble = bubbles[i];
            bubble.timeLeft -= Time.deltaTime;
            if (bubble.target == null || bubble.timeLeft <= 0f || cam == null)
            {
                Destroy(bubble.rect.gameObject);
                bubbles.RemoveAt(i);
                continue;
            }

            Vector3 head = bubble.targetRenderer != null
                ? new Vector3(bubble.target.position.x, bubble.targetRenderer.bounds.max.y, bubble.target.position.z)
                : bubble.target.position;
            Vector3 screen = cam.WorldToScreenPoint(head);

            bool inFront = screen.z > 0f;
            bubble.rect.gameObject.SetActive(inFront);
            if (!inFront) continue;

            Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, uiCamera, out Vector2 local);
            bubble.rect.anchoredPosition = KeepOnScreen(local + Vector2.up * gapAboveHead, bubble.rect.rect.size);
        }
    }

    // Up close the customers' heads are at the top of the screen, so keep bubbles under the HUD panels
    private Vector2 KeepOnScreen(Vector2 position, Vector2 size)
    {
        Rect area = canvasRect.rect;
        float halfWidth = size.x * 0.5f;
        position.x = Mathf.Clamp(position.x, area.xMin + halfWidth + 20f, area.xMax - halfWidth - 20f);
        position.y = Mathf.Clamp(position.y, area.yMin + 120f, area.yMax - topMargin - size.y);
        return position;
    }

    private Bubble CreateBubble(Transform target)
    {
        var box = new GameObject("Speech Bubble", typeof(RectTransform), typeof(Image),
            typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        box.transform.SetParent(canvasRect, false);

        var rect = (RectTransform)box.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0f);

        box.GetComponent<Image>().color = bubbleColor;
        box.GetComponent<Image>().raycastTarget = false;

        var layout = box.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(18, 18, 10, 10);
        layout.childControlWidth = layout.childControlHeight = true;

        var fitter = box.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var textObj = new GameObject("Text", typeof(RectTransform));
        textObj.transform.SetParent(box.transform, false);
        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.color = textColor;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;

        var bubble = new Bubble
        {
            target = target,
            targetRenderer = target.GetComponentInChildren<Renderer>(),
            rect = rect,
            text = text,
        };
        bubbles.Add(bubble);
        return bubble;
    }
}
