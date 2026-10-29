#if ENABLE_INPUT_SYSTEM
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace MoriEchoes.Diagnostics
{
    // Development-player opt-in only. Never writes campaign progress to disk.
    [DefaultExecutionOrder(-200)]
    public sealed class SwimCampaignSmokeTest : MonoBehaviour
    {
        [Serializable] private sealed class Report
        {
            public string unity, startedAt, completedAt;
            public string scope = "Windows runtime: real menu callbacks and synthetic keyboard swimming; assisted teleports, invulnerability, accelerated oxygen timers and boss HP thresholds. Not human balance, physical controller or cross-machine acceptance.";
            public bool passed;
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
        private bool finished, capturedSettings;
        private InputSettings.BackgroundBehavior originalBackground;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Launch()
        {
            string[] args = Environment.GetCommandLineArgs();
            int flag = Array.IndexOf(args, "-moriSwimSmoke");
            if (flag < 0 || !Application.isEditor && !Debug.isDebugBuild) return;
            CampaignProgress.Volatile = true; CampaignProgress.ClearVolatile();
            var entity = new GameObject("SwimCampaignIntegration"); DontDestroyOnLoad(entity);
            var test = entity.AddComponent<SwimCampaignSmokeTest>();
            test.folder = flag + 1 < args.Length ? Path.GetFullPath(args[flag + 1]) : Path.Combine(Application.persistentDataPath, "SwimTest");
            Directory.CreateDirectory(test.folder);
            test.report.unity = Application.unityVersion; test.report.startedAt = DateTimeOffset.Now.ToString("O");
            test.deadline = Time.realtimeSinceStartup + 240;
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
            yield return DiagnosticWindow.Prepare();
            game = FindAnyObjectByType<GameManager>();
            Check(game != null && game.State == RunState.Menu, "Boot opens character menu");
            originalBackground = InputSystem.settings.backgroundBehavior; capturedSettings = true;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            foreach (InputDevice device in InputSystem.devices)
                if (device.enabled && (device is Keyboard || device is Mouse || device is Gamepad)) suppressed.Add(device);
            foreach (InputDevice device in suppressed) InputSystem.DisableDevice(device);
            keyboard = InputSystem.AddDevice<Keyboard>("SwimTestKeyboard"); mouse = InputSystem.AddDevice<Mouse>("SwimTestMouse");
            Check(game.Config.characters.Length == 8 && game.Config.themes.Length >= 10, "Preserved eight-character roster and ten-stage campaign");
            for (int character = 0; character < game.Config.characters.Length; character++)
            {
                Button("Character" + character).onClick.Invoke(); yield return null;
                Check(game.SelectedCharacter == character, "Menu selects character " + character);
                int id = game.CharacterId;
                Check(game.Hud.SelectedAbilityDescription(0) == CharacterAbilityCatalog.Primary(id)
                    && game.Hud.SelectedAbilityDescription(1) == CharacterAbilityCatalog.Technique(id, 0)
                    && game.Hud.SelectedAbilityDescription(2) == CharacterAbilityCatalog.Technique(id, 1)
                    && game.Hud.SelectedAbilityDescription(3).StartsWith(CharacterAbilityCatalog.Ultimate(id)), "All four skill descriptions refresh for " + game.Character.name);
                Canvas.ForceUpdateCanvases();
                foreach (Text text in game.Hud.GetComponentsInChildren<Text>())
                    if (text.name.StartsWith("AbilityDescription_") || text.name.StartsWith("AbilityTitle_"))
                        Check(text.text.Length > 0 && text.preferredHeight <= text.rectTransform.rect.height + 1, "Skill text fits: " + character + "/" + text.name);
            }
            yield return Snap("01_menu_skills_1280.png");
            Screen.SetResolution(1920, 1080, FullScreenMode.Windowed); yield return new WaitForSecondsRealtime(0.5f);
            Button("Character4").onClick.Invoke(); yield return Snap("02_menu_skills_1920.png");
            Screen.SetResolution(1280, 720, FullScreenMode.Windowed); yield return new WaitForSecondsRealtime(0.5f);
            yield return ProbeStage(8);
            yield return ProbeStage(9);
        }
        private IEnumerator ProbeStage(int stage)
        {
            game.ReturnToMenu(); yield return null;
            game.SelectCharacter(0); game.SelectStage(stage); Button("Start").onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.5f);
            string prefix = (stage + 1).ToString("00");
            SwimStageDirector swim = game.Level.Swim;
            Check(game.IsPlaying && swim != null && swim.Underwater == (stage == 9), prefix + " starts correct aquatic movement model");
            Check(game.Level.SecretRoutes == null && game.Level.Segments.Length == 8, prefix + " has eight sections and no hidden route");
            game.Player.Health.Invulnerable = true;
            var motor = game.Player.Motor; var body = game.Player.GetComponent<Rigidbody2D>();
            float startX = body.position.x;
            Keys(Key.D); yield return new WaitForSecondsRealtime(0.55f); Keys();
            Check(body.position.x > startX + 0.8f && body.gravityScale == 0, prefix + " keyboard forward propulsion without ground gravity");
            if (!swim.Underwater)
            {
                motor.ResetAt(new Vector2(8, swim.SurfaceHeightAt(8)));
                yield return new WaitForSecondsRealtime(0.2f); int jumps = motor.JumpCount;
                Keys(Key.Space); yield return new WaitForSecondsRealtime(0.24f); Keys();
                Check(motor.JumpCount > jumps && body.position.y > swim.SurfaceY + 0.6f, "Sea jump works without a ground Surface reference");
                yield return Snap(prefix + "_surface_jump.png");
                yield return new WaitForSecondsRealtime(0.9f);
                Check(body.position.y >= swim.MinY && body.position.y <= swim.MaxY, "Sea jump returns within shallow-water bounds");
                Keys(Key.S); yield return new WaitForSecondsRealtime(0.8f); Keys();
                Check(body.position.y < swim.SurfaceY - 0.6f && body.position.y >= swim.MinY, "Sea shallow dip does not enter full deep-diving model");
                Check(swim.FloatingPlatforms.Count == 24, "Sea has twelve pairs of buoyant jumpable decks");
                PlatformSurface lower = swim.FloatingPlatforms[0];
                PlatformSurface upper = swim.FloatingPlatforms[1];
                motor.ResetAt(new Vector2(11,swim.SurfaceY));
                yield return new WaitForSecondsRealtime(0.2f);
                Keys(Key.D,Key.Space); yield return new WaitForSecondsRealtime(0.55f);
                Keys(Key.Space); yield return new WaitForSecondsRealtime(0.8f); Keys();
                Check(motor.StandingSurface == lower && motor.Grounded, "Keyboard water leap lands on first floating deck");
                float deckRelative = body.position.y - lower.transform.position.y;
                yield return new WaitForSecondsRealtime(0.55f);
                Check(Mathf.Abs(body.position.y-lower.transform.position.y-deckRelative)<0.15f, "Player rides pontoon buoyancy without forced crouch");
                Keys(Key.D,Key.Space); yield return new WaitForSecondsRealtime(0.85f);
                Keys(Key.Space); yield return new WaitForSecondsRealtime(0.65f); Keys();
                Check(motor.StandingSurface == upper && !motor.Crouching, "Keyboard jump transfers from low deck to upper deck");
                yield return Snap(prefix+"_pontoon_chain.png");
                Keys(Key.S,Key.Space); yield return new WaitForSecondsRealtime(0.12f);
                Keys(Key.S); yield return new WaitForSecondsRealtime(0.8f); Keys();
                Check(motor.StandingSurface != upper && body.position.y < upper.transform.position.y, "Down and jump drops through one-way pontoon");
            }
            else
            {
                Check(swim.FloatingPlatforms.Count == 0,"Deep stage has no sea-only floating decks");
                motor.ResetAt(new Vector2(8, 0)); yield return new WaitForSecondsRealtime(0.5f);
                float cameraY = game.CameraRig.Camera.transform.position.y;
                Keys(Key.W); yield return new WaitForSecondsRealtime(1.1f); Keys();
                Check(body.position.y > 2 && game.CameraRig.Camera.transform.position.y > cameraY + 0.15f, "Deep upward thrust and vertical camera follow");
                float highY = body.position.y; Keys(Key.S); yield return new WaitForSecondsRealtime(1.3f); Keys();
                Check(body.position.y < highY - 2, "Deep downward thrust responds after reversing direction");
                yield return new WaitForSecondsRealtime(0.65f);
                Check(body.linearVelocity.magnitude < 0.6f, "Deep water damping stops released input");
                motor.SetCinematicLocked(true); swim.RestoreOxygen(-100);
                game.Player.Health.Invulnerable = false; float hp = game.Player.Health.Current;
                swim.TickSurvival(2.6f);
                Check(game.Player.Health.Current < hp && game.Player.Health.Current > hp * 0.65f, "Oxygen exhaustion causes bounded periodic damage, not instant death");
                yield return new WaitForSeconds(0.7f); // Allow the gameplay-time hit-protection window to expire.
                swim.RestoreOxygen(100); motor.TeleportTo(new Vector2(8, swim.MinY + 0.1f));
                yield return new WaitForSecondsRealtime(0.1f); // Rigidbody teleport reaches the interpolated Transform on the next physics frame.
                hp = game.Player.Health.Current;
                Check(swim.NormalizedDepth(game.Player.transform.position.y) > 0.92f, "Pressure probe reaches crush-depth band");
                swim.TickSurvival(4.1f);
                Check(game.Player.Health.Current < hp && !game.Player.Health.IsDead, "Crush-depth warning zone deals survivable pressure damage"
                    + " (before=" + hp + ", after=" + game.Player.Health.Current + ", invulnerable=" + game.Player.Health.Invulnerable + ", state=" + game.State + ")");
                game.Player.Health.Invulnerable = true; game.Player.Health.Restore(game.Player.Health.Maximum);
                swim.RestoreOxygen(100); motor.TeleportTo(new Vector2(8, 0)); motor.SetCinematicLocked(false);
            }
            motor.ResetAt(new Vector2(10, swim.Underwater ? 0 : swim.SurfaceY));
            yield return new WaitForSecondsRealtime(0.25f);
            var capsules = game.Level.GetComponent<WeaponCapsuleDirector>(); capsules.AutomaticSupplies = false; capsules.ClearAll();
            WeaponCapsule spawned = null; bool collected = false;
            capsules.Spawned += capsule => spawned = capsule; capsules.Collected += kind => collected = true;
            Check(capsules.Drop(body.position + Vector2.right * 3.5f), prefix + " spawns supply in current water layer");
            float capsuleY = spawned.transform.position.y; yield return new WaitForSecondsRealtime(0.9f);
            Check(spawned != null && spawned.IsFloating && !spawned.IsFalling && Mathf.Abs(spawned.transform.position.y - capsuleY) < 0.65f, prefix + " capsule floats without sinking");
            motor.ResetAt(spawned.transform.position); yield return new WaitForSecondsRealtime(0.3f);
            Check(collected, prefix + " floating capsule is collected on contact");
            var roles = new HashSet<SwimDepthRole>();
            for (int index = 0; index < 3; index++)
            {
                EnemyAI enemy = game.Level.Spawn(EnemyKind.Gunner, body.position + new Vector2(8 + index, index));
                var controller = enemy.gameObject.AddComponent<SwimEnemyController>(); controller.Initialize(game, enemy, swim, 0, index);
                roles.Add(controller.DepthRole);
                Check(controller.PreferredDepth >= swim.MinY && controller.PreferredDepth <= swim.MaxY, prefix + " enemy depth target in water bounds " + index);
            }
            Check(roles.Count == 3, prefix + " surfaced, submerged and full-column enemy roles");
            yield return Snap(prefix + "_water_layer.png");
            motor.ResetAt(new Vector2(211, swim.Underwater ? 0 : swim.SurfaceY)); motor.SetCinematicLocked(true);
            yield return new WaitForSecondsRealtime(0.7f);
            EnemyAI boss = game.Level.Boss;
            Check(boss != null && boss.GetComponent<SwimBossController>() != null, prefix + " final section spawns aquatic boss");
            game.Win(); Check(game.IsPlaying, prefix + " exit blocked while boss HP remains");
            var bossMotor = boss.GetComponent<SwimBossController>();
            int waterBefore = game.Effects.WaterEmissions;
            for (int phase = 1; phase <= 3; phase++)
            {
                if (phase > 1) boss.Health.SetCurrent(boss.Health.Maximum * (phase == 2 ? 0.64f : 0.28f));
                yield return new WaitForSecondsRealtime(1.4f);
                Check(bossMotor.Phase == phase && game.IsPlaying, prefix + " phase " + phase + " keeps fight active");
                int attacks = bossMotor.AttacksResolved; float until = Time.realtimeSinceStartup + 9;
                while (!bossMotor.Preparing && Time.realtimeSinceStartup < until) yield return null;
                Check(bossMotor.Preparing && bossMotor.TrackingStrike != null, prefix + " phase " + phase + " shows red target warning");
                int tracked = bossMotor.TrackedWarnings;
                motor.TeleportTo(body.position + Vector2.up * (swim.Underwater ? 0.6f : 0.25f));
                yield return new WaitForSecondsRealtime(0.18f);
                Check(bossMotor.TrackedWarnings > tracked, prefix + " warning follows moving target before commitment");
                yield return Snap(prefix + "_boss_form_" + phase + ".png");
                BossField committed = bossMotor.TrackingStrike;
                until = Time.realtimeSinceStartup + 1.2f;
                while (!committed.TargetCommitted && Time.realtimeSinceStartup < until) yield return null;
                Check(committed.TargetCommitted && committed.WarningRemaining >= 0.75f, prefix+" phase "+phase+" leaves at least 0.75 seconds after commitment");
                Vector2 marked = committed.transform.position;
                Check(!committed.FollowWarningAt(marked+Vector2.right*8),prefix+" phase "+phase+" rejects sticky retargeting after commitment");
                motor.SetCinematicLocked(false); Keys(Key.D);
                yield return new WaitForSecondsRealtime(committed.WarningRemaining+0.08f); Keys();
                Check(committed != null && Vector2.Distance(committed.transform.position,marked)<0.02f
                    && !committed.OverlapsCapsule(body.position,game.Player.GetComponent<CapsuleCollider2D>().size), prefix+" phase "+phase+" keyboard movement escapes fixed target strike");
                motor.SetCinematicLocked(true);
                until = Time.realtimeSinceStartup + 4;
                while (bossMotor.AttacksResolved == attacks && Time.realtimeSinceStartup < until) yield return null;
                Check(bossMotor.AttacksResolved > attacks, prefix + " phase " + phase + " resolves an actual attack");
                Vector3 viewport = game.CameraRig.Camera.WorldToViewportPoint(boss.transform.position);
                Check(viewport.y > 0.05f && viewport.y < 0.95f, prefix + " boss remains within vertical view in phase " + phase);
            }
            int ultimates = game.Effects.BossUltimateEmissions; float ultimateDeadline = Time.realtimeSinceStartup + 26;
            while (game.Effects.BossUltimateEmissions == ultimates && Time.realtimeSinceStartup < ultimateDeadline) yield return null;
            Check(game.Effects.BossUltimateEmissions > ultimates, prefix + " third-phase ultimate plays naturally");
            yield return Snap(prefix + "_boss_ultimate.png");
            Check(game.Effects.WaterEmissions > waterBefore && game.Effects.ActiveCount <= game.Effects.Capacity && game.Projectiles.RejectedShots == 0, prefix + " bubble trails active and finite effect/projectile pools hold encounter");
            boss.Health.SetCurrent(1); game.Win(); Check(game.IsPlaying, prefix + " one remaining boss HP still blocks victory");
            boss.Health.Invulnerable = false; boss.Health.TakeDamage(boss.Health.Maximum * 2, Team.Player, true);
            Check(bossMotor.DeathEffectsPlayed && boss.Health.IsDead && boss.Health.Current == 0 && game.State == RunState.Won, prefix + " zero HP triggers themed death and victory");
            yield return Snap(prefix + "_boss_defeated.png");
        }
        private Button Button(string name)
        {
            foreach (Button button in game.Hud.GetComponentsInChildren<Button>())
                if (button.name == name && button.isActiveAndEnabled) return button;
            throw new InvalidOperationException("Missing active menu button: " + name);
        }
        private void Keys(params Key[] keys) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
        private IEnumerator Snap(string name)
        {
            yield return new WaitForEndOfFrame();
            Texture2D capture = ScreenCapture.CaptureScreenshotAsTexture();
            var colors = new HashSet<Color32>();
            for (int y = 1; y < 12; y++)
                for (int x = 1; x < 20; x++) colors.Add(capture.GetPixel(capture.width * x / 20, capture.height * y / 12));
            Check(colors.Count >= 8, "Screenshot contains rendered scene and UI: " + name);
            string path = Path.Combine(folder, name); File.WriteAllBytes(path, capture.EncodeToPNG()); Destroy(capture);
            report.screenshots.Add(path);
        }
        private void Check(bool condition, string description)
        {
            if (!condition) throw new InvalidOperationException(description);
            report.checks.Add(description); Debug.Log("MORI_SWIM_PASS | " + description);
        }
        private void OnLog(string message, string stack, LogType type)
        { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) report.errors.Add(message + "\n" + stack); }
        private void Update()
        {
            if (finished) return;
            if (keyboard != null) { if (!keyboard.enabled) InputSystem.EnableDevice(keyboard); keyboard.MakeCurrent(); }
            if (mouse != null) { if (!mouse.enabled) InputSystem.EnableDevice(mouse); mouse.MakeCurrent(); }
            if (game != null && game.State == RunState.Paused) game.Resume();
            if (Time.realtimeSinceStartup > deadline) { report.errors.Add("Swim test exceeded 240 second deadline"); Complete(); }
        }
        private void Complete()
        {
            if (finished) return; finished = true;
            if (keyboard != null) InputSystem.RemoveDevice(keyboard); if (mouse != null) InputSystem.RemoveDevice(mouse);
            foreach (InputDevice device in suppressed) if (device.added) InputSystem.EnableDevice(device);
            if (capturedSettings) InputSystem.settings.backgroundBehavior = originalBackground;
            report.completedAt = DateTimeOffset.Now.ToString("O"); report.passed = report.errors.Count == 0;
            File.WriteAllText(Path.Combine(folder, "swim-campaign-smoke.json"), JsonUtility.ToJson(report, true));
            Application.logMessageReceived -= OnLog;
            Debug.Log("MORI_SWIM_RESULT | " + report.passed + " | checks=" + report.checks.Count);
            Application.Quit(report.passed ? 0 : 1);
        }
    }
}
#endif
