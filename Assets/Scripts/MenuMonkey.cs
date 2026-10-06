using UnityEngine;

public class MenuMonkeyDance : MonoBehaviour
{
    [SerializeField] private Transform menuCamera;
    [SerializeField] private float bounceHeight = 0.08f;
    [SerializeField] private float swayAngle = 8f;
    [SerializeField] private float danceSpeed = 5f;

    private Vector3 startingPosition;
    private float danceTime;

    private void Awake()
    {
        startingPosition = transform.localPosition;
    }

    private void LateUpdate()
    {
        danceTime += Time.unscaledDeltaTime * danceSpeed;

        float bounce = Mathf.Abs(Mathf.Sin(danceTime))
            * bounceHeight;

        transform.localPosition = startingPosition
            + Vector3.up * bounce;

        if (menuCamera != null)
        {
            Vector3 direction = menuCamera.position
                - transform.position;

            direction.y = 0f;

            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion facing = Quaternion.LookRotation(
                    -direction, Vector3.up
                );

                // Tilt from side to side.
                float sway = Mathf.Sin(danceTime) * swayAngle;

                transform.rotation = facing
                    * Quaternion.Euler(0f, 0f, sway);
            }
        }
    }

    private void OnDisable()
    {
        transform.localPosition = startingPosition;
    }
}