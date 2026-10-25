using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class StoreShots
{
    const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    static string Project => Directory.GetCurrentDirectory();
    static string Root => Path.Combine(Project,"StoreScreenshots");
    static string Tools => Path.Combine(Project,"output/store-screenshots-tools");
    static string Backup => Path.Combine(Tools,"editor-backup.json");
    static readonly List<object> Shots = new List<object>();
    static MatchCollection Routes;
    static MethodInfo Encoder;
    static int StartFrame;

    static Type EditorType(string name) => typeof(Editor).Assembly.GetType("UnityEditor."+name);
    static object Sizes => EditorType("GameViewSizes").BaseType.GetProperty("instance",Flags).GetValue(null);
    static object Group => EditorType("GameViewSizes").GetProperty("currentGroup",Flags).GetValue(Sizes);
    static EditorWindow View => EditorWindow.GetWindow(EditorType("GameView"));
    static void Write(string path,object value) => File.WriteAllText(path,JsonConvert.SerializeObject(value,Formatting.Indented));

    public static object Setup()
    {
        if(EditorApplication.isPlaying || SceneManager.GetActiveScene().isDirty) throw new Exception("Capture requires a stopped Editor and saved scene.");
        if(Directory.Exists(Root) && Directory.GetFiles(Root,"*.png").Length>0) throw new Exception("Screenshot destination already contains images.");
        Directory.CreateDirectory(Root); Directory.CreateDirectory(Tools);
        var group=Group; var view=View;
        int oldIndex=(int)EditorType("GameView").GetProperty("selectedSizeIndex",Flags).GetValue(view);
        int count=(int)group.GetType().GetMethod("GetTotalCount",Flags).Invoke(group,null);
        var autoType=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Unity.Pipeline.Editor.Commands.AutoTickCommand")).First(t=>t!=null);
        Write(Backup,new {
            scene=SceneManager.GetActiveScene().path,oldIndex=oldIndex,captureIndex=count,
            background=Application.runInBackground,
            progressPresent=PlayerPrefs.HasKey("ColorPuzzle.CurrentStageNumber"),progress=PlayerPrefs.GetInt("ColorPuzzle.CurrentStageNumber",1),
            statePresent=PlayerPrefs.HasKey("ColorPuzzle.StageProgress.v2"),progressState=PlayerPrefs.GetString("ColorPuzzle.StageProgress.v2",""),
            autoTick=(bool)autoType.GetProperty("IsEnabled",Flags).GetValue(null),
            autoInterval=Convert.ToInt32(autoType.GetProperty("CurrentIntervalMs",Flags).GetValue(null))
        });
        var kind=EditorType("GameViewSizeType");
        var ctor=EditorType("GameViewSize").GetConstructor(Flags,null,new[]{kind,typeof(int),typeof(int),typeof(string)},null);
        var size=ctor.Invoke(new object[]{Enum.Parse(kind,"FixedResolution"),1080,1920,"Store screenshots 1080x1920"});
        group.GetType().GetMethod("AddCustomSize",Flags).Invoke(group,new[]{size});
        EditorType("GameView").GetProperty("selectedSizeIndex",Flags).SetValue(view,count);
        view.Focus(); view.Repaint(); Application.runInBackground=true;
        return new {ready=true,width=1080,height=1920,destination=Root};
    }

    public static object Start()
    {
        if(!EditorApplication.isPlaying) throw new Exception("Enter Play mode first.");
        Routes=Regex.Matches(File.ReadAllText(Path.Combine(Project,"Assets/Tests/PlayMode/StageTestReference.cs")),"new Reference\\(\"([^\"]*)\", \"([^\"]*)\", \"[^\"]*\", \"[^\"]*\", \"[^\"]*\"\\)");
        if(Routes.Count!=60) throw new Exception("Expected sixty reference solutions.");
        var capture=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Unity.Pipeline.Editor.Commands.Capture.CaptureCommands")).First(t=>t!=null);
        Encoder=capture.GetMethod("EncodeScreenToPng",Flags);
        Shots.Clear(); StartFrame=Time.frameCount;
        Run().Forget();
        return new {started=true,planned=20,startFrame=StartFrame};
    }

    static async UniTaskVoid Run()
    {
        try
        {
            Status("running","Starting capture");
            await UniTask.DelayFrame(6);
            if(Time.frameCount<=StartFrame) throw new Exception("The player loop is not advancing.");
            StageProgress.EnterStage(1);
            await Load("Title"); await Capture("01_title",0,"Title","title");
            var game=await Stage(1);
            await UniTask.Delay(1800,ignoreTimeScale:true);
            await Capture("02_stage01_tutorial",1,"Stage 1 / tutorial guide","tutorial");
            int[] stages={3,3,5,6,8,10,11,13,15,21,24,32,40,45,60};
            bool[] starts={true,false,false,true,false,false,true,false,false,true,false,false,false,false,false};
            for(int i=0;i<stages.Length;i++)
            {
                int stage=stages[i]; game=await Stage(stage);
                string state=starts[i]?"start":"mixing";
                if(!starts[i])
                {
                    bool reverse=i%3==2;
                    var first=RouteCells(stage,reverse?2:1); var second=RouteCells(stage,reverse?1:2);
                    Draw(game,first,first.Length); await game.CommitLineAsync();
                    int count=Mathf.Clamp(Mathf.RoundToInt(second.Length*(.72f+.07f*(i%3))),2,second.Length-1);
                    int crossing=Array.FindIndex(second,p=>first.Contains(p));
                    if(crossing>=0) count=Math.Min(second.Length-1,Math.Max(count,crossing+2));
                    Draw(game,second,count); await UniTask.Delay(320,ignoreTimeScale:true);
                }
                await Capture($"{i+3:00}_stage{stage:00}_{state}",stage,$"Stage {stage} / {state}",state);
            }
            int[] clears={3,8,13};
            for(int i=0;i<clears.Length;i++)
            {
                int stage=clears[i]; game=await Stage(stage);
                var first=RouteCells(stage,1); var second=RouteCells(stage,2);
                Draw(game,first,first.Length); await game.CommitLineAsync(); Draw(game,second,second.Length);
                if(!game.StageClearCheck()) throw new Exception($"Stage {stage} solution failed.");
                game.CommitLineAsync().Forget();
                var clear=UnityEngine.Object.FindFirstObjectByType<StageClearPresentation>(FindObjectsInactive.Include);
                await UniTask.WaitUntil(()=>clear.IsPlaying);
                float target=i==0?1.75f:3.68f;
                await UniTask.WaitUntil(()=>clear.ElapsedSeconds>=target);
                string state=i==0?"celebration":"clear";
                await Capture($"{18+i:00}_stage{stage:00}_{state}",stage,$"Stage {stage} / {state}",state);
                await UniTask.WaitUntil(()=>game.State==GameState.Cleared);
            }
            Write(System.IO.Path.Combine(Tools,"capture-manifest.json"),new {source="Native 1080x1920 Unity Game view, including overlay UI",startFrame=StartFrame,endFrame=Time.frameCount,shots=Shots});
            Status("complete","All twenty screenshots captured");
        }
        catch(Exception e) { Status("failed",e.ToString()); Debug.LogError("Store screenshot capture: "+e); }
    }

    static async UniTask Load(string scene)
    {
        Status("running","Loading "+scene);
        var previous=UnityEngine.Object.FindFirstObjectByType<GameManager>();
        if(previous!=null && previous.State==GameState.Drawing) previous.CancelLine();
        await SceneManager.LoadSceneAsync(scene,LoadSceneMode.Single);
        View.Focus(); View.Repaint();
        await UniTask.DelayFrame(5); await UniTask.Delay(400,ignoreTimeScale:true);
    }
    static async UniTask<GameManager> Stage(int stage)
    {
        await Load(StageCatalog.SceneName(stage));
        var game=UnityEngine.Object.FindFirstObjectByType<GameManager>();
        if(game==null || !game.IsInitialized || game.GetComponent<StageClearNavigation>().StageNumber!=stage) throw new Exception("Stage is not ready.");
        return game;
    }
    static Vector2Int[] RouteCells(int stage,int role)
    {
        var corners=Routes[stage-1].Groups[role].Value.Split(new[]{' '},StringSplitOptions.RemoveEmptyEntries).Select(s=>new Vector2Int(s[1]-'1',s[0]-'1')).ToArray();
        var cells=new List<Vector2Int>{corners[0]};
        for(int i=1;i<corners.Length;i++)
        {
            var from=cells[cells.Count-1]; var to=corners[i];
            if(from.x!=to.x && from.y!=to.y) throw new Exception("Diagonal reference path.");
            var step=new Vector2Int(Math.Sign(to.x-from.x),Math.Sign(to.y-from.y));
            while(from!=to) { from+=step; cells.Add(from); }
        }
        return cells.ToArray();
    }
    static void Draw(GameManager game,Vector2Int[] path,int count)
    {
        var start=game.GetTile(path[0]);
        if(!game.TryBeginLine(start.TileIndex,start.StartColor)) throw new Exception("Cannot begin line.");
        for(int i=1;i<count;i++) if(!game.TryExtendLine(game.GetTile(path[i]).TileIndex)) throw new Exception("Cannot extend line.");
    }
    static async UniTask Capture(string name,int stage,string label,string state)
    {
        Canvas.ForceUpdateCanvases(); View.Repaint(); await UniTask.DelayFrame(3);
        var type=EditorType("PlayModeView"); var view=type.GetMethod("GetMainPlayModeView",Flags).Invoke(null,null);
        var rt=type.GetField("m_TargetTexture",Flags).GetValue(view) as RenderTexture;
        if(rt==null || rt.width!=1080 || rt.height!=1920) throw new Exception($"Unexpected render target: {rt?.width}x{rt?.height}");
        byte[] bytes=(byte[])Encoder.Invoke(null,new object[]{1080,1920});
        if(bytes.Length>=8000000) throw new Exception("Screenshot exceeds 8MB.");
        string file=name+".png"; File.WriteAllBytes(System.IO.Path.Combine(Root,file),bytes);
        var game=UnityEngine.Object.FindFirstObjectByType<GameManager>();
        Shots.Add(new {file=file,label=label,state=state,stage=stage,width=1080,height=1920,bytes=bytes.Length,frame=Time.frameCount,
            correctTiles=game!=null?game.CorrectTileCount:-1,palette=game!=null?game.Palette.First+"+"+game.Palette.Second:"",gameState=game!=null?game.State.ToString():""});
        Status("running",label);
    }
    static void Status(string state,string message) => Write(System.IO.Path.Combine(Tools,"status.json"),new {state=state,count=Shots.Count,message=message,frame=Time.frameCount});

    public static object Restore()
    {
        if(EditorApplication.isPlaying) throw new Exception("Stop Play mode first.");
        var saved=JObject.Parse(File.ReadAllText(Backup));
        if((bool)saved["progressPresent"]) PlayerPrefs.SetInt("ColorPuzzle.CurrentStageNumber",(int)saved["progress"]); else PlayerPrefs.DeleteKey("ColorPuzzle.CurrentStageNumber");
        if((bool)saved["statePresent"]) PlayerPrefs.SetString("ColorPuzzle.StageProgress.v2",(string)saved["progressState"]); else PlayerPrefs.DeleteKey("ColorPuzzle.StageProgress.v2");
        PlayerPrefs.Save(); Application.runInBackground=(bool)saved["background"];
        EditorType("GameView").GetProperty("selectedSizeIndex",Flags).SetValue(View,(int)saved["oldIndex"]);
        Group.GetType().GetMethod("RemoveCustomSize",Flags).Invoke(Group,new object[]{(int)saved["captureIndex"]});
        var autoType=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Unity.Pipeline.Editor.Commands.AutoTickCommand")).First(t=>t!=null);
        autoType.GetMethod("SetAutoTick",Flags).Invoke(null,new object[]{(bool)saved["autoTick"],(int)saved["autoInterval"],false});
        if(SceneManager.GetActiveScene().path!=(string)saved["scene"]) UnityEditor.SceneManagement.EditorSceneManager.OpenScene((string)saved["scene"]);
        View.Repaint();
        if(PlayerPrefs.GetString("ColorPuzzle.StageProgress.v2")!=(string)saved["progressState"]) throw new Exception("Saved progress restoration failed.");
        var result=new {restored=true,scene=SceneManager.GetActiveScene().path,dirty=SceneManager.GetActiveScene().isDirty,playing=EditorApplication.isPlaying};
        Write(System.IO.Path.Combine(Tools,"restoration.json"),result); return result;
    }
}
