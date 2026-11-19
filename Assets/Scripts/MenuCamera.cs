using UnityEngine;

public class MenuCameraOrbit : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private Transform menuCamera;

    private void LateUpdate()
    {
        transform.Rotate(
            Vector3.up,
            rotationSpeed * Time.unscaledDeltaTime,
            Space.World
        );

        if (menuCamera != null)
        {
            menuCamera.LookAt(transform.position);
        }
    }
}