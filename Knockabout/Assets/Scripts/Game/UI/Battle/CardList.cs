using IFramework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
public class CardList : MonoBehaviour
{
    class Pool : ObjectPool<Card>
    {
        public Card prefab;

        public Pool(Card prefab)
        {
            this.prefab = prefab;
        }

        protected override Card CreateNew()
        {
            return GameObject.Instantiate(prefab, prefab.transform.parent);
        }
        protected override bool OnSet(Card t)
        {
            t.gameObject.SetActive(false);
            return base.OnSet(t);
        }
        public override Card Get()
        {
            var result = base.Get();
            result.transform.rotation = Quaternion.identity;
            result.transform.localScale = Vector3.one;
            //result.transform.SetParent(this.prefab.transform.parent, true);
            result.gameObject.SetActive(true);
            return result;
        }
    }

    public Card prefab;
    private CardListPutPad putpad;

    public float space = 50;
    public float space_expand = 50;
    public float radius = 1920;
    public float expandSpeed = 10;
    public float expandUp = 20;

    private int expandIndex = -1;
    private Vector2 center;
    private float radius_pow, width;
    private List<Card> cards = new List<Card>();
    private Pool pool;



    private float dirty;
    [HideInInspector] public Camera _camera;
    [HideInInspector] public RectTransform rectTransform;
    private void SetDirty()
    {
        dirty = 2;
    }
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        putpad = GetComponentInChildren<CardListPutPad>();
        pool = new Pool(prefab);
        _camera = GetComponentInParent<Canvas>().worldCamera;
        prefab.gameObject.SetActive(false);
        width = GetComponent<RectTransform>().rect.width;
        radius_pow = radius * radius;
        center = new Vector2(transform.position.x, transform.position.y - Mathf.Sqrt(radius_pow));
        HidePutPad();
    }


    private void Update()
    {
        if (dirty <= 0) return;
        if (draging) return;
        dirty -= Time.deltaTime;
        var count = cards.Count;
        if (count == 0) return;
        if (expandIndex < 0 || expandIndex >= count)
            expandIndex = -1;
        var totoal_width = (count - 1) * space + (expandIndex == -1 ? 0 : -space + space_expand);
        var start = center.x - totoal_width / 2;
        float mid = count % 2 == 0 ? (count - 1) / 2f : count / 2;
        for (int i = 0; i < count; i++)
        {
            var rect = cards[i];
            //if (rect.dragging) continue;
            var targetPos = new Vector2(start, center.y + Mathf.Sqrt(radius_pow - Mathf.Pow((start - center.x), 2)));
            var dir = (targetPos - center).normalized;
            if (expandIndex == i)
                targetPos += dir * expandUp;
            var target_rotation = Quaternion.Euler(0, 0, Vector2.SignedAngle(Vector2.up, dir));
            Vector2 now = rect.transform.localPosition;
            targetPos = Vector2.Lerp(now, targetPos, Time.deltaTime * expandSpeed);
            rect.transform.localPosition = targetPos;
            rect.transform.rotation = Quaternion.Lerp(rect.transform.rotation, target_rotation, Time.deltaTime * expandSpeed);
            start += expandIndex == i ? space_expand : space;
        }


    }







    public void SetExpand(int index = -1)
    {
        if (draging) return;
        this.expandIndex = index;
        SetDirty();
    }
    public void AddCard(Vector3 pos, int card_id)
    {
        var result = pool.Get();
        result.transform.position = pos;
        result.transform.SetAsLastSibling();
        result.SetIndex(this, card_id);
        cards.Add(result);
        for (int i = 0; i < cards.Count; i++)
        {
            cards[i].SetIndex(i);
        }

        SetDirty();
    }
    public void Clear()
    {
        for (int i = 0; i < cards.Count; i++)
        {
            pool.Set(cards[i]);

        }
        cards.Clear();
        SetExpand();
    }

    private void HidePutPad(bool hide = true)
    {
        putpad.gameObject.SetActive(!hide);

    }

    private bool draging = false;
    public void BeginDrag(Card card)
    {
        draging = true;
        FreshIndex();
        HidePutPad(false);
    }
    public void EndDrag(Card card)
    {
        draging = false;
        if (putpad.enter)
        {
            OnUseCard.Invoke(card);
            FreshIndex();
            SetExpand();
        }
        else
        {
            SetExpand(this.expandIndex);
        }
        HidePutPad();
    }
    private void FreshIndex()
    {
        for (int i = 0; i < cards.Count; i++)
        {
            cards[i].SetIndex(i);
        }
    }

    public void RealUseCard(int index)
    {
        var card = cards[index];
        cards.Remove(card);
        pool.Set(card);
    }

    public UnityEvent<Card> OnUseCard = new UnityEvent<Card>();
}
