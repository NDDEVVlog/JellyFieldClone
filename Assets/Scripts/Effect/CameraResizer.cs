using UnityEngine;

public class CameraAutoFit : MonoBehaviour
{
    private Camera _cam;
    [SerializeField] private float padding = 1.5f; // Khoảng cách lề bao quanh Grid

    void Awake() => _cam = GetComponent<Camera>();

    // Gọi hàm này sau khi Grid đã được tạo xong hoàn toàn
    public void FitCamera(int width, int height, float spacing)
    {
        
        float gridWidth = (width - 1) * spacing;
        float gridHeight = (height - 1) * spacing;
        Vector3 center = new Vector3(gridWidth / 2f, 0, gridHeight / 2f);

        
        Vector3 newCamPos = center;
        newCamPos.y = transform.position.y; 
        newCamPos.z -= 10f; 
        transform.position = newCamPos;
        transform.LookAt(center + Vector3.up * 0.5f); 

        
        if (_cam.orthographic)
        {
            float screenRatio = (float)Screen.width / Screen.height;
            float targetRatio = gridWidth / gridHeight;

            if (screenRatio >= targetRatio)
            {
                _cam.orthographicSize = (gridHeight / 2) + padding;
            }
            else
            {
                float differenceInSize = targetRatio / screenRatio;
                _cam.orthographicSize = (gridHeight / 2) * differenceInSize + padding;
            }
        }
    }
}