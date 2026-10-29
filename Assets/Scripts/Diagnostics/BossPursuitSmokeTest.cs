#if ENABLE_INPUT_SYSTEM
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace MoriEchoes.Diagnostics
{
    public sealed class BossPursuitSmokeTest : MonoBehaviour
    {
        [Serializable] private sealed class Report
        {
            public bool passed;
            public string scope = "Windows runtime across ten bosses: obstacle collision bypass, telegraphed fast approach, pause, preserved bullet hitbox, live player tracking, real stationary damage and synthetic-keyboard escape after commitment. Assisted arena placement, burst cooldown and player health; not human balance or an unassisted campaign.";
            public List<string> checks = new List<string>();
            public List<string> errors = new List<string>();
            public List<string> screenshots = new List<string>();
        }
        private readonly Report report = new Report();
        private readonly List<InputDevice> suppressed = new List<InputDevice>();
        private GameManager game;
        private Keyboard keyboard;
        private Mouse mouse;
        private string folder;
        private float deadline;
        private bool finished;
        private InputSettings.BackgroundBehavior previousBackground;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Launch()
        {
            string[] args = Environment.GetCommandLineArgs();
            int flag = Array.IndexOf(args, "-moriBossPursuitSmoke");
            if (flag < 0 || !Application.isEditor && !Debug.isDebugBuild) return;
            CampaignProgress.Volatile = true; CampaignProgress.ClearVolatile(); CampaignProgress.Unlock(10);
            var entity = new GameObject("BossPursuitRuntimeAcceptance"); DontDestroyOnLoad(entity);
            var test = entity.AddComponent<BossPursuitSmokeTest>();
            test.folder = Path.GetFullPath(args[flag + 1]); Directory.CreateDirectory(test.folder);
            test.deadline = Time.realtimeSinceStartup + 300;
            Application.logMessageReceived += test.OnLog;
            test.StartCoroutine(test.Guard(test.Run()));
        }

        private IEnumerator Guard(IEnumerator root)
        {
            var stack = new Stack<IEnumerator>(); stack.Push(root);
            while (stack.Count > 0 && !finished)
            {
                object current = null;
                try
                {
                    if (!stack.Peek().MoveNext()) { stack.Pop(); continue; }
                    current = stack.Peek().Current;
                    if (current is IEnumerator nested) { stack.Push(nested); continue; }
                }
                catch (Exception error) { report.errors.Add(error.ToString()); Complete(); yield break; }
                yield return current;
            }
            Complete();
        }

        private IEnumerator Run()
        {
            yield return DiagnosticWindow.Prepare(); game = FindAnyObjectByType<GameManager>();
            previousBackground = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            foreach (InputDevice device in InputSystem.devices)
                if (device.enabled && (device is Keyboard || device is Mouse || device is Gamepad)) suppressed.Add(device);
            foreach (InputDevice device in suppressed) InputSystem.DisableDevice(device);
            keyboard = InputSystem.AddDevice<Keyboard>("BossPursuitKeyboard"); mouse = InputSystem.AddDevice<Mouse>("BossPursuitMouse");
            for (int stage = 0; stage < 10; stage++) yield return Probe(stage);
        }

        private IEnumerator Probe(int stage)
        {
            Keys(); game.ReturnToMenu(); yield return null;
            game.SelectCharacter(0); game.SelectStage(stage); game.BeginRun();
            yield return new WaitForSecondsRealtime(0.3f);
            game.Level.enabled = false;
            foreach (EnemyAI mob in new List<EnemyAI>(game.Enemies)) if (mob != null) mob.Recycle();
            float left = game.Level.Segments[game.Level.Segments.Length - 1].start;
            Vector2 initial = stage < 5 ? new Vector2(left + 4, 1.8f)
                : stage < 8 ? new Vector2(-5.6f, 103) : new Vector2(212, stage == 8 ? 2 : 0);
            Vector2 player = stage < 5 ? new Vector2(game.Level.Length - 6, 1.0f)
                : stage < 8 ? new Vector2(5.3f, 106) : new Vector2(236, stage == 8 ? 2 : 0);
            game.Player.Motor.ResetAt(player); game.Player.Motor.SetCinematicLocked(true);
            game.Player.Health.Invulnerable = true;
            EnemyAI actor = game.Level.Spawn(EnemyKind.Boss, initial); game.Level.SetBoss(actor);
            BossController boss = actor.GetComponent<BossController>();
            BossPursuitController pursuit = boss.Pursuit;
            Collider2D bossHull = actor.GetComponent<CapsuleCollider2D>();
            Check(pursuit != null && bossHull.enabled && !bossHull.isTrigger, stage, "Pursuit preserves enabled non-trigger weapon hitbox");
            var obstacle = new GameObject("PursuitAcceptanceWall"); obstacle.transform.SetParent(game.Level.transform, false);
            obstacle.transform.position = (initial + player) * 0.5f;
            BoxCollider2D wall = obstacle.AddComponent<BoxCollider2D>(); wall.size = new Vector2(1.8f, 12);
            if ((stage & 1) != 0) obstacle.AddComponent<Health>().Initialize(Team.Enemy, 1000);
            typeof(BossPursuitController).GetField("burstAt", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(pursuit, Time.time);
            float until = Time.realtimeSinceStartup + 3;
            while (!pursuit.Closing && Time.realtimeSinceStartup < until) yield return null;
            Check(pursuit.Closing && !pursuit.Flying, stage, "Fast approach begins with a windup");
            Check(Physics2D.GetIgnoreCollision(bossHull, wall), stage, "Solid wall or destructible cover cannot block boss");
            Vector2 start = actor.GetComponent<Rigidbody2D>().position;
            Vector2 destination = pursuit.Destination;
            yield return new WaitForSecondsRealtime(0.12f);
            Check(Vector2.Distance(actor.GetComponent<Rigidbody2D>().position, start) < 0.05f, stage, "Boss does not teleport before visible anticipation");
            game.Pause(); yield return new WaitForSecondsRealtime(0.2f);
            Check(Vector2.Distance(actor.GetComponent<Rigidbody2D>().position, start) < 0.05f && pursuit.Closing, stage, "Pause freezes approach clock and position");
            game.Resume(); game.Player.Health.Invulnerable = false;
            float before = game.Player.Health.Current;
            until = Time.realtimeSinceStartup + 2;
            while (pursuit.Closing && Time.realtimeSinceStartup < until) yield return null;
            Vector2 landed = actor.GetComponent<Rigidbody2D>().position;
            Check(!pursuit.Closing && Vector2.Distance(landed, destination) < 0.45f && Vector2.Distance(landed, start) > 7, stage,
                "Fast flyby crosses obstacle and reaches committed destination (start="+start+", end="+landed+", destination="+destination+")");
            Check(Vector2.Distance(landed, game.Player.transform.position) >= 2.4f && game.Player.Health.Current == before, stage, "Approach leaves player separation without contact damage");
            Check(!Physics2D.GetIgnoreCollision(bossHull, game.Player.GetComponent<CapsuleCollider2D>()), stage, "Temporary contact exclusion restores after flyby");
            game.Player.Health.Invulnerable = true;
            Destroy(obstacle);
            float bossHp = actor.Health.Current;
            game.Projectiles.Fire(landed + Vector2.right * 1.3f, Vector2.left, 20, 20, Team.Player, 1, Color.cyan);
            yield return new WaitForSecondsRealtime(0.12f);
            Check(actor.Health.Current < bossHp, stage, "Boss still receives real player projectile damage after phasing");
            yield return Snap($"stage-{stage + 1:00}-approach.png");
            player = stage < 5 ? new Vector2(left + 15, 1.1f) : stage < 8 ? new Vector2(0, 102.15f) : new Vector2(219, stage == 8 ? 2 : 0);
            game.Player.Motor.ResetAt(player);
            until = Time.realtimeSinceStartup + 13;
            BossField strike = Tracking(boss);
            while ((strike == null || !strike.IsWarning || strike.TargetCommitted) && Time.realtimeSinceStartup < until)
            { yield return null; strike = Tracking(boss); }
            Check(strike != null && strike.IsWarning && !strike.TargetCommitted, stage, "Native skill opens live tracking at player location");
            Vector2 old = strike.transform.position;
            game.Player.Motor.ResetAt(player + Vector2.right * 0.65f);
            yield return new WaitForSecondsRealtime(0.09f);
            Check(Vector2.Distance(strike.transform.position, old) > 0.35f && strike.Contains(game.Player.transform.position), stage, "Native warning follows player steps during windup");
            until = Time.realtimeSinceStartup + 1.5f;
            while (!strike.TargetCommitted && Time.realtimeSinceStartup < until) yield return null;
            game.Player.Health.Invulnerable = false; before = game.Player.Health.Current;
            yield return new WaitForSecondsRealtime(strike.WarningRemaining + 0.12f);
            Check(game.Player.Health.Current < before && !game.Player.Health.IsDead, stage, "Standing still takes real survivable boss skill damage");
            game.Player.Health.Invulnerable = true; game.Player.Health.Restore(game.Player.Health.Maximum);
            BossField previous = strike;
            until = Time.realtimeSinceStartup + 15;
            do { yield return null; strike = Tracking(boss); }
            while ((strike == null || strike == previous || !strike.IsWarning || strike.TargetCommitted) && Time.realtimeSinceStartup < until);
            Check(strike != null && strike != previous && strike.IsWarning, stage, "Next native cast reacquires current player location");
            until = Time.realtimeSinceStartup + 1.5f;
            while (!strike.TargetCommitted && Time.realtimeSinceStartup < until) yield return null;
            Vector2 marked = strike.transform.position;
            Check(!strike.FollowWarningAt(marked + Vector2.one * 9), stage, "Attack stops tracking before impact");
            game.Player.Motor.SetCinematicLocked(false); Keys(stage < 5 ? Key.A : Key.D);
            yield return new WaitForSecondsRealtime(strike.WarningRemaining + 0.08f); Keys();
            Check(strike != null && Vector2.Distance(strike.transform.position, marked) < 0.03f
                && !strike.OverlapsCapsule(game.Player.transform.position, game.Player.GetComponent<CapsuleCollider2D>().size), stage,
                "Real keyboard movement escapes committed native impact");
            yield return Snap($"stage-{stage + 1:00}-dodge.png");
        }

        private static BossField Tracking(BossController boss) => boss.Side != null ? boss.Side.TrackingStrike
            : boss.Vertical != null ? boss.Vertical.TrackingStrike : boss.Swim.TrackingStrike;
        private void Keys(params Key[] keys) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
        private IEnumerator Snap(string name)
        {
            yield return new WaitForEndOfFrame(); Texture2D capture = ScreenCapture.CaptureScreenshotAsTexture();
            string path = Path.Combine(folder, name); File.WriteAllBytes(path, capture.EncodeToPNG()); Destroy(capture);
            report.screenshots.Add(path);
        }
        private void Check(bool condition, int stage, string description)
        {
            string message = "Stage " + (stage + 1) + ": " + description;
            if (!condition) throw new InvalidOperationException(message);
            report.checks.Add(message); Debug.Log("BOSS_PURSUIT_PASS | " + message);
        }
        private void OnLog(string message, string trace, LogType type)
        { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) report.errors.Add(message + "\n" + trace); }
        private void Update()
        {
            if (finished) return;
            if (Time.realtimeSinceStartup > deadline) { report.errors.Add("Boss pursuit test exceeded deadline"); Complete(); }
        }
        private void Complete()
        {
            if (finished) return; finished = true;
            if (keyboard != null) InputSystem.RemoveDevice(keyboard); if (mouse != null) InputSystem.RemoveDevice(mouse);
            foreach (InputDevice device in suppressed) if (device.added) InputSystem.EnableDevice(device);
            InputSystem.settings.backgroundBehavior = previousBackground;
            report.passed = report.errors.Count == 0;
            File.WriteAllText(Path.Combine(folder, "boss-pursuit-smoke.json"), JsonUtility.ToJson(report, true));
            Application.logMessageReceived -= OnLog; Application.Quit(report.passed ? 0 : 1);
        }
    }
}
#endif
