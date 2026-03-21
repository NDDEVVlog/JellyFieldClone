using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour
{
    Camera cam;

    void Awake()
    {
        cam = Camera.main;
    }

    void Update()
    {
        // Chuột phải
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            HandleRightClick();
        }
    }

    void HandleRightClick()
    {
        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            var cube = hit.collider.GetComponent<GridCellCube>();
            if (cube == null) return;

            var cell = cube.GetComponentInParent<GridCell>();
            if (cell == null) return;

            // 💥 remove
            //cell.RemoveCubeAt(cube.gameObject);


        }
    }
}