
using UnityEngine;
using UnityEngine.InputSystem;

public class HarvestCursor : MonoBehaviour
{
    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        Vector2 mouse = Mouse.current.position.ReadValue();

        Vector3 position = mainCamera.ScreenToWorldPoint(
            new Vector3(mouse.x, mouse.y, -mainCamera.transform.position.z)
        );

        position.z = 0f;
        transform.position = position;
    }
}
