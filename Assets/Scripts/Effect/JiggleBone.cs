using UnityEngine;

public class JiggleBone : MonoBehaviour
{
    [Header("Spring Settings (Nảy)")]
    public float stiffness = 150f;
    public float damping = 0.5f;
    public float inertiaInfluence = 0.8f;
    public float maxDistance = 0.5f;

    [Header("Elasticity Curve (Độ dẻo)")]
    [Tooltip("Trục X: Độ giãn (0-1), Trục Y: Hệ số nhân độ cứng")]
    public AnimationCurve elasticityCurve = AnimationCurve.Linear(0, 1, 1, 2);

    [Header("Wobble Settings (Sóng sánh)")]
    public float tiltAmount = 25f;   
    public float wobbleSpeed = 10f;  
    public float maxTilt = 15f;      

    private Vector3 dynamicPos;
    private Vector3 velocity;
    private Vector3 lastParentPos;
    private Vector3 localTargetPos;
    private Quaternion localTargetRot;

    void Awake()
    {
        localTargetPos = transform.localPosition;
        localTargetRot = transform.localRotation;
        dynamicPos = transform.position;
        lastParentPos = transform.parent.position;
    }

    void LateUpdate()
    {
        Vector3 currentParentPos = transform.parent.position;
        Vector3 parentVelocity = (currentParentPos - lastParentPos) / Time.deltaTime;
        Vector3 targetWorldPos = transform.parent.TransformPoint(localTargetPos);

        // 1. TÍNH TOÁN LỰC DỰA TRÊN CURVE
        float dist = Vector3.Distance(targetWorldPos, dynamicPos);
        float distNormalized = Mathf.Clamp01(dist / maxDistance);
        
        // Lấy giá trị từ Curve để nhân vào Stiffness
        float curveStiffnessMultiplier = elasticityCurve.Evaluate(distNormalized);

        // 2. VẬT LÝ NẢY
        velocity += parentVelocity * inertiaInfluence;
        Vector3 force = (targetWorldPos - dynamicPos) * (stiffness * curveStiffnessMultiplier);
        velocity += force * Time.deltaTime;
        velocity *= (1f - damping);
        dynamicPos += velocity * Time.deltaTime;

        // Giới hạn khoảng cách
        Vector3 offset = dynamicPos - targetWorldPos;
        if (offset.magnitude > maxDistance)
            dynamicPos = targetWorldPos + offset.normalized * maxDistance;

        transform.position = dynamicPos;

        // 3. XOAY SÓNG SÁNH
        if (tiltAmount > 0)
        {
            Vector3 relVel = transform.parent.InverseTransformDirection(parentVelocity);
            Quaternion tiltGoal = Quaternion.Euler(relVel.z * tiltAmount * 0.1f, 0, -relVel.x * tiltAmount * 0.1f);
            float angle = Quaternion.Angle(Quaternion.identity, tiltGoal);
            if (angle > maxTilt) tiltGoal = Quaternion.Slerp(Quaternion.identity, tiltGoal, maxTilt / angle);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, localTargetRot * tiltGoal, Time.deltaTime * wobbleSpeed);
        }

        lastParentPos = currentParentPos;
    }
}