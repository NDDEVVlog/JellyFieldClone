using UnityEngine;
using System.Collections.Generic;

public class CubePool : MonoBehaviour
{
    public static CubePool Instance;

    [SerializeField] private GameObject cubePrefab;
    [SerializeField] private int initialSize = 20;

    private Stack<GameObject> _pool = new Stack<GameObject>();

    void Awake()
    {
        Instance = this;
        // Khởi tạo trước một lượng Cube nhất định
        for (int i = 0; i < initialSize; i++)
        {
            CreateNewCube();
        }
    }

    private GameObject CreateNewCube()
    {
        GameObject obj = Instantiate(cubePrefab, transform);
        obj.SetActive(false);
        _pool.Push(obj);
        return obj;
    }

    public GameObject Get(Transform parent)
    {
        GameObject obj = _pool.Count > 0 ? _pool.Pop() : Instantiate(cubePrefab);
        
        obj.transform.SetParent(parent);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localScale = Vector3.one;
        obj.transform.localRotation = Quaternion.identity;
        obj.SetActive(true);
        
        return obj;
    }

    public void ReturnToPool(GameObject obj)
    {
        if (obj == null) return;

        // Reset lại trạng thái trước khi cất đi
        obj.SetActive(false);
        obj.transform.SetParent(transform);
        _pool.Push(obj);
    }
}