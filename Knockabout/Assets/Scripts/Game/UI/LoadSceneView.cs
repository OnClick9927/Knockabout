/*********************************************************************************
 *Author:         OnClick
 *Version:        1.0
 *UnityVersion:   2020.3.46f1c1
 *Date:           2024-04-20
*********************************************************************************/

using IFramework;
using IFramework.UI;
using System.Collections;
using UnityEngine;
using WooAsset;
using static EventDefine;



public class LoadSceneView : UIView, IEventHandler<LoadSceneArgs>
{
    //FieldsStart
    private UnityEngine.Animator Ani;

    //FieldsEnd
    protected override void InitComponents()
    {
        //InitComponentsStart
        Ani = GetComponent<UnityEngine.Animator>("Ani@sm");

        //InitComponentsEnd

    }

    private SceneAsset sceneAsset = null;
    [Inject(UIServiceEx.defaultName)] UIService UI;
    [Inject] GGame game;
    protected override void OnLoad()
    {
        GameTools.RegisterEventHandlers(this);
        UI.Hide(PanelNames.LoadScene);
    }
    void IEventHandler<LoadSceneArgs>.OnEvent(LoadSceneArgs message)
    {
        game.StartCoroutine(LoadIE(message));
    }




    private IEnumerator LoadIE(LoadSceneArgs args)
    {
        UI.Show(PanelNames.LoadScene);
        Ani.SetTrigger("Enter");

        sceneAsset = Assets.LoadSceneAssetAsync(args.sceneName);
        while (!sceneAsset.isDone) yield return null;
        yield return sceneAsset.LoadSceneAsync(args.mode);
        Events.Notify(new LoadSceneEndArgs());

        yield return new WaitForSeconds(0.5f);
        Ani.SetTrigger("Exit");
        yield return new WaitForSeconds(1f);
        UI.Hide(PanelNames.LoadScene);

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

    protected override void OnShow()
    {

    }

    protected override void OnHide()
    {
    }


}
