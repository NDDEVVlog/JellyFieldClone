using UnityEngine;
using DG.Tweening;
using Cysharp.Threading.Tasks;

public class PopUpAnimation : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float duration = 0.3f;
    [SerializeField] private Ease showEase = Ease.OutBack;
    [SerializeField] private Ease hideEase = Ease.InBack;

    private void OnEnable()
    {

        transform.localScale = Vector3.zero;
    }

    public void ShowUI() => Show().Forget();
    public void HideUI() => Hide().Forget();


    public async UniTask Show()
    {
        transform.DOKill(true);
        gameObject.SetActive(true);
        transform.localScale = Vector3.zero;

        await transform.DOScale(1f, duration)
            .SetEase(showEase)
            .SetUpdate(true)
            .AsyncWaitForCompletion(); 

        Debug.Log("Hiện xong!");
    }

    public async UniTask Hide()
    {
        transform.DOKill(true);

        await transform.DOScale(0f, duration)
            .SetEase(hideEase)
            .SetUpdate(true)
            .AsyncWaitForCompletion();

        gameObject.SetActive(false);
    }
}