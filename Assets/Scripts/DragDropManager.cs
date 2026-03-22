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
        
        // Cache camera để tăng hiệu năng (gọi Camera.main mỗi khung hình rất tốn)
        _mainCam = Camera.main;
    }

    public Vector3? GetMouseWorldPosition()
    {
        Vector2 screenPosition;

        // Ưu tiên lấy vị trí Touch (Android/iOS)
        if (Touchscreen.current != null && Touchscreen.current.touches.Count > 0)
        {
            screenPosition = Touchscreen.current.touches[0].position.ReadValue();
        }
        // Nếu không có Touch thì mới dùng Mouse (Editor/PC)
        else if (Mouse.current != null)
        {
            screenPosition = Mouse.current.position.ReadValue();
        }
        else
        {
            return null;
        }

        Ray ray = _mainCam.ScreenPointToRay(screenPosition);
        
        // Plane nằm ở độ cao Y = 0 (mặt đất)
        Plane plane = new Plane(Vector3.up, Vector3.zero);

        if (plane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }

        return null;
    }
}