using UnityEngine;

public class PuddingZAxisPhysics : MonoBehaviour
{
    public float stiffness = 120f;  // Độ cứng của lò xo
    public float damping = 5f;      // Độ nhún (lắc bao lâu thì đứng yên)
    public float sensitivity = 0.5f; // Độ nhạy với di chuyển

    private Material mat;
    private Vector3 lastPos;
    private Vector3 velocity;
    private Vector3 wobbleAmount;
    private Vector3 wobbleVelocity;

    void Start()
    {
        mat = GetComponent<MeshRenderer>().material;
        lastPos = transform.position;
    }

    void Update()
    {
        // 1. Tính vận tốc dựa trên cả 3 trục X, Y, Z
        velocity = (transform.position - lastPos) / Time.deltaTime;
        lastPos = transform.position;

        // 2. Tính toán lực quán tính (Inertia)
        // Khi di chuyển sang phải (X+), đỉnh khối phải bị kéo về trái (X-)
        Vector3 targetWobble = -velocity * sensitivity * 0.02f;
        
        // Công thức lò xo để tạo độ tưng tửng (Boing)
        Vector3 force = stiffness * (targetWobble - wobbleAmount) - damping * wobbleVelocity;
        wobbleVelocity += force * Time.deltaTime;
        wobbleAmount += wobbleVelocity * Time.deltaTime;

        // 3. Chuyển sang Local Space để xoay object không bị lỗi hướng lắc
        Vector3 localWobble = transform.InverseTransformDirection(wobbleAmount);

        // 4. TRUYỀN CẢ X VÀ Z VÀO SHADER
        // localWobble.x điều khiển lắc trái phải, localWobble.z điều khiển lắc trước sau
        mat.SetVector("_WobbleVector", new Vector4(localWobble.x, 0, localWobble.z, 0));
    }
}