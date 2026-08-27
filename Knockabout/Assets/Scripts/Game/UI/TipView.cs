using IFramework;
using IFramework.UI;
using System.Collections;
using UnityEngine;
using static EventDefine;

public class TipView : UIView,IEventHandler<ShowTipArgs>
{
    //FieldsStart
		private TMPro.TextMeshProUGUI tip;

    //FieldsEnd
    protected override void InitComponents()
    {
        //InitComponentsStart
			tip = GetComponent<TMPro.TextMeshProUGUI>("tip/tip@sm");

        //InitComponentsEnd
    }
    private Vector3 pos;
    [Inject(UIServiceEx.defaultName)] UIService UI;
    [Inject] GGame game;
    protected override void OnLoad()
    {
        UI.Hide(PanelNames.Tip);
        this.RegisterEventHandlers();
        pos = tip.transform.parent.position;
    }
    private Coroutine coroutine;

    void IEventHandler<ShowTipArgs>.OnEvent(ShowTipArgs message)
    {
        tip.transform.parent.position = pos;

        tip.text = message.tip;
        UI.Show(PanelNames.Tip);

        if (coroutine != null)
        {
            game.StopCoroutine(coroutine);
        }
        coroutine = game.StartCoroutine(HideIE());
    }

    private IEnumerator HideIE()
    {
        for (int i = 0; i < 90; i++)
        {
            yield return null;
            tip.transform.parent.position += Vector3.up/100;
        }
        coroutine = null;
        UI.Hide(PanelNames.Tip);
        //OnHide();
    }

    protected override void OnShow()
    {

        //SetActive(true);
        //OnHide();

    }

    protected override void OnHide()
    {
        //SetActive(false);
    }

    protected override void OnClose()
    {
    

    }
    protected override void OnBecameVisible()
    {
    }

    protected override void OnBecameInvisible()
    {
    }

}
