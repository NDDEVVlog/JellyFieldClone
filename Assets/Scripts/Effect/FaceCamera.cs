using UnityEngine;

public class FaceCamera : MonoBehaviour
{
    private Transform _mainCamTransform;

    void Start()
    {
        if (Camera.main != null) 
            _mainCamTransform = Camera.main.transform;
    }

    void LateUpdate()
    {
        if (_mainCamTransform == null) return;

       
        transform.forward = _mainCamTransform.forward;
    }
}