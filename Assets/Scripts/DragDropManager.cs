using UnityEngine;
using UnityEngine.InputSystem;

public class DragDropManager : MonoBehaviour
{
    public static DragDropManager Instance { get; private set; }
    [SerializeField] private LayerMask groundLayer;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public Vector3? GetMouseWorldPosition()
    {
        if (Mouse.current == null) return null;
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        Plane plane = new Plane(Vector3.up, Vector3.zero);
        return plane.Raycast(ray, out float distance) ? ray.GetPoint(distance) : null;
    }
}