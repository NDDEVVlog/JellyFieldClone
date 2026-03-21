using UnityEngine;
using UnityEngine.EventSystems;
using Cysharp.Threading.Tasks;

public class DraggableCell : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    private Vector3 _startPos;
    private GridCell _cell;

    void Awake() => _cell = GetComponent<GridCell>();

    public void OnPointerDown(PointerEventData eventData)
    {
        _startPos = transform.position;
        // Hiệu ứng nhấc lên: hơi to ra và nảy nhẹ
        transform.localScale *= 1.15f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector3? mousePos = DragDropManager.Instance.GetMouseWorldPosition();
        if (mousePos.HasValue) transform.position = mousePos.Value + Vector3.up * 0.5f;
    }

    public async void OnPointerUp(PointerEventData eventData)
    {
        transform.localScale /= 1.15f;
        bool success = await GridManager.Instance.TryPlaceCell(_cell);
        
        if (success)
        {
            CellSpawner.Instance.OnCellPlaced(this);
            Destroy(this); // Không cho kéo nữa
        }
        else
        {
            // Bay về chỗ cũ bằng UniTask
            MoveBack().Forget();
        }
    }

    private async UniTaskVoid MoveBack()
    {
        float e = 0;
        Vector3 cur = transform.position;
        while(e < 0.2f)
        {
            e += Time.deltaTime;
            transform.position = Vector3.Lerp(cur, _startPos, e/0.2f);
            await UniTask.Yield();
        }
    }
}