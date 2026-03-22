using UnityEngine;
using UnityEngine.Pool;

public class ParticlePoolManager : MonoBehaviour
{
    public static ParticlePoolManager Instance { get; private set; }

    [SerializeField] private GameObject particlePrefab;
    [SerializeField] private int defaultCapacity = 10;
    [SerializeField] private int maxSize = 20;

    private IObjectPool<GameObject> _pool;

    void Awake()
    {
        Instance = this;
        _pool = new ObjectPool<GameObject>(CreateParticle, OnGetFromPool, OnReturnToPool, OnDestroyObject, true, defaultCapacity, maxSize);
    }

    private GameObject CreateParticle()
    {
        // Instantiate trực tiếp làm con của Manager
        GameObject go = Instantiate(particlePrefab, transform);
        var returner = go.AddComponent<ParticleReturnToPool>();
        returner.Pool = _pool;
        return go;
    }

    private void OnGetFromPool(GameObject go) => go.SetActive(true);
    private void OnReturnToPool(GameObject go) => go.SetActive(false);
    private void OnDestroyObject(GameObject go) => Destroy(go);

    public void PlayEffect(Vector3 position, Color color)
    {
        var effect = _pool.Get();
        effect.transform.position = position;

        var ps = effect.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            var main = ps.main;
            main.startColor = color;

            ps.Play(false); 
        }
    }
}

public class ParticleReturnToPool : MonoBehaviour
{
    public IObjectPool<GameObject> Pool;

    void OnParticleSystemStopped()
    {
        if (gameObject.activeSelf) Pool.Release(gameObject);
    }
}