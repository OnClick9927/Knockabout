using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Card : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IEndDragHandler, IDragHandler
{
    public int card_id;
    private CardList list;
    public int index {  get; private set; }
    public bool dragging { get; private set; }
    private CanvasGroup group;
    private void Awake()
    {
        group = gameObject.GetComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        list.BeginDrag(this);

        dragging = true;
        group.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        var pos= eventData.position;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(list.rectTransform,pos,list._camera,out pos);
        //transform.position = eventData.position;
        transform.localPosition = new Vector3(pos.x, pos.y, 0);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        group.blocksRaycasts = true;

        dragging = false;

        list.EndDrag(this);

    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        list.SetExpand(this.index);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (dragging) return;
        list.SetExpand();
    }
    public void SetIndex(int index)
    {
        this.index = index;
    }
    internal void SetIndex(CardList list, int card_id)
    {
        this.list = list;
        this.card_id = card_id;

        GetComponent<Image>().color = Random.ColorHSV();
    }
}
