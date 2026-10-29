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
    // Opt-in Development Build diagnostic. Never writes campaign progress to disk.
    public sealed class FeedbackTraversalSmokeTest : MonoBehaviour
    {
        [Serializable] private sealed class Report
        {
            public string scope = "Windows runtime: fresh in-memory saves, UI callbacks, real projectile/field damage, synthetic movement and jump input; contact-trigger secret entrance/exit, combat/reward/retry, full physical shaft ascent without teleport/position writes. Climbing uses invulnerability and high-damage piercing projectiles to clear gates and nearby enemies; secret entrance probes position at the entrance. Not human balance/controller/cross-machine acceptance.";
            public bool passed;
            public List<string> checks = new List<string>();
            public List<string> errors = new List<string>();
        }
        private readonly Report report = new Report();
        private readonly List<InputDevice> suppressed = new List<InputDevice>();
        private GameManager game;
        private Keyboard keyboard;
        private Mouse mouse;
        private string folder;
        private bool done;
        private float deadline;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Launch()
        {
            string[] args = Environment.GetCommandLineArgs();
            int flag = Array.IndexOf(args, "-moriFeedbackSmoke");
            if (flag < 0 || !Application.isEditor && !Debug.isDebugBuild) return;
            CampaignProgress.Volatile = true; CampaignProgress.ClearVolatile();
            var probe = new GameObject("FeedbackTraversalSmoke").AddComponent<FeedbackTraversalSmokeTest>();
            probe.folder = Path.GetFullPath(args[flag + 1]); Directory.CreateDirectory(probe.folder);
            probe.deadline = Time.realtimeSinceStartup + 540;
            Application.logMessageReceived += probe.OnLog;
            probe.StartCoroutine(probe.Guard(probe.Run()));
        }
        private IEnumerator Guard(IEnumerator routine)
        {
            var stack = new Stack<IEnumerator>(); stack.Push(routine);
            while (stack.Count > 0 && !done)
            {
                object current;
                try
                {
                    if (!stack.Peek().MoveNext()) { stack.Pop(); continue; }
                    current = stack.Peek().Current;
                    if (current is IEnumerator nested) { stack.Push(nested); continue; }
                }
                catch (Exception exception) { report.errors.Add(exception.ToString()); Finish(); yield break; }
                yield return current;
            }
            Finish();
        }
        private IEnumerator Run()
        {
            yield return DiagnosticWindow.Prepare();
            yield return null; yield return null;
            game = FindAnyObjectByType<GameManager>();
            Check(game != null && game.Config.Validate(out _), "Valid eight-stage project boots");
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            foreach (InputDevice device in InputSystem.devices)
                if (device.enabled && (device is Keyboard || device is Mouse || device is Gamepad)) suppressed.Add(device);
            foreach (InputDevice device in suppressed) InputSystem.DisableDevice(device);
            keyboard = InputSystem.AddDevice<Keyboard>("FeedbackKeyboard");
            mouse = InputSystem.AddDevice<Mouse>("FeedbackMouse");
            yield return ProbeIncomingDamage();
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-moriClimbOnly") >= 0)
            {
                for (int stage = 5; stage < 8; stage++) yield return Climb(stage);
                yield break;
            }
            Check(CampaignProgress.Unlocked == 1, "Fresh save has no artificial stage unlock");
            for (int stage = 0; stage < 8; stage++)
            {
                Button button = Button("Stage_" + stage);
                Check(button.interactable, "Stage " + (stage + 1) + " menu entry is selectable on a fresh save");
                button.onClick.Invoke();
                Check(game.SelectedStage == stage, "Stage " + (stage + 1) + " UI selection reaches GameManager");
            }
            yield return Snap("01_all_eight_stages.png");
            game.SelectStage(0); game.BeginRun(); yield return new WaitForSecondsRealtime(0.5f);
            game.Level.enabled = false;
            game.Player.Motor.SetCinematicLocked(true);
            float before = game.Player.Health.Current;
            Vector2 position = game.Player.transform.position;
            game.Projectiles.Fire(position + Vector2.left * 1.4f, Vector2.right, 12, 12, Team.Enemy, 1, Color.red);
            yield return new WaitForSeconds(0.18f);
            Check(game.Player.Health.Current < before && game.Hud.PlayerHitsPresented > 0, "A real enemy projectile reduces health and triggers player-hit feedback");
            Check(Mathf.Abs(game.Hud.PlayerHealthFill - game.Player.Health.Current / game.Player.Health.Maximum) < 0.002f,
                "Visible health fill equals actual damaged health");
            Check(game.Hud.PlayerDamageTrail > game.Hud.PlayerHealthFill && game.Hud.PlayerHitOpacity > 0.15f,
                "Recent damage remains visible as a red trail and screen-edge impact");
            yield return Snap("02_projectile_hit.png");
            yield return new WaitForSeconds(1.1f);
            Check(Mathf.Abs(game.Hud.PlayerDamageTrail - game.Hud.PlayerHealthFill) < 0.002f, "Damage trail animates down to current health");

            var source = new GameObject("DiagnosticBossSource").AddComponent<Health>(); source.Initialize(Team.Enemy, 100);
            var field = new GameObject("DiagnosticBossAttack").AddComponent<BossField>();
            field.transform.SetParent(game.Level.transform); field.transform.position = game.Player.transform.position;
            field.InitializeAttack(game, source, "HitFeedback", BossFieldShape.Disc, BossFieldStyle.Prism, Vector2.one * 3, 0.8f, 0.4f, 8);
            before = game.Player.Health.Current;
            yield return new WaitForSeconds(0.15f);
            SpriteRenderer warning = field.transform.Find("DamageFootprint").GetComponent<SpriteRenderer>();
            Check(warning.enabled && Red(warning.color), "Boss windup shows a translucent red landing area");
            Check(Mathf.Approximately(game.Player.Health.Current, before), "Boss warning itself causes no damage");
            yield return Snap("03_boss_red_warning.png");
            yield return new WaitForSeconds(0.72f);
            Check(game.Player.Health.Current < before && game.Hud.PlayerDamageTrail > game.Hud.PlayerHealthFill,
                "Actual boss-field contact visibly removes health after its warning");
            yield return Snap("04_boss_hit.png");
            Destroy(source.gameObject);
            yield return new WaitForSeconds(0.5f);
            game.Player.Health.TakeDamage(10000, Team.Enemy, true);
            yield return null;
            Check(game.State == RunState.Lost && game.Hud.PlayerHealthFill == 0, "Lethal hit visibly empties the health bar");
            yield return new WaitForSecondsRealtime(2.3f);
            Check(Button("Restart").isActiveAndEnabled, "Defeat results appear after the health-loss animation");
            for (int stage = 0; stage < 8; stage++) yield return ProbeSecret(stage);
            for (int stage = 5; stage < 8; stage++) yield return Climb(stage);
        }

        private IEnumerator ProbeSecret(int stage)
        {
            game.ReturnToMenu(); game.SelectStage(stage); game.BeginRun();
            yield return new WaitForSecondsRealtime(0.4f);
            game.Player.Health.Invulnerable = true;
            CheckLegacyGeometry(stage);
            yield return ProbeSurfaces(stage);
            SecretBranchDirector secret = game.Level.SecretRoutes;
            Check(secret != null && secret.BranchCount == 1, "Stage " + (stage + 1) + " has exactly one secret entrance");
            Vector2 entry = secret.EntryPosition(secret.GuardianId);
            float progress = stage < 5 ? entry.x / game.Level.Length : (entry.y - game.Level.Vertical.StartPoint.y) / game.Level.Length;
            Check(progress > 0.4f && progress < 0.6f, "Stage " + (stage + 1) + " entrance is in the middle 40-60% of the level");
            Check(secret.GuardWaveBudget >= 16, "Stage " + (stage + 1) + " door zone adds 16-19 guards beyond its regular encounter");
            game.Level.enabled = false;
            game.Player.Motor.ResetAt(entry + Vector2.left * (stage < 5 ? 3.2f : -2.7f));
            game.Player.Motor.SetCinematicLocked(true);
            // Place near the portal for a deterministic crowd observation; entry itself is walked with input below.
            float guardDeadline = Time.time + 10;
            while (secret.GuardsSpawned < secret.GuardWaveBudget && Time.time < guardDeadline) yield return null;
            Check(secret.GuardsSpawned == secret.GuardWaveBudget, "Stage " + (stage + 1) + " actually spawns its complete extra guard wave");
            Check(!secret.Inside && secret.OpenedCount == 0, "Stage " + (stage + 1) + " proximity alone does not enter the door");
            game.Player.Motor.SetCinematicLocked(false);
            int segment = game.Level.SegmentIndex;
            Keys(stage < 5 ? Key.D : Key.A);
            float enterDeadline = Time.time + 2.8f;
            while (!secret.Inside && Time.time < enterDeadline) yield return null;
            Keys();
            Check(secret.Inside && secret.OpenedCount == 1, "Stage " + (stage + 1) + " enters on actual door contact without E");
            yield return new WaitForFixedUpdate(); yield return null;
            Check(secret.CameraBounds.Contains(game.Player.transform.position), "Stage " + (stage + 1) + " portal moves the player into its separate chamber");
            yield return new WaitForSeconds(0.2f);
            Check(secret.CameraBounds.Contains(game.CameraRig.Camera.transform.position), "Stage " + (stage + 1) + " camera follows into the secret room");
            Check(game.Level.SegmentIndex == segment && !game.Level.BossSpawned, "Stage " + (stage + 1) + " secret entry does not advance main-route progress or spawn its final boss");
            var guardians = game.Level.GetComponentsInChildren<SecretMiniBossController>();
            Check(guardians.Length == 1 && guardians[0].GetComponent<EnemyAI>().Kind != EnemyKind.Boss,
                "Stage " + (stage + 1) + " secret uses one independent guardian");
            int warnings = game.Effects.BossWarningEmissions;
            float windupDeadline = Time.time + 3;
            while (game.Effects.BossWarningEmissions == warnings && Time.time < windupDeadline) yield return null;
            Check(game.Effects.BossWarningEmissions > warnings, "Stage " + (stage + 1) + " secret guardian warns in translucent red before its signature attack");
            yield return Snap("secret_stage_" + (stage + 1) + ".png");
            int chips = game.Level.Chips;
            var guardian = guardians[0].GetComponent<EnemyAI>();
            guardian.Health.TakeDamage(guardian.Health.Maximum + 10, Team.Player);
            yield return null;
            Check(secret.CompletedCount == 1 && game.Level.Chips == chips + 1, "Stage " + (stage + 1) + " guardian defeat awards a single relic chip");
            Check(game.State != RunState.Won, "Stage " + (stage + 1) + " secret guardian defeat keeps the main campaign running");
            // Walk into the return door (no interact key); room entry and reward effects were observed above.
            game.Player.Motor.ResetAt(secret.ExitPosition + Vector2.right * 2.4f);
            yield return new WaitForSeconds(0.15f);
            Keys(Key.A);
            float exitDeadline = Time.time + 2;
            while (secret.Inside && Time.time < exitDeadline) yield return null;
            Keys();
            yield return new WaitForFixedUpdate(); yield return null;
            Check(!secret.Inside && Vector2.Distance(game.Player.transform.position, entry) < 5,
                "Stage " + (stage + 1) + " returns to the midpoint through door contact");
            game.Player.Motor.ResetAt(entry);
            yield return new WaitForSeconds(1.2f);
            Check(!secret.Inside && secret.BranchCount == 1 && secret.CompletedCount == 1 && game.Level.Chips == chips + 1,
                "Stage " + (stage + 1) + " cleared entrance never respawns or duplicates rewards");
            game.Lose(); yield return null; game.RetryCheckpoint();
            yield return new WaitForSecondsRealtime(0.3f);
            Check(!secret.Inside && secret.CompletedCount == 1 && secret.BranchCount == 1,
                "Stage " + (stage + 1) + " retry preserves its cleared single secret");
            Check(Mathf.Approximately(game.Player.Health.MaximumHitFraction, game.Config.playerMaximumHitFraction),
                "Stage " + (stage + 1) + " retry preserves the configured player damage protection");
        }

        private IEnumerator Climb(int stage)
        {
            game.ReturnToMenu(); game.SelectStage(stage); game.BeginRun();
            yield return new WaitForSecondsRealtime(0.5f);
            CheckLegacyGeometry(stage);
            VerticalStageDirector vertical = game.Level.Vertical;
            Check(vertical != null && game.Player.Motor.Grounded, "Stage " + (stage + 1) + " starts on its physical shaft floor");
            game.Player.Health.Invulnerable = true;
            VerticalSkullGate[] gates = game.Level.GetComponentsInChildren<VerticalSkullGate>();
            Collider2D bossFloor = null;
            foreach (PlatformSurface surface in game.Level.GetComponentsInChildren<PlatformSurface>())
                if (surface.name == "Vertical_BossArenaFloor") bossFloor = surface.GetComponent<Collider2D>();
            Check(bossFloor != null, "Stage " + (stage + 1) + " has a physical boss landing floor");
            float jumpUntil = 0, nextCombatShot = 0, maximum = game.Player.transform.position.y;
            float stageDeadline = Time.time + 120;
            int initialJumps = game.Player.Motor.JumpCount;
            bool firstRoomCaptured = false;
            while (!StandingOnBossFloor(bossFloor) && Time.time < stageDeadline)
            {
                // Route-clearance shots must not start a boss execution and freeze the
                // diagnostic's game-time waits before its warning observation.
                if (game.Level.Boss != null) game.Level.Boss.Health.Invulnerable = true;
                float y = game.Player.transform.position.y;
                maximum = Mathf.Max(maximum, y);
                if (game.Player.Motor.Grounded && Time.time > jumpUntil + 0.1f) jumpUntil = Time.time + 0.43f;
                var held = new List<Key>();
                if (Time.time < jumpUntil) held.Add(Key.Space);
                if (game.Player.transform.position.x > 0.12f) held.Add(Key.A);
                else if (game.Player.transform.position.x < -0.12f) held.Add(Key.D);
                Keys(held.ToArray());
                if (Time.time > nextCombatShot)
                {
                    // Route diagnostic only: enemies have physical bodies, so a jumping-only bot
                    // must also clear its path. These real piercing shots exercise gate collisions
                    // without allowing a crowd to turn this into an accidental balance benchmark.
                    float assistDamage = 1000;
                    foreach (VerticalSkullGate gate in gates) assistDamage = Mathf.Max(assistDamage, gate.Health.Maximum + 1);
                    Vector2 origin = game.Player.Motor.MuzzleOrigin;
                    game.Projectiles.Fire(origin, Vector2.up, 30, assistDamage, Team.Player, 16, Color.cyan);
                    foreach (EnemyAI enemy in game.Enemies)
                    {
                        if (enemy == null || !enemy.IsActive || enemy.Kind == EnemyKind.Boss) continue;
                        Vector2 delta = (Vector2)enemy.transform.position - origin;
                        if (delta.sqrMagnitude > 25 || delta.sqrMagnitude < 0.01f) continue;
                        game.Projectiles.Fire(origin, delta.normalized, 30, assistDamage, Team.Player, 16, Color.cyan);
                    }
                    nextCombatShot = Time.time + 0.3f;
                }
                if (!firstRoomCaptured && y > 20)
                {
                    firstRoomCaptured = true;
                    Check(vertical.ActiveSection >= 1 && game.Player.Motor.JumpCount > initialJumps + 3,
                        "Stage " + (stage + 1) + " physically climbs past its first encounter into later rooms");
                    yield return Snap("stage_" + (stage + 1) + "_past_first_wave.png");
                }
                yield return null;
            }
            Keys();
            yield return Snap("stage_" + (stage + 1) + "_climb_end.png");
            string status = " position=" + game.Player.transform.position + " velocity=" + game.Player.GetComponent<Rigidbody2D>().linearVelocity
                + " grounded=" + game.Player.Motor.Grounded + " broken=" + vertical.BrokenSkulls
                + " capsule=" + game.Player.GetComponent<CapsuleCollider2D>().bounds + " floor=" + bossFloor.bounds;
            foreach (VerticalSkullGate gate in gates) if (!gate.Broken) status += " gate" + gate.Index + "=" + gate.Health.Current;
            Check(StandingOnBossFloor(bossFloor),
                "Stage " + (stage + 1) + " reaches the boss floor through physical jump/move input with combat assist; maximum y=" + maximum + status);
            Check(vertical.BrokenSkulls == 7, "Stage " + (stage + 1) + " all seven gates open through projectile collisions");
            // Release any queued jump from the final climb and observe sustained support.
            // A single fixed delay may sample the buffered jump while it is still airborne.
            float settleDeadline = Time.time + 5, stableSince = -1;
            while (Time.time < settleDeadline)
            {
                if (StandingOnBossFloor(bossFloor))
                {
                    if (stableSince < 0) stableSince = Time.time;
                    if (Time.time - stableSince >= 0.3f) break;
                }
                else stableSince = -1;
                yield return null;
            }
            yield return Snap("stage_" + (stage + 1) + "_boss_landing.png");
            Check(stableSince >= 0 && Time.time - stableSince >= 0.3f,
                "Stage " + (stage + 1) + " boss arena supports the player after jump release; position="
                + game.Player.transform.position + " velocity=" + game.Player.GetComponent<Rigidbody2D>().linearVelocity
                + " capsule=" + game.Player.GetComponent<CapsuleCollider2D>().bounds + " floor=" + bossFloor.bounds);
            Check(game.Level.Boss != null && game.Level.BossSpawned, "Stage " + (stage + 1) + " progression spawns its final boss");
            int warnings = game.Effects.BossWarningEmissions;
            float wait = Time.time + 8;
            while (game.Effects.BossWarningEmissions == warnings && Time.time < wait) yield return null;
            Check(game.Effects.BossWarningEmissions > warnings, "Stage " + (stage + 1) + " boss commits red warning areas before its volley");
            yield return Snap("stage_" + (stage + 1) + "_boss_warning.png");
            game.Lose(); yield return null; game.RetryCheckpoint(); yield return new WaitForSecondsRealtime(0.4f);
            Check(Array.TrueForAll(gates, gate => gate.Broken), "Stage " + (stage + 1) + " checkpoint retry preserves already-open gates");
        }
        private bool StandingOnBossFloor(Collider2D floor)
        {
            if (floor == null || !game.Player.Motor.Grounded) return false;
            CapsuleCollider2D player = game.Player.GetComponent<CapsuleCollider2D>();
            if (player == null) return false;
            if (player.bounds.min.y < floor.bounds.max.y - 0.08f) return false;
            var hits = new RaycastHit2D[16];
            int count = player.Cast(Vector2.down, new ContactFilter2D { useTriggers = false }, hits, 0.2f);
            for (int index = 0; index < count; index++)
                if (hits[index].collider == floor && hits[index].normal.y >= 0.5f) return true;
            return false;
        }
        private void CheckLegacyGeometry(int stage)
        {
            string[] forbidden = { "WalkableEdge", "PlatformTop", "MetalHighlight", "Hologram_NoCollision", "SecretRouteLightRail", "SecretEntry_Key", "SecretEntry_Halo", "Shaft_LumenRail", "LuminousPlatformLip", "WallRunicEngraving", "Gate_RunicBolt", "GraveGate_RunicRidge", "BarrelEnergyStripe", "CoverWeaknessStripe" };
            foreach (SpriteRenderer renderer in game.Level.GetComponentsInChildren<SpriteRenderer>(true))
                if (Array.IndexOf(forbidden, renderer.name) >= 0) throw new InvalidOperationException("Stage " + (stage + 1) + " retains legacy renderer " + renderer.name);
            Check(true, "Stage " + (stage + 1) + " contains no legacy colored geometry markers");
            foreach (StageHazard trap in game.Level.GetComponentsInChildren<StageHazard>())
                Check(trap.GetComponentInChildren<SpriteRenderer>().sprite != game.Art.Square && trap.DamageSize.x > 0,
                    "Stage " + (stage + 1) + " trap uses illustrated art with independent damage dimensions");
        }
        private IEnumerator ProbeIncomingDamage()
        {
            game.SelectStage(0); game.BeginRun(); yield return null;
            game.Level.enabled = false;
            game.Player.Health.Invulnerable = true;
            game.Player.Motor.SetCinematicLocked(true);
            Health target = new GameObject("IncomingDamageProbe").AddComponent<Health>();
            float[] limits = { game.Config.enemyHitLimit, game.Config.hazardHitLimit, game.Config.bossSkillHitLimit, game.Config.bossUltimateHitLimit };
            foreach (CharacterDefinition character in game.Config.characters)
            {
                for (int kind = 0; kind < limits.Length; kind++)
                {
                    bool valid = true;
                    for (int difficulty = 0; difficulty < 3; difficulty++)
                        for (int stage = 0; stage < 8; stage++)
                            for (int sample = 0; sample < 2; sample++)
                            {
                                target.Initialize(Team.Player, game.Config.playerHealth * character.health);
                                target.ConfigurePlayerDamage(game.Config);
                                float raw = sample == 0 ? 20 * (1 + difficulty * 0.2f) * (game.Config.incomingDamageMultiplier + stage * game.Config.stageDamageStep) : 200000;
                                target.TakeDamage(raw, Team.Enemy, false, (IncomingHitKind)kind);
                                valid &= !target.IsDead && target.LastDamageTaken <= game.Config.playerHealth * limits[kind] + 0.001f
                                    && target.LastDamageTaken <= target.Maximum * game.Config.playerMaximumHitFraction + 0.001f;
                            }
                    Check(valid, character.name + " survives full-health " + (IncomingHitKind)kind + " hits across 8 stages, 3 difficulties and excessive raw damage");
                }
            }
            target.Initialize(Team.Player, game.Config.playerHealth); target.ConfigurePlayerDamage(game.Config);
            target.DamageReduction = 0.7f;
            target.TakeDamage(200000, Team.Enemy, false, IncomingHitKind.BossUltimate);
            Check(Mathf.Abs(target.LastDamageTaken - game.Config.playerHealth * game.Config.bossUltimateHitLimit * 0.3f) < 0.01f,
                "Shield reduction still reduces the capped boss ultimate");
            Check(!target.TakeDamage(200000, Team.Enemy), "Same-frame overlapping attacks cannot stack into a one-shot");
            yield return new WaitForSeconds(game.Config.playerHitProtectionSeconds + 0.03f);
            Check(target.TakeDamage(200000, Team.Enemy) && !target.IsDead, "Another hit is accepted after the configured 0.6-second protection");
            Destroy(target.gameObject);
            game.ReturnToMenu(); yield return null;
        }

        private IEnumerator ProbeSurfaces(int stage)
        {
            Sprite expected = StageSurfaceArt.Resolve(game, game.Level);
            // Destructible barrels/covers also carry PlatformSurface for collision;
            // they keep their own theme prop art rather than a floor illustration.
            PlatformSurface[] surfaces = Array.FindAll(game.Level.GetComponentsInChildren<PlatformSurface>(true),
                surface => surface.GetComponentInParent<DestructibleCover>() == null);
            Check(surfaces.Length > 10 && expected != game.Art.Side.Concrete, "Stage " + (stage + 1) + " has its own illustrated surface material");
            foreach (PlatformSurface surface in surfaces)
            {
                SpriteRenderer[] panels = surface.GetComponentsInChildren<SpriteRenderer>(true);
                Check(panels.Length > 0 && Array.TrueForAll(panels, panel => panel.sprite == expected),
                    "Stage " + (stage + 1) + " " + surface.name + " shares its main floor art (including walls, lifts and secret ledges)");
                if (!surface.Elevator) continue;
                BoxCollider2D lift = surface.GetComponent<BoxCollider2D>();
                Bounds travel = new Bounds(surface.Origin + Vector2.up * 1.8f, new Vector3(lift.size.x, 3.6f + lift.size.y, 1));
                foreach (PlatformSurface other in surfaces)
                {
                    if (other == surface || other.Elevator) continue;
                    Collider2D collider = other.GetComponent<Collider2D>();
                    Check(collider == null || !travel.Intersects(collider.bounds),
                        "Stage " + (stage + 1) + " lift travel is clear of " + other.name);
                }
            }
            Check(game.Level.transform.Find("ElectromagneticDoor") == null
                && Array.TrueForAll(game.Level.GetComponentsInChildren<Transform>(), child => child.name != "ElectromagneticDoor" && child.name != "DoorSwitch_E" && !child.name.StartsWith("EncounterGate_")),
                "Stage " + (stage + 1) + " contains no permanently open redundant door walls");
            game.Level.enabled = false;
            PlatformSurface sampleSurface = Array.Find(surfaces, candidate => candidate.Elevator)
                ?? Array.Find(surfaces, candidate => candidate.name == "OneWayPlatform" || candidate.name == "Vertical_Climb_0");
            game.Player.Motor.SetCinematicLocked(true);
            game.Player.Motor.ResetAt((Vector2)sampleSurface.transform.position + Vector2.up * 1.2f);
            game.CameraRig.ResetFollow();
            yield return new WaitForSeconds(0.2f);
            yield return Snap("surfaces_stage_" + (stage + 1) + ".png");
        }
        private bool Red(Color color) => color.r > 0.9f && color.g < 0.15f && color.b < 0.2f && color.a > 0 && color.a < 0.5f;
        private Button Button(string name)
        {
            Button fallback = null;
            foreach (Button button in game.Hud.GetComponentsInChildren<Button>(true))
            {
                if (button.name != name) continue;
                if (button.isActiveAndEnabled) return button;
                fallback = button;
            }
            if (fallback != null) return fallback;
            throw new InvalidOperationException("Missing UI " + name);
        }
        private void Keys(params Key[] keys) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
        private IEnumerator Snap(string name)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(folder, name)); yield return null;
        }
        private void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
            report.checks.Add(message);
        }
        private void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) report.errors.Add(message + "\n" + stack);
        }
        private void Update()
        {
            if (done) return;
            if (game != null && game.State == RunState.Paused) game.Resume();
            if (game != null && game.State == RunState.Upgrade) game.Experience.Choose(0);
            if (Time.realtimeSinceStartup > deadline) { report.errors.Add("Runtime diagnostic exceeded its 540s deadline"); Finish(); }
        }
        private void Finish()
        {
            if (done) return; done = true;
            report.passed = report.errors.Count == 0;
            File.WriteAllText(Path.Combine(folder, "feedback-traversal.json"), JsonUtility.ToJson(report, true));
            Application.logMessageReceived -= OnLog;
            foreach (InputDevice device in suppressed) if (device.added) InputSystem.EnableDevice(device);
            Debug.Log("FEEDBACK_TRAVERSAL " + (report.passed ? "PASS" : "FAIL") + " checks=" + report.checks.Count);
            Application.Quit(report.passed ? 0 : 1);
        }
    }
}
#endif
