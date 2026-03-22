using UnityEngine;
using UnityEngine.EventSystems;
using Cysharp.Threading.Tasks;
using UnityEngine.Events;

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
        
        transform.localScale *= 1.1f;
        _isDragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging) return;

        
        Vector3? mousePos = DragDropManager.Instance.GetMouseWorldPosition();
        if (mousePos.HasValue) 
        {
            
            transform.position = mousePos.Value + Vector3.up * 0.5f;
        }
    }

    public async void OnPointerUp(PointerEventData eventData)
    {
        if (!_isDragging) return;
        _isDragging = false;

        transform.localScale /= 1.1f;

       
        bool success = await GridController.Instance.TryPlaceCell(_cell);
        
        if (success)
        {
            
            CellSpawner.Instance.OnCellPlaced(this);
           
            Destroy(this); 
        }
        else
        {
        
            MoveBack().Forget();
        }
    }

    
    private async UniTaskVoid MoveBack()
    {
        var ct = this.GetCancellationTokenOnDestroy(); 
        float duration = 0.2f;
        float elapsed = 0;
        Vector3 currentPos = transform.position;

        while(elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(currentPos, _startPos, elapsed / duration);
            await UniTask.Yield(PlayerLoopTiming.Update, ct); 
        }
        transform.position = _startPos;
    }
}