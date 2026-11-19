using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Applies the approved title motion without rebuilding the title UI or other scenes.</summary>
public static class TitleMotionBuilder
{
    [MenuItem("Color Puzzle/Apply Approved Title Motion")]
    public static void ApplyTitle()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode before applying title motion.");
        Scene title=SceneManager.GetSceneByPath(MenuSceneBuilder.TitlePath);
        bool opened=!title.IsValid()||!title.isLoaded;
        if(!opened&&title.isDirty)throw new InvalidOperationException("Save the open title scene before applying title motion.");
        try
        {
            if(opened)title=EditorSceneManager.OpenScene(MenuSceneBuilder.TitlePath,OpenSceneMode.Additive);
            MenuNavigation menu=title.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<MenuNavigation>(true)).Single();
            Apply(menu);EditorSceneManager.MarkSceneDirty(title);EditorSceneManager.SaveScene(title);AssetDatabase.SaveAssets();
        }
        finally{if(opened&&title.IsValid())EditorSceneManager.CloseScene(title,true);}
        Debug.Log("Approved title motion applied. Stage progression and stage scenes retained.");
    }
    public static void Apply(MenuNavigation menu)
    {
        var content=menu.transform.Find("ToonSafeArea/ToonTitleContent") as RectTransform;
        if(content==null)throw new InvalidOperationException("The existing Toon title content is missing.");
        var logo=content.Find("ToonTitleLogo") as RectTransform;
        var mix=content.Find("ToonTitleMix") as RectTransform;
        var settings=new SerializedObject(menu);
        var button=settings.FindProperty("_continueButton").objectReferenceValue as UnityEngine.UI.Button;
        if(logo==null||mix==null||button==null)throw new InvalidOperationException("The existing title logo, mix or button is missing.");
        var arrow=button.transform.Find("ToonPlayIcon") as RectTransform;
        var sky=menu.gameObject.scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<ToonSkyGraphic>(true)).SingleOrDefault();
        if(arrow==null||sky==null)throw new InvalidOperationException("The existing title arrow or sky is missing.");
        var motion=Get<TitlePresentation>(menu.gameObject);
        var logoGroup=Get<CanvasGroup>(logo.gameObject);var mixGroup=Get<CanvasGroup>(mix.gameObject);var buttonGroup=Get<CanvasGroup>(button.gameObject);
        logoGroup.alpha=mixGroup.alpha=buttonGroup.alpha=1;
        logoGroup.blocksRaycasts=logoGroup.interactable=mixGroup.blocksRaycasts=mixGroup.interactable=false;
        buttonGroup.blocksRaycasts=buttonGroup.interactable=true;
        var colors=button.colors;colors.disabledColor=colors.normalColor;button.colors=colors;
        var mixLight=Get<TitleMixGraphic>(Rect(mix,"TitleMixLight").gameObject);
        mixLight.raycastTarget=false;mixLight.color=Color.white;
        // The built-in UI mask clips the white sheen to the original transparent logo.
        var maskRect=Rect(logo,"TitleLogoSheenMask");var maskImage=Get<UnityEngine.UI.Image>(maskRect.gameObject);
        maskImage.sprite=logo.GetComponent<UnityEngine.UI.Image>().sprite;maskImage.preserveAspect=true;maskImage.raycastTarget=false;
        Get<UnityEngine.UI.Mask>(maskRect.gameObject).showMaskGraphic=false;
        var logoSheen=Get<ClearSheenGraphic>(Rect(maskRect,"TitleLogoSheen").gameObject);logoSheen.Configure(0,0);logoSheen.raycastTarget=false;
        var buttonSheen=Get<ClearSheenGraphic>(Rect(button.transform,"TitleButtonSheen").gameObject);
        buttonSheen.Configure(20,54);buttonSheen.color=new Color(1,1,1,.65f);buttonSheen.raycastTarget=false;
        var sparkles=Get<TitleSparkleGraphic>(Rect(content,"TitleSparkles").gameObject);sparkles.raycastTarget=false;sparkles.color=Color.white;
        Set(motion,"_logo",logo);Set(motion,"_mix",mix);Set(motion,"_button",button.transform);Set(motion,"_arrow",arrow);
        Set(motion,"_logoOpacity",logoGroup);Set(motion,"_mixOpacity",mixGroup);Set(motion,"_buttonOpacity",buttonGroup);
        Set(motion,"_sky",sky);Set(motion,"_mixLight",mixLight);Set(motion,"_sparkles",sparkles);
        Set(motion,"_logoSheen",logoSheen);Set(motion,"_buttonSheen",buttonSheen);
        settings.Update();settings.FindProperty("_titlePresentation").objectReferenceValue=motion;settings.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(menu);EditorUtility.SetDirty(motion);
    }
    private static T Get<T>(GameObject obj)where T:Component
    {T value=obj.GetComponent<T>();return value!=null?value:obj.AddComponent<T>();}
    private static RectTransform Rect(Transform parent,string name)
    {
        var rect=parent.Find(name)as RectTransform;
        if(rect==null){rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);}
        rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
        rect.localScale=Vector3.one;rect.localRotation=Quaternion.identity;rect.SetAsLastSibling();return rect;
    }
    private static void Set(UnityEngine.Object target,string field,UnityEngine.Object value)
    {var s=new SerializedObject(target);s.FindProperty(field).objectReferenceValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
}
