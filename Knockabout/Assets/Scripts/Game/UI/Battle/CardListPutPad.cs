using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardListPutPad : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private bool _enter;
    public bool enter { get => _enter; private set {
            if (_enter == value) return;
            _enter = value;
            //image.color =value? Color.green:Color.white;
        }
    }
    //Image image;
    private void OnDisable()
    {
        enter = false;
    }
    void Awake()
    {
        //image = GetComponent<Image>();
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        enter = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        enter = false;

    }





}
