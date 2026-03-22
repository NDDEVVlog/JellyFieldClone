using UnityEngine;
using UnityEngine.EventSystems;
using Cysharp.Threading.Tasks;

public class DraggableCell : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    private Vector3 _startPos;
    private GridCell _cell;
    private bool _isDragging = false;

    void Awake() => _cell = GetComponent<GridCell>();

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_isDragging) return;
        
        _startPos = transform.position;
        // Hiệu ứng nhấc lên: hơi to ra và nảy nhẹ
        transform.localScale *= 1.1f;
        _isDragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging) return;

        // Sử dụng Manager để lấy vị trí chuột trong không gian 3D
        Vector3? mousePos = DragDropManager.Instance.GetMouseWorldPosition();
        if (mousePos.HasValue) 
        {
            // Nhấc cao hơn mặt đất (Y = 0.5) để không bị xuyên qua sàn
            transform.position = mousePos.Value + Vector3.up * 0.5f;
        }
    }

    public async void OnPointerUp(PointerEventData eventData)
    {
        if (!_isDragging) return;
        _isDragging = false;

        transform.localScale /= 1.1f;

        // ĐỔI TỪ GridManager SANG GridController
        // await để đợi logic xử lý (check match, animation...) hoàn tất
        bool success = await GridController.Instance.TryPlaceCell(_cell);
        
        if (success)
        {
            // Nếu đặt thành công, thông báo cho Spawner để sinh cell mới vào khay
            CellSpawner.Instance.OnCellPlaced(this);
            
            // Hủy script kéo thả này để cell nằm yên trên lưới
            Destroy(this); 
        }
        else
        {
            // Nếu đặt không hợp lệ (ngoài biên, ô đã có cell...), bay về chỗ cũ
            MoveBack().Forget();
        }
    }

    private async UniTaskVoid MoveBack()
    {
        float duration = 0.2f;
        float elapsed = 0;
        Vector3 currentPos = transform.position;

        while(elapsed < duration)
        {
            elapsed += Time.deltaTime;
            // Dùng Lerp để tạo cảm giác bay về mượt mà
            transform.position = Vector3.Lerp(currentPos, _startPos, elapsed / duration);
            await UniTask.Yield();
        }
        
        transform.position = _startPos;
    }
}