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
    public sealed class RuntimeSmokeTest : MonoBehaviour
    {
        [Serializable]
        private sealed class Report
        {
            public string unity;
            public string startedAt;
            public string completedAt;
            public bool passed;
            public int screenWidth;
            public int screenHeight;
            public string graphicsDevice;
            public string scope = "Automated short-run integration with synthetic input; not full default-difficulty playthrough, human visual review or performance certification.";
            public List<string> checks = new List<string>();
            public List<string> errors = new List<string>();
            public List<string> screenshots = new List<string>();
        }

        private readonly Report report = new Report();
        private string folder;
        private Keyboard keyboard;
        private Mouse mouse;
        private Gamepad gamepad;
        private readonly List<InputDevice> suppressedDevices = new List<InputDevice>();
        private GameConfig config;
        private GameConfig originalConfig;
        private GameManager game;
        private bool finished;
        private float deadline;
        private string previousBestKills;
        private bool hadBestKills;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Launch()
        {
            string[] args = Environment.GetCommandLineArgs();
            if (!Application.isEditor && !Debug.isDebugBuild) return;
            int index = Array.IndexOf(args, "-moriSmoke");
            if (index < 0) return;
            var entity = new GameObject("MoriEchoes_AutomatedSmokeTest");
            DontDestroyOnLoad(entity);
            var runner = entity.AddComponent<RuntimeSmokeTest>();
            runner.folder = index + 1 < args.Length && !args[index + 1].StartsWith("-", StringComparison.Ordinal)
                ? Path.GetFullPath(args[index + 1]) : Path.Combine(Application.persistentDataPath, "SmokeTest");
            Directory.CreateDirectory(runner.folder);
            runner.report.unity = Application.unityVersion;
            runner.report.startedAt = DateTimeOffset.Now.ToString("O");
            runner.report.screenWidth = Screen.width;
            runner.report.screenHeight = Screen.height;
            runner.report.graphicsDevice = SystemInfo.graphicsDeviceName;
            runner.deadline = Time.realtimeSinceStartup + 100;
            runner.hadBestKills = PlayerPrefs.HasKey("MoriEchoes.BestKills");
            runner.previousBestKills = PlayerPrefs.GetInt("MoriEchoes.BestKills", 0).ToString();
            var bootstrap = FindAnyObjectByType<GameBootstrap>();
            if (bootstrap != null && bootstrap.config != null)
            {
                runner.originalConfig = bootstrap.config;
                runner.config = Instantiate(bootstrap.config);
                runner.config.sideScroller = false;
                runner.config.wavesBeforeBoss = 1;
                runner.config.waveSeconds = 5;
                runner.config.intermissionSeconds = 0.2f;
                runner.config.maximumEnemies = 8;
                bootstrap.config = runner.config;
            }
            Application.logMessageReceived += runner.OnLog;
            runner.StartCoroutine(runner.Guard(runner.Run()));
        }

        private IEnumerator Guard(IEnumerator test)
        {
            while (!finished)
            {
                object current;
                try
                {
                    if (!test.MoveNext()) { Complete(); yield break; }
                    current = test.Current;
                }
                catch (Exception exception)
                {
                    report.errors.Add(exception.ToString());
                    Complete();
                    yield break;
                }
                yield return current;
            }
        }

        private IEnumerator Run()
        {
            yield return null;
            yield return null;
            game = FindAnyObjectByType<GameManager>();
            Check(game != null, "Boot scene initializes GameManager");
            Check(game.State == RunState.Menu, "Boot opens menu");
            Check(game.CameraRig.Camera != null && game.Hud != null, "Camera and UGUI initialize");
            Check(game.UiFont != null && game.UiFont.HasCharacter('末') && game.UiFont.HasCharacter('响'), "OS fallback provides Chinese title glyphs on this machine");
            yield return new WaitForEndOfFrame();
            Capture("01_menu.png");
            Check(config != null && game.Config == config, "Smoke uses temporary config clone without modifying asset");
            foreach (InputDevice device in InputSystem.devices)
            {
                if (!device.enabled || !(device is Keyboard || device is Mouse || device is Gamepad)) continue;
                suppressedDevices.Add(device);
            }
            foreach (InputDevice device in suppressedDevices) InputSystem.DisableDevice(device);
            keyboard = InputSystem.AddDevice<Keyboard>("MoriSmokeKeyboard");
            mouse = InputSystem.AddDevice<Mouse>("MoriSmokeMouse");
            var start = FindButton("Start");
            Vector2 startPosition = RectTransformUtility.WorldToScreenPoint(null, start.transform.position);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = startPosition, buttons = 1 });
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = startPosition });
            yield return new WaitForSecondsRealtime(0.4f);
            Check(game.IsPlaying && game.Player != null, "Synthetic pointer click on Start begins run through UI input module");
            game.Waves.enabled = false;
            Check(config.Validate(out _), "Default shoulder camera configuration validates");
            float savedPitch = config.camPitchDeg;
            config.camPitchDeg = float.NaN;
            Check(!config.Validate(out _), "Camera validation rejects NaN");
            config.camPitchDeg = savedPitch;
            float savedAds = config.camAdsSize;
            config.camAdsSize = config.camBaseSize + 1;
            Check(!config.Validate(out _), "Camera validation rejects ADS larger than base view");
            config.camAdsSize = savedAds;
            Check(game.Hud.ReticleVisible && Cursor.lockState == CursorLockMode.Locked && !Cursor.visible,
                "Gameplay locks cursor and shows center reticle");
            Vector3 framing = game.CameraRig.Camera.WorldToViewportPoint(game.Player.transform.position);
            Check(framing.y > 0.2f && framing.y < 0.43f && framing.x > 0.25f && framing.x < 0.6f,
                "Player is framed below center in shoulder view");
            Vector2 idleForward = game.CameraRig.ViewForward;
            yield return new WaitForSecondsRealtime(0.25f);
            Check(Vector2.Dot(idleForward, game.CameraRig.ViewForward) > 0.99999f, "Side shoulder offset does not rotate idle view");
            InputSystem.QueueStateEvent(mouse, new MouseState { delta = new Vector2(600, 0) });
            yield return null;
            yield return null;
            Check(Vector2.Dot(game.CameraRig.ViewForward, Vector2.right) > 0.95f, "Relative mouse delta rotates view clockwise by sensitivity");
            Vector2 forwardBefore = game.Player.Aim;
            Vector2 before = game.Player.transform.position;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
            yield return new WaitForSecondsRealtime(0.3f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            Check(Vector2.Dot((Vector2)game.Player.transform.position - before, forwardBefore) > 0.8f,
                "W moves along aim after turning camera");
            Vector2 strafeRight = new Vector2(game.Player.Aim.y, -game.Player.Aim.x);
            before = game.Player.transform.position;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
            yield return new WaitForSecondsRealtime(0.4f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            Check(Vector2.Dot((Vector2)game.Player.transform.position - before, strafeRight) > 0.8f,
                "D strafes right relative to rotated aim");
            before = game.Player.transform.position;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.D));
            yield return new WaitForSecondsRealtime(0.35f);
            Check(game.Input.Move.magnitude > 0.99f && game.Input.Move.magnitude < 1.01f, "Diagonal input is normalized");
            Check(Vector2.Distance(before, game.Player.transform.position) < config.moveSpeed * 0.5f, "Diagonal movement does not exceed normalized speed");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
            yield return null;
            yield return null;
            Check(game.Player.IsDodging && game.Player.Health.Invulnerable, "Space activates dodge and invulnerability");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return new WaitForSecondsRealtime(0.3f);
            Check(!game.Player.IsDodging && !game.Player.Health.Invulnerable, "Dodge ends and clears invulnerability");
            gamepad = InputSystem.AddDevice<Gamepad>("MoriSmokeGamepad");
            for (int index = 0; index < 3; index++)
            {
                config.camPitchDeg = index == 0 ? 0 : index == 1 ? savedPitch : 55;
                InputSystem.QueueStateEvent(gamepad, new GamepadState { rightStick = index == 1 ? Vector2.right : Vector2.up });
                yield return new WaitForSecondsRealtime(0.4f);
                Check(game.CameraRig.GroundPoint(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f), out Vector2 ground),
                    "Center ray intersects XY plane at pitch " + config.camPitchDeg);
                Vector3 projected = game.CameraRig.Camera.WorldToScreenPoint(ground);
                Check(Vector2.Distance(projected, new Vector2(Screen.width * 0.5f, Screen.height * 0.5f)) < 1f,
                    "Tilted ground point projects back to reticle at pitch " + config.camPitchDeg);
                Check(Vector2.Dot(game.Player.Aim, (ground - (Vector2)game.Player.transform.position).normalized) > 0.999f,
                    "Aim aligns with center ground target at pitch " + config.camPitchDeg);
                Transform visual = game.Player.transform.Find("Body");
                Check(Quaternion.Angle(visual.rotation, game.CameraRig.transform.rotation) < 0.1f,
                    "Player sprite billboards at pitch " + config.camPitchDeg);
                Check(game.CameraRig.transform.position.z < -1 && projected.z > game.CameraRig.Camera.nearClipPlane,
                    "Camera retains safe distance from ground at pitch " + config.camPitchDeg);
            }
            config.camPitchDeg = savedPitch;
            InputSystem.QueueStateEvent(gamepad, new GamepadState { rightStick = Vector2.up, leftTrigger = 1 });
            yield return new WaitForSecondsRealtime(0.55f);
            Check(game.Input.UsingGamepad && game.CameraRig.IsAds && Mathf.Abs(game.CameraRig.Camera.orthographicSize - savedAds) < 0.1f,
                "Gamepad LT smoothly zooms to ADS size");
            Check(game.Hud.ScopeOpacity > 0.95f, "ADS shows scope vignette");
            InputSystem.QueueStateEvent(gamepad, new GamepadState());
            InputSystem.QueueStateEvent(mouse, new MouseState { buttons = 2 });
            yield return new WaitForSecondsRealtime(0.2f);
            Check(!game.Input.UsingGamepad && game.CameraRig.IsAds, "Mouse right button takes over ADS from gamepad");
            before = game.Player.transform.position;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
            yield return new WaitForSecondsRealtime(0.3f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            float adsDistance = Vector2.Distance(before, game.Player.transform.position);
            Check(adsDistance > 0.45f && adsDistance < config.moveSpeed * 0.3f * 0.85f, "ADS applies reduced movement speed");
            yield return new WaitForEndOfFrame();
            Capture("02_ads.png");
            game.CameraRig.AddRecoil(10);
            Check(Mathf.Abs(game.CameraRig.Recoil - config.camRecoilMax) < 0.001f, "Recoil accumulates to configured cap");
            game.Pause();
            yield return null;
            Check(!game.CameraRig.IsAds && game.CameraRig.Recoil == 0 && !game.Hud.ReticleVisible && Cursor.visible
                && Cursor.lockState == CursorLockMode.None && game.Hud.ScopeOpacity == 0, "Pause clears ADS and recoil and releases cursor");
            InputSystem.QueueStateEvent(mouse, new MouseState());
            game.Resume();
            yield return new WaitForSecondsRealtime(0.5f);
            Check(Mathf.Abs(game.CameraRig.Camera.orthographicSize - config.camBaseSize) < 0.1f, "ADS release restores base size");
            game.CameraRig.AddRecoil(config.camRecoilMax);
            yield return new WaitForSecondsRealtime(0.3f);
            Check(game.CameraRig.Recoil < 0.001f, "Recoil decays back to zero during gameplay");
            Vector2 centerPosition = game.Player.transform.position;
            var rigidbody = game.Player.GetComponent<Rigidbody2D>();
            for (int index = 0; index < 4; index++)
            {
                Vector2 corner = new Vector2((index % 2 == 0 ? -1 : 1) * (config.arenaHalfSize.x - 0.6f),
                    (index < 2 ? -1 : 1) * (config.arenaHalfSize.y - 0.6f));
                rigidbody.position = corner;
                InputSystem.QueueStateEvent(gamepad, new GamepadState { rightStick = corner.normalized, leftTrigger = 1 });
                yield return new WaitForSecondsRealtime(0.5f);
                Vector3 viewport = game.CameraRig.Camera.WorldToViewportPoint(game.Player.transform.position);
                Check(viewport.x > 0.05f && viewport.x < 0.95f && viewport.y > 0.1f && viewport.y < 0.5f
                    && Vector2.Dot(game.Player.Aim, game.CameraRig.ViewForward) > 0.1f,
                    "Outward aim stays forward and player remains visible at arena corner " + index);
            }
            yield return new WaitForEndOfFrame();
            Capture("02_boundary_ads.png");
            rigidbody.position = centerPosition;
            InputSystem.QueueStateEvent(gamepad, new GamepadState());
            InputSystem.QueueStateEvent(mouse, new MouseState());
            game.CameraRig.ResetFollow();
            yield return new WaitForSecondsRealtime(0.4f);
            for (int index = 0; index < 3; index++)
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(index == 0 ? Key.Digit1 : index == 1 ? Key.Digit2 : Key.Digit3));
                yield return null;
                yield return null;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return new WaitForSecondsRealtime(0.25f);
                Check(game.Player.Weapon.Selected == index, "Number key selects weapon " + index);
                if (index == 2)
                {
                    InputSystem.QueueStateEvent(mouse, new MouseState { buttons = 2 });
                    yield return new WaitForSecondsRealtime(0.55f);
                    Check(game.CameraRig.IsAds && game.CameraRig.AdsAmount > 0.95f, "Sniper hit test uses active ADS projection");
                }
                game.CameraRig.GroundPoint(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f), out Vector2 targetPoint);
                var target = Spawn(EnemyKind.Chaser, targetPoint);
                target.enabled = false;
                float hp = target.Health.Current;
                int ammo = game.Player.Weapon.Ammo;
                Vector3 pointer = game.CameraRig.Camera.WorldToScreenPoint(target.transform.position);
                InputSystem.QueueStateEvent(mouse, new MouseState { position = pointer, buttons = (ushort)(index == 2 ? 3 : 1) });
                float maximumRecoil = 0;
                float fireUntil = Time.realtimeSinceStartup + (index == 0 ? 0.3f : 0.6f);
                while (Time.realtimeSinceStartup < fireUntil)
                {
                    maximumRecoil = Mathf.Max(maximumRecoil, game.CameraRig.Recoil);
                    yield return null;
                }
                InputSystem.QueueStateEvent(mouse, new MouseState { position = pointer });
                yield return null;
                Check(game.Player.Weapon.Ammo < ammo, "Weapon " + index + " consumes ammunition through mouse input"
                    + " [state=" + game.State + ", fire=" + game.Input.Fire + ", ready=" + game.ControlsReady
                    + ", focused=" + Application.isFocused + ", reload=" + game.Player.Weapon.IsReloading
                    + ", ammo=" + ammo + "->" + game.Player.Weapon.Ammo + "]");
                Check(target == null || target.Health.Current < hp, "Weapon " + index + " projectile damages collider target");
                Check(maximumRecoil > 0 && maximumRecoil <= config.camRecoilMax, "Weapon " + index + " produces bounded recoil");
                if (target != null) target.Health.TakeDamage(10000, Team.Player);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.R));
                yield return null;
                yield return null;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return new WaitForSecondsRealtime(game.Player.Weapon.Definition.reloadSeconds + 0.15f);
                Check(game.Player.Weapon.Ammo == game.Player.Weapon.Capacity, "Weapon " + index + " reload completes");
            }
            Check(game.Kills >= 3, "Enemy deaths increment kills");
            int level = game.Experience.Track.Level;
            game.Pickups.Drop(game.Player.transform.position, game.Experience.Track.Required);
            yield return new WaitForSecondsRealtime(0.15f);
            Check(game.State == RunState.Upgrade && game.Experience.Track.Level > level, "Pickup collection opens upgrade and increments level");
            Check(!game.Hud.ReticleVisible && Cursor.visible && !game.CameraRig.IsAds, "Upgrade releases aiming controls for card selection");
            Check(Time.timeScale == 0 && game.Experience.Options.Length == 3, "Upgrade pauses game and provides three choices");
            var choices = game.Experience.Options;
            Check(choices[0].Kind != choices[1].Kind && choices[1].Kind != choices[2].Kind && choices[0].Kind != choices[2].Kind, "Upgrade cards are distinct");
            yield return new WaitForEndOfFrame();
            Capture("02_upgrade.png");
            UpgradeKind chosen = choices[0].Kind;
            Click("Option_0");
            Check(game.Stats.Rank(chosen) == 1 && game.IsPlaying, "Upgrade button applies chosen effect and resumes");
            game.Experience.Gain(100);
            Check(game.Experience.Track.PendingChoices > 1, "Large XP gain queues multiple choices");
            int pending = game.Experience.Track.PendingChoices;
            for (int index = 0; index < pending; index++) Click("Option_0");
            Check(game.IsPlaying && game.Experience.Track.PendingChoices == 0, "Queued choices resolve without losing levels");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
            yield return null;
            yield return null;
            Check(game.State == RunState.Paused && Time.timeScale == 0, "Escape pauses gameplay");
            Vector3 pausedPosition = game.Player.transform.position;
            float pausedElapsed = game.Elapsed;
            yield return new WaitForSecondsRealtime(0.25f);
            Check(game.Player.transform.position == pausedPosition && game.Elapsed == pausedElapsed, "Pause freezes player and run timer");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            Click("Resume");
            Check(game.IsPlaying && Time.timeScale == 1, "Resume button restores gameplay");
            game.Stats.Apply(UpgradeKind.Pulse);
            var pulseTarget = Spawn(EnemyKind.Chaser, (Vector2)game.Player.transform.position + Vector2.up * 2);
            pulseTarget.enabled = false;
            yield return new WaitForSecondsRealtime(0.3f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Q));
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            Check(pulseTarget == null || pulseTarget.Health.IsDead, "Unlocked Q pulse damages nearby enemy");
            var chaser = Spawn(EnemyKind.Chaser, (Vector2)game.Player.transform.position + Vector2.up * 5);
            float distance = Vector2.Distance(chaser.transform.position, game.Player.transform.position);
            yield return new WaitForSecondsRealtime(1.6f);
            Check(Vector2.Distance(chaser.transform.position, game.Player.transform.position) < distance - 0.5f, "Chaser AI approaches player");
            chaser.Health.TakeDamage(10000, Team.Player);
            while (game.State == RunState.Upgrade) Click("Option_0");
            var gunner = Spawn(EnemyKind.Gunner, (Vector2)game.Player.transform.position + Vector2.right * 5);
            bool observedEnemyShot = false;
            float observeUntil = Time.realtimeSinceStartup + 3.8f;
            while (Time.realtimeSinceStartup < observeUntil)
            {
                observedEnemyShot |= game.Projectiles.FreeCount < config.maximumProjectiles;
                yield return null;
            }
            Check(observedEnemyShot, "Gunner AI emits enemy projectiles");
            gunner.Health.TakeDamage(10000, Team.Player);
            while (game.State == RunState.Upgrade) Click("Option_0");
            yield return new WaitForEndOfFrame();
            Capture("03_battle.png");
            yield return new WaitForSeconds(3.3f);
            while (game.State == RunState.Upgrade) Click("Option_0");
            Check(game.Projectiles.FreeCount == config.maximumProjectiles, "Residual projectiles recycle before isolated defeat test");
            Debug.Log("MORI_SMOKE_DEFEAT_PRECONDITION | state=" + game.State + " | hp=" + game.Player.Health.Current
                + " | invulnerable=" + game.Player.Health.Invulnerable + " | time=" + Time.time);
            bool fatalApplied = game.Player.Health.TakeDamage(10000, Team.Enemy, true);
            Check(fatalApplied, "Fatal test damage accepted after gameplay-time hit protection expires");
            Check(game.State == RunState.Lost && Time.timeScale == 0, "Player death opens defeat result");
            yield return new WaitForEndOfFrame();
            Capture("04_defeat.png");
            Click("Restart");
            yield return new WaitForSecondsRealtime(0.35f);
            Check(game.IsPlaying && game.Kills == 0 && game.Experience.Track.Level == 1 && game.Stats.PulseRank == 0, "Restart resets run progress and upgrades");
            game.Player.Health.Invulnerable = true;
            float until = Time.realtimeSinceStartup + 14;
            while (!game.Waves.BossSpawned && Time.realtimeSinceStartup < until)
            {
                if (game.State == RunState.Upgrade) Click("Option_0");
                yield return null;
            }
            Check(game.Waves.BossSpawned && game.Waves.Boss != null, "Shortened test wave spawns Boss");
            var boss = game.Waves.Boss;
            Vector2 bossBefore = boss.transform.position;
            yield return new WaitForSecondsRealtime(4.8f);
            Check(Vector2.Distance(bossBefore, boss.transform.position) > 0.2f || game.Projectiles.FreeCount < config.maximumProjectiles, "Boss controller executes movement or projectile attack");
            yield return new WaitForEndOfFrame();
            Capture("04_boss.png");
            boss.Health.TakeDamage(10000, Team.Player, true);
            Check(game.State == RunState.Won && Time.timeScale == 0, "Boss death opens victory result");
            yield return new WaitForEndOfFrame();
            Capture("05_victory.png");
            Click("Menu");
            yield return null;
            Check(game.State == RunState.Menu && game.Player == null, "Return to menu clears run");
            Check(!game.Hud.ReticleVisible && Cursor.visible && game.CameraRig.Recoil == 0 && !game.CameraRig.IsAds, "Menu resets camera and aiming UI");
            Check(originalConfig.wavesBeforeBoss == 10 && originalConfig.waveSeconds == 55, "Default ten-wave configuration remains unchanged");
        }

        private EnemyAI Spawn(EnemyKind kind, Vector2 position)
        {
            var entity = new GameObject("SmokeTarget_" + kind);
            entity.transform.SetParent(game.Waves.transform, false);
            entity.transform.position = position;
            var enemy = entity.AddComponent<EnemyAI>();
            enemy.Initialize(game, kind, 1);
            Physics2D.SyncTransforms();
            return enemy;
        }

        private void Click(string name)
        {
            FindButton(name).onClick.Invoke();
        }

        private Button FindButton(string name)
        {
            foreach (var button in game.Hud.GetComponentsInChildren<Button>())
            {
                if (button.name != name || !button.isActiveAndEnabled) continue;
                return button;
            }
            throw new InvalidOperationException("Active UI button not found: " + name);
        }

        private void Capture(string name)
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
            var texture = ScreenCapture.CaptureScreenshotAsTexture();
            Color32[] pixels = texture.GetPixels32();
            int visibleSamples = 0;
            for (int index = 0; index < pixels.Length; index += 157)
            {
                Color32 pixel = pixels[index];
                if (pixel.r > 40 || pixel.g > 40 || pixel.b > 40) visibleSamples++;
            }
            Check(visibleSamples > 20, "Rendered screenshot contains visible content: " + name);
            string path = Path.Combine(folder, name);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Destroy(texture);
            report.screenshots.Add(path);
        }

        private void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("FAIL: " + message);
            report.checks.Add(message);
            Debug.Log("MORI_SMOKE_PASS | " + message);
        }

        private void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) report.errors.Add(message + "\n" + stack);
        }

        private void Update()
        {
            if (!finished && Time.realtimeSinceStartup > deadline)
            {
                report.errors.Add("Smoke test timed out after 100 seconds.");
                Complete();
            }
        }

        private void Complete()
        {
            if (finished) return;
            finished = true;
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            if (mouse != null) InputSystem.RemoveDevice(mouse);
            if (gamepad != null) InputSystem.RemoveDevice(gamepad);
            foreach (InputDevice device in suppressedDevices)
                if (device.added) InputSystem.EnableDevice(device);
            if (hadBestKills) PlayerPrefs.SetInt("MoriEchoes.BestKills", int.Parse(previousBestKills));
            else PlayerPrefs.DeleteKey("MoriEchoes.BestKills");
            PlayerPrefs.Save();
            report.completedAt = DateTimeOffset.Now.ToString("O");
            report.passed = report.errors.Count == 0;
            File.WriteAllText(Path.Combine(folder, "runtime-smoke.json"), JsonUtility.ToJson(report, true));
            Application.logMessageReceived -= OnLog;
            Debug.Log("MORI_SMOKE_RESULT | " + report.passed + " | checks=" + report.checks.Count + " | errors=" + report.errors.Count);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit(report.passed ? 0 : 1);
#endif
        }
    }
}
#endif
