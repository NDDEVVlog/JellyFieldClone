using UnityEngine;
using UnityEngine.InputSystem;

public class DragDropManager : MonoBehaviour
{
    public static DragDropManager Instance { get; private set; }
    private Camera _mainCam;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        

        _mainCam = Camera.main;
    }

    public Vector3? GetMouseWorldPosition()
    {
        Vector2 screenPosition;


        if (Touchscreen.current != null && Touchscreen.current.touches.Count > 0)
        {
            screenPosition = Touchscreen.current.touches[0].position.ReadValue();
        }
        else if (Mouse.current != null)
        {
            screenPosition = Mouse.current.position.ReadValue();
        }
        else
        {
            return null;
        }

        Ray ray = _mainCam.ScreenPointToRay(screenPosition);
        
  
        Plane plane = new Plane(Vector3.up, Vector3.zero);

        if (plane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }

        return null;
    }
}