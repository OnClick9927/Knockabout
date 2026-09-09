/*********************************************************************************
 *Author:         OnClick
 *Date:           2025-12-28
*********************************************************************************/
using IFramework;
using IFramework.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using static IFramework.UI.UnityEventHelper;
namespace RGBC
{
	public class MainView : UIView
    {
		class View {
//FieldsStart

//FieldsEnd
		public View(MainView context){
//InitComponentsStart

//InitComponentsEnd
			}
		}
		private View view;
		private RectTransform draggable;
		private Vector2 dragOffset;

		protected override void InitComponents()
		{
			view = new View(this);
			draggable = (RectTransform)transform.Find("Image");
		
		}
		protected override void OnLoad()
        {
            var trigger = draggable.GetComponent<EventTrigger>() ?? draggable.gameObject.AddComponent<EventTrigger>();
            var begin = new EventTrigger.Entry { eventID = EventTriggerType.BeginDrag };
            var drag = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
            this.Bind(begin.callback, data => OnBeginDrag((PointerEventData)data));
            this.Bind(drag.callback, data => OnDrag((PointerEventData)data));
            this.Bind(() => { trigger.triggers.Add(begin); trigger.triggers.Add(drag); },
                () => { trigger.triggers.Remove(begin); trigger.triggers.Remove(drag); });
        }
		protected override void OnShow(){}
		protected override void OnHide(){}
		protected override void OnClose(){}

        private void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)draggable.parent,
                eventData.pressPosition, eventData.pressEventCamera, out var point))
                dragOffset = (Vector2)draggable.localPosition - point;
        }

        private void OnDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            var parent = (RectTransform)draggable.parent;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, eventData.position,
                eventData.pressEventCamera, out var point)) return;
            var position = point + dragOffset;
            var bounds = parent.rect;
            var size = draggable.rect.size;
            position.x = Mathf.Clamp(position.x, bounds.xMin + size.x * draggable.pivot.x,
                bounds.xMax - size.x * (1 - draggable.pivot.x));
            position.y = Mathf.Clamp(position.y, bounds.yMin + size.y * draggable.pivot.y,
                bounds.yMax - size.y * (1 - draggable.pivot.y));
            draggable.localPosition = new Vector3(position.x, position.y, draggable.localPosition.z);
        }
	}
}
