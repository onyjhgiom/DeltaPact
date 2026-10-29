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
    [DefaultExecutionOrder(-200)]
    public sealed class SideScrollerSmokeTest : MonoBehaviour
    {
        [Serializable]
        private sealed class Report
        {
            public string unity;
            public string startedAt;
            public string completedAt;
            public string scope = "Visible Windows integration: synthetic input, invulnerable traversal assist; stage 1 enemies use real bullets; later encounters and boss HP thresholds use explicit test damage/teleports. Harness resumes incidental focus pauses except during explicit Esc test. Not human input, focus/multi-monitor, balance or AAA art acceptance. Campaign saves are in-memory only.";
            public int screenWidth;
            public int screenHeight;
            public bool passed;
            public List<string> checks = new List<string>();
            public List<string> errors = new List<string>();
            public List<string> screenshots = new List<string>();
            public List<string> screenshotSizes = new List<string>();
        }
        private readonly Report report = new Report();
        private readonly List<InputDevice> suppressed = new List<InputDevice>();
        private GameManager game;
        private Keyboard keyboard;
        private Mouse mouse;
        private string folder;
        private float deadline;
        private bool finished;
        private bool resumeFocusPauses = true;
        private bool artOnly;
        private bool arsenalOnly;
        private bool encounterOnly;
        private InputSettings.BackgroundBehavior originalBackgroundBehavior;
        private bool inputSettingsCaptured;
        private float nextHeartbeat;
        private float nextJump;
        private float lastPositionTime;
        private float lastPosition;
        private float jumpHeldUntil;
        private float dropHeldUntil;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Launch()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(arguments, "-moriStage1");
            bool artOnly = index < 0;
            if (artOnly) index = Array.IndexOf(arguments, "-moriArtSmoke");
            bool arsenalOnly = index < 0;
            if (arsenalOnly) index = Array.IndexOf(arguments, "-moriArsenalSmoke");
            bool encounterOnly = index < 0;
            if (encounterOnly) index = Array.IndexOf(arguments, "-moriEncounterSmoke");
            if (index < 0 || !Application.isEditor && !Debug.isDebugBuild) return;
            CampaignProgress.Volatile = true; CampaignProgress.ClearVolatile();
            var entity = new GameObject("SideScrollerIntegrationTest"); DontDestroyOnLoad(entity);
            var runner = entity.AddComponent<SideScrollerSmokeTest>();
            runner.artOnly = artOnly;
            runner.arsenalOnly = arsenalOnly;
            runner.encounterOnly = encounterOnly;
            if (artOnly) runner.report.scope = "Windows sprite-integration diagnostics: installed art captures when present, followed by synthetic in-memory regression probes. No human balance, controller or cross-machine acceptance. Temporary config restored after probe.";
            if (arsenalOnly) runner.report.scope = "Arsenal runtime regression: synthetic input, eight character skills, sixteen techniques, eight ultimates, nine weapons, blast/piercing, viewmodels, scene feedback, window sizing and UI bounds. No physical controller or cross-machine acceptance.";
            if (encounterOnly) runner.report.scope = "Encounter Windows integration: five themed enemy rosters and multi-direction waves, five bosses through three HP phases, natural attack cycles, red boss-only warning areas, field anchoring, cleanup and damage bounds. Assisted invulnerability/teleports/HP thresholds. Not human balance or long-duration performance acceptance.";
            runner.folder = index + 1 < arguments.Length ? Path.GetFullPath(arguments[index + 1]) : Path.Combine(Application.persistentDataPath, "StageTest");
            Directory.CreateDirectory(runner.folder);
            runner.report.unity = Application.unityVersion; runner.report.startedAt = DateTimeOffset.Now.ToString("O");
            runner.deadline = Time.realtimeSinceStartup + 420;
            Application.logMessageReceived += runner.OnLog;
            runner.StartCoroutine(runner.Guard(runner.Run()));
        }

        private IEnumerator Guard(IEnumerator test)
        {
            var stack = new Stack<IEnumerator>(); stack.Push(test);
            while (stack.Count > 0 && !finished)
            {
                object current = null;
                try
                {
                    if (!stack.Peek().MoveNext()) { stack.Pop(); continue; }
                    current = stack.Peek().Current;
                    if (current is IEnumerator nested) { stack.Push(nested); continue; }
                }
                catch (Exception exception) { report.errors.Add(exception.ToString()); Complete(); yield break; }
                yield return current;
            }
            Complete();
        }

        private IEnumerator Run()
        {
            yield return DiagnosticWindow.Prepare();
            yield return null; yield return null;
            game = FindAnyObjectByType<GameManager>();
            Check(game != null && game.Config.sideScroller && game.State == RunState.Menu, "Boot opens side-scroller menu");
            report.screenWidth = Screen.width; report.screenHeight = Screen.height;
            Check(game.Config.Validate(out _), "Side config passes runtime validation");
            Check(!game.Effects.AttackTelegraphsEnabled, "Mob and environment debug overlays remain disabled; boss warnings use a separate policy");
            originalBackgroundBehavior = InputSystem.settings.backgroundBehavior;
            inputSettingsCaptured = true;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            foreach (InputDevice device in InputSystem.devices)
                if (device.enabled && (device is Keyboard || device is Mouse || device is Gamepad)) suppressed.Add(device);
            foreach (InputDevice device in suppressed) InputSystem.DisableDevice(device);
            keyboard = InputSystem.AddDevice<Keyboard>("SideTestKeyboard"); mouse = InputSystem.AddDevice<Mouse>("SideTestMouse");
            if (encounterOnly) { yield return ProbeEncounters(); yield break; }
            if (arsenalOnly) { yield return ProbeArsenal(); yield break; }
            if (artOnly) { yield return ProbeInstalledArt(); yield return ProbeAnimeIntegration(); yield break; }
            Check(FindButton("Character0").interactable && FindButton("Character1").interactable && FindButton("Character2").interactable == game.Config.allCharactersAvailable, "Roster availability follows configuration");
            Check(game.SelectStage(1) == game.Config.allStagesAvailable && game.SelectCharacter(2) == game.Config.allCharactersAvailable, "Stage and character selection follow configured availability policies");
            game.SelectStage(0);
            yield return Snap("01_roster.png");
            yield return ClickPointer("Character1");
            Check(game.SelectedCharacter == 1, "Actual UI pointer selects Vanguard");
            yield return ClickPointer("Character0");
            yield return ClickPointer("Start");
            yield return new WaitForSecondsRealtime(0.4f);
            Check(game.IsPlaying && game.Waves == null && game.Level != null, "Campaign replaces survival waves");
            game.Level.enabled = false;
            Check(game.CameraRig.Camera.orthographic && Cursor.visible, "Orthographic side view keeps pointer visible");
            Check(game.Player.GetComponent<CapsuleCollider2D>().enabled && !game.Player.GetComponent<CircleCollider2D>().enabled, "Player uses platform capsule");
            Check(game.Player.GetComponent<Rigidbody2D>().gravityScale > 0 && game.Player.Motor.Grounded, "Gravity and ground sensor are active");
            Check(game.Player.GetComponent<SideCharacterVisual>() != null, "Articulated character rig attached");
            Sprite head = game.Art.Side.Heads[0]; var textures = new SecondarySpriteTexture[1];
            Check(head.GetSecondaryTextures(textures) == 1 && textures[0].name == "_NormalMap", "Character normal texture is bound to generated sprite");
            float start = game.Player.transform.position.x;
            Keys(Key.D); yield return new WaitForSecondsRealtime(0.3f); Keys();
            Check(game.Player.transform.position.x > start + 0.8f, "Keyboard D moves player horizontally");
            game.Player.Motor.ResetAt(new Vector2(3, 1)); yield return new WaitForSecondsRealtime(0.25f);
            Keys(Key.Space); yield return new WaitForSecondsRealtime(0.28f);
            float heldHeight = game.Player.transform.position.y;
            Check(heldHeight > 2.3f, "Held jump gains platform height");
            Keys(); yield return new WaitForSecondsRealtime(1f);
            // A held jump can land on the new approach platform. Compare both arcs from the same floor.
            game.Player.Motor.ResetAt(new Vector2(3, 1)); yield return new WaitForSecondsRealtime(0.3f);
            Check(game.Player.Motor.Grounded, "Short-jump comparison starts on the original floor");
            Keys(Key.Space); yield return new WaitForSecondsRealtime(0.045f); Keys();
            float tapMax = 0; float tapUntil = Time.realtimeSinceStartup + 0.7f;
            while (Time.realtimeSinceStartup < tapUntil) { tapMax = Mathf.Max(tapMax, game.Player.transform.position.y); yield return null; }
            Check(tapMax < heldHeight, "Release jump produces a lower arc without repeated velocity truncation; tap=" + tapMax + "; held=" + heldHeight);
            yield return new WaitForSecondsRealtime(0.3f);
            Keys(Key.S); yield return new WaitForSecondsRealtime(0.1f);
            Check(game.Player.Motor.Crouching && game.Player.GetComponent<CapsuleCollider2D>().size.y < 1.1f, "Crouch shrinks hitbox");
            Keys(); yield return null;
            start = game.Player.transform.position.x;
            Keys(Key.D, Key.LeftShift); yield return new WaitForSecondsRealtime(0.12f);
            Check(game.Player.Motor.IsDodging && game.Player.Health.Invulnerable && game.Player.transform.position.x > start + 1, "Shift dash has burst movement and invulnerability");
            Keys(); yield return new WaitForSecondsRealtime(0.3f);
            PlatformSurface platform = FindOneWay(); Bounds bounds = platform.GetComponent<Collider2D>().bounds;
            game.Player.Motor.ResetAt(new Vector2(bounds.center.x, bounds.max.y + 0.85f));
            yield return new WaitForSecondsRealtime(0.3f);
            Check(game.Player.Motor.Grounded, "One-way platform supports player");
            Keys(Key.S, Key.Space); yield return new WaitForSecondsRealtime(0.18f); Keys();
            Check(game.Player.transform.position.y < bounds.max.y + 0.5f, "Down+jump drops through one-way platform");
            yield return new WaitForSecondsRealtime(0.6f);
            game.Player.Motor.ResetAt(new Vector2(bounds.max.x - 0.2f, bounds.max.y + 0.85f));
            yield return new WaitForSecondsRealtime(0.2f);
            Keys(Key.D); float edgeDeadline = Time.realtimeSinceStartup + 2;
            while (game.Player.Motor.Grounded && Time.realtimeSinceStartup < edgeDeadline) yield return null;
            Keys(Key.D, Key.Space); yield return new WaitForSecondsRealtime(0.09f); Keys();
            Check(game.Player.Motor.CoyoteJumpUsed, "Coyote jump succeeds immediately after leaving platform edge");
            game.Player.Motor.ResetAt(new Vector2(3, 1.06f)); game.Player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.down;
            Keys(Key.Space); yield return new WaitForSecondsRealtime(0.18f); Keys();
            Check(game.Player.Motor.BufferedJumpUsed, "Buffered jump is consumed on landing");
            game.Player.Motor.ResetAt(new Vector2(3, 1)); yield return new WaitForSecondsRealtime(0.4f);
            foreach (ViewMode mode in new[] { ViewMode.Shoulder, ViewMode.IronSight, ViewMode.Tactical })
            {
                Keys(Key.Tab); yield return null; Keys(); yield return new WaitForSecondsRealtime(0.5f);
                Check(game.CameraRig.Side.Mode == mode && game.CameraRig.Camera.orthographic, "Tab switches to " + mode + " without changing projection");
                if (mode == ViewMode.IronSight) { Check(game.Hud.ScopeOpacity > 0.8f, "Iron sight scope overlay fades in"); yield return Snap("02_ironsight.png"); }
            }
            Pointer(game.CameraRig.Camera.WorldToScreenPoint(game.Player.Motor.MuzzleOrigin + Vector2.right * 5), 2);
            yield return new WaitForSecondsRealtime(0.4f);
            Check(game.CameraRig.IsAds && game.CameraRig.Camera.orthographicSize < game.Config.viewTacticalSize - 1, "Right mouse smoothly enters ADS"); Pointer(Vector2.zero);
            // Supplies can be collected during the traversal probes above and intentionally lock
            // the powered weapon. Isolate the normal number-key/ammunition contract here.
            WeaponCapsuleDirector supplies = game.Level.GetComponent<WeaponCapsuleDirector>();
            if (supplies != null) { supplies.AutomaticSupplies = false; supplies.ClearAll(); }
            game.Player.Weapon.ClearPowerup();
            for (int index = 0; index < 3; index++)
            {
                Keys(index == 0 ? Key.Digit1 : index == 1 ? Key.Digit2 : Key.Digit3); yield return new WaitForSecondsRealtime(0.08f); Keys();
                yield return new WaitForSecondsRealtime(0.3f); int ammo = game.Player.Weapon.Ammo;
                Check(game.Player.Weapon.Selected == index && game.IsPlaying, "Weapon selection " + index + " active; state=" + game.State);
                Pointer(game.CameraRig.Camera.WorldToScreenPoint(game.Player.Motor.MuzzleOrigin + Vector2.right * 8), 1);
                float fireUntil = Time.realtimeSinceStartup + 1.8f;
                while (game.Player.Weapon.Ammo == ammo && Time.realtimeSinceStartup < fireUntil) yield return null;
                Pointer(Vector2.zero);
                Check(game.Player.Weapon.Selected == index && game.Player.Weapon.Ammo < ammo, "Weapon " + index + " consumes ammo and fires; before=" + ammo + " after=" + game.Player.Weapon.Ammo + " selected=" + game.Player.Weapon.Selected + " state=" + game.State + " fire=" + game.Input.Fire);
                yield return new WaitForSecondsRealtime(0.1f);
            }
            if (supplies != null) supplies.AutomaticSupplies = true;
            Keys(Key.Digit1); yield return null; Keys(); yield return new WaitForSecondsRealtime(0.3f);
            resumeFocusPauses = false;
            Keys(Key.Escape); yield return new WaitForSecondsRealtime(0.1f); Keys();
            Check(game.State == RunState.Paused && Time.timeScale == 0, "Escape pauses campaign simulation");
            yield return new WaitForSecondsRealtime(0.1f);
            Keys(Key.Escape); yield return new WaitForSecondsRealtime(0.1f); Keys();
            Check(game.IsPlaying && Time.timeScale == 1, "Escape resumes campaign simulation");
            resumeFocusPauses = true;
            game.Experience.Gain(100); Check(game.State == RunState.Upgrade, "XP triggers three-choice upgrade");
            Check(game.Experience.Options.Length == 3, "Upgrade offers three options");
            ChooseAll(); yield return new WaitForSecondsRealtime(0.3f);
            yield return Snap("03_city.png");
            game.Level.enabled = true;

            for (int stage = 0; stage < 5; stage++)
            {
                if (stage > 0) { Invoke("NextStage"); yield return new WaitForSecondsRealtime(0.4f); }
                Check(game.SelectedStage == stage && game.Level.Segments.Length == 8 && game.Level.Segments[7].boss,
                    "Stage " + (stage + 1) + " initializes seven combat segments and a boss arena");
                Check(game.Level.GetComponentsInChildren<ParallaxLayer>().Length == 5, "Stage " + (stage + 1) + " has five parallax layers");
                if (stage == 1) Check(FindObjectsByType<StageHazard>().Length > 0 && HasConveyor(), "Factory includes belts and timed presses");
                if (stage == 2) Check(HasIce() && game.SelectCharacter(2) == false, "Ice floor active; live character swapping rejected");
                if (stage == 3) Check(HasElevator(), "Future city includes moving lifts");
                while (!game.Level.BossSpawned)
                {
                    ChooseAll(); game.Player.Health.Invulnerable = true;
                    if (game.Level.SecretRoutes != null && game.Level.SecretRoutes.Inside)
                    {
                        // This probe follows the main route. The dedicated feedback test clears all eight secret rooms.
                        Keys(); Pointer(Vector2.zero);
                        game.Player.Motor.ResetAt(game.Level.SecretRoutes.ExitPosition);
                        float exitDeadline = Time.realtimeSinceStartup + 3;
                        while (game.Level.SecretRoutes.Inside && Time.realtimeSinceStartup < exitDeadline)
                        { ChooseAll(); yield return null; }
                        Check(!game.Level.SecretRoutes.Inside, "Main-route probe returns through a contacted secret exit");
                        continue;
                    }
                    if (Time.realtimeSinceStartup >= nextHeartbeat)
                    {
                        nextHeartbeat = Time.realtimeSinceStartup + 5;
                        Debug.Log("MORI_STAGE_TRAVERSE | stage=" + stage + " | segment=" + game.Level.SegmentIndex + " | x=" + game.Player.transform.position.x + " | enemies=" + game.Enemies.Count + " | grounded=" + game.Player.Motor.Grounded + " | jumps=" + game.Player.Motor.JumpCount + " | state=" + game.State + " | input=" + game.Input.Move + "/" + game.Input.Fire + " | devices=" + keyboard.enabled + "/" + mouse.enabled);
                        yield return Snap("traversal_latest.png");
                    }
                    if (stage == 0)
                    {
                        EnemyAI target = ClosestEnemy();
                        StageInteractable barrel = ClosestBarrel();
                        Vector2 aim = target != null ? (Vector2)target.transform.position : barrel != null ? (Vector2)barrel.transform.position : game.Player.Motor.MuzzleOrigin + Vector2.right * 8;
                        DestructibleCover obstruction = null;
                        foreach (DestructibleCover cover in game.Level.GetComponentsInChildren<DestructibleCover>())
                        {
                            float ahead = cover.transform.position.x - game.Player.transform.position.x;
                            if (!cover.IsDestroyed && ahead > 0 && ahead < 3
                                && cover.BlockingBounds.max.y > game.Player.transform.position.y - 0.7f)
                            { obstruction = cover; aim = cover.transform.position; break; }
                        }
                        Pointer(game.CameraRig.Camera.WorldToScreenPoint(aim), target != null || barrel != null || obstruction != null ? (ushort)1 : (ushort)0);
                        bool elevated = target != null && target.transform.position.y > game.Player.transform.position.y + 1.1f;
                        bool blocked = Mathf.Abs(game.Player.transform.position.x - lastPosition) < 0.1f && Time.realtimeSinceStartup - lastPositionTime > 1.2f;
                        if (Mathf.Abs(game.Player.transform.position.x - lastPosition) > 0.05f) { lastPosition = game.Player.transform.position.x; lastPositionTime = Time.realtimeSinceStartup; }
                        if ((elevated || blocked) && game.Player.Motor.Grounded && Time.realtimeSinceStartup >= nextJump)
                        {
                            if (blocked && game.Player.transform.position.y > 2)
                            { dropHeldUntil = Time.realtimeSinceStartup + 0.22f; jumpHeldUntil = 0; }
                            else jumpHeldUntil = Time.realtimeSinceStartup + 0.25f;
                            nextJump = Time.realtimeSinceStartup + 0.9f;
                        }
                        if (Time.realtimeSinceStartup < dropHeldUntil) Keys(Key.D, Key.S, Key.Space);
                        else if (Time.realtimeSinceStartup < jumpHeldUntil) Keys(Key.D, Key.Space); else Keys(Key.D);
                        if (game.Level.SegmentIndex == 2 && game.Level.CheckpointSegment == 2 && game.Level.Deaths == 0)
                        {
                            Keys(); Pointer(Vector2.zero); game.Player.Health.Invulnerable = false;
                            yield return new WaitForSecondsRealtime(0.65f); game.Player.Health.TakeDamage(10000, Team.Enemy, true);
                            Check(game.State == RunState.Lost, "Player death opens defeat screen"); yield return Snap("04_defeat.png");
                            int xp = game.Experience.Track.Current; int checkpoint = game.Level.CheckpointSegment;
                            Invoke("Restart"); yield return new WaitForSecondsRealtime(0.4f);
                            Check(game.IsPlaying && game.Level.SegmentIndex == checkpoint && game.Level.Deaths == 1, "Retry restores checkpoint and counts death");
                            Check(game.Experience.Track.Current == xp / 2 && game.Player.Health.Current > 0, "Retry halves unspent XP and restores health");
                        }
                    }
                    else
                    {
                        foreach (EnemyAI enemy in game.Enemies.ToArray()) if (enemy != null) enemy.Health.TakeDamage(10000, Team.Player);
                        SegmentDefinition segment = game.Level.Segments[game.Level.SegmentIndex];
                        game.Player.Motor.ResetAt(new Vector2(game.Level.EncounterStarted ? segment.End - 1 : segment.start + 6, 1));
                        Pointer(Vector2.zero); Keys();
                    }
                    yield return null;
                }
                Keys(); Pointer(Vector2.zero); ChooseAll(); game.Player.Health.Invulnerable = true;
                Check(game.Level.CheckpointSegment == 7, "Stage " + (stage + 1) + " advances all seven encounter checkpoints");
                EnemyAI boss = game.Level.Boss; SideBossController controller = boss.GetComponent<BossController>().Side;
                game.Player.Motor.ResetAt(new Vector2(game.Level.Segments[game.Level.Segments.Length - 1].start + 11, 1));
                yield return WaitGameplay(1.2f); yield return Snap("stage_" + (stage + 1) + "_boss.png");
                Vector3 bossScreen = game.CameraRig.Camera.WorldToViewportPoint(boss.transform.position);
                Check(bossScreen.x > 0.02f && bossScreen.x < 0.98f && bossScreen.y > 0 && bossScreen.y < 1, "Stage " + (stage + 1) + " boss is framed in tactical view");
                for (int phase = 1; phase <= 3; phase++)
                {
                    if (phase > 1)
                    {
                        float fraction = phase == 2 ? 0.54f : 0.24f;
                        boss.Health.TakeDamage(Mathf.Max(1, boss.Health.Current - boss.Health.Maximum * fraction), Team.Player, true);
                        yield return WaitGameplay(0.2f);
                        Check(controller.Phase == phase && controller.Transitioning && boss.Health.Invulnerable, "Stage " + (stage + 1) + " enters protected phase " + phase);
                        yield return WaitGameplay(2.1f);
                        if (phase == 3) yield return Snap("stage_" + (stage + 1) + "_phase3.png");
                    }
                    int attacks = controller.AttacksResolved; float attackDeadline = Time.realtimeSinceStartup + 5;
                    while (controller.AttacksResolved <= attacks && Time.realtimeSinceStartup < attackDeadline) { ChooseAll(); game.Player.Health.Invulnerable = true; yield return null; }
                    Check(controller.AttacksResolved > attacks, "Stage " + (stage + 1) + " phase " + phase + " resolves telegraphed attack");
                }
                if (stage == 3) Check(controller.UnlockedAttackCount == 3
                    && controller.GetAttackId(2) == nameof(BossAttackKind.CyberMirrorExchange)
                    && controller.FieldsCreated > 0, "Zero phase 3 unlocks mirror exchange alongside its board and scan attacks");
                if (stage == 4)
                {
                    Check(controller.Arms != null && controller.Arms.Length == 2
                        && !controller.Arms[0].IsDead && !controller.Arms[1].IsDead
                        && !boss.Health.Invulnerable, "Final form exposes a damageable core and two independent rift counter anchors");
                    int riftFields = 0;
                    foreach (BossField field in game.Level.GetComponentsInChildren<BossField>())
                        if (field.Owner == boss.Health && field.Style == BossFieldStyle.Rift) riftFields++;
                    Check(riftFields == 2, "Both living anchors produce separate rift slashes");
                    foreach (Health arm in controller.Arms) arm.TakeDamage(10000, Team.Player);
                    yield return new WaitForSecondsRealtime(0.1f);
                    riftFields = 0;
                    foreach (BossField field in game.Level.GetComponentsInChildren<BossField>())
                        if (field.Owner == boss.Health && field.Style == BossFieldStyle.Rift) riftFields++;
                    Check(controller.Arms[0].IsDead && controller.Arms[1].IsDead
                        && riftFields == 0 && !boss.Health.Invulnerable, "Destroying both counter anchors cancels their rift slashes and leaves the core vulnerable");
                }
                boss.Health.TakeDamage(10000, Team.Player, true);
                Check(controller.ExecutionWindow, "Stage " + (stage + 1) + " enters its execution presentation");
                yield return new WaitForSecondsRealtime(1.5f);
                Check(game.State == RunState.Won && Time.timeScale == 0, "Stage " + (stage + 1) + " boss death produces victory");
                Check(CampaignProgress.Unlocked >= Mathf.Min(5, stage + 2), "Stage " + (stage + 1) + " records campaign unlock");
                yield return Snap("stage_" + (stage + 1) + "_victory.png");
            }
            Invoke("Menu"); yield return null;
            Check(game.Player == null && game.State == RunState.Menu, "Returning to menu clears campaign objects");
            Check(game.SelectCharacter(2) && game.SelectCharacter(3), "Wraith and Havoc remain selectable after campaign completion");
            game.SelectCharacter(2); Invoke("Start"); yield return WaitGameplay(0.4f);
            Check(Mathf.Approximately(game.Player.Health.Maximum, game.Config.playerHealth * 0.75f), "Wraith uses reduced HP");
            Keys(); yield return WaitGameplay(0.08f);
            int firstJump = game.Player.Motor.JumpCount;
            Keys(Key.Space); yield return WaitGameplay(0.15f);
            Check(game.Player.Motor.JumpCount == firstJump + 1 && !game.Player.Motor.Grounded, "Wraith first jump is airborne before double-jump probe");
            Keys(); yield return WaitGameplay(0.08f);
            int jumpCount = game.Player.Motor.JumpCount;
            Keys(Key.Space); yield return WaitGameplay(0.15f); Keys();
            Check(game.Player.Motor.JumpCount == jumpCount + 1, "Wraith performs an airborne second jump");
            game.ReturnToMenu(); game.SelectCharacter(1); Invoke("Start"); yield return WaitGameplay(0.3f);
            Check(Mathf.Approximately(game.Player.Health.Maximum, game.Config.playerHealth * 1.4f), "Vanguard uses increased HP");
            game.ReturnToMenu(); game.SelectCharacter(3);
            Check(game.SelectStage(4), "Final stage can be replayed");
            Invoke("Start"); yield return new WaitForSecondsRealtime(0.3f);
            Check(Mathf.Abs(game.Player.Weapon.Capacity - Mathf.CeilToInt(game.Config.weapons[0].magazine * 1.3f)) < 1, "Havoc receives expanded magazine");
            Check(game.Player.Motor != null && CampaignProgress.Volatile, "Diagnostics do not write campaign progress to disk");
            game.ReturnToMenu();
            yield return ProbeAnimeIntegration();
        }

        private IEnumerator ProbeEncounters()
        {
            CampaignProgress.Unlock(5);
            var uniqueSprites = new HashSet<Sprite>();
            var uniqueNames = new HashSet<string>();
            var uniqueArchetypes = new HashSet<EnemyArchetype>();
            var uniqueBossAttacks = new HashSet<string>();
            Check(game.Config.characters.Length == 8, "All eight configured player characters are available to the encounter test");
            for (int stage = 0; stage < 5; stage++)
            {
                game.ReturnToMenu(); yield return null;
                game.SelectStage(stage); game.BeginRun(); game.Level.enabled = false;
                yield return WaitGameplay(0.3f);
                ThemeProfile theme = game.Level.Theme;
                Check(theme.encounterEffects != null && theme.encounterEffects.Length == 8, "Stage " + stage + " has eight dedicated effect textures");
                Check(theme.hazardAnimations != null && theme.hazardAnimations.Length == 2
                    && theme.hazardAnimations[0].IsReady && theme.hazardAnimations[1].IsReady
                    && theme.stationaryAnimation != null && theme.stationaryAnimation.IsReady,
                    "Stage " + stage + " uses painted rock, spike and turret frames");
                Check(theme.guardianSprites != null && theme.guardianSprites.Length == 4
                    && theme.guardianSprites[0] != theme.BossForm(1).Frame(0),
                    "Stage " + stage + " secret guardians have their own art");
                Check(theme.BossForm(1).Frame(0) != theme.BossForm(2).Frame(0)
                    && theme.BossForm(2).Frame(0) != theme.BossForm(3).Frame(0),
                    "Stage " + stage + " has three visually distinct boss forms");
                Check(Mathf.Approximately(game.DamageScale, game.Config.incomingDamageMultiplier + stage * game.Config.stageDamageStep),
                    "Stage " + stage + " applies the configured moderately-high incoming damage scale");
                EncounterHazardDirector hazards = game.Level.GetComponent<EncounterHazardDirector>();
                Check(game.Level.Segments.Length == 8 && hazards != null && hazards.TriggerCount >= 14,
                    "Stage " + stage + " has seven trap-filled combat segments and an independent boss arena");
                var stageArchetypes = new HashSet<EnemyArchetype>();
                for (int variant = 0; variant < 4; variant++)
                {
                    SegmentDefinition probeSegment = new SegmentDefinition { start = variant * 34, width = 34, layout = variant % 3 };
                    EncounterSpawn[] formation = EncounterDirector.Build(stage, probeSegment, variant);
                    Check(formation.Length >= 6 && formation.Length <= EncounterDirector.MaximumEncounterEnemies
                        && Array.Exists(formation, spawn => spawn.Position.x < probeSegment.start + 5)
                        && Array.Exists(formation, spawn => spawn.Kind == EnemyKind.Runner && spawn.Position.y > 5),
                        "Stage " + stage + " formation " + variant + " combines a rear flank, overhead wave and dense roster");
                    foreach (EncounterSpawn spawn in formation) { stageArchetypes.Add(spawn.Archetype); uniqueArchetypes.Add(spawn.Archetype); }
                }
                int expectedArchetypes = stage == 3 ? 5 : 4; // City includes the independent laser sentry.
                Check(stageArchetypes.Count == expectedArchetypes,
                    "Stage " + stage + " uses " + expectedArchetypes + " dedicated enemy archetypes; actual=" + stageArchetypes.Count);
                Check(hazards.TriggerAt(EncounterHazardKind.FallingRocks, new Vector2(25, 0.1f), 0),
                    "Stage " + stage + " schedules its environmental attack");
                Check(!HasVisibleAttackOverlay(), "Stage " + stage + " hides environmental landing markers and warning boxes");
                yield return WaitGameplay(1.4f);
                Check(hazards.ActiveHazards > 0, "Stage " + stage + " scheduled attack becomes a visible falling rock");
                hazards.ClearTransient();
                var minions = new List<EnemyAI>();
                for (int kind = 0; kind < 4; kind++)
                {
                    SideSpriteAnimation animation = theme.EnemyAnimation((EnemyKind)kind, null);
                    Check(animation != null && animation.IsReady && animation.frames.Length == 2 && uniqueSprites.Add(animation.Frame(0)), "Stage " + stage + " kind " + kind + " has unique two-frame art");
                    Check(uniqueNames.Add(EnemyRoster.Name(stage, (EnemyKind)kind)), "Stage " + stage + " kind " + kind + " has distinct identity");
                    EnemyAI minion = game.Level.Spawn((EnemyKind)kind, new Vector2(7 + kind * 2.1f, kind == 2 ? 3.5f : 1.2f));
                    minions.Add(minion);
                    Check(minion.transform.Find("AnimeAnimatedSprite").GetComponent<SpriteRenderer>().sprite == animation.Frame(0), "Stage " + stage + " minion " + kind + " actually uses theme art");
                }
                float volleyDeadline = Time.time + 8;
                while ((minions[1].GetComponent<SideEnemyMotor>().VolleysFired == 0
                    || minions[2].GetComponent<SideEnemyMotor>().VolleysFired == 0) && Time.time < volleyDeadline)
                    yield return WaitGameplay(0.1f);
                yield return Snap("encounter_" + stage + "_roster.png");
                Check(minions[1].GetComponent<SideEnemyMotor>().VolleysFired > 0 && minions[2].GetComponent<SideEnemyMotor>().VolleysFired > 0,
                    "Stage " + stage + " gunner and drone execute their attacks");
                Check(!HasVisibleAttackOverlay(), "Stage " + stage + " enemy attacks do not leave predictive aim lines");
                foreach (EnemyAI minion in minions) { minion.gameObject.SetActive(false); Destroy(minion.gameObject); game.Enemies.Remove(minion); }
                game.Projectiles.ClearAll(); game.Effects.ClearAll();
                Vector2 arena = new Vector2(game.Level.Segments[game.Level.Segments.Length - 1].start + 12, 1.2f);
                game.Player.Motor.ResetAt(arena); game.CameraRig.ResetFollow();
                EnemyAI bossEnemy = game.Level.Spawn(EnemyKind.Boss, arena + new Vector2(5, 0.6f));
                game.Level.SetBoss(bossEnemy);
                Check(game.Level.Boss == bossEnemy && game.Level.BossSpawned,
                    "Stage " + stage + " registers the required main boss before testing its victory gate");
                SideBossController boss = bossEnemy.GetComponent<BossController>().Side;
                game.Win();
                Check(game.State == RunState.Playing, "Stage " + stage + " cannot clear while the main boss still has health");
                float bossStartHealth = bossEnemy.Health.Current;
                bossEnemy.Health.TakeDamage(10, Team.Player);
                Check(Mathf.Abs(bossEnemy.Health.LastDamageTaken - 3.8f) < 0.02f,
                    "Stage " + stage + " boss armour reduces a real ten-point hit to 3.8");
                bossEnemy.Health.TakeDamage(100000, Team.Player);
                Check(Mathf.Abs(bossEnemy.Health.LastDamageTaken / bossEnemy.Health.Maximum - 0.008f) < 0.0001f,
                    "Stage " + stage + " boss caps a huge hit at 0.8 percent maximum health");
                bossEnemy.Health.SetCurrent(bossStartHealth);
                Check(Mathf.Approximately(bossEnemy.Health.Maximum, 1350 * (1 + stage * 0.2f)),
                    "Stage " + stage + " boss has 1.5x its previous health pool");
                for (int attack = 0; attack < 3; attack++)
                    Check(uniqueBossAttacks.Add(boss.GetAttackId(attack)), "Stage " + stage + " attack " + attack + " is unique across all bosses");
                Pointer(new Vector2(Screen.width * 0.8f, Screen.height * 0.5f));
                Check(bossEnemy.GetComponent<BossSignatureVfx>().ThemeIndex == stage, "Stage " + stage + " uses a distinct boss signature effect");
                bool trackingStrikeChecked = false;
                for (int phase = 1; phase <= 3; phase++)
                {
                    if (phase > 1) bossEnemy.Health.SetCurrent(bossEnemy.Health.Maximum * (phase == 2 ? 0.5f : 0.2f));
                    yield return WaitGameplay(0.12f);
                    Check(boss.Phase == phase, "Stage " + stage + " enters phase " + phase);
                    BossSignatureVfx physical = bossEnemy.GetComponent<BossSignatureVfx>();
                    Check(physical.UsesIllustratedParts && physical.PartCount == 4,
                        "Stage " + stage + " phase " + phase + " uses four tangible generated equipment pieces");
                    if (phase > 1)
                    {
                        Check(boss.Transitioning && bossEnemy.Health.Invulnerable, "Phase transition protects boss and displays reactor burst");
                        yield return Snap("encounter_" + stage + "_phase_" + phase + ".png");
                        yield return WaitGameplay(2.1f);
                    }
                    int attacks = boss.AttacksResolved;
                    int effects = game.Effects.EncounterEmissions;
                    int impactEffects = game.Effects.BossSkillImpactEmissions;
                    int ultimates = game.Effects.BossUltimateEmissions;
                    bool overlaysHidden = true;
                    bool preparationCaptured = false;
                    int required = stage == 4 ? 4 : phase == 3 ? 3 : 2;
                    float until = Time.time + 25;
                    bool captured = false;
                    bool ultimateCaptured = false;
                    while (boss.AttacksResolved < attacks + required && Time.time < until)
                    {
                        overlaysHidden &= !HasVisibleAttackOverlay();
                        if (!preparationCaptured && boss.Preparing)
                        {
                            preparationCaptured = true;
                            bool hasRedWarning = false;
                            foreach (BossField warningField in game.Level.GetComponentsInChildren<BossField>())
                            {
                                Transform footprint = warningField.transform.Find("DamageFootprint");
                                SpriteRenderer renderer = footprint != null ? footprint.GetComponent<SpriteRenderer>() : null;
                                if (warningField.IsWarning && warningField.Damage > 0 && renderer != null && renderer.enabled
                                    && renderer.color.r > 0.9f && renderer.color.g < 0.15f && renderer.color.a > 0 && renderer.color.a < 0.5f)
                                    hasRedWarning = true;
                            }
                            Check(hasRedWarning, "Stage " + stage + " phase " + phase + " previews its attack using a translucent red damage area");
                            yield return Snap("encounter_" + stage + "_red_preparation_phase_" + phase + ".png");
                        }
                        if (!trackingStrikeChecked && boss.Preparing && boss.TrackingStrike != null)
                        {
                            trackingStrikeChecked = true;
                            Check(boss.TrackingStrike.IsWarning && boss.TrackingStrike.Contains(game.Player.transform.position),
                                "Stage " + stage + " targets a stationary player with a themed tracked strike warning");
                            Vector2 movedTarget = (Vector2)game.Player.transform.position + Vector2.right * 0.72f;
                            game.Player.Motor.ResetAt(movedTarget);
                            yield return WaitGameplay(0.08f);
                            Check(boss.TrackingStrike != null && boss.TrackingStrike.IsWarning
                                && boss.TrackingStrike.Contains(game.Player.transform.position),
                                "Stage " + stage + " retargets its warning after player movement during windup");
                        }
                        if (!captured && boss.AttacksResolved > attacks)
                        {
                            captured = true;
                            yield return Snap("encounter_" + stage + "_attack_phase_" + phase + ".png");
                        }
                        if (!ultimateCaptured && game.Effects.BossUltimateEmissions > ultimates)
                        {
                            ultimateCaptured = true;
                            yield return WaitGameplay(0.18f);
                            yield return Snap("encounter_" + stage + "_ultimate.png");
                        }
                        yield return WaitGameplay(0.06f);
                    }
                    Check(boss.AttacksResolved >= attacks + required, "Stage " + stage + " phase " + phase + " resolves " + required + " natural attack patterns");
                    Check(game.Effects.EncounterEmissions > effects, "Stage " + stage + " phase " + phase + " retains actual attack VFX");
                    Check(game.Effects.BossSkillImpactEmissions > impactEffects,
                        "Stage " + stage + " phase " + phase + " emits layered stage-specific boss skill effects");
                    if (phase == 3)
                    {
                        Check(game.Effects.BossUltimateEmissions > ultimates,
                            "Stage " + stage + " final form naturally emits its dedicated four-frame ultimate");
                        if (!ultimateCaptured && game.Effects.BossUltimateEmissions > ultimates)
                        { yield return WaitGameplay(0.18f); yield return Snap("encounter_" + stage + "_ultimate.png"); }
                    }
                    Check(overlaysHidden && !HasVisibleAttackOverlay() && game.Effects.WarningEmissions == game.Effects.BossWarningEmissions,
                        "Stage " + stage + " phase " + phase + " shows no mob/environment debug warnings or countdowns; boss red areas are intentional");
                    foreach (BossField field in game.Level.GetComponentsInChildren<BossField>())
                        Check(field.transform.parent == game.Level.transform && field.Remaining > 0, "Live boss damage field is anchored to world, independent of boss motion");
                }
                if (stage == 1) yield return WaitGameplay(10.1f);
                float minimum = float.MaxValue, maximum = float.MinValue;
                yield return WaitGameplay(2.5f);
                float stableUntil = Time.time + 3;
                while (Time.time < stableUntil)
                {
                    if (!boss.Transitioning && !boss.Charging)
                    {
                        minimum = Mathf.Min(minimum, bossEnemy.transform.position.y);
                        maximum = Mathf.Max(maximum, bossEnemy.transform.position.y);
                    }
                    yield return WaitGameplay(0.04f);
                }
                Check(maximum >= minimum && maximum - minimum < 0.12f, "Stage " + stage + " boss body remains vertically stable; effects carry idle motion");
                if (stage == 1 || stage == 2 || stage == 3 || stage == 4) Check(boss.FieldsCreated > 0, "Stage " + stage + " emitted elemental or laser fields");
                if (boss.Arms != null) foreach (Health arm in boss.Arms) { arm.Invulnerable = false; arm.TakeDamage(10000, Team.Player); }
                bossEnemy.Health.Invulnerable = false; bossEnemy.Health.TakeDamage(100000, Team.Player, true);
                Check(boss.ExecutionWindow && Time.timeScale < 1, "Stage " + stage + " triggers a slow-motion execution before victory");
                yield return new WaitForSecondsRealtime(0.5f);
                yield return Snap("encounter_" + stage + "_execution.png");
                yield return new WaitForSecondsRealtime(1.0f);
                Check(game.State == RunState.Won && game.Level.GetComponentsInChildren<BossField>().Length == 0,
                    "Stage " + stage + " boss defeat clears dangerous fields and wins; state=" + game.State
                    + ", fields=" + game.Level.GetComponentsInChildren<BossField>().Length);
                yield return Snap("encounter_" + stage + "_defeated.png");
            }
            Check(uniqueArchetypes.Count == 21, "Every campaign stage has its own four-to-five enemy archetypes");
            game.ReturnToMenu(); yield return null;
            game.SelectStage(0); game.BeginRun(); game.Level.enabled = false;
            yield return WaitGameplay(0.3f);
            const float probeX = 1000;
            const float probeY = 100;
            EnemyAI owner = game.Level.Spawn(EnemyKind.Elite, new Vector2(probeX + 5, probeY));
            owner.GetComponent<SideEnemyMotor>().Stun(30);
            owner.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
            var entity = new GameObject("FieldBoundsProbe"); entity.transform.SetParent(game.Level.transform);
            entity.transform.position = new Vector2(probeX, probeY);
            BossField probe = entity.AddComponent<BossField>(); probe.Initialize(game, owner.Health, new Vector2(2, 0.3f), 10, true);
            game.Player.Motor.SetCinematicLocked(true);
            game.Player.Health.Invulnerable = true;
            game.Player.Motor.ResetAt(new Vector2(probeX + 4, probeY));
            Physics2D.SyncTransforms();
            yield return new WaitForSeconds(0.15f);
            float health = game.Player.Health.Current;
            game.Player.Health.Invulnerable = false;
            yield return new WaitForSeconds(0.15f);
            Check(game.Player.Health.Current == health,
                "Outside visual field bounds receives no damage (player " + game.Player.transform.position + ", field " + probe.transform.position + ")");
            game.Player.Health.Invulnerable = true;
            game.Player.Motor.ResetAt(new Vector2(probeX, probeY));
            Physics2D.SyncTransforms();
            yield return new WaitForSeconds(0.15f);
            game.Player.Health.Invulnerable = false;
            yield return new WaitForSeconds(0.15f);
            Check(game.Player.Health.Current < health, "Inside visual field bounds takes real damage");
            resumeFocusPauses = false; game.Pause();
            float remaining = probe.Remaining;
            yield return new WaitForSecondsRealtime(0.2f);
            Check(Mathf.Abs(probe.Remaining - remaining) < 0.001f, "Pause freezes boss field lifetime");
            game.Resume(); resumeFocusPauses = true;
            Vector3 anchored = probe.transform.position; owner.transform.position += Vector3.right * 2;
            yield return null;
            Check(probe.transform.position == anchored, "Moving caster does not move active field");
            game.Level.Retry(); yield return null; yield return null;
            Check(game.Level.GetComponentsInChildren<BossField>().Length == 0, "Checkpoint retry removes owner-bound fields");
            game.Player.Motor.SetCinematicLocked(false);
            game.ReturnToMenu();
        }

        private IEnumerator ProbeAnimeIntegration()
        {
            CharacterDefinition character = game.Config.characters[0];
            ThemeProfile theme = game.Config.themes[0];
            SideSpriteAnimation originalCharacter = character.animation;
            SideSpriteAnimation originalBoss = theme.bossAnimation;
            SideSpriteAnimation[] originalBossForms = theme.bossForms;
            SideSpriteAnimation[] originalEnemies = game.Config.enemyAnimations;
            Sprite originalBackground = theme.background;
            Sprite[] originalEffects = game.Config.combatEffectSprites;
            var texture = new Texture2D(16, 32, TextureFormat.RGBA32, false);
            var colors = new Color[16 * 32];
            for (int index = 0; index < colors.Length; index++) colors[index] = Color.cyan;
            texture.SetPixels(colors); texture.Apply();
            var frames = new Sprite[8];
            for (int index = 0; index < frames.Length; index++) frames[index] = Sprite.Create(texture, new Rect(0, 0, 16, 32), Vector2.one * 0.5f, 20);
            try
            {
                character.animation = new SideSpriteAnimation { frames = frames, facesRight = true };
                theme.bossAnimation = new SideSpriteAnimation { frames = frames, facesRight = false };
                theme.bossForms = null; // Isolate the legacy frame selector from the installed production forms.
                theme.background = frames[0];
                game.Config.enemyAnimations = new[] { new SideSpriteAnimation { frames = frames, facesRight = false }, null, null, null };
                game.Config.combatEffectSprites = frames;
                game.SelectCharacter(0); game.SelectStage(0); game.BeginRun();
                game.Level.enabled = false;
                yield return WaitGameplay(0.4f);
                SideSpriteAnimator animator = game.Player.GetComponent<SideSpriteAnimator>();
                SpriteRenderer visual = game.Player.transform.Find("AnimeAnimatedSprite").GetComponent<SpriteRenderer>();
                Check(animator != null && visual.sprite == frames[0], "Synthetic sprite probe: imported idle replaces procedural body");
                bool environment = false;
                foreach (SpriteRenderer renderer in game.Level.GetComponentsInChildren<SpriteRenderer>())
                    if (renderer.name == "AnimeEnvironment" && renderer.sprite == frames[0]) environment = true;
                Check(environment, "Synthetic sprite probe: theme background assigned to parallax");
                Keys(Key.S); yield return WaitGameplay(0.1f);
                Check(animator.CurrentFrame == 7 && visual.sprite == frames[7], "Synthetic sprite probe: crouch selects frame 7");
                Keys(); yield return WaitGameplay(0.1f);
                Keys(Key.D); yield return WaitGameplay(0.2f);
                Check(animator.CurrentFrame >= 1 && animator.CurrentFrame <= 5, "Synthetic sprite probe: running cycles through animation frames");
                Keys(Key.Space); yield return WaitGameplay(0.15f); Keys();
                Check(animator.CurrentFrame == 6, "Synthetic sprite probe: jump selects frame 6");
                Pointer(game.CameraRig.Camera.WorldToScreenPoint(game.Player.Motor.MuzzleOrigin + Vector2.left * 5));
                yield return WaitGameplay(0.1f);
                Check(visual.flipX, "Synthetic sprite probe: imported player faces cursor left");
                Keys(Key.Tab); yield return null; Keys(); yield return WaitGameplay(0.2f);
                Keys(Key.Tab); yield return null; Keys(); yield return WaitGameplay(0.2f);
                Check(game.CameraRig.Side.Mode == ViewMode.IronSight && !visual.enabled, "Synthetic sprite probe: imported body hides in iron sight");
                Keys(Key.Tab); yield return null; Keys(); yield return WaitGameplay(0.2f);
                Check(visual.enabled && game.Player.Weapon != null, "Synthetic sprite probe: tactical view restores body and independent weapon");
                var entity = new GameObject("SyntheticArtEnemy"); entity.transform.SetParent(game.Player.transform.parent);
                entity.transform.position = game.Player.transform.position + Vector3.right * 6;
                EnemyAI enemy = entity.AddComponent<EnemyAI>(); enemy.Initialize(game, EnemyKind.Chaser, 1);
                yield return null; yield return null;
                Check(enemy.GetComponent<SideSpriteAnimator>() != null && entity.transform.Find("EnemyHealth") != null, "Synthetic sprite probe: enemy animation preserves health bar");
                Pointer(game.CameraRig.Camera.WorldToScreenPoint(game.Player.Motor.MuzzleOrigin + Vector2.left * 5));
                yield return WaitGameplay(0.1f);
                Sprite expectedGhost = frames[animator.CurrentFrame];
                game.Effects.ClearAll();
                game.Effects.Ghost(game.Player.transform.position);
                SpriteRenderer ghost = null;
                foreach (SpriteRenderer renderer in game.Effects.GetComponentsInChildren<SpriteRenderer>())
                    if (renderer.enabled && renderer.gameObject.activeSelf && renderer.sprite == expectedGhost && renderer.flipX) ghost = renderer;
                Check(ghost != null, "Synthetic sprite probe: dash ghost uses facing and current frame");
                game.Effects.ClearAll(); game.Effects.Muzzle(game.Player.transform.position, Vector2.right, Color.white);
                bool muzzle = false;
                foreach (SpriteRenderer renderer in game.Effects.GetComponentsInChildren<SpriteRenderer>())
                    if (renderer.gameObject.activeSelf && renderer.sprite == frames[game.Player.Weapon.Selected]) { muzzle = true; Check(!renderer.flipX && !renderer.flipY, "Synthetic sprite probe: pooled muzzle resets ghost flip"); }
                Check(muzzle, "Synthetic sprite probe: weapon muzzle uses assigned production effect");
                var bossEntity = new GameObject("SyntheticArtBoss"); bossEntity.transform.SetParent(game.Player.transform.parent);
                bossEntity.transform.position = game.Player.transform.position + Vector3.right * 10;
                EnemyAI boss = bossEntity.AddComponent<EnemyAI>(); boss.Initialize(game, EnemyKind.Boss, 1);
                yield return null; yield return null;
                SideSpriteAnimator bossAnimator = boss.GetComponent<SideSpriteAnimator>();
                Check(bossAnimator != null, "Synthetic sprite probe: theme boss uses sprite animator");
                // Boss AI has an intentional 0.85-second spawn telegraph before phase logic becomes active.
                yield return WaitGameplay(0.95f);
                boss.Health.TakeDamage(boss.Health.Maximum * 0.48f, Team.Player, true);
                yield return WaitGameplay(0.15f);
                Check(bossAnimator.CurrentFrame == 2, "Synthetic sprite probe: boss phase transition selects frame 2");
                yield return WaitGameplay(2.1f);
                boss.Health.TakeDamage(boss.Health.Maximum * 0.32f, Team.Player, true);
                yield return WaitGameplay(0.15f);
                Check(bossAnimator.CurrentFrame == 3, "Synthetic sprite probe: boss rage phase selects frame 3");
            }
            finally
            {
                game.ReturnToMenu();
                character.animation = originalCharacter; theme.bossAnimation = originalBoss; theme.bossForms = originalBossForms; theme.background = originalBackground;
                game.Config.enemyAnimations = originalEnemies; game.Config.combatEffectSprites = originalEffects;
                foreach (Sprite frame in frames) Destroy(frame);
                Destroy(texture);
            }
            Check(character.animation == originalCharacter && theme.background == originalBackground, "Synthetic sprite probe: temporary assets restored; no art acceptance implied");
        }

        private IEnumerator ProbeArsenal()
        {
            Check(game.Config.weapons.Length == 9 && game.Config.characters.Length == 8 && game.Config.allCharactersAvailable,
                "Nine weapons and eight immediately selectable characters installed");
            int originalWidth = Screen.width, originalHeight = Screen.height;
            foreach (Vector2Int size in new[] { new Vector2Int(1024, 768), new Vector2Int(1600, 720), new Vector2Int(1600, 900) })
            {
                Screen.SetResolution(size.x, size.y, FullScreenMode.Windowed);
                yield return new WaitForSecondsRealtime(0.6f);
                Check(Screen.width == size.x && Screen.height == size.y, "Window applies resolution " + size);
                foreach (Button button in game.Hud.GetComponentsInChildren<Button>())
                {
                    var corners = new Vector3[4]; button.GetComponent<RectTransform>().GetWorldCorners(corners);
                    foreach (Vector3 corner in corners)
                        Check(corner.x >= -1 && corner.y >= -1 && corner.x <= Screen.width + 1 && corner.y <= Screen.height + 1, "Menu button " + button.name + " fits viewport " + size);
                }
                yield return Snap("arsenal_menu_" + size.x + "x" + size.y + ".png");
            }
            Keys(Key.F11); yield return null; yield return null; Keys(); yield return new WaitForSecondsRealtime(0.8f);
            Check(Screen.fullScreenMode == FullScreenMode.FullScreenWindow, "F11 enters borderless fullscreen");
            Keys(Key.F11); yield return null; yield return null; Keys(); yield return new WaitForSecondsRealtime(0.6f);
            Check(!Screen.fullScreen && Screen.width == 1600 && Screen.height == 900, "F11 restores previous window size");
            Screen.SetResolution(originalWidth, originalHeight, FullScreenMode.Windowed);
            yield return new WaitForSecondsRealtime(0.4f);
            for (int character = 0; character < game.Config.characters.Length; character++)
            {
                game.ReturnToMenu(); yield return null;
                yield return ClickPointer("Character" + character);
                yield return ClickPointer("Loadout_" + (character % game.Config.weapons.Length));
                Check(game.SelectedCharacter == character, "Pointer selects character " + character);
                Check(game.Character.SkillName.Length > 0, "Character " + character + " resolves a unique skill description");
                Invoke("Start"); yield return WaitGameplay(0.45f);
                Check(!game.Player.Energy.Ready && !game.Player.Energy.TryActivateUltimate(),
                    "Character " + character + " cannot use ultimate before charging");
                Check(game.Player.Weapon.Selected == game.StartingWeapon, "Starting loadout reaches combat for character " + character);
                Check(game.Environment != null && game.Environment.PropCount >= 20, "Interactive scenery is present for character " + character);
                game.Level.enabled = false;
                var skillFloor = new GameObject("CharacterSkillProbeFloor");
                skillFloor.transform.SetParent(game.Level.transform);
                skillFloor.transform.position = new Vector2(120, 19);
                skillFloor.AddComponent<BoxCollider2D>().size = new Vector2(40, 0.4f);
                skillFloor.AddComponent<PlatformSurface>();
                game.Player.Motor.TeleportTo(new Vector2(120, 20));
                game.CameraRig.ResetFollow();
                yield return WaitGameplay(0.3f);
                EnemyAI target = game.Level.Spawn(EnemyKind.Elite, game.Player.transform.position + Vector3.right * 3.5f);
                target.Health.IncreaseMaximum(500, 500);
                if (character != 2)
                {
                    // Keep the target registered: disabling EnemyAI intentionally unregisters it from area attacks.
                    target.Motor.Stun(30);
                    target.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
                }
                yield return WaitGameplay(0.95f);
                Pointer(game.CameraRig.Camera.WorldToScreenPoint(target.transform.position)); yield return null;
                if (character == 2 || character == 5 || character == 7)
                {
                    Pointer(game.CameraRig.Camera.WorldToScreenPoint(game.Player.Motor.MuzzleOrigin + Vector2.right * 7));
                    yield return null; yield return null;
                }
                if (character == 1 || character == 6) game.Player.Health.SetCurrent(game.Player.Health.Maximum * 0.5f);
                Vector2 skillOrigin = game.Player.transform.position;
                float healthBeforeSkill = game.Player.Health.Current;
                float before = target.Health.Current;
                Keys(Key.Q); yield return null; yield return null; Keys();
                yield return WaitGameplay(0.12f);
                CharacterSkill skill = game.Player.Skill;
                Check(skill.Icon != null, "Character " + character + " resolves a dedicated skill sigil");
                Check(game.CharacterId == new[] { 0, 1, 2, 3, 8, 9, 10, 11 }[character],
                    "Character " + character + " preserves stable gameplay identity");
                if (character >= 4)
                    Check(game.Character.animation != null && game.Character.animation.IsReady
                        && game.Character.poweredAnimation != null && game.Character.poweredAnimation.IsReady && game.Character.skillSprite != null
                        && game.Character.ultimateSprite != null, "Character " + character + " binds generated animation and skills");
                Check(skill.Activations == 1 && skill.Remaining > 0, "Q activates character skill " + character
                    + "; activations=" + skill.Activations + "; state=" + game.State + "; aim=" + game.Player.Motor.Aim
                    + "; position=" + game.Player.transform.position);
                Keys(Key.Q); yield return null; yield return null; Keys(); yield return null;
                Check(skill.Activations == 1, "Cooldown prevents repeated skill " + character);
                if (character == 0) Check(skill.FireRateMultiplier > 1.5f && skill.CriticalBonus >= 0.2f, "Delta focus boosts fire rate and critical chance");
                if (character == 1)
                {
                    Check(Mathf.Abs(game.Player.Health.Current - healthBeforeSkill - game.Player.Health.Maximum * 0.25f) < 0.01f,
                        "Vanguard restores exactly 25 percent maximum health on activation");
                    game.Player.Health.Invulnerable = false;
                    float hp = game.Player.Health.Current;
                    game.Player.Health.TakeDamage(20, Team.Enemy);
                    Check(Mathf.Abs(hp - game.Player.Health.Current - 6) < 0.01f, "Vanguard shield reduces actual incoming damage by 70 percent");
                    Check(target.Health.Current < before, "Vanguard activation damages nearby enemy");
                }
                if (character == 2)
                {
                    Check(skill.LastTeleportDistance > 0.5f && skill.LastTeleportDistance <= 5.51f && Vector2.Distance(skillOrigin, game.Player.transform.position) > 0.5f,
                        "Wraith input causes an actual bounded teleport");
                    Check(Mathf.Abs(target.GetComponent<Rigidbody2D>().linearVelocity.x) < 0.01f, "Wraith skill freezes nearby non-boss enemy movement");
                    game.Level.enabled = false;
                    game.Player.Motor.TeleportTo(new Vector2(120, 20));
                    yield return WaitGameplay(0.1f);
                    float start = game.Player.transform.position.x;
                    Keys(Key.D); yield return WaitGameplay(0.35f); Keys();
                    Check(game.Player.transform.position.x > start + 1.6f, "Wraith skill produces faster actual movement");
                }
                if (character == 3)
                {
                    yield return WaitGameplay(0.6f);
                    Check(target.Health.Current < before && game.Player.Weapon.Ammo == game.Player.Weapon.Capacity, "Havoc volley hits target without consuming weapon ammo");
                }
                if (character == 4) Check(skill.RecoilMultiplier <= 0.35f, "KALT suppresses recoil during precision stance");
                if (character == 5) Check(Vector2.Distance(skillOrigin, game.Player.transform.position) > 1, "VEIL performs its shadow blink");
                if (character == 6)
                    Check(skill.LastHealing > 0 && game.Player.Health.DamageReduction >= 0.19f,
                        "SERA combines immediate healing, regeneration and protection");
                if (character == 7)
                {
                    yield return WaitGameplay(0.1f);
                    Check(Vector2.Distance(skillOrigin, game.Player.transform.position) > 1 && target.Health.Current < before,
                        "ROOK charges forward and damages nearby enemies");
                }
                yield return Snap("arsenal_skill_" + character + ".png");
                game.Level.enabled = false;
                for (int technique = 0; technique < 2; technique++)
                {
                    game.Player.Motor.TeleportTo(new Vector2(120, 20));
                    Pointer(game.CameraRig.Camera.WorldToScreenPoint(game.Player.Motor.MuzzleOrigin + Vector2.right * 8));
                    yield return null; yield return null;
                    Check(game.Player.Techniques.TryActivate(technique), "Character " + character + " technique " + technique + " activates");
                    Check(game.Player.Techniques.Remaining(technique) > 0 && !game.Player.Techniques.TryActivate(technique),
                        "Character " + character + " technique " + technique + " consumes its cooldown");
                }
                game.Player.Motor.TeleportTo(new Vector2(120, 20));
                Pointer(game.CameraRig.Camera.WorldToScreenPoint(game.Player.Motor.MuzzleOrigin + Vector2.right * 8));
                yield return null; yield return null;
                game.Player.Energy.Add(PlayerEnergySystem.Maximum);
                Check(game.Player.Energy.Ready, "Character " + character + " reaches full ultimate energy");
                Check(game.Player.Energy.TryActivateUltimate(), "Character " + character + " activates its charged ultimate");
                Check(game.Player.Energy.Current >= 0 && game.Player.Energy.Current <= PlayerEnergySystem.Maximum,
                    "Character " + character + " energy stays bounded while ultimate hits can recharge it");
                yield return WaitGameplay(0.15f);
                resumeFocusPauses = false; game.Pause();
                float remaining = skill.Remaining;
                yield return new WaitForSecondsRealtime(0.2f);
                Check(Mathf.Abs(skill.Remaining - remaining) < 0.01f, "Skill cooldown freezes while paused " + character);
                game.Resume(); resumeFocusPauses = true;
                if (character == 1)
                {
                    yield return WaitGameplay(10.1f);
                    game.Player.Health.Invulnerable = false;
                    float hp = game.Player.Health.Current; game.Player.Health.TakeDamage(20, Team.Enemy);
                    Check(!skill.Active && Mathf.Abs(hp - game.Player.Health.Current - 17.6f) < 0.01f, "Expired shield restores Vanguard passive reduction");
                    skill.ResetForCheckpoint(); game.Player.Health.SetCurrent(game.Player.Health.Maximum - 1);
                    Check(skill.TryActivate() && Mathf.Abs(skill.LastHealing - 1) < 0.01f && game.Player.Health.Current == game.Player.Health.Maximum,
                        "Healing clamps to maximum health rather than overhealing");
                }
                if (character == 2 || character == 5 || character == 7)
                {
                    game.Level.enabled = false;
                    game.Player.Motor.ResetAt(new Vector2(120, 20));
                    var wall = new GameObject("BlinkSolidWallProbe"); wall.transform.SetParent(game.Level.transform);
                    wall.transform.position = new Vector2(122, 22);
                    BoxCollider2D collider = wall.AddComponent<BoxCollider2D>(); collider.size = new Vector2(0.4f, 8);
                    yield return WaitGameplay(0.25f);
                    Pointer(game.CameraRig.Camera.WorldToScreenPoint(game.Player.Motor.MuzzleOrigin + Vector2.right * 8));
                    yield return null; yield return null;
                    Check(skill.TryActivate(), "Blink can advance up to a nearby wall");
                    yield return WaitGameplay(0.12f);
                    float blinkPosition = game.Player.GetComponent<Rigidbody2D>().position.x;
                    Check(blinkPosition < 121.55f && blinkPosition > 120.3f,
                        "Capsule sweep prevents character " + character + " teleporting through a solid wall; x=" + blinkPosition);
                    skill.ResetForCheckpoint();
                    Check(!skill.TryActivate() && skill.Remaining == 0, "Blocked teleport preserves cooldown");
                    game.Player.Energy.Add(PlayerEnergySystem.Maximum);
                    Check(!game.Player.Energy.TryActivateUltimate() && game.Player.Energy.Ready,
                        "Blocked ultimate preserves full energy for character " + character);
                    Destroy(wall);
                    game.Level.Retry();
                    Check(!skill.Active && skill.Remaining == 0 && skill.LastTeleportDistance == 0, "Checkpoint clears teleport status and cooldown");
                }
            }
            for (int weapon = 0; weapon < game.Config.weapons.Length; weapon++)
            {
                game.ReturnToMenu(); game.SelectCharacter(0); game.SelectStartingWeapon(0); game.BeginRun();
                yield return WaitGameplay(0.45f);
                while (!game.ControlsReady) yield return null;
                Keys(); yield return null; yield return null;
                Key[] slots = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9 };
                Keys(slots[weapon]); yield return null; yield return null;
                int requested = game.Input.SwitchWeapon;
                bool ready = game.ControlsReady;
                Keys();
                yield return WaitGameplay(0.25f);
                Check(game.Player.Weapon.Selected == weapon, "Number key selects weapon " + weapon + "; actual=" + game.Player.Weapon.Selected + "; key=" + slots[weapon] + "; requested=" + requested + "; ready=" + ready + "; state=" + game.State);
                EnemyAI first = game.Level.Spawn(EnemyKind.Elite, new Vector2(weapon == 6 ? 4.3f : 6.5f, 1));
                EnemyAI second = game.Level.Spawn(EnemyKind.Elite, new Vector2(7.6f, 1));
                foreach (EnemyAI target in new[] { first, second })
                {
                    target.Motor.Stun(30); target.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
                    target.Health.IncreaseMaximum(1000, 1000);
                }
                float firstHealth = first.Health.Current, secondHealth = second.Health.Current;
                Vector2 aim = game.CameraRig.Camera.WorldToScreenPoint(first.transform.position);
                Pointer(aim); yield return null; yield return null;
                int ammo = game.Player.Weapon.Ammo;
                Pointer(aim, 1); yield return WaitGameplay(0.22f); Pointer(aim);
                yield return WaitGameplay(0.5f);
                Check(game.Player.Weapon.Ammo < ammo && first.Health.Current < firstHealth, "Weapon " + weapon + " consumes ammo and damages actual target");
                if (weapon == 2 || weapon == 4 || weapon == 5 || weapon == 7 || weapon == 8)
                    Check(second.Health.Current < secondHealth, "Weapon " + weapon + " pierces or explodes through second target");
                Keys(Key.Tab); yield return null; yield return null; Keys(); yield return WaitGameplay(0.1f);
                Keys(Key.Tab); yield return null; yield return null; Keys(); yield return WaitGameplay(0.35f);
                Image foreground = null;
                foreach (Image item in game.Hud.GetComponentsInChildren<Image>()) if (item.name == "IronSightWeapon") foreground = item;
                Check(game.CameraRig.Side.Mode == ViewMode.IronSight && foreground != null && foreground.sprite == game.Config.weaponViewSprites[weapon], "Weapon " + weapon + " uses its own first-person foreground");
                yield return Snap("arsenal_view_" + weapon + ".png");
                Keys(Key.R); yield return null; yield return null; Keys(); yield return WaitGameplay(0.1f);
                Check(game.Player.Weapon.IsReloading, "Weapon " + weapon + " can reload");
                yield return WaitGameplay(game.Player.Weapon.ReloadDuration + 0.1f);
                Check(game.Player.Weapon.Ammo == game.Player.Weapon.Capacity, "Weapon " + weapon + " refills independently");
            }
            int reactions = game.Environment.ReactionCount;
            game.Projectiles.Fire(new Vector2(4, 1), Vector2.down, 20, 1, Team.Player, 1, Color.cyan);
            yield return WaitGameplay(0.2f);
            Check(game.Environment.ReactionCount > reactions, "Projectile hitting ground triggers scenery feedback");
            game.ReturnToMenu();
            yield return Snap("arsenal_final_menu.png");
        }

        private IEnumerator ProbeInstalledArt()
        {
            if (game.Config.characters[0].animation == null || !game.Config.characters[0].animation.IsReady) yield break;
            CampaignProgress.Unlock(5);
            game.ReturnToMenu();
            yield return Snap("production-roster.png");
            for (int stage = 0; stage < 5; stage++)
            {
                game.SelectStage(stage); game.SelectCharacter(stage % 4); game.BeginRun();
                game.Level.enabled = false;
                yield return WaitGameplay(0.5f);
                Check(game.Player.GetComponent<SideSpriteAnimator>() != null && game.Level.Theme.background != null, "Installed art: stage " + stage + " character and background active");
                SpriteRenderer visual = game.Player.transform.Find("AnimeAnimatedSprite").GetComponent<SpriteRenderer>();
                var secondary = new SecondarySpriteTexture[1];
                Check(visual.sprite.GetSecondaryTextures(secondary) == 1 && secondary[0].name == "_NormalMap", "Installed art: stage " + stage + " normal map bound");
                Pointer(game.CameraRig.Camera.WorldToScreenPoint(game.Player.Motor.MuzzleOrigin + Vector2.right * 8), 1);
                yield return WaitGameplay(0.3f);
                Check(game.Player.Weapon.Ammo < game.Player.Weapon.Capacity, "Installed art: stage " + stage + " weapon fires");
                yield return Snap("production-stage-" + stage + ".png"); Pointer(Vector2.zero);
                if (stage == 0)
                {
                    for (int kind = 0; kind < 4; kind++)
                    {
                        var enemyEntity = new GameObject("ArtPreviewEnemy"); enemyEntity.transform.SetParent(game.Player.transform.parent);
                        enemyEntity.transform.position = new Vector3(6 + kind * 2.2f, 1, 0);
                        EnemyAI enemy = enemyEntity.AddComponent<EnemyAI>(); enemy.Initialize(game, (EnemyKind)kind, 1);
                        Check(enemy.GetComponent<SideSpriteAnimator>() != null, "Installed art: enemy type " + kind + " uses animation");
                    }
                    yield return WaitGameplay(0.15f); yield return Snap("production-enemies.png");
                    foreach (EnemyAI enemy in game.Enemies.ToArray()) if (enemy != null) Destroy(enemy.gameObject);
                }
                float arena = game.Level.Segments[game.Level.Segments.Length - 1].start;
                game.Player.Motor.ResetAt(new Vector2(arena + 9, 1));
                var bossEntity = new GameObject("ArtPreviewBoss"); bossEntity.transform.SetParent(game.Player.transform.parent);
                bossEntity.transform.position = new Vector3(arena + 14, 1.7f, 0);
                EnemyAI boss = bossEntity.AddComponent<EnemyAI>(); boss.Initialize(game, EnemyKind.Boss, 1);
                yield return WaitGameplay(0.7f);
                Check(boss.GetComponent<SideSpriteAnimator>() != null, "Installed art: stage " + stage + " boss animated");
                yield return Snap("production-boss-" + stage + ".png");
                boss.Health.TakeDamage(boss.Health.Maximum * 0.48f, Team.Player, true); yield return WaitGameplay(2.2f);
                boss.Health.TakeDamage(boss.Health.Maximum * 0.32f, Team.Player, true); yield return WaitGameplay(0.2f);
                Check(boss.GetComponent<SideSpriteAnimator>().CurrentFrame == 3, "Installed art: stage " + stage + " boss rage artwork selected");
                yield return Snap("production-boss-rage-" + stage + ".png");
                game.ReturnToMenu(); yield return null;
            }
        }

        private IEnumerator WaitGameplay(float seconds)
        {
            float until = Time.time + seconds;
            while (Time.time < until) { ChooseAll(); if (game.Player != null) game.Player.Health.Invulnerable = true; yield return null; }
        }

        private PlatformSurface FindOneWay()
        {
            foreach (PlatformSurface platform in FindObjectsByType<PlatformSurface>())
                if (platform.name == "OneWayPlatform" && platform.transform.position.x < 20) return platform;
            throw new InvalidOperationException("First one-way platform missing");
        }
        private bool HasConveyor() { foreach (PlatformSurface item in FindObjectsByType<PlatformSurface>()) if (item.ConveyorSpeed != 0) return true; return false; }
        private bool HasIce() { foreach (PlatformSurface item in FindObjectsByType<PlatformSurface>()) if (item.Ice) return true; return false; }
        private bool HasElevator() { foreach (PlatformSurface item in FindObjectsByType<PlatformSurface>()) if (item.Elevator) return true; return false; }
        private EnemyAI ClosestEnemy()
        {
            EnemyAI closest = null; float distance = float.MaxValue;
            foreach (EnemyAI enemy in game.Enemies)
                if (enemy != null && !enemy.Health.IsDead && Vector2.Distance(enemy.transform.position, game.Player.transform.position) < distance)
                { closest = enemy; distance = Vector2.Distance(enemy.transform.position, game.Player.transform.position); }
            return closest;
        }
        private StageInteractable ClosestBarrel()
        {
            StageInteractable closest = null; float distance = 8;
            foreach (StageInteractable item in game.Level.GetComponentsInChildren<StageInteractable>())
            {
                float ahead = item.transform.position.x - game.Player.transform.position.x;
                if (item.Barrel && ahead > -0.5f && ahead < distance) { closest = item; distance = ahead; }
            }
            return closest;
        }
        private bool HasVisibleAttackOverlay()
        {
            if (game.Effects.WarningEmissions != game.Effects.BossWarningEmissions) return true;
            foreach (SpriteRenderer renderer in game.Level.GetComponentsInChildren<SpriteRenderer>())
            {
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                string name = renderer.name;
                if (name == "GroundWarning" || name == "VerticalHazard_WarningGlyph"
                    || name == "ActivationCountdown_NoDamage" || name == "GravityInfluence_NoDamage") return true;
                if (name == "EnemyAimWarning")
                {
                    SideEnemyMotor motor = renderer.GetComponentInParent<SideEnemyMotor>();
                    if (motor != null && motor.Preparing) return true;
                }
            }
            return false;
        }
        private void Keys(params Key[] keys) { InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys)); }
        private void Pointer(Vector2 position, ushort buttons = 0) { InputSystem.QueueStateEvent(mouse, new MouseState { position = position, buttons = buttons }); }
        private Button FindButton(string name)
        {
            foreach (Button button in game.Hud.GetComponentsInChildren<Button>()) if (button.name == name && button.isActiveAndEnabled) return button;
            throw new InvalidOperationException("UI button missing: " + name);
        }
        private void Invoke(string name) { FindButton(name).onClick.Invoke(); }
        private IEnumerator ClickPointer(string name)
        {
            Vector2 point = RectTransformUtility.WorldToScreenPoint(null, FindButton(name).transform.position);
            Pointer(point, 1); yield return null; yield return null; Pointer(point); yield return null; yield return null;
        }
        private void ChooseAll() { while (game.State == RunState.Upgrade) Invoke("Option_0"); }
        private IEnumerator Snap(string name)
        {
            yield return new WaitForEndOfFrame();
            Texture2D image = ScreenCapture.CaptureScreenshotAsTexture();
            report.screenshotSizes.Add(name + ": " + image.width + "x" + image.height);
            string path = Path.Combine(folder, name); File.WriteAllBytes(path, image.EncodeToPNG()); Destroy(image); report.screenshots.Add(path);
        }
        private void Check(bool condition, string name)
        {
            if (!condition) throw new InvalidOperationException(name);
            report.checks.Add(name); Debug.Log("MORI_STAGE_PASS | " + name);
        }
        private void OnLog(string message, string stack, LogType type)
        { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) report.errors.Add(message + "\n" + stack); }
        private void Update()
        {
            if (!finished && keyboard != null && mouse != null)
            {
                if (!keyboard.enabled) InputSystem.EnableDevice(keyboard);
                if (!mouse.enabled) InputSystem.EnableDevice(mouse);
                keyboard.MakeCurrent(); mouse.MakeCurrent();
            }
            if (resumeFocusPauses && !finished && game != null && game.State == RunState.Paused) game.Resume();
            if (!finished && Time.realtimeSinceStartup > deadline) { report.errors.Add("Stage test exceeded 420s deadline"); Complete(); }
        }
        private void Complete()
        {
            if (finished) return; finished = true;
            if (keyboard != null) InputSystem.RemoveDevice(keyboard); if (mouse != null) InputSystem.RemoveDevice(mouse);
            foreach (InputDevice device in suppressed) if (device.added) InputSystem.EnableDevice(device);
            if (inputSettingsCaptured) InputSystem.settings.backgroundBehavior = originalBackgroundBehavior;
            report.completedAt = DateTimeOffset.Now.ToString("O"); report.passed = report.errors.Count == 0;
            File.WriteAllText(Path.Combine(folder, "stage-smoke.json"), JsonUtility.ToJson(report, true));
            Application.logMessageReceived -= OnLog;
            Debug.Log("MORI_STAGE_RESULT | " + report.passed + " | checks=" + report.checks.Count + " | errors=" + report.errors.Count);
            Application.Quit(report.passed ? 0 : 1);
        }
        private void OnApplicationQuit()
        {
            if (finished || string.IsNullOrEmpty(folder)) return;
            report.errors.Add("Player closed before the diagnostic run completed; this run is not a pass.");
            report.completedAt = DateTimeOffset.Now.ToString("O");
            report.passed = false;
            File.WriteAllText(Path.Combine(folder, "stage-smoke.json"), JsonUtility.ToJson(report, true));
        }
    }
}
#endif
