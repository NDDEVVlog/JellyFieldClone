using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Linq;

public class MergeAnimator : MonoBehaviour
{
    public static MergeAnimator Instance;

    [Header("Settings")]
    public float liftHeight = 1.2f;
    public float liftDuration = 0.2f;
    public float moveDuration = 0.4f;
    public AnimationCurve liftCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    void Awake() => Instance = this;

    public async UniTask PlayMergeAnimation(List<GameObject> cubes)
    {
        if (cubes == null || cubes.Count == 0) return;

        // 1. Tính toán tâm điểm của vụ nổ (Center Point)
        Vector3 center = Vector3.zero;
        foreach (var c in cubes) center += c.transform.position;
        center /= cubes.Count;
        center.y += liftHeight; // Tâm điểm ở trên cao

        // 2. Nâng tất cả các khối lên
        List<UniTask> liftTasks = new List<UniTask>();
        foreach (var cube in cubes)
        {
            liftTasks.Add(AnimateLift(cube.transform));
        }
        await UniTask.WhenAll(liftTasks);

        // 3. Bay vào tâm và thu nhỏ
        List<UniTask> moveTasks = new List<UniTask>();
        foreach (var cube in cubes)
        {
            moveTasks.Add(AnimateMoveToCenter(cube.transform, center));
        }
        await UniTask.WhenAll(moveTasks);

        // 4. Dọn dẹp
        foreach (var cube in cubes)
        {
            if (cube) CubePool.Instance.ReturnToPool(cube);
        }
    }

    private async UniTask AnimateLift(Transform t)
    {
        Vector3 startPos = t.position;
        Vector3 targetPos = startPos + Vector3.up * liftHeight;
        float elapsed = 0;
        while (elapsed < liftDuration)
        {
            if (t == null) return;
            elapsed += Time.deltaTime;
            t.position = Vector3.LerpUnclamped(startPos, targetPos, liftCurve.Evaluate(elapsed / liftDuration));
            await UniTask.Yield();
        }
    }

    private async UniTask AnimateMoveToCenter(Transform t, Vector3 center)
    {
        Vector3 startPos = t.position;
        Vector3 startScale = t.localScale;
        float elapsed = 0;
        while (elapsed < moveDuration)
        {
            if (t == null) return;
            elapsed += Time.deltaTime;
            float p = moveCurve.Evaluate(elapsed / moveDuration);
            t.position = Vector3.LerpUnclamped(startPos, center, p);
            t.localScale = Vector3.LerpUnclamped(startScale, Vector3.zero, p);
            await UniTask.Yield();
        }
    }
}