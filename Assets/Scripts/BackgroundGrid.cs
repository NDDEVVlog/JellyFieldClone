using UnityEngine;
using UnityEngine.EventSystems;

public class BackgroundGrid : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Vector2Int coord;
    
    private SpriteRenderer _sprite;
    private Color _originalColor;

    private void Awake()
    {
        _sprite = GetComponent<SpriteRenderer>();
        if (_sprite != null)
        {
            _originalColor = _sprite.color;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {   
       
        if (_sprite == null) return;

        _sprite.color = Color.white;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_sprite == null) return;

        _sprite.color = _originalColor;
    }
}