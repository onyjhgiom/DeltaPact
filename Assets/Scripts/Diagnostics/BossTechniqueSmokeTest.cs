using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace MoriEchoes.Diagnostics
{
    public sealed class BossTechniqueSmokeTest : MonoBehaviour
    {
        [Serializable] private sealed class Report
        {
            public bool passed;
            public string scope = "Windows runtime: all 30 skills; live warning/path retargeting, final commitment, stationary-player HP loss and expired-field cleanup. Assisted teleports, cinematic player lock, partial invulnerability and reflection-forced phase/cadence. Not an unassisted campaign or human balance test.";
            public List<string> checks = new List<string>();
            public List<string> errors = new List<string>();
            public List<string> screenshots = new List<string>();
        }
        private readonly Report report = new Report();
        private GameManager game;
        private string folder;
        private float deadline;
        private bool finished;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Launch()
        {
            string[] args=Environment.GetCommandLineArgs();
            int flag=Array.IndexOf(args,"-moriBossTechniqueSmoke");
            if(flag<0 || !Application.isEditor && !Debug.isDebugBuild) return;
            CampaignProgress.Volatile=true;CampaignProgress.ClearVolatile();CampaignProgress.Unlock(10);
            var entity=new GameObject("BossTechniqueRuntimeAcceptance");DontDestroyOnLoad(entity);
            var test=entity.AddComponent<BossTechniqueSmokeTest>();
            test.folder=Path.GetFullPath(args[flag+1]);Directory.CreateDirectory(test.folder);
            test.deadline=Time.realtimeSinceStartup+270;
            Application.logMessageReceived+=test.OnLog;
            test.StartCoroutine(test.Guard(test.Run()));
        }

        private IEnumerator Guard(IEnumerator root)
        {
            var stack=new Stack<IEnumerator>();stack.Push(root);
            while(stack.Count>0 && !finished)
            {
                object current=null;
                try
                {
                    if(!stack.Peek().MoveNext()){stack.Pop();continue;}
                    current=stack.Peek().Current;
                    if(current is IEnumerator nested){stack.Push(nested);continue;}
                }
                catch(Exception error){report.errors.Add(error.ToString());Complete();yield break;}
                yield return current;
            }
            Complete();
        }

        private IEnumerator Run()
        {
            yield return DiagnosticWindow.Prepare();game=FindAnyObjectByType<GameManager>();
            var names=new HashSet<string>();var images=new HashSet<Sprite>();
            for(int stage=0;stage<10;stage++)
            {
                game.ReturnToMenu();yield return null;game.SelectCharacter(0);game.SelectStage(stage);game.BeginRun();
                yield return new WaitForSecondsRealtime(0.35f);
                game.Level.enabled=false;
                foreach(EnemyAI mob in new List<EnemyAI>(game.Enemies)) if(mob!=null) mob.Recycle();
                Vector2 arena = stage<5 ? new Vector2(game.Level.Segments[game.Level.Segments.Length-1].End-8,1.8f)
                    : stage<8 ? new Vector2(0,game.Level.Vertical.DepthLength-7.2f)
                    : new Vector2(218,stage==8?game.Level.Swim.SurfaceY:0);
                Vector2 player=stage<5?arena+new Vector2(-5,0):stage<8?arena+new Vector2(0,-3.4f):arena+Vector2.left*5;
                game.Player.Health.Invulnerable=true;game.Player.Motor.ResetAt(player);game.Player.Motor.SetCinematicLocked(true);
                EnemyAI actor=game.Level.Spawn(EnemyKind.Boss,arena);game.Level.SetBoss(actor);
                BossController boss=actor.GetComponent<BossController>();boss.enabled=false;
                if(game.Level.Swim!=null) game.Level.Swim.Tick();
                yield return new WaitForSecondsRealtime(0.4f);
                BossTechniqueController techniques=boss.Techniques;
                Check(techniques!=null,"Stage "+(stage+1)+" has an independent technique scheduler");
                for(int slot=0;slot<3;slot++)
                {
                    SetPhase(boss,slot+1);
                    typeof(BossTechniqueController).GetField("nextNativeCount",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(techniques,0);
                    BossTechniqueProfile profile=game.Level.Theme.bossTechniques[slot];
                    Check(names.Add(profile.name) && !string.IsNullOrWhiteSpace(profile.release) && !string.IsNullOrWhiteSpace(profile.visual)
                        && !string.IsNullOrWhiteSpace(profile.dodge),"Unique named/described skill "+profile.name);
                    Check(profile.frames.Length==2 && images.Add(profile.frames[0]) && images.Add(profile.frames[1]),"Dedicated release and impact art: "+profile.name);
                    int before=game.Effects.BossTechniqueEmissions;
                    Check(techniques.TryBegin() && techniques.LastSlot==slot,"Phase "+(slot+1)+" unlocks "+profile.name);
                    Check(game.Effects.BossTechniqueEmissions>before && techniques.Fields.Count>0,"Actual release art and damage fields: "+profile.name);
                    foreach(BossField field in techniques.Fields)
                    {
                        Check(field.Damage>=17 && field.Damage<=23 && field.WarningRemaining>=1.05f,"Bounded damage and at least 1.05s warning: "+profile.name);
                        Check(!field.TargetCommitted && field.PullRadius==0 && field.FreezeSeconds==0,"Live warning starts without pull or freeze: "+profile.name);
                    }
                    var origins=new Dictionary<BossField,Vector2>();
                    var routes=new Dictionary<BossField,Vector2[]>();
                    foreach(BossField field in techniques.Fields){origins[field]=field.transform.position;routes[field]=field.Path;}
                    Vector2 oldTarget=game.Player.transform.position;
                    Vector2 shift=new Vector2(0.85f,0.25f);
                    game.Player.Motor.ResetAt(oldTarget+shift);
                    yield return new WaitForSecondsRealtime(0.12f);
                    techniques.Step();
                    foreach(BossField field in techniques.Fields)
                    {
                        Check(Vector2.Distance((Vector2)field.transform.position,origins[field]+shift)<0.08f,"Warning follows player before commitment: "+profile.name);
                        Vector2[] route=field.Path;
                        for(int point=0;point<route.Length;point++)
                            Check(Vector2.Distance(route[point],routes[field][point]+shift)<0.08f,"Swept damage route follows its red preview: "+profile.name);
                    }
                    float lockDeadline=Time.realtimeSinceStartup+1.5f;
                    while(!techniques.TargetCommitted && Time.realtimeSinceStartup<lockDeadline){techniques.Step();yield return null;}
                    Check(techniques.TargetCommitted && techniques.TargetStrike.WarningRemaining>=0.7f,"Final commitment leaves a readable escape window: "+profile.name);
                    foreach(BossField field in techniques.Fields)
                        Check(field.TargetCommitted && !field.FollowWarningAt((Vector2)field.transform.position+Vector2.right*9),"All components stop tracking together: "+profile.name);
                    Check(techniques.TargetStrike.Contains(game.Player.transform.position),"Skill directly threatens stationary player: "+profile.name);
                    float healthBefore=game.Player.Health.Current;
                    game.Player.Health.Invulnerable=false;
                    yield return Snap($"stage-{stage+1:00}-skill-{slot+1}-warning.png");
                    bool captured=false;float until=Time.realtimeSinceStartup+7;
                    while(techniques.Active && Time.realtimeSinceStartup<until)
                    {
                        techniques.Step();
                        if(game.Player.Health.Current<healthBefore) game.Player.Health.Invulnerable=true;
                        if(!captured)
                            foreach(BossField field in techniques.Fields)
                                if(field!=null && field.Active)
                                {
                                    captured=true;
                                    SpriteRenderer art=field.GetComponentInChildren<SpriteRenderer>();
                                    Check(art!=null,"Runtime effect renderer exists: "+profile.name);
                                    yield return Snap($"stage-{stage+1:00}-skill-{slot+1}-active.png");break;
                                }
                        yield return null;
                    }
                    Check(captured && !techniques.Active,"Attack resolves and returns control: "+profile.name
                        +" (state="+game.State+", HP="+game.Player.Health.Current+")");
                    Check(game.Player.Health.Current<healthBefore && !game.Player.Health.IsDead,"Stationary player takes survivable real damage: "+profile.name);
                    game.Player.Health.Invulnerable=true;game.Player.Health.Restore(game.Player.Health.Maximum);
                    foreach(BossField field in techniques.Fields) Check(field==null || !field.Active,"Expired field cannot continue damaging: "+profile.name);
                }
            }
            Check(names.Count==30 && images.Count==60,"Ten bosses: thirty distinct skills and sixty dedicated images");
        }

        private static void SetPhase(BossController boss,int phase)
        {
            object target=boss.Side!=null?(object)boss.Side:boss.Vertical!=null?(object)boss.Vertical:boss.Swim;
            string field=boss.Swim!=null?"phase":"<Phase>k__BackingField";
            target.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,phase);
        }

        private IEnumerator Snap(string name)
        {
            yield return new WaitForEndOfFrame();
            Texture2D texture=ScreenCapture.CaptureScreenshotAsTexture();
            string path=Path.Combine(folder,name);File.WriteAllBytes(path,texture.EncodeToPNG());Destroy(texture);
            report.screenshots.Add(path);
        }

        private void Check(bool value,string description)
        {
            if(!value) throw new InvalidOperationException(description);
            report.checks.Add(description);
        }
        private void OnLog(string message,string trace,LogType type)
        {if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)report.errors.Add(message+"\n"+trace);}
        private void Update()
        {
            if(finished)return;
            if(game!=null && game.State==RunState.Paused)game.Resume();
            if(Time.realtimeSinceStartup>deadline){report.errors.Add("Technique runtime test timed out");Complete();}
        }
        private void Complete()
        {
            if(finished)return;finished=true;report.passed=report.errors.Count==0;
            File.WriteAllText(Path.Combine(folder,"boss-techniques-smoke.json"),JsonUtility.ToJson(report,true));
            Application.logMessageReceived-=OnLog;Application.Quit(report.passed?0:1);
        }
    }
}
