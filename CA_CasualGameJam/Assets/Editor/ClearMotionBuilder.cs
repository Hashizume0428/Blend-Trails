using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Applies only the approved clear motion, retaining puzzles and existing UI components.</summary>
public static class ClearMotionBuilder
{
    public static string[] ExistingStagePaths() => Enumerable.Range(1,StageCatalog.Count)
        .Select(StageCatalog.ScenePath).Where(File.Exists).ToArray();

    [MenuItem("Color Puzzle/Apply Approved Clear Motion")]
    public static void ApplyAll()
    {
        if (EditorApplication.isPlaying || Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))
            throw new InvalidOperationException("Stop Play mode and save open scenes before applying clear motion.");
        string[] paths = ExistingStagePaths();
        var puzzles = new Dictionary<string,string>();
        if (paths.Length == 0) throw new InvalidOperationException("No authored stage scenes exist.");
        string original = SceneManager.GetActiveScene().path;
        try
        {
            // URP global lights also register from preview scenes. Keep the active scene empty during validation.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            foreach (string path in paths)
            {
            Scene preview = EditorSceneManager.OpenPreviewScene(path);
            try
            {
                GameHud hud = preview.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<GameHud>(true)).Single();
                if (hud.ClearPresentation == null || hud.transform.Find("ClearPanel/Card/ToonClearHeading/SuccessBadge") == null)
                    throw new InvalidOperationException("Apply the B UI before clear motion: "+path);
                puzzles[path]=PuzzleSignature(preview.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<GameManager>(true)).Single());
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
            }
            foreach (string path in paths)
            {
                Scene scene = EditorSceneManager.OpenScene(path);
                Apply(UnityEngine.Object.FindFirstObjectByType<GameHud>());
                if(PuzzleSignature(UnityEngine.Object.FindFirstObjectByType<GameManager>())!=puzzles[path])
                    throw new InvalidOperationException("Puzzle data changed while applying presentation: "+path);
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("Approved clear motion applied to "+paths.Length+" existing stage scenes.");
        }
        finally { if (!string.IsNullOrEmpty(original)) EditorSceneManager.OpenScene(original); }
    }
    private static string PuzzleSignature(GameManager game)
    {
        var values=new List<string>{game.BoardSize.ToString()};
        foreach(TileController tile in game.TilesList)
        {
            var s=new SerializedObject(tile);
            var colors=s.FindProperty("_requiredColors");
            values.Add(tile.TileIndex+"|"+tile.Coordinate+"|"+tile.IsStartTile+"|"+
                s.FindProperty("_startColor").intValue+"|"+s.FindProperty("_hasRequiredColors").boolValue+"|"+
                s.FindProperty("_forbiddenColor").intValue+"|"+string.Join(",",Enumerable.Range(0,colors.arraySize).Select(i=>colors.GetArrayElementAtIndex(i).intValue)));
        }
        return string.Join(";",values);
    }
    public static void PrepareForSkin(GameHud hud)
    {
        Transform heading = hud.transform.Find("ClearPanel/Card/ToonClearHeading");
        Transform details = heading != null ? heading.Find("ClearDetails") : null;
        if (details != null)
            foreach (Transform child in details.Cast<Transform>().ToArray()) child.SetParent(heading,false);
        var backdrop = hud.ClearPresentation != null ? new SerializedObject(hud.ClearPresentation).FindProperty("_cardBackdrop").objectReferenceValue as CanvasGroup : null;
        Transform anchor = backdrop != null ? backdrop.transform.Find("SafeArea/CreamCardAnchor") : null;
        Transform card = anchor != null ? anchor.Find("CreamCard") : null;
        if (card != null) card.SetParent(anchor.parent,false);
    }
    public static void Apply(GameHud hud)
    {
        if (hud == null) throw new ArgumentNullException(nameof(hud));
        var panel = (RectTransform)hud.transform.Find("ClearPanel");
        var heading = panel != null ? panel.Find("Card/ToonClearHeading") as RectTransform : null;
        if (heading == null) return; // The historical, unskinned builder can still run before the B skin.
        StageClearPresentation presentation = hud.ClearPresentation;
        var ps = new SerializedObject(presentation);
        var backdrop = ps.FindProperty("_cardBackdrop").objectReferenceValue as CanvasGroup;
        if (backdrop == null) throw new InvalidOperationException("The B clear backdrop is missing.");
        var safe = (RectTransform)backdrop.transform.Find("SafeArea");
        var anchor = Rect(safe,"CreamCardAnchor");
        var cream = anchor.Find("CreamCard") as RectTransform;
        if (cream == null) cream = safe.Find("CreamCard") as RectTransform;
        if (cream == null) throw new InvalidOperationException("The cream clear card is missing.");
        cream.SetParent(anchor,false); Stretch(cream);
        CanvasGroup body = Get<CanvasGroup>(cream.gameObject); body.alpha=0; body.blocksRaycasts=body.interactable=false;
        var layout = hud.GetComponent<StageScreenLayout>(); Set(layout,"_clearFrame",anchor);
        var details = Rect(heading,"ClearDetails"); Stretch(details);
        foreach (string name in new[]{"ToonClearStage","ToonClearMessage"})
        {
            Transform item = heading.Find(name);
            if (item != null) item.SetParent(details,false);
        }
        Place((RectTransform)details.Find("ToonClearStage"),180,507,581,89);
        Place((RectTransform)details.Find("ToonClearMessage"),100,602,741,53);
        var detailGroup = Get<CanvasGroup>(details.gameObject); detailGroup.alpha=0; detailGroup.blocksRaycasts=detailGroup.interactable=false;
        var intro = Get<UnityEngine.UI.Image>(Rect(panel,"ClearIntroScrim").gameObject);
        Stretch(intro.rectTransform); intro.rectTransform.SetAsFirstSibling(); intro.color=new Color(0,.07f,.19f,0); intro.raycastTarget=false;
        var sheen = Get<ClearSheenGraphic>(Rect(panel,"ClearBoardSheen").gameObject);
        Stretch(sheen.rectTransform); sheen.rectTransform.SetSiblingIndex(1); sheen.Configure(12,54); sheen.raycastTarget=false;
        var rear = panel.Find("ClearConfetti").GetComponent<ClearConfetti>();
        rear.transform.SetSiblingIndex(2); SetBool(rear,"_foreground",false);
        var front = Get<ClearConfetti>(Rect(panel,"ClearForegroundConfetti").gameObject);
        Stretch(front.rectTransform); front.rectTransform.SetAsLastSibling(); SetBool(front,"_foreground",true); front.raycastTarget=false;
        var celebration = Get<ClearCelebrationGraphic>(Rect(heading,"ClearCelebration").gameObject);
        Stretch(celebration.rectTransform); celebration.rectTransform.SetAsFirstSibling(); celebration.raycastTarget=false;
        var title = heading.Find("ToonClearTitle").GetComponent<TMPro.TMP_Text>();
        const string titleMaterialPath="Assets/UI/ToonBold/ClearTitle.mat";
        var titleMaterial=AssetDatabase.LoadAssetAtPath<Material>(titleMaterialPath);
        if(titleMaterial==null)
        {
            titleMaterial=new Material(ToonSkinAssets.Current.OutlinedText){name="ClearTitle"};
            titleMaterial.EnableKeyword("UNDERLAY_ON");
            titleMaterial.SetColor("_UnderlayColor",new Color32(0,137,221,255));
            titleMaterial.SetFloat("_UnderlayOffsetY",-.65f);
            titleMaterial.SetFloat("_UnderlayDilate",.2f);
            AssetDatabase.CreateAsset(titleMaterial,titleMaterialPath);
        }
        title.fontSharedMaterial=titleMaterial;
        var animator = Get<ClearTitleAnimator>(title.gameObject);
        var next = (RectTransform)panel.Find("Card/ClearActions/NextStageButton");
        var nextSheen = Get<ClearSheenGraphic>(Rect(next,"ClearButtonSheen").gameObject);
        Stretch(nextSheen.rectTransform); nextSheen.rectTransform.SetAsLastSibling(); nextSheen.Configure(30,60); nextSheen.raycastTarget=false;
        var ls = new SerializedObject(layout);
        foreach (string name in new[]{"_home","_stageLabel","_mixLegend","_reloadPanel"})
        {
            var rect = ls.FindProperty(name).objectReferenceValue as RectTransform;
            if (rect != null) Get<CanvasGroup>(rect.gameObject).alpha=1;
        }
        Set(presentation,"_badge",heading.Find("SuccessBadge").GetComponent<ToonClearBadge>());
        Set(presentation,"_cardBody",body); Set(presentation,"_card",cream); Set(presentation,"_details",detailGroup);
        Set(presentation,"_nextButton",next); Set(presentation,"_nextArrow",next.Find("ToonNextIcon"));
        Set(presentation,"_introScrim",intro); Set(presentation,"_backgroundScrim",backdrop.transform.Find("Scrim").GetComponent<UnityEngine.UI.Image>());
        Set(presentation,"_boardVisuals",Get<ClearBoardVisuals>(hud.gameObject)); Set(presentation,"_titleAnimator",animator);
        Set(presentation,"_celebration",celebration); Set(presentation,"_boardSheen",sheen); Set(presentation,"_buttonSheen",nextSheen);
        Set(presentation,"_foregroundConfetti",front);
        presentation.ResetPresentation(); Canvas.ForceUpdateCanvases(); layout.ApplyLayout();
        EditorUtility.SetDirty(layout); EditorUtility.SetDirty(presentation); EditorUtility.SetDirty(hud);
    }
    private static RectTransform Rect(Transform parent,string name)
    {
        var rect=parent.Find(name) as RectTransform;
        if (rect!=null)return rect;
        rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);return rect;
    }
    private static void Stretch(RectTransform r) { r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;r.localScale=Vector3.one; }
    private static void Place(RectTransform r,float x,float y,float w,float h) { r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=Vector2.one*.5f;r.anchoredPosition=new Vector2(x+w*.5f,-y-h*.5f);r.sizeDelta=new Vector2(w,h); }
    private static T Get<T>(GameObject go) where T:Component { T value=go.GetComponent<T>();return value!=null?value:go.AddComponent<T>(); }
    private static void Set(UnityEngine.Object target,string name,UnityEngine.Object value) { var s=new SerializedObject(target);s.FindProperty(name).objectReferenceValue=value;s.ApplyModifiedPropertiesWithoutUndo(); }
    private static void SetBool(UnityEngine.Object target,string name,bool value) { var s=new SerializedObject(target);s.FindProperty(name).boolValue=value;s.ApplyModifiedPropertiesWithoutUndo(); }
}
