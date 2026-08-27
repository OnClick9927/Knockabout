using AOT;
using IFramework;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using TMPro.EditorUtilities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using EditorTools = IFramework.EditorTools;
using Object = UnityEngine.Object;
partial class EditorGameTools
{
    [MenuItem("CONTEXT/RectTransform/LogSizeDelta")]
    static void LogSizeDelta(MenuCommand command)
    {
        RectTransform rectTransform = command.context as RectTransform;
        Debug.Log(rectTransform.sizeDelta);
    }

    [MenuItem("CONTEXT/RectTransform/LogAnchoredPosition")]
    static void LogAnchoredPosition(MenuCommand command)
    {
        RectTransform rectTransform = command.context as RectTransform;
        Debug.Log(rectTransform.anchoredPosition);
    }

    [MenuItem("CONTEXT/TextMeshProUGUI/Remove Component")]
    static void RemoveTMP(MenuCommand command)
    {
        var renderer = (command.context as MonoBehaviour).GetComponent<CanvasRenderer>();
        Object.DestroyImmediate(command.context);
        Object.DestroyImmediate(renderer);

    }




    [OnAddComponent(typeof(TextMeshProUGUI))]
    static void TMP(TMPro.TextMeshProUGUI text)
    {
        if (text == null) return;
        text.raycastTarget = false;
        text.richText = false;
        text.enableAutoSizing = false;
    }


    [MenuItem("GameObject/UI/Button - TextMeshPro", true, 2031)]
    static void AddButton(MenuCommand menuCommand) { }
    [MenuItem("GameObject/UI/Dropdown - TextMeshPro", true, 2036)]
    static void AddDropdown(MenuCommand menuCommand) { }
    [MenuItem("GameObject/UI/Text - TextMeshPro", true, 2001)]
    static void AddText(MenuCommand menuCommand) { }
    [MenuItem("GameObject/UI/Input Field - TextMeshPro", true, 2037)]
    static void AddInput(MenuCommand menuCommand) { }




    [MenuItem("GameObject/UI/Text -> TextMeshPro", false, 2031)]
    static void AddText2(MenuCommand menuCommand)
    {
        var method = typeof(TMPro_CreateObjectMenu).GetMethod("CreateTextMeshProGuiObjectPerform", BindingFlags.NonPublic | BindingFlags.Static);
        method.Invoke(null, new object[] { menuCommand });
    }
    [MenuItem("GameObject/UI/Button -> TextMeshPro", false, 2032)]
    static void AddButton2(MenuCommand menuCommand)
    {
        TMPro_CreateObjectMenu.AddButton(menuCommand);
        EditorTools.CallAddComponent(Selection.activeGameObject.GetComponentInChildren<TextMeshProUGUI>());

    }

    [MenuItem("GameObject/UI/Dropdown -> TextMeshPro", false, 2036)]
    static void AddDropdown2(MenuCommand menuCommand)
    {
        TMPro_CreateObjectMenu.AddDropdown(menuCommand);
        var drop = Selection.activeGameObject;

        var imgs = drop.GetComponentsInChildren<Image>(true);
        foreach (var item in imgs)
            EditorTools.CallAddComponent(item);
        var txts = drop.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var item in txts)
            EditorTools.CallAddComponent(item);
        var togs = drop.GetComponentsInChildren<Toggle>(true);
        foreach (var item in togs)
            EditorTools.CallAddComponent(item);
        EditorTools.CallAddComponent(drop.GetComponentInChildren<ScrollRect>(true));
        EditorTools.CallAddComponent(drop.GetComponentInChildren<Scrollbar>(true));
        drop.GetComponent<Image>().raycastTarget = true;
    }
    [MenuItem("GameObject/UI/Input Field -> TextMeshPro", false, 2037)]
    static void AddInput2(MenuCommand menuCommand)
    {

        var method = typeof(TMPro_CreateObjectMenu).GetMethod("AddTextMeshProInputField", BindingFlags.NonPublic | BindingFlags.Static);
        method.Invoke(null, new object[] { menuCommand });
        var drop = Selection.activeGameObject;

        var imgs = drop.GetComponentsInChildren<Image>(true);
        foreach (var item in imgs)
            EditorTools.CallAddComponent(item);
        var txts = drop.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var item in txts)
            EditorTools.CallAddComponent(item);
    }
}
partial class EditorGameTools
{
    [IFramework.OnAddComponent(typeof(Reporter))]
    static void Reporter(Reporter reporter)
    {
        MonoScript reporterScript = MonoScript.FromMonoBehaviour(reporter);
        string reporterPath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(reporterScript));
        reporter.LoadImages(reporterPath);
    }

    [MenuItem("Tools/开始游戏")]
    static void StartGame()
    {
        if (!EditorApplication.isPlaying)
        {
            var target = UnityEditor.EditorBuildSettings.scenes.FirstOrDefault();
            if (target == null) return;
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != target.path)
                EditorSceneManager.OpenScene(target.path, OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
        }
    }
}
