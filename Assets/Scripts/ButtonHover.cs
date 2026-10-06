using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class ButtonHover : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private float hoverScale = 1.15f;
    [SerializeField] private float moveAmount = 3f;
    [SerializeField] private float wiggleSpeed = 6f;
    [SerializeField] private float smoothSpeed = 12f;

    private RectTransform rect;
    private Vector3 originalScale;
    private Vector2 originalPosition;
    private bool hovering;
    private float hoverTime;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        originalScale = rect.localScale;
        originalPosition = rect.anchoredPosition;
    }

    private void Update()
    {
        Vector3 targetScale = originalScale;
        Vector2 targetPosition = originalPosition;

        if (hovering)
        {
            hoverTime += Time.unscaledDeltaTime;

            targetScale *= hoverScale;

            targetPosition += new Vector2(
                Mathf.Sin(hoverTime * wiggleSpeed),
                Mathf.Sin(hoverTime * wiggleSpeed * 1.3f)
            ) * moveAmount;
        }

        float blend = 1f - Mathf.Exp(
            -smoothSpeed * Time.unscaledDeltaTime
        );

        rect.localScale = Vector3.Lerp(
            rect.localScale, targetScale, blend
        );

        rect.anchoredPosition = Vector2.Lerp(
            rect.anchoredPosition, targetPosition, blend
        );
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovering = true;
        hoverTime = 0f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovering = false;
    }

    private void OnDisable()
    {
        hovering = false;

        if (rect != null)
        {
            rect.localScale = originalScale;
            rect.anchoredPosition = originalPosition;
        }
    }
}